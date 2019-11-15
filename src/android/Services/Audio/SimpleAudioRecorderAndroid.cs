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
using Android.Media;
using System;
using System.IO;
using System.Threading.Tasks;
using Plugin.SimpleAudioRecorder;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Android.Services.Audio;

[assembly: Dependency(typeof(SimpleAudioRecorderAndroid))]

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Android.Services.Audio
{
    public class SimpleAudioRecorderAndroid : ISimpleAudioRecorder
    {
        public bool CanRecordAudio => true;
        
        private volatile bool isRecording;
        public bool IsRecording => isRecording;
        
        private volatile string filePath;
        private volatile MediaRecorder recorder;

        public Task RecordAsync()
        {
            if (isRecording)
            {
                return Task.CompletedTask;
            }

            return Task.Run(() =>
            {
                try
                {
                    isRecording = true;
                    filePath = Path.GetTempFileName();

                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    
                    recorder = new MediaRecorder();

                    recorder.Reset();
                    recorder.SetAudioSource(AudioSource.Mic);
                    recorder.SetOutputFormat(OutputFormat.AacAdts);
                    recorder.SetAudioEncoder(AudioEncoder.Aac);
                    recorder.SetOutputFile(filePath);
                    recorder.Prepare();
                    recorder.Start();
                }
                catch (Exception e)
                {
                    isRecording = false;
                    Console.Out.WriteLine(e.StackTrace);
                }
            });
        }

        public Task<AudioRecording> StopAsync()
        {
            return Task.Run(() =>
            {
                if (!isRecording || recorder is null)
                {
                    return null;
                }

                isRecording = false;
                recorder.Stop();
                recorder.Release();
                recorder = null;

                return new AudioRecording(filePath);
            });
        }
    }
}
