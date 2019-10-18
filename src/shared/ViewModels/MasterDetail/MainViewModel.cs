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
using GlitchedPolygons.GlitchedEpistle.Client.Services.Settings;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Factories;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels.MasterDetail
{
    public class MainViewModel : ViewModel
    {
        #region Constants

        private readonly IAppSettings appSettings;
        private readonly IUserSettings userSettings;
        private readonly IViewModelFactory viewModelFactory;
        
        #endregion

        #region Commands

        #endregion

        #region UI Bindings

        private string username;
        public string Username
        {
            get => username;
            set => Set(ref username, value);
        }

        private ContentView masterPage;
        public ContentView MasterPage
        {
            get => masterPage;
            set => Set(ref masterPage, value);
        }

        private ContentView detailPage;
        public ContentView DetailPage
        {
            get => detailPage;
            set => Set(ref detailPage, value);
        }

        #endregion

        public MainViewModel(IViewModelFactory viewModelFactory, IAppSettings appSettings, IUserSettings userSettings)
        {
            this.appSettings = appSettings;
            this.userSettings = userSettings;
            this.viewModelFactory = viewModelFactory;

            Username = userSettings.Username;
        }
    }
}