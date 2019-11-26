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

using Android.App;
using Android.Content;
using Android.Support.V4.App;

using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Alerts;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Android.Services.Alerts;

using Application = Android.App.Application;

[assembly: Dependency(typeof(GenericMessageNotification))]

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Android.Services.Alerts
{
    public class GenericMessageNotification : IGenericMessageNotification
    {
        private readonly ILocalization localization = DependencyService.Get<ILocalization>();

        public void Push()
        {
            // Set up an intent so that tapping the notifications returns to this app:
            var intent = new Intent(Application.Context, typeof(MainActivity));
            var pendingIntent = PendingIntent.GetActivity(Application.Context, 0, intent, PendingIntentFlags.OneShot);

            // Instantiate the builder and set notification elements:
            var builder = new NotificationCompat.Builder(Application.Context, "Messages")
                .SetStyle(new NotificationCompat.BigTextStyle())
                .SetAutoCancel(true)
                .SetContentIntent(pendingIntent)
                .SetDefaults(~(int)NotificationDefaults.All)
                .SetContentTitle(localization["NewMessagesNotificationTitle"])
                .SetContentText(localization["NewMessagesNotificationText"])
                .SetSmallIcon(Resource.Drawable.epistle_notification_icon);

            // Build & publish the notification:
            var notification = builder.Build();
            var notificationManager = Application.Context?.GetSystemService(Context.NotificationService) as NotificationManager;
            
            notificationManager?.Notify(0, notification);
        }
    }
}