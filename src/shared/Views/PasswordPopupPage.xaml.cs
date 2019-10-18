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
using Xamarin.Forms.Xaml;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using GlitchedPolygons.ExtensionMethods;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PasswordPopupPage : PopupPage
    {
        public string Password { get; private set; }

        public PasswordPopupPage()
        {
            InitializeComponent();
        }

        private void OkButton_Clicked(object sender, System.EventArgs e)
        {
            if (PasswordTextEntry.Text.NullOrEmpty())
                return;

            Password = PasswordTextEntry.Text;
            PopupNavigation.Instance.PopAsync();
        }
        
        private void CancelButton_Clicked(object sender, EventArgs e)
        {
            Password = null;
            PopupNavigation.Instance.PopAsync();
        }

        // Invoked when a hardware back button is pressed
        protected override bool OnBackButtonPressed()
        {
            Password = null;
            return base.OnBackButtonPressed();
        }

        // Invoked when background is clicked
        protected override bool OnBackgroundClicked()
        {
            Password = null;
            return base.OnBackgroundClicked();
        }
    }
}
