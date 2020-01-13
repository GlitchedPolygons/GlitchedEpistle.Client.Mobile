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

using Android.OS;
using Xamarin.Forms;
using System.IO;
using System.Threading.Tasks;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Paths;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Android.Services.Paths;

[assembly: Dependency(typeof(AndroidDownloadPath))]

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Android.Services.Paths
{
    /// <summary>
    /// Path helper service for getting the current Android default download directory.
    /// </summary>
    public class AndroidDownloadPath : IDownloadPath
    {
        public Task<string> GetDownloadDirectoryPath()
        {
            return Task.FromResult(Path.Combine(Environment.ExternalStorageDirectory.AbsolutePath, Environment.DirectoryDownloads));
        }
    }
}
