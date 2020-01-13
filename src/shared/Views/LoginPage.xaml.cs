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

using System;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using FFImageLoading.Forms;
using FFImageLoading.Transformations;
using GlitchedPolygons.ExtensionMethods;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels.Interfaces;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoginPage : ContentPage
    {
        private TintTransformation idle, disabled, pressed;
        private readonly ILocalization localization = DependencyService.Get<ILocalization>();

        public LoginPage()
        {
            InitializeComponent();
            RefreshTintTransformations();

            if (UserIdTextBox.Text.NullOrEmpty())
            {
                UserIdTextBox.Focus();
            }
            else
            {
                PasswordBox.Focus();
            }
        }
        
        private void RefreshTintTransformations()
        {
            Application.Current.Resources.TryGetValue("HeaderButtonIdleColorHex", out var idleColorHex);
            Application.Current.Resources.TryGetValue("HeaderButtonPressedColorHex", out var pressedColorHex);
            Application.Current.Resources.TryGetValue("HeaderButtonDisabledColorHex", out var disabledColorHex);

            idle = new TintTransformation(idleColorHex?.ToString() ?? "#ffffff") { EnableSolidColor = true };
            pressed = new TintTransformation(pressedColorHex?.ToString() ?? "#00b4dd") { EnableSolidColor = true };
            disabled = new TintTransformation(disabledColorHex?.ToString() ?? "#bababa") { EnableSolidColor = true };
        }

        private void ResetHeaderButtonColor(CachedImage cachedImage)
        {
            cachedImage?.Transformations.Clear();
            cachedImage?.Transformations.Add(cachedImage.IsEnabled ? idle : disabled);
            cachedImage?.ReloadImage();
        }

        private async Task OnPressedCachedImage(CachedImage cachedImage)
        {
            if (cachedImage is null || !cachedImage.IsEnabled)
            {
                return;
            }

            cachedImage.Transformations.Clear();
            cachedImage.Transformations.Add(pressed);
            cachedImage.ReloadImage();

            await Task.Delay(250);

            ResetHeaderButtonColor(cachedImage);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            (BindingContext as IOnAppearingListener)?.OnAppearing();
            
            UserIdTextBox_TextChanged(this, null);
            PasswordBox_TextChanged(this, null);
            TotpTextBox_TextChanged(this, null);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            (BindingContext as IOnDisappearingListener)?.OnDisappearing();
        }

        private async void Help2FAButton_OnClick(object sender, EventArgs e)
        {
            var _ = OnPressedCachedImage(Help2FA_Button);
            await Application.Current.MainPage.DisplayAlert(localization["HelpDialogTitle2FA"], localization["HelpDialogText2FA"], "OK");
        }

        private void UserIdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoginButton.IsEnabled = FormReady;
        }

        private void PasswordBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoginButton.IsEnabled = FormReady;
        }

        private void TotpTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoginButton.IsEnabled = FormReady;
        }

        private bool FormReady => UserIdTextBox.Text.NotNullNotEmpty() && PasswordBox.Text.NotNullNotEmpty();
    }
}