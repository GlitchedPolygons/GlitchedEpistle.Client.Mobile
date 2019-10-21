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
using System.Threading.Tasks;
using FFImageLoading.Forms;
using FFImageLoading.Transformations;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using GlitchedPolygons.ExtensionMethods;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels.Interfaces;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ConvosPage : ContentPage
    {
        private TintTransformation idle, pressed, pressedLogout;

        public ConvosPage()
        {
            InitializeComponent();
            RefreshTintTransformations();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            (BindingContext as IOnAppearingListener)?.OnAppearing();
            ResetAllHeaderButtonColors();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            ResetAllHeaderButtonColors();
        }

        private void RefreshTintTransformations()
        {
            idle = new TintTransformation(Application.Current.Resources["HeaderButtonIdleColorHex"].ToString()) {EnableSolidColor = true};
            pressed = new TintTransformation(Application.Current.Resources["HeaderButtonPressedColorHex"].ToString()) {EnableSolidColor = true};
            pressedLogout = new TintTransformation(Application.Current.Resources["LogoutHeaderButtonPressedColorHex"].ToString()) {EnableSolidColor = true};
        }

        private void ResetAllHeaderButtonColors()
        {
            if (HeaderButtons is null || HeaderButtons.Children.NullOrEmpty())
            {
                return;
            }

            RefreshTintTransformations();

            foreach (var c in HeaderButtons.Children)
            {
                if (c is CachedImage cachedImage)
                {
                    ResetHeaderButtonColor(cachedImage);
                }
            }
        }

        private void ResetHeaderButtonColor(CachedImage cachedImage)
        {
            cachedImage?.Transformations.Clear();
            cachedImage?.Transformations.Add(idle);
            cachedImage?.ReloadImage();
        }

        private async void HeaderButtonTapGestureRecognizer_OnTapped(object sender, EventArgs e)
        {
            var cachedImage = sender as CachedImage;
            if (cachedImage is null) return;

            cachedImage.Transformations.Clear();
            cachedImage.Transformations.Add(pressed);
            cachedImage.ReloadImage();

            await Task.Delay(250);
            ResetHeaderButtonColor(cachedImage);
        }

        private void LogoutHeaderButtonTapGestureRecognizer_OnTapped(object sender, EventArgs e)
        {
            var cachedImage = sender as CachedImage;
            if (cachedImage is null) return;

            cachedImage.Transformations.Clear();
            cachedImage.Transformations.Add(pressedLogout);
            cachedImage.ReloadImage();
        }
    }
}
