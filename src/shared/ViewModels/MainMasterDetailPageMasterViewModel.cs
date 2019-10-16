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

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels
{
    public class MainMasterDetailPageMasterViewModel : ViewModel
    {
        #region Constants
        #endregion

        #region Commands
        #endregion

        #region UI Bindings
        public ObservableCollection<MainMasterDetailPageMasterMenuItem> MenuItems { get; set; }

        private string username;
        public string Username { get => username; set => Set(ref username, value); }
        #endregion

        public MainMasterDetailPageMasterViewModel()
        {
            MenuItems = new ObservableCollection<MainMasterDetailPageMasterMenuItem>(new[]
            {
                    new MainMasterDetailPageMasterMenuItem { Id = 0, Title = "Page 1" },
                    new MainMasterDetailPageMasterMenuItem { Id = 1, Title = "Page 2" },
                    new MainMasterDetailPageMasterMenuItem { Id = 2, Title = "Page 3" },
                    new MainMasterDetailPageMasterMenuItem { Id = 3, Title = "Page 4" },
                    new MainMasterDetailPageMasterMenuItem { Id = 4, Title = "Page 5" },
            });
        }
    }
}