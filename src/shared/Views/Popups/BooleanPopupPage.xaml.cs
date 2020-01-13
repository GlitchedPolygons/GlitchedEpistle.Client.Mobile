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

using Xamarin.Forms.Xaml;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Views.Popups
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BooleanPopupPage : PopupPage
    {
        private readonly bool disableBackButton, dontCloseOnBackgroundClicked;

        public bool Result { get; private set; }

        public BooleanPopupPage(string title, string description, string trueButtonLabel, string falseButtonLabel, bool defaultResult = false, bool disableBackButton = false, bool dontCloseOnBackgroundClicked = false)
        {
            this.disableBackButton = disableBackButton;
            this.dontCloseOnBackgroundClicked = dontCloseOnBackgroundClicked;

            InitializeComponent();

            TitleLabel.Text = title;
            DescriptionLabel.Text = description;
            TrueButton.Text = trueButtonLabel;
            FalseButton.Text = falseButtonLabel;

            Result = defaultResult;
        }

        private async void TrueButton_Clicked(object sender, System.EventArgs e)
        {
            TrueButton.IsEnabled = FalseButton.IsEnabled = false;

            Result = true;
            await PopupNavigation.Instance.PopAsync();
        }

        private async void FalseButton_Clicked(object sender, System.EventArgs e)
        {
            TrueButton.IsEnabled = FalseButton.IsEnabled = false;

            Result = false;
            await PopupNavigation.Instance.PopAsync();
        }

        // ### Overridden methods which can prevent closing a popup page ###

        // Invoked when a hardware back button is pressed
        protected override bool OnBackButtonPressed()
        {
            if (disableBackButton)
            {
                return true;
            }

            return base.OnBackButtonPressed();
        }

        // Invoked when background is clicked
        protected override bool OnBackgroundClicked()
        {
            if (dontCloseOnBackgroundClicked)
            {
                return false;
            }

            return base.OnBackgroundClicked();
        }
    }
}