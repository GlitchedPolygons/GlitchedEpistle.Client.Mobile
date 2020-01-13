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

using System.Threading.Tasks;
using Xamarin.Forms;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Alerts;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Permissions;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Android.Services.Permissions;

[assembly: Dependency(typeof(PermissionChecker))]

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Android.Services.Permissions
{
    /// <summary>
    /// Ensures that Epistle has a necessary <see cref="Permission"/>.
    /// </summary>
    public class PermissionChecker : IPermissionChecker
    {
        public async Task<bool> CheckPermission(Permission permission, string dialogTitle, string dialogText, string cancellationText)
        {
            PermissionStatus status = await CrossPermissions.Current.CheckPermissionStatusAsync(permission);

            if (status != PermissionStatus.Granted)
            {
                if (await CrossPermissions.Current.ShouldShowRequestPermissionRationaleAsync(permission))
                {
                    await Application.Current.MainPage.DisplayAlert(
                        title: dialogTitle,
                        message: dialogText,
                        cancel: "OK"
                    );
                }

                await CrossPermissions.Current.RequestPermissionsAsync(permission);
            }

            status = await CrossPermissions.Current.CheckPermissionStatusAsync(permission);
            
            if (status != PermissionStatus.Granted)
            {
                DependencyService.Get<IAlertService>().AlertLong(cancellationText);
                return false;
            }

            return true;
        }
    }
}
