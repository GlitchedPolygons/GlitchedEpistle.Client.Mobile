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
using GlitchedPolygons.GlitchedEpistle.Client.Models;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels;
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
            (BindingContext as IOnDisappearingListener)?.OnDisappearing();
            ResetAllHeaderButtonColors();
        }

        private void RefreshTintTransformations()
        {
            Application.Current.Resources.TryGetValue("HeaderButtonIdleColorHex", out var idleColorHex);
            Application.Current.Resources.TryGetValue("HeaderButtonPressedColorHex", out var pressedColorHex);
            Application.Current.Resources.TryGetValue("LogoutHeaderButtonPressedColorHex", out var pressedLogoutColorHex);
            
            idle = new TintTransformation(idleColorHex?.ToString() ?? "#ffffff") {EnableSolidColor = true};
            pressed = new TintTransformation(pressedColorHex?.ToString() ?? "#00b4dd") {EnableSolidColor = true};
            pressedLogout = new TintTransformation(pressedLogoutColorHex?.ToString() ?? "#cc0000") {EnableSolidColor = true};
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

        private void ConvosListView_OnItemTapped(object sender, ItemTappedEventArgs e)
        {
            (BindingContext as ConvosViewModel)?.OnClickedOnConvo(e.Item);
            ConvosListView.SelectedItem = null;
        }
    }
}
