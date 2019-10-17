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

using System.Collections.ObjectModel;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Models;
using Xamarin.Forms;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels.MasterDetail
{
    public class MasterViewModel : ViewModel
    {
        #region Constants

        #endregion

        #region Commands

        #endregion

        #region UI Bindings

        private ObservableCollection<MasterMenuItem> menuItems;
        public ObservableCollection<MasterMenuItem> MenuItems
        {
            get => menuItems;
            set => Set(ref menuItems, value);
        }

        private ImageSource iconImageSource;
        public ImageSource IconImageSource
        {
            get => iconImageSource;
            set => Set(ref iconImageSource, value);
        }

        private string username;
        public string Username
        {
            get => username;
            set => Set(ref username, value);
        }

        #endregion

        public MasterViewModel()
        {
            MenuItems = new ObservableCollection<MasterMenuItem>(new[]
            {
                new MasterMenuItem {Id = 0, Title = "Page 1"},
                new MasterMenuItem {Id = 1, Title = "Page 2"},
                new MasterMenuItem {Id = 2, Title = "Page 3"},
                new MasterMenuItem {Id = 3, Title = "Page 4"},
                new MasterMenuItem {Id = 4, Title = "Page 5"},
            });
        }
    }
}