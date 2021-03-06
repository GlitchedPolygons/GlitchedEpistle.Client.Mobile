﻿/*
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

using System;
using System.Threading;
using System.Threading.Tasks;
using Android.OS;
using Android.App;
using Android.Content;
using Android.Views;
using Android.Runtime;
using Android.Content.PM;
using Plugin.Fingerprint;
using Plugin.Permissions;
using FFImageLoading.Forms.Platform;
using Plugin.CurrentActivity;
using Xamarin.Forms;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Android
{
    [Activity(
        Label = "Glitched Epistle",
        Theme = "@style/MainTheme",
        Icon = "@mipmap/icon",
        ScreenOrientation = ScreenOrientation.Portrait,
        WindowSoftInputMode = SoftInput.AdjustPan,
        LaunchMode = LaunchMode.SingleTask)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        [Service]
        private class AuthRefreshService : Service
        {
            private volatile CancellationTokenSource authRefreshCycle = null;
            
            public override IBinder OnBind(Intent intent)
            {
                return null;
            }

            public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
            {
                authRefreshCycle?.Cancel();
                authRefreshCycle = new CancellationTokenSource();
                
                Task.Run(async () =>
                {
                    do
                    {
                        MessagingCenter.Send<object>(this, "GlitchedEpistle_RefreshAuth");
                        await Task.Delay(TimeSpan.FromMinutes(10));
                    }
                    while (!authRefreshCycle.IsCancellationRequested);
                }, authRefreshCycle.Token);

                return StartCommandResult.Sticky;
            }

            public override void OnDestroy()
            {
                authRefreshCycle?.Cancel();
                authRefreshCycle = null;
                base.OnDestroy();
            }
        };

        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            MessagingCenter.Subscribe<App>(this, "GlitchedEpistle_StopRefreshingAuth", sender =>
            {
                StopService(new Intent(this, typeof(AuthRefreshService)));
            });

            MessagingCenter.Subscribe<App>(this, "GlitchedEpistle_StartRefreshingAuth", sender =>
            {
                StartService(new Intent(this, typeof(AuthRefreshService)));
            });

            CrossFingerprint.SetCurrentActivityResolver(() => CrossCurrentActivity.Current.Activity);

            Rg.Plugins.Popup.Popup.Init(this, savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            Plugin.CurrentActivity.CrossCurrentActivity.Current.Init(this, savedInstanceState);
            global::ZXing.Net.Mobile.Forms.Android.Platform.Init();
            CachedImageRenderer.Init(enableFastRenderer: true);
            CachedImageRenderer.InitImageViewHandler();
            CreateNotificationChannel();

            LoadApplication(new App());

            Window.SetStatusBarColor(global::Android.Graphics.Color.Argb(255, 0, 0, 0));
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] global::Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            global::ZXing.Net.Mobile.Android.PermissionsHandler.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            PermissionsImplementation.Current.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        public override void OnBackPressed()
        {
            if (Rg.Plugins.Popup.Popup.SendBackPressed(base.OnBackPressed))
            {
                // Do something if there are some pages in the `PopupStack`
            }
            else
            {
                // Do something if there are not any pages in the `PopupStack`
            }
        }

        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                // Notification channels are new in API 26 (and not a part of the
                // support library). There is no need to create a notification
                // channel on older versions of Android.
                return;
            }

            var channel = new NotificationChannel("Messages", "Messages", NotificationImportance.Default)
            {
                Description = ""
            };

            var notificationManager = GetSystemService(NotificationService) as NotificationManager;
            notificationManager?.CreateNotificationChannel(channel);

            //----------------

            /*
            Calendar calendar = Calendar.GetInstance(TimeZone.Default);

            calendar.Set(CalendarField.);
            var pendingIntent = PendingIntent.GetService(Application.Context, 0, new Intent(Application.Context, MyClass.class),PendingIntent.FLAG_UPDATE_CURRENT);
            
            var alarmManager = (AlarmManager)GetSystemService(AlarmService);
            alarmManager.SetInexactRepeating(AlarmType.RtcWakeup, 420000, 420000, pendingIntent);
            */
        }
    }
}