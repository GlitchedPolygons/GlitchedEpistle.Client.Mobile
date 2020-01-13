/*
    Glitched Epistle - Mobile Client
    Copyright (C) 2020 Raphael Beck

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

using UIKit;
using Foundation;
using Xamarin.Forms;
using GlitchedEpistle.Client.Mobile.iOS.Services.Alerts;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Alerts;

[assembly: Dependency(typeof(iOSAlerts))]

namespace GlitchedEpistle.Client.Mobile.iOS.Services.Alerts
{
    /// <summary>
    /// iOS <see cref="IAlertService"/> implementation using <see cref="UIAlertController"/>.
    /// </summary>
    public class iOSAlerts : IAlertService
    {
        private const double SHORT_DELAY = 2.5d;
        private const double LONG_DELAY = 3.75d;

        /// <summary>
        /// Shows a small alert message overlay to the user for a short amount of time.
        /// </summary>
        /// <param name="message">The message <c>string</c> to display to the user.</param>
        public void AlertShort(string message)
        {
            ShowAlert(message, SHORT_DELAY);
        }

        /// <summary>
        /// Shows a medium-sized alert message overlay to the user for a moderate amount of time.
        /// </summary>
        /// <param name="message">The message <c>string</c> to display to the user.</param>
        public void AlertLong(string message)
        {
            ShowAlert(message, LONG_DELAY);
        }
        
        private void ShowAlert(string message, double seconds)
        {
            UIAlertController alert = UIAlertController.Create(null, message, UIAlertControllerStyle.Alert);

            NSTimer alertDelay = NSTimer.CreateScheduledTimer(seconds, obj =>
            {
                DismissMessage(alert, obj);
            });

            UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(alert, true, null);
        }
        
        private void DismissMessage(UIAlertController alert, NSTimer alertDelay)
        {
            alert?.DismissViewController(true, null);
            alertDelay?.Dispose();
        }
    }
}
