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
using Xamarin.Forms.Xaml;
using Xamarin.Essentials;
using GlitchedPolygons.ExtensionMethods;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Alerts;
using GlitchedPolygons.Services.MethodQ;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class UserCreationSuccessfulView : ContentPage
    {
        private readonly IMethodQ methodQ;
        private readonly ILocalization localization;

        private ulong? scheduledCopyButtonReset;

        public UserCreationSuccessfulView()
        {
            InitializeComponent();
            methodQ = new MethodQ();
            localization = DependencyService.Get<ILocalization>();
        }

        private void SecretTextBox_Focused(object sender, FocusEventArgs e)
        {
            //nop
        }

        private void TotpTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            VerifyButton.IsEnabled = TotpTextBox.Text.NotNullNotEmpty();
        }

        private void CopyButton_Clicked(object sender, EventArgs e)
        {
            Clipboard.SetTextAsync(SecretTextBox.Text);

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
            }, DateTime.UtcNow.AddSeconds(3));
        }
    }
}