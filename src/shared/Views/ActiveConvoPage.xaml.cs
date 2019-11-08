﻿/*
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
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using FFImageLoading.Forms;
using FFImageLoading.Transformations;
using GlitchedPolygons.ExtensionMethods;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels.Interfaces;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ActiveConvoPage : ContentPage
    {
        private TintTransformation idle, pressed;

        public ActiveConvoPage()
        {
            InitializeComponent();
            RefreshTintTransformations();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            (BindingContext as IOnAppearingListener)?.OnAppearing();
            TextBox_OnTextChanged(null, null);
            ScrollToBottomButton_OnClick(null, null);
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

            idle = new TintTransformation(idleColorHex?.ToString() ?? "#ffffff") {EnableSolidColor = true};
            pressed = new TintTransformation(pressedColorHex?.ToString() ?? "#00b4dd") {EnableSolidColor = true};
        }

        private void ResetAllHeaderButtonColors()
        {
            if (ExitButton is null || ScrollToBottomButton is null)
            {
                return;
            }

            RefreshTintTransformations();

            ResetHeaderButtonColor(ExitButton);
            ResetHeaderButtonColor(ScrollToBottomButton);
        }

        private void ResetHeaderButtonColor(CachedImage cachedImage)
        {
            cachedImage?.Transformations.Clear();
            cachedImage?.Transformations.Add(idle);
            cachedImage?.ReloadImage();
        }
        
        private async Task TintHeaderButtonColor(CachedImage cachedImage)
        {
            if (cachedImage is null)
            {
                return;
            }

            cachedImage.Transformations.Clear();
            cachedImage.Transformations.Add(pressed);
            cachedImage.ReloadImage();

            await Task.Delay(250);
            
            ResetHeaderButtonColor(cachedImage);
        }

        private void ScrollToBottomButton_OnClick(object sender, EventArgs e)
        {
            var _=TintHeaderButtonColor(ScrollToBottomButton);
            object last = MessagesListBox.ItemsSource.Cast<object>().LastOrDefault();
            MessagesListBox.ScrollTo(last, ScrollToPosition.End, true);
        }

        private void ExitButton_OnClick(object sender, EventArgs e)
        {
            var _=TintHeaderButtonColor(ExitButton);
            Application.Current?.MainPage?.Navigation?.PopModalAsync();
        }

        private void SendTextButton_OnClick(object sender, EventArgs e)
        {
            var _=TintHeaderButtonColor(SendTextButton);
            ScrollToBottomButton_OnClick(null, null);
        }

        private void SendAudioButton_OnClick(object sender, EventArgs e)
        {
            var _=TintHeaderButtonColor(SendAudioButton);
            ScrollToBottomButton_OnClick(null, null);
        }
        
        private void EditConvoButton_OnClick(object sender, EventArgs e)
        {
            var _=TintHeaderButtonColor(EditConvoButton);
        }

        private void TextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            SendTextButton.IsEnabled = SendTextButton.IsVisible = TextBox.Text.NotNullNotEmpty();
            SendAudioButton.IsEnabled = SendAudioButton.IsVisible = TextBox.Text.NullOrEmpty();
        }

        private void SendFileButton_OnClick(object sender, EventArgs e)
        {
            //nop
        }
    }
}
