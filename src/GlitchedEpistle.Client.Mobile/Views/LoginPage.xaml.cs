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

using GlitchedPolygons.ExtensionMethods;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoginPage : ContentPage
    {
        public LoginPage()
        {
            InitializeComponent();
            
            if (UserIdTextBox.Text.NullOrEmpty())
            {
                UserIdTextBox.Focus();
            }
            else
            {
                PasswordBox.Focus();
            }

            RegisterButton.IsEnabled = UserIdTextBox.Text.NullOrEmpty();
        }

        private void UserIdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoginButton.IsEnabled = FormReady;
            RegisterButton.IsEnabled = UserIdTextBox.Text.NullOrEmpty();
        }

        private void PasswordBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoginButton.IsEnabled = FormReady;
        }

        private void TotpTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoginButton.IsEnabled = FormReady;
        }

        private bool FormReady => UserIdTextBox.Text.NotNullNotEmpty()
                                  && PasswordBox.Text.NotNullNotEmpty()
                                  && TotpTextBox.Text.NotNullNotEmpty();
    }
}