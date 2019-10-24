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

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using GlitchedPolygons.ExtensionMethods;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ChangePasswordPage : ContentPage
    {
        public ChangePasswordPage()
        {
            InitializeComponent();
            UpdateSubmitButton();
        }

        private void CurrentPasswordBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateSubmitButton();
        }

        private void NewPasswordBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateSubmitButton();
        }

        private void NewPasswordConfirmationBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateSubmitButton();
        }

        private void UpdateSubmitButton()
        {
            SubmitButton.IsEnabled =
                CurrentPasswordBox.Text.NotNullNotEmpty()
                && NewPasswordBox.Text.NotNullNotEmpty()
                && NewPasswordBox.Text.Length > 6
                && NewPasswordBox.Text == NewPasswordConfirmationBox.Text;
        }
    }
}
