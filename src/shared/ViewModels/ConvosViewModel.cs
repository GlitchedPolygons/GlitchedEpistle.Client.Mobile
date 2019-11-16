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
using GlitchedPolygons.GlitchedEpistle.Client.Services.Logging;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Settings;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Web.Convos;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Web.Users;
using GlitchedPolygons.Services.MethodQ;
using Prism.Events;
using Newtonsoft.Json;
using Xamarin.Forms;
using Xamarin.Essentials;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels
{
    public class ConvosViewModel : ViewModel, IOnAppearingListener
    {
        #region Constants

        private readonly User user;
        private readonly ILogger logger;
        private readonly IMethodQ methodQ;
        private readonly IUserService userService;
        private readonly IAppSettings appSettings;
        private readonly IAlertService alertService;
        private readonly IUserSettings userSettings;
        private readonly IConvoService convoService;
        private readonly ILocalization localization;
        private readonly IViewModelFactory viewModelFactory;
        private readonly IConvoPasswordProvider convoPasswordProvider;
        private readonly IEventAggregator eventAggregator;
        private readonly IAsymmetricCryptographyRSA crypto;

        #endregion

        #region Commands

        // Header controls
        public ICommand CopyUserIdToClipboardCommand { get; }
        public ICommand CreateConvoCommand { get; }
        public ICommand JoinConvoCommand { get; }
        public ICommand ChangePasswordCommand { get; }
        public ICommand SettingsCommand { get; }
        public ICommand LogoutCommand { get; }

        // List controls
        public ICommand RefreshCommand { get; }
        public ICommand OpenConvoCommand { get; }
        public ICommand EditConvoCommand { get; }
        public ICommand CopyConvoIdCommand { get; }

        #endregion

        #region UI Bindings

        private ObservableCollection<Convo> convos;
        public ObservableCollection<Convo> Convos { get => convos; set => Set(ref convos, value); }

        private bool isRefreshing;
        public bool IsRefreshing { get => isRefreshing; set => Set(ref isRefreshing, value); }
        
        private string userId = string.Empty;
        public string UserId { get => userId; set => Set(ref userId, value); }
        
        private string username = string.Empty;
        public string Username { get => username; set => Set(ref username, value); }

        private bool headerButtonsEnabled = true;
        public bool HeaderButtonsEnabled { get => headerButtonsEnabled; set => Set(ref headerButtonsEnabled, value); }
        
        private bool userIdCopiedTickVisible = false;
        public bool UserIdCopiedTickVisible { get => userIdCopiedTickVisible; set => Set(ref userIdCopiedTickVisible, value); }
        
        private bool joining = false;
        public bool Joining { get => joining; set => Set(ref joining, value); }
        
        #endregion

        private DateTime lastRefresh;

        public ConvosViewModel(User user, IConvoService convoService, IConvoPasswordProvider convoPasswordProvider, IEventAggregator eventAggregator, IAsymmetricCryptographyRSA crypto, ILogger logger, IUserService userService, IUserSettings userSettings, IMethodQ methodQ, IViewModelFactory viewModelFactory, IAppSettings appSettings)
        {
            localization = DependencyService.Get<ILocalization>();
            alertService = DependencyService.Get<IAlertService>();
            
            this.user = user;
            this.crypto = crypto;
            this.logger = logger;
            this.methodQ = methodQ;
            this.userService = userService;
            this.appSettings = appSettings;
            this.userSettings = userSettings;
            this.convoService = convoService;
            this.eventAggregator = eventAggregator;
            this.viewModelFactory = viewModelFactory;
            this.convoPasswordProvider = convoPasswordProvider;

            CreateConvoCommand = new DelegateCommand(OnClickedCreateConvo);
            JoinConvoCommand = new DelegateCommand(OnClickedJoinConvo);
            ChangePasswordCommand = new DelegateCommand(OnClickedChangePassword);
            SettingsCommand = new DelegateCommand(OnClickedSettings);
            LogoutCommand = new DelegateCommand(_ =>
            {
                if (HeaderButtonsEnabled)
                {
                    HeaderButtonsEnabled = false;
                    methodQ.Schedule(() => ExecUI(eventAggregator.GetEvent<LogoutEvent>().Publish), DateTime.UtcNow.AddSeconds(0.2));
                }
            });
            
            CopyUserIdToClipboardCommand = new DelegateCommand(_ =>
            {
                Clipboard.SetTextAsync(UserId);
                UserIdCopiedTickVisible = true;
                methodQ.Schedule(() => UserIdCopiedTickVisible = false, DateTime.UtcNow.AddSeconds(2.5));
                ExecUI(() => alertService.AlertLong(localization["Copied"]));
            });

            OpenConvoCommand = new DelegateCommand(OnClickedOnConvo);
            EditConvoCommand = new DelegateCommand(OnClickedEditConvo);
            CopyConvoIdCommand = new DelegateCommand(OnClickedCopyConvoIdToClipboard);
            RefreshCommand = new DelegateCommand(_ => UpdateList(true));

            eventAggregator.GetEvent<JoinedConvoEvent>().Subscribe(_ => UpdateList(true));
            eventAggregator.GetEvent<DeletedConvoEvent>().Subscribe(_ => UpdateList(true));
            eventAggregator.GetEvent<ChangedConvoMetadataEvent>().Subscribe(_ => UpdateList(true));
            eventAggregator.GetEvent<ConvoCreationSucceededEvent>().Subscribe(_ => UpdateList(true));
            eventAggregator.GetEvent<UsernameChangedEvent>().Subscribe(newName => Username = newName);
        }
        
        public void OnAppearing()
        {
            UpdateList(false);

            UserId = user.Id;
            Username = userSettings.Username;
        }

        private void UpdateList(bool forceRefresh = false)
        {
            if (!forceRefresh && DateTime.UtcNow - lastRefresh < TimeSpan.FromMinutes(3))
            {
                return;
            }
            
            if (user.Token is null || user.Token.Item2.NullOrEmpty())
            {
                IsRefreshing = false;
                return;
            }

            Task.Run(async () =>
            {
                IsRefreshing = true;
                
                try
                {
                    var userConvos = (await userService.GetConvos(user.Id, user.Token.Item2))
                        .Select(dto => (Convo) dto)
                        .Distinct()
                        .Where(convo => !convo.IsExpired())
                        .OrderByDescending(convo => convo.ExpirationUTC)
                        .ThenBy(convo => convo.Name.ToUpper());

                    lastRefresh = DateTime.UtcNow;

                    Convos = new ObservableCollection<Convo>(userConvos);
                    ExecUI(eventAggregator.GetEvent<UpdatedUserConvosEvent>().Publish);
                }
                catch (Exception e)
                {
                    logger?.LogError($"{nameof(ConvosViewModel)}::{nameof(UpdateList)}: User convos sync failed! Thrown exception: " + e.ToString());
                }
                
                IsRefreshing = false;
            });
        }

        public void OnClickedOnConvo(object commandParam)
        {
            var _convo = commandParam as Convo;
            if (_convo is null || Joining)
            {
                return;
            }

            Joining = true;

            Task.Run(async () =>
            {
                bool useSavedPw = appSettings["SaveConvoPasswords", true];
                
                string cachedPwSHA512 = convoPasswordProvider.GetPasswordSHA512(_convo.Id);
            
                if (cachedPwSHA512.NullOrEmpty() && useSavedPw)
                {
                    cachedPwSHA512 = await SecureStorage.GetAsync($"convo:{_convo.Id}_pw:SHA512");
                }

                if (cachedPwSHA512.NullOrEmpty())
                {
                    var viewModel = viewModelFactory.Create<JoinConvoViewModel>();
                    viewModel.ConvoId = _convo.Id;

                    var view = new JoinConvoPage {BindingContext = viewModel};
                    view.Disappearing += (_, __) => Joining = false;

                    ExecUI(async () => await Application.Current.MainPage.Navigation.PushModalAsync(view));
                    
                    return;
                }

                var dto = new ConvoJoinRequestDto
                {
                    ConvoId = _convo.Id,
                    ConvoPasswordSHA512 = cachedPwSHA512
                };

                var body = new EpistleRequestBody
                {
                    UserId = user.Id,
                    Auth = user.Token.Item2,
                    Body = JsonConvert.SerializeObject(dto)
                };

                bool joined = await convoService.JoinConvo(body.Sign(crypto, user.PrivateKeyPem));

                if (!joined)
                {
                    convoPasswordProvider.RemovePasswordSHA512(_convo.Id);
                    SecureStorage.Remove($"convo:{_convo.Id}_pw:SHA512");
                    ExecUI(() => Application.Current.MainPage.DisplayAlert(localization["Error"], localization["JoinConvoFailedErrorMessage"], "OK"));

                    Joining = false;
                    return;
                }

                convoPasswordProvider.SetPasswordSHA512(_convo.Id, cachedPwSHA512);

                ConvoMetadataDto metadata = await convoService.GetConvoMetadata(_convo.Id, cachedPwSHA512, user.Id, user.Token.Item2);
                if (metadata != null)
                {
                    if (appSettings["SaveConvoPasswords", true])
                    {
                        await SecureStorage.SetAsync($"convo:{_convo.Id}_pw:SHA512", cachedPwSHA512);
                    }

                    ExecUI(delegate
                    {
                        eventAggregator.GetEvent<JoinedConvoEvent>().Publish(metadata);
                        Joining = false;
                    });
                }
                else
                {
                    Joining = false;
                    logger.LogWarning($"Joined convo {_convo.Id} successfully using the cached convo password's SHA512 (starts with {cachedPwSHA512.Substring(0, 6)}) but failed to pull its metadata from the server...");
                }
            });
        }
        
        private async void OnClickedEditConvo(object commandParam)
        {
            var convo = commandParam as Convo;
            if (convo is null)
            {
                return;
            }

            var view = new ConvoMetadataPage();
            var viewModel = viewModelFactory.Create<ConvoMetadataViewModel>();
            viewModel.Convo = convo;
            view.BindingContext = viewModel;

            await Application.Current.MainPage.Navigation.PushModalAsync(view);
        }

        private async void OnClickedJoinConvo(object commandParam)
        {
            if (!HeaderButtonsEnabled)
            {
                return;
            }
            
            var view = new JoinConvoPage { BindingContext = viewModelFactory.Create<JoinConvoViewModel>() };
            await Application.Current.MainPage.Navigation.PushModalAsync(view);
        }

        private async void OnClickedCreateConvo(object commandParam)
        {
            if (!HeaderButtonsEnabled)
            {
                return;
            }
            
            var view = new CreateConvoPage { BindingContext = viewModelFactory.Create<CreateConvoViewModel>() };
            await Application.Current.MainPage.Navigation.PushModalAsync(view);
        }

        private async void OnClickedChangePassword(object commandParam)
        {
            if (!HeaderButtonsEnabled)
            {
                return;
            }

            var view = new ChangePasswordPage { BindingContext = viewModelFactory.Create<ChangePasswordViewModel>() };
            await Application.Current.MainPage.Navigation.PushModalAsync(view);
        }

        private async void OnClickedSettings(object commandParam)
        {
            if (!HeaderButtonsEnabled)
            {
                return;
            }

            var view = new SettingsPage { BindingContext = viewModelFactory.Create<SettingsViewModel>() };
            await Application.Current.MainPage.Navigation.PushModalAsync(view);
        }

        private void OnClickedCopyConvoIdToClipboard(object commandParam)
        {
            if (commandParam is Convo convo)
            {
                Clipboard.SetTextAsync(convo.Id);
                DependencyService.Get<IAlertService>()?.AlertShort(localization["Copied"]);
            }
        }
    }
}