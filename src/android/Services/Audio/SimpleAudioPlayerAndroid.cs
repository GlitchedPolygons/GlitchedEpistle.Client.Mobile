/*
    Glitched Epistle - Mobile Client
    Copyright (C) 2019 Raphael Beck

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using Xamarin.Forms;

using System;
using System.IO;

using Android.OS;
using Android.Media;
using Android.Content.Res;
using Android.Hardware;
using Plugin.SimpleAudioPlayer;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Android.Services.Audio;

using Uri = Android.Net.Uri;
using Stream = System.IO.Stream;
using Runtime = Android.Runtime;
using Environment = System.Environment;
using Application = Android.App.Application;

[assembly: Dependency(typeof(SimpleAudioPlayerAndroid))]

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Android.Services.Audio
{
    /// <summary>
    /// Android implementation of <see cref="ISimpleAudioPlayer"/>.
    /// </summary>
    [Runtime.PreserveAttribute(AllMembers = true)]
    public class SimpleAudioPlayerAndroid : ISimpleAudioPlayer
    {
        ///<Summary>
        /// Raised when audio playback completes successfully.
        ///</Summary>
        public event EventHandler PlaybackEnded;

        ///<Summary>
        /// Length of audio in seconds.
        ///</Summary>
        public double Duration => player == null ? 0 : ((double)player.Duration) / 1000.0d;

        ///<Summary>
        /// Current position of audio playback in seconds.
        ///</Summary>
        public double CurrentPosition => player == null ? 0 : ((double)player.CurrentPosition) / 1000.0d;

        ///<Summary>
        /// Playback volume [0;1]
        ///</Summary>
        public double Volume
        {
            get => volume;
            set => SetVolume(volume = value < 0 ? 0 : value > 1 ? 1 : value, Balance);
        }
        private double volume = 1.0;

        ///<Summary>
        /// Balance left/right:             <para> </para>
        /// -1 is 100% left &amp; 0% right, <para> </para>
        /// 1 is 100% right &amp; 0% left,  <para> </para>
        /// 0 is equal volume left/right.
        ///</Summary>
        public double Balance
        {
            get => balance;
            set => SetVolume(Volume, balance = value);
        }
        private double balance = 0;

        ///<Summary>
        /// Indicates if the currently loaded audio file is playing.
        ///</Summary>
        public bool IsPlaying => player?.IsPlaying ?? false;

        ///<Summary>
        /// Continuously repeats the currently playing sound.
        ///</Summary>
        public bool Loop
        {
            get => loop;
            set
            {
                loop = value;
                if (player != null) player.Looping = loop;
            }
        }
        private bool loop;

        ///<Summary>
        /// Indicates if the position of the loaded audio file can be updated.
        ///</Summary>
        public bool CanSeek => player != null;

        private MediaPlayer player;
        private string deleteOnDispose;
        private volatile bool disposed = false;

        public SimpleAudioPlayerAndroid()
        {
            player = new MediaPlayer { Looping = Loop };
            player.Completion += OnPlaybackEnded;    
        }
        
        ///<Summary>
        /// Load wav or mp3 audio file as a stream
        ///</Summary>
        public bool Load(Stream audioStream)
        {
            if (audioStream is null)
            {
                return false;
            }

            try
            {
                string tempFile = Path.GetTempFileName();

                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }

                using (var fileStream = File.OpenWrite(tempFile))
                {
                    audioStream.CopyTo(fileStream);
                }

                if (Load(tempFile))
                {
                    deleteOnDispose = tempFile;
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
            
        }

        ///<Summary>
        /// Load an audio file into the player.
        ///</Summary>
        public bool Load(string fileName)
        {
            player.Reset();

            try
            {
                player.SetDataSource(fileName);
                return PreparePlayer();
            }
            catch(Exception e1)
            {
                try
                {
                    var uri = Uri.Parse(Uri.Encode(fileName));
                    player.SetDataSource(Application.Context, uri);

                    return PreparePlayer();
                }
                catch(Exception e2)
                {
                    return false;
                }
            }
        }

        private bool PreparePlayer()
        {
            player?.Prepare();
            
            return player != null;
        }

        ///<Summary>
        /// Begin playback or resume if paused.
        ///</Summary>
        public void Play()
        {
            if (player == null)
            {
                return;
            }

            if (IsPlaying)
            {
                Pause();
                Seek(0);
            }

            player.Start();
        }

        ///<Summary>
        /// Stop playback and set the current position to the beginning.
        ///</Summary>
        public void Stop()
        {
            if (!IsPlaying)
            {
                return;
            }

            Pause();
            Seek(0);
        }

        ///<Summary>
        /// Pause playback if playing (does not resume).
        ///</Summary>
        public void Pause()
        {
            player?.Pause();
        }

        ///<Summary>
        /// Set the current playback position (in seconds).
        ///</Summary>
        public void Seek(double position)
        {
            if (CanSeek)
            {
                player?.SeekTo((int)position * 1000);
            }
        }

        ///<Summary>
        /// Sets the playback volume as a <c>double</c> between 0 and 1. <para> </para>
        /// Sets both left and right channels.
        ///</Summary>
        private void SetVolume(double volume, double balance)
        {
            this.volume = volume < 0 ? 0 : volume > 1 ? 1 : volume;
            this.balance = balance < 0 ? 0 : balance > 1 ? 1 : balance;

            // Using the "constant power pan rule." See: http://www.rs-met.com/documents/tutorials/PanRules.pdf
            double left = Math.Cos((Math.PI * (balance + 1)) / 4) * volume;
            double right = Math.Sin((Math.PI * (balance + 1)) / 4) * volume;

            player?.SetVolume((float)left, (float)right);
        }

        private void OnPlaybackEnded(object sender, EventArgs e)
        {
            PlaybackEnded?.Invoke(sender, e);

            // This improves stability on older devices but has minor performance impact.
            if (Build.VERSION.SdkInt < BuildVersionCodes.M)
            {
                player.SeekTo(0);
                player.Stop();
                player.Prepare();
            }
        }

        ///<Summary>
        /// Dispose the player and release all of its resources.
        ///</Summary>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            
            DeletePlayer();
            DeleteTempFile();
            
            disposed = true;
            GC.SuppressFinalize(this);
        }
        
        private void DeletePlayer()
        {
            Stop();

            if (player != null)
            {
                player.Completion -= OnPlaybackEnded;
                player.Release();
                player.Dispose();
                player = null;
            }

            DeleteTempFile();
        }

        private void DeleteTempFile()
        {
            if (string.IsNullOrEmpty(deleteOnDispose) || !File.Exists(deleteOnDispose))
            {
                return;
            }

            File.Delete(deleteOnDispose);
            deleteOnDispose = null;
        }
        
        ~SimpleAudioPlayerAndroid()
        {
            Dispose();
        }
    }
}
