﻿/*
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

using System.Threading.Tasks;

using Android.OS;
using Android.App;
using Android.Util;
using Android.Content;
using Android.Content.PM;
using Android.Support.V7.App;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Android
{
    [Activity(
        Label = "Glitched Epistle", 
        Icon = "@mipmap/icon", 
        Theme = "@style/MainTheme.Splash", 
        NoHistory = true,
        MainLauncher = true, 
        ScreenOrientation = ScreenOrientation.Portrait,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation
    )]
    public class SplashActivity : AppCompatActivity
    {
        private static readonly string TAG = "X:" + typeof(SplashActivity).Name;

        public override void OnCreate(Bundle savedInstanceState, PersistableBundle persistentState)
        {
            base.OnCreate(savedInstanceState, persistentState);
            Log.Debug(TAG, "SplashActivity.OnCreate");
        }

        // Launches the startup task.
        protected override void OnResume()
        {
            base.OnResume();
            Task startupWork = new Task(SimulateStartup);
            startupWork.Start();
        }

        // Prevent the back button from canceling the startup process.
        public override void OnBackPressed()
        {
            //nop
        }

        // Simulates background work that happens behind the splash screen.
        private void SimulateStartup()
        {
            StartActivity(new Intent(Application.Context, typeof(MainActivity)));
        }
    }
}