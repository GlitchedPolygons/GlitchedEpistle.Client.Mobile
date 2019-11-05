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
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels.Interfaces;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ActiveConvoPage : ContentPage
    {
        public ActiveConvoPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            (BindingContext as IOnAppearingListener)?.OnAppearing();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            (BindingContext as IOnDisappearingListener)?.OnDisappearing();
        }

        private void ScrollToBottomButton_OnClick(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
