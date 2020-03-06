/*
    Glitched Epistle - Mobile Client
    Copyright (C) 2020 Raphael Beck

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

using System.Text.Json;
using Xamarin.Forms;
using System.Windows.Input;
using System.Threading.Tasks;
using GlitchedPolygons.ExtensionMethods;
using GlitchedPolygons.GlitchedEpistle.Client.Models;
using GlitchedPolygons.GlitchedEpistle.Client.Models.DTOs;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Commands;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.PubSubEvents;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels.Interfaces;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Logging;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Settings;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Web.Convos;
using GlitchedPolygons.Services.Cryptography.Asymmetric;
using Prism.Events;
using Xamarin.Essentials;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels
{
    public class JoinConvoViewModel : ViewModel, IOnAppearingListener
    {
        #region Constants

        // Injections:
        private readonly User user;
        private readonly ILogger logger;
        private readonly IAppSettings appSettings;
        private readonly IConvoService convoService;
        private readonly ILocalization localization;
        private readonly IEventAggregator eventAggregator;
        private readonly IAsymmetricCryptographyRSA crypto;
        private readonly IConvoPasswordProvider convoPasswordProvider;

        #endregion

        #region Commands

        public ICommand JoinCommand { get; }
        public ICommand CancelCommand { get; }

        #endregion

        #region UI Bindings

        private string convoId;
        public string ConvoId
        {
            get => convoId;
            set
            {
                Set(ref convoId, value);
                UpdateJoinButton();
            }
        }

        private string convoPassword;
        public string ConvoPassword
        {
            get => convoPassword;
            set
            {
                Set(ref convoPassword, value);
                UpdateJoinButton();
            }
        }

        private bool cancelButtonEnabled = true;
        public bool CancelButtonEnabled
        {
            get => cancelButtonEnabled;
            set => Set(ref cancelButtonEnabled, value);
        }

        private bool joinButtonEnabled = false;
        public bool JoinButtonEnabled
        {
            get => joinButtonEnabled;
            set => Set(ref joinButtonEnabled, value);
        }

        #endregion

        public JoinConvoViewModel(User user, ILogger logger, IAppSettings appSettings, IConvoService convoService, IConvoPasswordProvider convoPasswordProvider, IAsymmetricCryptographyRSA crypto, IEventAggregator eventAggregator)
        {
            localization = DependencyService.Get<ILocalization>();

            this.user = user;
            this.logger = logger;
            this.crypto = crypto;
            this.appSettings = appSettings;
            this.convoService = convoService;
            this.eventAggregator = eventAggregator;
            this.convoPasswordProvider = convoPasswordProvider;

            CancelCommand = new DelegateCommand(OnCancel);
            JoinCommand = new DelegateCommand(OnClickedJoin);
        }

        public void OnAppearing()
        {
            UpdateJoinButton();
        }

        private void UpdateJoinButton()
        {
            JoinButtonEnabled = ConvoId.NotNullNotEmpty() && ConvoPassword.NotNullNotEmpty();
        }

        private void OnClickedJoin(object commandParam)
        {
            if (ConvoId.NullOrEmpty())
            {
                ErrorMessage = localization["EmptyConvoIdFieldErrorMessage"];
                return;
            }

            string passwordSHA512 = ConvoPassword?.SHA512();

            if (passwordSHA512.NullOrEmpty())
            {
                ErrorMessage = localization["EmptyPasswordFieldErrorMessage"];
                return;
            }

            JoinButtonEnabled = CancelButtonEnabled = false;

            Task.Run(async () =>
            {
                var dto = new ConvoJoinRequestDto
                {
                    ConvoId = ConvoId,
                    ConvoPasswordSHA512 = passwordSHA512
                };

                var body = new EpistleRequestBody
                {
                    UserId = user.Id,
                    Auth = user.Token.Item2,
                    Body = JsonSerializer.Serialize(dto)
                };

                if (!await convoService.JoinConvo(body.Sign(crypto, user.PrivateKeyPem)))
                {
                    convoPasswordProvider.RemovePasswordSHA512(ConvoId);
                    ErrorMessage = localization["JoinConvoFailedErrorMessage"];
                    CancelButtonEnabled = true;
                    UpdateJoinButton();
                    return;
                }

                ConvoMetadataDto metadata = await convoService.GetConvoMetadata(ConvoId, passwordSHA512, user.Id, user.Token.Item2);

                if (metadata is null)
                {
                    convoPasswordProvider.RemovePasswordSHA512(ConvoId);
                    ErrorMessage = localization["JoinConvoFailedErrorMessage"];
                    CancelButtonEnabled = true;
                    UpdateJoinButton();
                    return;
                }

                convoPasswordProvider.SetPasswordSHA512(ConvoId, passwordSHA512);

                if (appSettings["SaveConvoPasswords", true])
                {
                    await SecureStorage.SetAsync($"convo:{ConvoId}_pw:SHA512", passwordSHA512);
                }

                ExecUI(() =>
                {
                    OnCancel(null);
                    eventAggregator.GetEvent<JoinedConvoEvent>().Publish(metadata);
                });
            });
        }

        private async void OnCancel(object commandParam)
        {
            JoinButtonEnabled = CancelButtonEnabled = false;
            ConvoId = ConvoPassword = null;
            await Application.Current.MainPage.Navigation.PopModalAsync();
        }
    }
}
