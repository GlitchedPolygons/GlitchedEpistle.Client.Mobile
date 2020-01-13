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
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Alerts;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Xamarin.Essentials;
using GlitchedPolygons.Services.MethodQ;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BackupCodesPage : ContentPage
    {
        private readonly IMethodQ methodQ;
        private readonly ILocalization localization;

        private ulong? scheduledCopyButtonReset;

        public BackupCodesPage(string backupText)
        {
            localization = DependencyService.Get<ILocalization>();
            methodQ = new MethodQ();
            InitializeComponent();
            BackupCodesText.Text = backupText;
        }

        private async void DismissButton_Clicked(object sender, EventArgs e)
        {
            await Application.Current.MainPage.Navigation.PopModalAsync();
        }

        private void CopyButton_Clicked(object sender, EventArgs e)
        {
            Clipboard.SetTextAsync(BackupCodesText.Text);

            DependencyService.Get<IAlertService>()?.AlertShort(localization["Copied"]);
            
            CopyButton.IsEnabled = false;
            CopyButton.Text = localization["Copied"];

            if (scheduledCopyButtonReset != null)
            {
                methodQ.Cancel(scheduledCopyButtonReset.Value);
            }

            scheduledCopyButtonReset = methodQ.Schedule(() =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    CopyButton.IsEnabled = true;
                    CopyButton.Text = localization["Copy"];
                });
            }, DateTime.UtcNow.AddSeconds(2.5));
        }
    }
}