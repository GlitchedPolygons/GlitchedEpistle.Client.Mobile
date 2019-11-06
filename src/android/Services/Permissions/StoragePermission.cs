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

using System.Threading.Tasks;
using Xamarin.Forms;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Alerts;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Permissions;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Android.Services.Permissions;

[assembly: Dependency(typeof(StoragePermission))]

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Android.Services.Permissions
{
    /// <summary>
    /// Ensures that Epistle has the necessary storage permissions for uploading and downloading attachments.
    /// </summary>
    public class StoragePermission : IStoragePermission
    {
        public async Task<bool> CheckPermission(string dialogTitle, string dialogText, string cancellationText)
        {
            PermissionStatus status = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Storage);

            if (status != PermissionStatus.Granted)
            {
                if (await CrossPermissions.Current.ShouldShowRequestPermissionRationaleAsync(Permission.Storage))
                {
                    await Application.Current.MainPage.DisplayAlert(
                        title: dialogTitle,
                        message: dialogText,
                        cancel: "OK"
                    );
                }

                await CrossPermissions.Current.RequestPermissionsAsync(Permission.Storage);
            }

            status = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Storage);
            
            if (status != PermissionStatus.Granted)
            {
                DependencyService.Get<IAlertService>().AlertLong(cancellationText);
                return false;
            }

            return true;
        }
    }
}
