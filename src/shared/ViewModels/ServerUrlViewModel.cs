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
    public class ServerUrlViewModel : ViewModel
    {
        private readonly IAppSettings appSettings;
        private readonly IServerConnectionTest test;
        private readonly ILocalization localization;
        private readonly IEventAggregator eventAggregator;

        #region Commands
        public ICommand ConnectCommand { get; }
        public ICommand TestConnectionCommand { get; }
        #endregion

        #region UI Bindings
        private string serverUrl = "https://epistle.glitchedpolygons.com";
        public string ServerUrl
        {
            get => serverUrl;
            set
            {
                Set(ref serverUrl, value);
                ConnectionOk = false;
            }
        }

        private bool connectionOk = false;
        public bool ConnectionOk { get => connectionOk; set => Set(ref connectionOk, value); }

        private bool testing = false;
        public bool Testing { get => testing; set => Set(ref testing, value); }
        #endregion

        public ServerUrlViewModel(IServerConnectionTest test, IAppSettings appSettings, IEventAggregator eventAggregator)
        {
            this.test = test;
            this.appSettings = appSettings;
            this.eventAggregator = eventAggregator;
            localization = DependencyService.Get<ILocalization>();

            string url = appSettings.ServerUrl;
            if (url.NotNullNotEmpty())
            {
                ServerUrl = url;
            }

            ConnectCommand = new DelegateCommand(OnClickedConnect);
            TestConnectionCommand = new DelegateCommand(OnClickedTestConnection);

            ConnectionOk = false;
        }

        private void OnClickedTestConnection(object commandParam)
        {
            if (ServerUrl.NullOrEmpty())
            {
                ErrorMessage = localization["ConnectionToServerFailed"];
                return;
            }

            Testing = true;
            ConnectionOk = false;

            Task.Run(async () =>
            {
                bool success = await test.TestConnection(ServerUrl);
                if (success) UrlUtility.SetEpistleServerUrl(ServerUrl);
                ExecUI(() =>
                {
                    Testing = false;
                    ConnectionOk = success;
                    ErrorMessage = ConnectionOk ? null : localization["ConnectionToServerFailed"];
                });
            });
        }

        private void OnClickedConnect(object commandParam)
        {
            ConnectionOk = false;
            Task.Run(async () =>
            {
                bool success = await test.TestConnection();
                if (success)
                {
                    UrlUtility.SetEpistleServerUrl(ServerUrl);
                    appSettings.ServerUrl = UrlUtility.EpistleBaseUrl;
                    ExecUI(() => eventAggregator.GetEvent<LogoutEvent>().Publish());
                    return;
                }
                ExecUI(() => ErrorMessage = localization["ConnectionToServerFailed"]);
            });
        }
    }
}
