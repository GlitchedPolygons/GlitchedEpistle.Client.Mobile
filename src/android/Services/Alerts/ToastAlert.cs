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
using Android.Widget;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Alerts;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Android.Services.Alerts;

using Application = Android.App.Application;

[assembly: Dependency(typeof(ToastAlert))]

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Android.Services.Alerts
{
    /// <summary>
    /// Android <see cref="IAlertService"/> implementation using Toast.
    /// </summary>
    public class ToastAlert : IAlertService
    {
        /// <summary>
        /// Shows a small alert message overlay to the user for a short amount of time.
        /// </summary>
        /// <param name="message">The message <c>string</c> to display to the user.</param>
        public void AlertShort(string message)
        {
            Toast.MakeText(Application.Context, message, ToastLength.Short).Show();
        }

        /// <summary>
        /// Shows a medium-sized alert message overlay to the user for a moderate amount of time.
        /// </summary>
        /// <param name="message">The message <c>string</c> to display to the user.</param>
        public void AlertLong(string message)
        {
            Toast.MakeText(Application.Context, message, ToastLength.Long).Show();
        }
    }
}
