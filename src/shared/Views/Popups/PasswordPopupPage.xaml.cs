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
using GlitchedPolygons.ExtensionMethods;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Views.Popups
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PasswordPopupPage : PopupPage
    {
        public bool Cancelled { get; set; }
        public string Password { get; private set; }

        public PasswordPopupPage(string title, string description, string cancelButtonLabel = null, string okButtonLabel = null)
        {
            var localization = DependencyService.Get<ILocalization>();
            
            InitializeComponent();

            TitleLabel.Text = title;
            DescriptionLabel.Text = description;
            
            OkButton.Text = okButtonLabel ?? "OK";
            CancelButton.Text = cancelButtonLabel ?? localization["CancelButton"];
        }

        private async void OkButton_Clicked(object sender, System.EventArgs e)
        {
            Cancelled = false;
            Password = PasswordTextEntry.Text;
            await PopupNavigation.Instance.PopAsync();
        }
        
        private async void CancelButton_Clicked(object sender, EventArgs e)
        {
            Password = null;
            Cancelled = true;
            await PopupNavigation.Instance.PopAsync();
        }

        // Invoked when a hardware back button is pressed.
        protected override bool OnBackButtonPressed()
        {
            Password = null;
            Cancelled = true;
            return base.OnBackButtonPressed();
        }

        // Invoked when background is clicked.
        protected override bool OnBackgroundClicked()
        {
            Password = null;
            Cancelled = true;
            return base.OnBackgroundClicked();
        }
    }
}
