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
using GlitchedPolygons.ExtensionMethods;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Xamarin.Essentials;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Alerts;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Views.Popups
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TextPromptPopupPage : PopupPage
    {
        public string Text { get; private set; }

        private readonly IAlertService alertService;
        private readonly ILocalization localization;
        private readonly bool allowCancel, allowNullOrEmptyString;

        public TextPromptPopupPage(string title, string description, string okButtonText = null, string cancelButtonText = null, bool allowCancel = true, bool allowPaste = false, bool allowNullOrEmptyString = false)
        {
            InitializeComponent();

            this.allowCancel = allowCancel;
            this.allowNullOrEmptyString = allowNullOrEmptyString;

            alertService = DependencyService.Get<IAlertService>();
            localization = DependencyService.Get<ILocalization>();

            TitleLabel.Text = title;
            DescriptionLabel.Text = description;

            if (okButtonText.NotNullNotEmpty())
            {
                OkButton.Text = okButtonText;
            }

            if (cancelButtonText.NotNullNotEmpty())
            {
                CancelButton.Text = cancelButtonText;
            }

            if (!allowCancel)
            {
                CancelButton.IsVisible = CancelButton.IsEnabled = false;
            }

            PasteFromClipboard.IsEnabled = PasteFromClipboard.IsVisible = allowPaste;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Entry_TextChanged(null, null);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
        }

        // ### Methods for supporting animations in your popup page ###

        // Invoked before an animation appearing
        protected override void OnAppearingAnimationBegin()
        {
            base.OnAppearingAnimationBegin();
        }

        // Invoked after an animation appearing
        protected override void OnAppearingAnimationEnd()
        {
            base.OnAppearingAnimationEnd();
        }

        // Invoked before an animation disappearing
        protected override void OnDisappearingAnimationBegin()
        {
            base.OnDisappearingAnimationBegin();
        }

        // Invoked after an animation disappearing
        protected override void OnDisappearingAnimationEnd()
        {
            base.OnDisappearingAnimationEnd();
        }

        protected override Task OnAppearingAnimationBeginAsync()
        {
            return base.OnAppearingAnimationBeginAsync();
        }

        protected override Task OnAppearingAnimationEndAsync()
        {
            return base.OnAppearingAnimationEndAsync();
        }

        protected override Task OnDisappearingAnimationBeginAsync()
        {
            return base.OnDisappearingAnimationBeginAsync();
        }

        protected override Task OnDisappearingAnimationEndAsync()
        {
            return base.OnDisappearingAnimationEndAsync();
        }

        private async void OkButton_Clicked(object sender, System.EventArgs e)
        {
            if (TextEntry.Text.NullOrEmpty() && !allowNullOrEmptyString)
            {
                return;
            }

            Text = TextEntry.Text;
            await PopupNavigation.Instance.PopAsync();
        }

        private async void CancelButton_OnClicked(object sender, EventArgs e)
        {
            if (!allowCancel)
            {
                return;
            }
            
            Text = TextEntry.Text = null;
            await PopupNavigation.Instance.PopAsync();
        }

        // ### Overridden methods which can prevent closing a popup page ###

        // Invoked when a hardware back button is pressed
        protected override bool OnBackButtonPressed()
        {
            // Return true if you don't want to close this popup page when a back button is pressed

            //return base.OnBackButtonPressed();
            return true;
        }

        // Invoked when background is clicked
        protected override bool OnBackgroundClicked()
        {
            // Return false if you don't want to close this popup page when a background of the popup page is clicked

            //return base.OnBackgroundClicked();
            return false;
        }

        private async void PasteFromClipboard_Clicked(object sender, EventArgs e)
        {
            string paste = await Clipboard.GetTextAsync();
            if (paste.NullOrEmpty())
            {
                alertService.AlertShort(localization["ClipboardEmpty"]);
                return;
            }
            TextEntry.Text = Text = paste;
        }

        private void Entry_TextChanged(object sender, Xamarin.Forms.TextChangedEventArgs e)
        {
            OkButton.IsEnabled = allowNullOrEmptyString || TextEntry.Text.NotNullNotEmpty();
        }
    }
}