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
using System.Linq;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using FFImageLoading.Forms;
using FFImageLoading.Transformations;

using GlitchedPolygons.ExtensionMethods;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels.Interfaces;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ActiveConvoPage : ContentPage
    {
        private TintTransformation idle, disabled, pressed;

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
            Application.Current.Resources.TryGetValue("HeaderButtonDisabledColorHex", out var disabledColorHex);

            idle = new TintTransformation(idleColorHex?.ToString() ?? "#ffffff") {EnableSolidColor = true};
            pressed = new TintTransformation(pressedColorHex?.ToString() ?? "#00b4dd") {EnableSolidColor = true};
            disabled = new TintTransformation(disabledColorHex?.ToString() ?? "#bababa") {EnableSolidColor = true};
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
            cachedImage?.Transformations.Add(cachedImage.IsEnabled ? idle : disabled);
            cachedImage?.ReloadImage();
        }
        
        private async Task OnPressedCachedImage(CachedImage cachedImage)
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
            var _=OnPressedCachedImage(ScrollToBottomButton);
            object last = MessagesListBox.ItemsSource.Cast<object>().LastOrDefault();
            MessagesListBox.ScrollTo(last, ScrollToPosition.End, true);
        }

        private void ExitButton_OnClick(object sender, EventArgs e)
        {
            var _=OnPressedCachedImage(ExitButton);
            Application.Current?.MainPage?.Navigation?.PopModalAsync();
        }

        private void SendTextButton_OnClick(object sender, EventArgs e)
        {
            var _=OnPressedCachedImage(SendTextButton);
            ScrollToBottomButton_OnClick(null, null);
        }

        private void SendAudioButton_OnClick(object sender, EventArgs e)
        {
            var _=OnPressedCachedImage(SendAudioButton);
            ScrollToBottomButton_OnClick(null, null);
        }
        
        private void EditConvoButton_OnClick(object sender, EventArgs e)
        {
            var _=OnPressedCachedImage(EditConvoButton);
        }

        private void TextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            SendTextButton.IsVisible = TextBox.Text.NotNullNotEmpty();
            SendAudioButton.IsVisible = TextBox.Text.NullOrEmpty();
        }

        private void SendFileButton_OnClick(object sender, EventArgs e)
        {
            //nop
        }

        private void MessagesListBox_OnItemAppearing(object sender, ItemVisibilityEventArgs e)
        {
            if (IsLastItem(e.Item))
            {
                ScrollToBottomButton.IsEnabled = false;
                ResetHeaderButtonColor(ScrollToBottomButton);
                return;
            }

            if (IsFirstItem(e.Item))
            {
                LoadPreviousMessagesButton.IsVisible = LoadPreviousMessagesButton.IsEnabled = true;
            }
        }

        private void MessagesListBox_OnItemDisappearing(object sender, ItemVisibilityEventArgs e)
        {
            if (IsLastItem(e.Item))
            {
                ScrollToBottomButton.IsEnabled = true;
                ResetHeaderButtonColor(ScrollToBottomButton);
            }
            
            if (IsFirstItem(e.Item))
            {
                LoadPreviousMessagesButton.IsVisible = LoadPreviousMessagesButton.IsEnabled = false;
            }
        }
        
        private bool IsFirstItem(object item)
        {
            return item == MessagesListBox.ItemsSource.Cast<object>().FirstOrDefault();
        }

        private bool IsLastItem(object item)
        {
            return item == MessagesListBox.ItemsSource.Cast<object>().LastOrDefault();
        }

        private async void LoadPreviousMessagesButton_OnClicked(object sender, EventArgs e)
        {
            object scrollLock = MessagesListBox.ItemsSource.Cast<object>().FirstOrDefault();
            
            if (scrollLock is null)
            {
                return;
            }
            
            if (BindingContext is ActiveConvoViewModel vm)
            {
                await vm.LoadPreviousMessages();
            }
            
            MessagesListBox.ScrollTo(scrollLock, ScrollToPosition.Start, false);
        }
    }
}
