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
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;
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
    public class ConvosViewModel : ViewModel
    {
        #region Constants

        private readonly User user;
        private readonly ILogger logger;
        private readonly IMethodQ methodQ;
        private readonly IUserService userService;
        private readonly IUserSettings userSettings;
        private readonly IConvoService convoService;
        private readonly ILocalization localization;
        private readonly IConvoPasswordProvider convoPasswordProvider;
        private readonly IEventAggregator eventAggregator;
        private readonly IAsymmetricCryptographyRSA crypto;

        #endregion

        #region Commands

        // Header controls
        public ICommand CreateConvoCommand { get; }
        public ICommand JoinConvoCommand { get; }
        public ICommand ChangePasswordCommand { get; }
        public ICommand SettingsCommand { get; }
        public ICommand LogoutCommand { get; }

        // List controls
        public ICommand OpenConvoCommand { get; }
        public ICommand EditConvoCommand { get; }
        public ICommand CopyConvoIdCommand { get; }

        #endregion

        #region UI Bindings

        private ObservableCollection<Convo> convos;
        public ObservableCollection<Convo> Convos { get => convos; set => Set(ref convos, value); }

        private bool canJoin = true;
        public bool CanJoin { get => canJoin; set => Set(ref canJoin, value); }
        
        private string userId = string.Empty;
        public string UserId { get => userId; set => Set(ref userId, value); }
        
        private string username = string.Empty;
        public string Username { get => username; set => Set(ref username, value); }
        
        #endregion

        public ConvosViewModel(User user, IConvoService convoService, IConvoPasswordProvider convoPasswordProvider, IEventAggregator eventAggregator, IAsymmetricCryptographyRSA crypto, ILogger logger, IUserService userService, IUserSettings userSettings, IMethodQ methodQ)
        {
            localization = DependencyService.Get<ILocalization>();

            this.user = user;
            this.crypto = crypto;
            this.logger = logger;
            this.methodQ = methodQ;
            this.userService = userService;
            this.userSettings = userSettings;
            this.convoService = convoService;
            this.eventAggregator = eventAggregator;
            this.convoPasswordProvider = convoPasswordProvider;

            UpdateList();

            UserId = user.Id;
            Username = userSettings.Username;

            CreateConvoCommand = new DelegateCommand(OnClickedCreateConvo);
            JoinConvoCommand = new DelegateCommand(OnClickedJoinConvo);
            ChangePasswordCommand = new DelegateCommand(OnClickedChangePassword);
            SettingsCommand = new DelegateCommand(OnClickedSettings);
            LogoutCommand = new DelegateCommand(_ => methodQ.Schedule(() => ExecUI(eventAggregator.GetEvent<LogoutEvent>().Publish), DateTime.UtcNow.AddSeconds(0.2)));

            OpenConvoCommand = new DelegateCommand(OnClickedOnConvo);
            EditConvoCommand = new DelegateCommand(OnClickedEditConvo);
            CopyConvoIdCommand = new DelegateCommand(OnClickedCopyConvoIdToClipboard);

            eventAggregator.GetEvent<LoginSucceededEvent>().Subscribe(UpdateList);
            eventAggregator.GetEvent<UpdatedUserConvosEvent>().Subscribe(UpdateList);
            eventAggregator.GetEvent<JoinedConvoEvent>().Subscribe(_ => UpdateList());
            eventAggregator.GetEvent<DeletedConvoEvent>().Subscribe(_ => UpdateList());
            eventAggregator.GetEvent<ChangedConvoMetadataEvent>().Subscribe(_ => UpdateList());
            eventAggregator.GetEvent<ConvoCreationSucceededEvent>().Subscribe(_ => UpdateList());
            eventAggregator.GetEvent<UsernameChangedEvent>().Subscribe(newName => Username = newName);
        }

        private void UpdateList()
        {
            if (user.Token is null || user.Token.Item2.NullOrEmpty())
            {
                return;
            }

            Task.Run(async () =>
            {
                try
                {
                    var userConvos = (await userService.GetConvos(user.Id, user.Token.Item2))
                        .Select(dto => (Convo) dto)
                        .Distinct()
                        .Where(convo => !convo.IsExpired())
                        .OrderByDescending(convo => convo.ExpirationUTC)
                        .ThenBy(convo => convo.Name.ToUpper());

                    ExecUI(() =>
                    {
                        Convos = new ObservableCollection<Convo>(userConvos);
                        eventAggregator.GetEvent<UpdatedUserConvosEvent>().Publish();
                    });
                }
                catch (Exception e)
                {
                    logger?.LogError($"{nameof(ConvosViewModel)}::{nameof(UpdateList)}: User convos sync failed! Thrown exception: " + e.ToString());
                    ExecUI(() => Convos = new ObservableCollection<Convo>());
                }
            });
        }

        private void OnClickedOnConvo(object commandParam)
        {
            var _convo = commandParam as Convo;
            if (_convo is null || !CanJoin)
            {
                return;
            }

            CanJoin = false;
            string cachedPwSHA512 = convoPasswordProvider.GetPasswordSHA512(_convo.Id);

            if (cachedPwSHA512.NotNullNotEmpty())
            {
                JoinConvo(_convo.Id, cachedPwSHA512);
            }
            else
            {
                var view = new PasswordPopupPage();
                view.Disappearing += (sender, e) =>
                {
                    if (view.Password.NotNullNotEmpty())
                    {
                        JoinConvo(_convo.Id, view.Password.SHA512());
                    }
                };
                Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(view);
            }
        }

        private void JoinConvo(string convoId, string pwSHA512)
        {
            Task.Run(async () =>
            {
                var dto = new ConvoJoinRequestDto
                {
                    ConvoId = convoId,
                    ConvoPasswordSHA512 = pwSHA512
                };

                var body = new EpistleRequestBody
                {
                    UserId = user.Id,
                    Auth = user.Token.Item2,
                    Body = JsonConvert.SerializeObject(dto)
                };

                if (!await convoService.JoinConvo(body.Sign(crypto, user.PrivateKeyPem)))
                {
                    convoPasswordProvider.RemovePasswordSHA512(convoId);

                    ExecUI(() =>
                    {
                        CanJoin = true;
                        Application.Current.MainPage.DisplayAlert(localization["Error"], localization["JoinConvoFailed"], "OK");
                    });

                    return;
                }

                ConvoMetadataDto metadata = await convoService.GetConvoMetadata(convoId, pwSHA512, user.Id, user.Token.Item2);
                if (metadata != null)
                {
                    ExecUI(() =>
                    {
                        CanJoin = true;
                        convoPasswordProvider.SetPasswordSHA512(convoId, pwSHA512);
                        eventAggregator.GetEvent<JoinedConvoEvent>().Publish(metadata);
                    });
                }
            });
        }

        private void OnClickedEditConvo(object commandParam)
        {
//            var convo = commandParam as Convo;
//            if (convo is null)
//            {
//                return;
//            }
//
//            var view = windowFactory.Create<EditConvoMetadataView>(true);
//            var viewModel = viewModelFactory.Create<EditConvoMetadataViewModel>();
//            viewModel.Convo = convo;
//            view.DataContext = viewModel;
//
//            view.Show();
//            view.Focus();
        }

        private void OnClickedJoinConvo(object commandParam)
        {
        }

        private void OnClickedCreateConvo(object commandParam)
        {
        }

        private void OnClickedChangePassword(object commandParam)
        {
        }

        private void OnClickedSettings(object commandParam)
        {
        }
        
        private void OnClickedCopyConvoIdToClipboard(object commandParam)
        {
            if (commandParam is Convo convo)
            {
                Clipboard.SetTextAsync(convo.Id);
            }
        }
    }
}