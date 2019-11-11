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
using System.Collections.Generic;

using Android.Webkit;
using Android.Content;
using Android.Support.V4.Content;

using GlitchedPolygons.ExtensionMethods;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Paths;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Alerts;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Android.Services.Paths;

using Application = Android.App.Application;

[assembly: Dependency(typeof(AndroidFileOpener))]

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Android.Services.Paths
{
    public class AndroidFileOpener : IFileOpener
    {
        private const string DEFAULT_MIME_TYPE = "*/*";
        private readonly IAlertService alertService = DependencyService.Get<IAlertService>();
        private readonly ILocalization localization = DependencyService.Get<ILocalization>();

        public void OpenFile(string filePath)
        {
            string attempt = TryOpenFile(filePath);

            if (attempt.NotNullNotEmpty())
            {
                attempt = TryOpenFile(filePath, DEFAULT_MIME_TYPE);

                if (attempt.NotNullNotEmpty())
                {
#if DEBUG
                    alertService.AlertLong(attempt);
#else
                    alertService.AlertShort(localization["OpenAttachmentFailedErrorMessage"]);
#endif
                }
            }
        }

        private static string TryOpenFile(string filePath, string mimeType = null)
        {
            try
            {
                var intent = new Intent();
                intent.AddFlags(ActivityFlags.NewTask);
                intent.AddFlags(ActivityFlags.GrantReadUriPermission);
                intent.SetAction(Intent.ActionView);
                var file = new Java.IO.File(filePath);
                var uri = FileProvider.GetUriForFile(Application.Context, "com.glitchedpolygons.glitchedepistle.client.mobile.provider", file);
                intent.SetDataAndType(uri, mimeType ?? GetType(filePath));
                Application.Context.StartActivity(intent);
                return null;
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        private static string GetType(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return DEFAULT_MIME_TYPE;
            }

            string mimeType = MimeTypeMap.Singleton.GetMimeTypeFromExtension(MimeTypeMap.GetFileExtensionFromUrl(filePath));

            if (mimeType.NullOrEmpty())
            {
                if (!FILE_TYPES.TryGetValue(Path.GetExtension(filePath), out mimeType))
                {
                    return DEFAULT_MIME_TYPE;
                }
            }

            return mimeType;
        }

        private static readonly Dictionary<string, string> FILE_TYPES = new Dictionary<string, string>
        {
            {".3gp", "video/3gpp"},
            {".apk", "application/vnd.android.package-archive"},
            {".asf", "video/x-ms-asf"},
            {".avi", "video/x-msvideo"},
            {".bin", "application/octet-stream"},
            {".bmp", "image/bmp"},
            {".c", "text/plain"},
            {".class", "application/octet-stream"},
            {".conf", "text/plain"},
            {".cpp", "text/plain"},
            {".doc", "application/msword"},
            {".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"},
            {".exe", "application/octet-stream"},
            {".gif", "image/gif"},
            {".gtar", "application/x-gtar"},
            {".gz", "application/x-gzip"},
            {".h", "text/plain"},
            {".htm", "text/html"},
            {".html", "text/html"},
            {".jar", "application/java-archive"},
            {".java", "text/plain"},
            {".jpeg", "image/jpeg"},
            {".json", "text/plain"},
            {".jpg", "image/jpeg"},
            {".js", "application/x-javascript"},
            {".log", "text/plain"},
            {".m3u", "audio/x-mpegurl"},
            {".m4a", "audio/mp4a-latm"},
            {".m4b", "audio/mp4a-latm"},
            {".m4p", "audio/mp4a-latm"},
            {".m4u", "video/vnd.mpegurl"},
            {".m4v", "video/x-m4v"},
            {".mov", "video/quicktime"},
            {".mp2", "audio/x-mpeg"},
            {".mp3", "audio/mp3"},
            {".mp4", "video/mp4"},
            {".mpc", "application/vnd.mpohun.certificate"},
            {".mpe", "video/mpeg"},
            {".mpeg", "video/mpeg"},
            {".mpg4", "video/mp4"},
            {".mpg", "video/mpeg"},
            {".mpga", "audio/mpeg"},
            {".msg", "application/vnd.ms-outlook"},
            {".ogg", "audio/ogg"},
            {".pdf", "application/pdf"},
            {".png", "image/png"},
            {".pps", "application/vnd.ms-powerpoint"},
            {".ppt", "application/vnd.ms-powerpoint"},
            {".prop", "text/plain"},
            {".rar", "application/x-rar-compressed"},
            {".rc", "text/plain"},
            {".rmvb", "audio/x-pn-realaudio"},
            {".rtf", "application/rtf"},
            {".sh", "text/plain"},
            {".tar", "application/x-tar"},
            {".tgz", "application/x-compressed"},
            {".txt", "text/plain"},
            {".wav", "audio/x-wav"},
            {".wma", "audio/x-ms-wma"},
            {".wmv", "audio/x-ms-wmv"},
            {".wps", "application/vnd.ms-works"},
            {".xml", "text/plain"},
            {".z", "application/x-compress"},
            {".zip", "application/zip"},
        };
    }
}
