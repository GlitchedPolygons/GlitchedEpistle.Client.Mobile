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
using System.Linq;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Threading;
using GlitchedPolygons.ExtensionMethods;
using GlitchedPolygons.Services.Cryptography.Asymmetric;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Commands;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.PubSubEvents;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Alerts;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Factories;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels.Interfaces;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Views;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Views.Popups;
using GlitchedPolygons.GlitchedEpistle.Client.Models;
using GlitchedPolygons.GlitchedEpistle.Client.Models.DTOs;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Cryptography.Messages;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Logging;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Settings;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Web.Convos;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Web.Users;
using GlitchedPolygons.RepositoryPattern;
using GlitchedPolygons.Services.MethodQ;
using Prism.Events;
using Newtonsoft.Json;
using Xamarin.Forms;
using Xamarin.Essentials;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels
{
    public class ActiveConvoViewModel : ViewModel, IOnAppearingListener, IOnDisappearingListener, IDisposable
    {
        #region Constants

        private const int MSG_COLLECTION_SIZE = 20;
        private const string MSG_TIMESTAMP_FORMAT = "dd.MM.yyyy HH:mm";
        private static readonly TimeSpan METADATA_PULL_FREQUENCY = TimeSpan.FromMilliseconds(30000);

        // Injections:
        private readonly User user;
        private readonly ILogger logger;
        private readonly IMethodQ methodQ;
        private readonly ILocalization localization;
        private readonly IAlertService alertService;
        private readonly IConvoService convoService;
        private readonly IUserService userService;
        private readonly IAppSettings appSettings;
        private readonly IUserSettings userSettings;
        private readonly IMessageCryptography crypto;
        private readonly IMessageSender messageSender;
        private readonly IMessageFetcher messageFetcher;
        private readonly IEventAggregator eventAggregator;
        private readonly IViewModelFactory viewModelFactory;
        private readonly IConvoPasswordProvider convoPasswordProvider;

        #endregion

        #region Commands

        public ICommand CopyConvoIdToClipboardCommand { get; }
        
        #endregion

        #region UI Bindings

        private bool canSend;
        public bool CanSend
        {
            get => canSend;
            set => Set(ref canSend, value);
        }

        private string name;
        public string Name
        {
            get => name;
            set
            {
                // Convo expiration check 
                // (adapt the title label accordingly).
                DateTime? exp = ActiveConvo?.ExpirationUTC;
                if (exp.HasValue)
                {
                    if (DateTime.UtcNow > exp.Value)
                    {
                        value += localization["Expired"];
                    }
                    else if ((exp.Value - DateTime.UtcNow).TotalDays < 3)
                    {
                        value += localization["ExpiresSoon"];
                    }
                }

                Set(ref name, value);
            }
        }

        private bool clipboardTickVisible = false;
        public bool ClipboardTickVisible
        {
            get => clipboardTickVisible;
            set => Set(ref clipboardTickVisible, value);
        }

        private ObservableCollection<MessageViewModel> messages = new ObservableCollection<MessageViewModel>();
        public ObservableCollection<MessageViewModel> Messages
        {
            get => messages;
            set => Set(ref messages, value);
        }

        #endregion

        private volatile bool disposed;
        private volatile int pageIndex;
        private volatile CancellationTokenSource autoFetch;
        private volatile CancellationTokenSource metadataUpdater;
        
        private ulong? scheduledHideGreenTickIcon;

        private Convo activeConvo;
        public Convo ActiveConvo
        {
            get => activeConvo;
            set
            {
                activeConvo = value;
                Name = value.Name;
            }
        }

        public ActiveConvoViewModel(User user, IConvoService convoService, IConvoPasswordProvider convoPasswordProvider, IEventAggregator eventAggregator, ILogger logger, IUserService userService, IUserSettings userSettings, IMethodQ methodQ, IViewModelFactory viewModelFactory, IAppSettings appSettings, IMessageCryptography crypto, IMessageSender messageSender, IMessageFetcher messageFetcher)
        {
            localization = DependencyService.Get<ILocalization>();
            alertService = DependencyService.Get<IAlertService>();

            this.user = user;
            this.logger = logger;
            this.crypto = crypto;
            this.methodQ = methodQ;
            this.userService = userService;
            this.convoService = convoService;
            this.appSettings = appSettings;
            this.userSettings = userSettings;
            this.messageSender = messageSender;
            this.messageFetcher = messageFetcher;
            this.eventAggregator = eventAggregator;
            this.viewModelFactory = viewModelFactory;
            this.convoPasswordProvider = convoPasswordProvider;
            
            CopyConvoIdToClipboardCommand = new DelegateCommand(OnClickedCopyConvoIdToClipboard);
        }

        ~ActiveConvoViewModel()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (disposed) 
                return;
            
            disposed = true;
            StopAutomaticPulling();
        }

        public void OnAppearing()
        {
            
        }

        public void OnDisappearing()
        {
            Dispose();
        }

        private void StopAutomaticPulling()
        {
            autoFetch?.Cancel();
            autoFetch = null;

            metadataUpdater?.Cancel();
            metadataUpdater = null;
        }
        
        private void OnClickedCopyConvoIdToClipboard(object commandParam)
        {
            Clipboard.SetTextAsync(ActiveConvo.Id);
            ClipboardTickVisible = true;

            if (scheduledHideGreenTickIcon.HasValue)
            {
                methodQ.Cancel(scheduledHideGreenTickIcon.Value);
            }

            scheduledHideGreenTickIcon = methodQ.Schedule(delegate
            {
                ClipboardTickVisible = false;
                scheduledHideGreenTickIcon = null;
            }, DateTime.UtcNow.AddSeconds(3));
        }
    }
}
