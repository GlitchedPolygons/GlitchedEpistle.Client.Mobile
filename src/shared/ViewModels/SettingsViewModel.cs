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

using Prism.Events;
using Xamarin.Forms;
using System.Windows.Input;
using System.Threading.Tasks;
using GlitchedPolygons.ExtensionMethods;
using GlitchedPolygons.GlitchedEpistle.Client.Utilities;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Settings;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Web.ServerHealth;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Commands;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.PubSubEvents;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels
{
    public class SettingsViewModel : ViewModel
    {
        private readonly IAppSettings appSettings;
        private readonly IServerConnectionTest test;
        private readonly ILocalization localization;
        private readonly IEventAggregator eventAggregator;

        #region Commands
        public ICommand ResetCommand { get; }
        #endregion

        #region UI Bindings
        #endregion

        public SettingsViewModel(IServerConnectionTest test, IAppSettings appSettings, IEventAggregator eventAggregator)
        {
            this.test = test;
            this.appSettings = appSettings;
            this.eventAggregator = eventAggregator;
            localization = DependencyService.Get<ILocalization>();


            ResetCommand = new DelegateCommand(OnClickedReset);
        }

        private void OnClickedReset(object commandParam)
        {
            
        }
    }
}
