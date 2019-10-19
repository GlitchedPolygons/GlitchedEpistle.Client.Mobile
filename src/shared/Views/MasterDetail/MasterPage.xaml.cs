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
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Models;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels.MasterDetail;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Views.MasterDetail
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MasterPage : ContentView
    {
        public MasterPage()
        {
            InitializeComponent();
        }

        private void ListView_OnItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (Application.Current.MainPage is MainPage mainPage)
            {
                mainPage.IsPresented = false;
                
                var item = e.SelectedItem as MasterMenuItem;
                if (item is null)
                {
                    return;
                }

                (mainPage.BindingContext as MainViewModel)?.OnSelectedMasterMenuItem(item.Id);
            }
        }

        private void ListView_OnItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (Application.Current.MainPage is MainPage mainPage)
            {
                mainPage.IsPresented = false;
                
                var item = e.Item as MasterMenuItem;
                if (item is null)
                {
                    return;
                }

                (mainPage.BindingContext as MainViewModel)?.OnSelectedMasterMenuItem(item.Id);
            }
        }
    }
}
