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

using System;
using Xamarin.Forms;
using Android.Media;
using Android.Content;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Android.Services.Audio;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services;

using Application = Android.App.Application;

[assembly: Dependency(typeof(SilenceDetector))]

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Android.Services.Audio
{
    /// <summary>
    /// Android implementation of <see cref="ISilenceDetector"/>.
    /// <seealso cref="ISilenceDetector"/>
    /// </summary>
    public class SilenceDetector : ISilenceDetector
    {
        private readonly Lazy<AudioManager> audioManager = new Lazy<AudioManager>(() => (AudioManager)Application.Context.GetSystemService(Context.AudioService));

        /// <summary>
        /// Checks if audio has been silenced on the current device.
        /// </summary>
        /// <returns>Whether audio has been silenced on the current device or not.</returns>
        public bool IsAudioMuted()
        {
            return audioManager.Value.RingerMode == RingerMode.Silent;
        }
    }
}
