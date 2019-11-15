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

using System;
using System.Linq;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using GlitchedPolygons.Services.MethodQ;
using GlitchedPolygons.ExtensionMethods;
using GlitchedPolygons.GlitchedEpistle.Client.Utilities;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Settings;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Web.ServerHealth;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Commands;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.PubSubEvents;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels.Interfaces;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels
{
    public class ServerUrlViewModel : ViewModel, IOnAppearingListener, IOnDisappearingListener
    {
        #region Constants

        private readonly IMethodQ methodQ;
        private readonly IAppSettings appSettings;
        private readonly IServerConnectionTest test;
        private readonly ILocalization localization;
        private readonly IEventAggregator eventAggregator;

        #endregion

        #region Commands
        public ICommand ResetCommand { get; }
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

        private Tuple<string, string> language;
        public Tuple<string, string> Language
        {
            get => language;
            set
            {
                if (Set(ref language, value))
                    appSettings["Language"] = value.Item1;

                if (initialized)
                    ShowLanguageRestartRequiredWarning = true;
            }
        }

        private ObservableCollection<Tuple<string, string>> languages;
        public ObservableCollection<Tuple<string, string>> Languages
        {
            get => languages;
            set => Set(ref languages, value);
        }

        private bool showLanguageRestartRequiredWarning = false;
        public bool ShowLanguageRestartRequiredWarning
        {
            get => showLanguageRestartRequiredWarning;
            set => Set(ref showLanguageRestartRequiredWarning, value);
        }

        private bool uiEnabled = true;
        public bool UIEnabled
        {
            get => uiEnabled;
            set => Set(ref uiEnabled, value);
        }

        #endregion

        private volatile bool initialized = false;

        public ServerUrlViewModel(IServerConnectionTest test, IAppSettings appSettings, IEventAggregator eventAggregator, IMethodQ methodQ)
        {
            this.test = test;
            this.methodQ = methodQ;
            this.appSettings = appSettings;
            this.eventAggregator = eventAggregator;

            localization = DependencyService.Get<ILocalization>();

            string url = appSettings.ServerUrl;
            if (url.NotNullNotEmpty())
            {
                ServerUrl = url;
            }

            ResetCommand = new DelegateCommand(OnClickedReset);
            ConnectCommand = new DelegateCommand(OnClickedConnect);
            TestConnectionCommand = new DelegateCommand(OnClickedTestConnection);
        }

        private void OnClickedReset(object commandParam)
        {
            ConnectionOk = Testing = false;
            ServerUrl = appSettings.ServerUrl ?? "https://epistle.glitchedpolygons.com";
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
                if (success)
                {
                    UrlUtility.SetEpistleServerUrl(ServerUrl);
                }

                Testing = false;
                ConnectionOk = success;
                ErrorMessage = ConnectionOk ? null : localization["ConnectionToServerFailed"];
            });
        }

        private void OnClickedConnect(object commandParam)
        {
            Testing = true;
            ConnectionOk = false;

            Task.Run(async () =>
            {
                bool success = await test.TestConnection();
                if (success)
                {
                    UIEnabled = false;
                    UrlUtility.SetEpistleServerUrl(ServerUrl);
                    appSettings.ServerUrl = UrlUtility.EpistleBaseUrl;
                    ExecUI(() => eventAggregator.GetEvent<LogoutEvent>().Publish());
                    return;
                }
                
                Testing = ConnectionOk = false;
                ErrorMessage = localization["ConnectionToServerFailed"];
            });
        }

        public void OnAppearing()
        {
            Languages = new ObservableCollection<Tuple<string, string>>(new List<Tuple<string, string>>
            {
                new Tuple<string, string>("en", localization["English"] + " (English)"),
                new Tuple<string, string>("de", localization["German"] + " (Deutsch)"),
                new Tuple<string, string>("gsw", localization["SwissGerman"] + " (Schwiizerdütsch)"),
                new Tuple<string, string>("it", localization["Italian"] + " (Italiano)")
            });

            Language = Languages.FirstOrDefault(tuple => tuple.Item1 == appSettings["Language", "en"]) ?? Languages[0];

            methodQ.Schedule(() => initialized = true, DateTime.UtcNow.AddMilliseconds(420));
        }

        public void OnDisappearing()
        {
            appSettings["Language"] = Language.Item1;
        }
    }
}
