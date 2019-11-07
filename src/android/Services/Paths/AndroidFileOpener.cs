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
using Android.Net;
using Android.Content;
using System.Collections.Generic;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Paths;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Android.Services.Paths;
using Application = Android.App.Application;

[assembly: Dependency(typeof(AndroidFileOpener))]

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Android.Services.Paths
{
    public class AndroidFileOpener : IFileOpener
    {
        public void OpenFile(string filePath)
        {
            var intent = new Intent();
            intent.AddFlags(ActivityFlags.NewTask);
            intent.SetAction(Intent.ActionView);
            intent.SetDataAndType(Uri.Parse(filePath), GetType(filePath));
            Application.Context.StartActivity(intent);
        }

        private static string GetType(string filePath)
        {
            string ext = string.Empty;
            for (int i = filePath.Length - 1; i >= 0; i--)
            {
                if (filePath[i] != '.')
                    ext += filePath[i];
                else break;
            }

            string reversedExt = ".";
            for (int i = ext.Length - 1; i >= 0; i--)
                reversedExt += ext[i];

            return FILE_TYPES[reversedExt];
        }

        private static readonly Dictionary<string, string> FILE_TYPES = new Dictionary<string, string>
        {
            {".rtf", "application/rtf"},
            {".doc", "application/msword"},
            {".docx", "application/msword"},
            {".pdf", "application/pdf"},
            {".jpg", "image/jpeg"},
            {".jpeg", "image/jpeg"},
            {".png", "image/png"},
            {".mp3", "audio/mp3"},
            {".mp4", "video/mp4"},
            {".log", "text/plain"},
            {".txt", "text/plain"},
            {".rar", "application/x-rar-compressed"},
            {".zip", "application/zip"},
        };
    }
}