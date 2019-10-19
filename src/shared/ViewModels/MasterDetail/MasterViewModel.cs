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
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;
using Xamarin.Forms;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels.MasterDetail
{
    public class MasterViewModel : ViewModel
    {
        #region Constants
        private readonly ILocalization localization;
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
            localization = DependencyService.Get<ILocalization>();
            
            MenuItems = new ObservableCollection<MasterMenuItem>(new[]
            {
                new MasterMenuItem {Id = 0, Title = localization["MasterMenuItemConvos"]},
                new MasterMenuItem {Id = 1, Title = localization["MasterMenuItemChangePassword"]},
                new MasterMenuItem {Id = 2, Title = localization["MasterMenuItemSettings"]},
                new MasterMenuItem {Id = 3, Title = localization["MasterMenuItemLogout"]},
            });
        }
    }
}