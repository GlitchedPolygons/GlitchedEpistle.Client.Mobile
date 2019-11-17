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
using Java.Interop;
using Xamarin.Forms;
using Android.Content;
using Android.Hardware;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Hardware;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Android.Services.Hardware;

using Application = Android.App.Application;

[assembly: Dependency(typeof(ProximitySensorAndroid))]

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Android.Services.Hardware
{
    public class ProximitySensorAndroid : IProximitySensor
    {
        private readonly Sensor proximitySensor;
        private readonly SensorManager sensorManager;
        private readonly ISensorEventListener eventListener;

        public event Action<bool> ProximitySensorChanged;

        private bool enabled;
        public bool Enabled
        {
            get => enabled;
            set
            {
                if (value == enabled || sensorManager is null || proximitySensor is null || eventListener is null)
                {
                    return;
                }

                if (value)
                {
                    sensorManager.RegisterListener(eventListener, proximitySensor, SensorDelay.Ui);
                }
                else
                {
                    sensorManager.UnregisterListener(eventListener, proximitySensor);
                }

                enabled = value;
            }
        }

        public ProximitySensorAndroid()
        {
            sensorManager = (SensorManager)Application.Context.GetSystemService(Context.SensorService);
            proximitySensor = sensorManager.GetDefaultSensor(SensorType.Proximity);
            eventListener = new ProximitySensorEventListener(_ => ProximitySensorChanged?.Invoke(_));
        }

        private class ProximitySensorEventListener : Java.Lang.Object, ISensorEventListener
        {
            private bool previous;
            private readonly Action<bool> callback;
            
            public ProximitySensorEventListener(Action<bool> callback)
            {
                this.callback = callback;
            }

            public void OnAccuracyChanged(Sensor sensor, SensorStatus accuracy)
            {
                //nop
            }

            public void OnSensorChanged(SensorEvent e)
            {
                if (e.Sensor.Type != SensorType.Proximity)
                {
                    return;
                }

                bool current = e.Values[0] < e.Sensor.MaximumRange;
                
                if (current != previous)
                {
                    previous = current;
                    callback?.Invoke(current);
                }
            }
        }
    }
}
