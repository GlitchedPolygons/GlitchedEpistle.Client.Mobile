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
using System.Windows.Input;
using System.Threading.Tasks;

using GlitchedPolygons.ExtensionMethods;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Logging;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Settings;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Web.Users;
using GlitchedPolygons.GlitchedEpistle.Client.Models;
using GlitchedPolygons.GlitchedEpistle.Client.Models.DTOs;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Commands;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.PubSubEvents;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels
{
    public class RegisterViewModel : ViewModel
    {
        #region Injections
        private readonly User user;
        private readonly ILogger logger;
        private readonly IUserSettings userSettings;
        private readonly ILocalization localization;
        private readonly IEventAggregator eventAggregator;
        private readonly IRegistrationService registrationService;
        #endregion

        #region UI Bindings
        private string username = string.Empty;
        public string Username
        {
            get => username;
            set
            {
                Set(ref username, value);
                ValidateForm();
            }
        }

        private string userCreationSecret = string.Empty;
        public string UserCreationSecret
        {
            get => userCreationSecret;
            set
            {
                Set(ref userCreationSecret, value);
                ValidateForm();
            }
        }

        private string password = string.Empty;
        public string Password
        {
            get => password;
            set
            {
                Set(ref password, value);
                ValidateForm();
            }
        }

        private string passwordConfirmation = string.Empty;
        public string PasswordConfirmation
        {
            get => passwordConfirmation;
            set
            {
                Set(ref passwordConfirmation, value);
                ValidateForm();
            }
        }

        private bool formValid;
        public bool FormValid
        {
            get => formValid;
            set => Set(ref formValid, value);
        }

        private bool pendingAttempt;
        public bool PendingAttempt
        {
            get => pendingAttempt;
            set => Set(ref pendingAttempt, value);
        }
        #endregion

        #region Commands
        public ICommand CancelCommand { get; }
        public ICommand RegisterCommand { get; }
        public ICommand EditServerUrlCommand { get; }
        #endregion

        public RegisterViewModel(IEventAggregator eventAggregator, IRegistrationService registrationService, IUserSettings userSettings, ILogger logger, User user)
        {
            this.user = user;
            this.logger = logger;
            this.userSettings = userSettings;
            this.eventAggregator = eventAggregator;
            this.registrationService = registrationService;

            localization = DependencyService.Get<ILocalization>();

            CancelCommand = new DelegateCommand(OnClickedCancel);
            RegisterCommand = new DelegateCommand(OnClickedRegister);
            EditServerUrlCommand = new DelegateCommand(_ =>
            {
                eventAggregator.GetEvent<ClickedConfigureServerUrlButtonEvent>().Publish();
            });
        }

        private void OnClickedCancel(object commandParam)
        {
            eventAggregator?.GetEvent<LogoutEvent>()?.Publish();
        }

        private void OnClickedRegister(object commandParam)
        {
            if (PendingAttempt == true
                || Username.NullOrEmpty()
                || Password.NullOrEmpty()
                || PasswordConfirmation.NullOrEmpty()
                || Password != PasswordConfirmation 
                || Password.Length < 7)
            {
                return;
            }

            FormValid = false;
            PendingAttempt = true;

            Task.Run(async () =>
            {
                Tuple<int, UserCreationResponseDto> result = await registrationService.CreateUser(Password, UserCreationSecret);

                switch (result.Item1)
                {
                    case 0: // Success!
                        user.Id = result.Item2.Id;
                        userSettings.Username = Username;
                        ExecUI(() =>
                        {
                            // Handle this event back in the main view,
                            // since it's there where the backup codes + 2FA secret will be shown.
                            eventAggregator.GetEvent<UserCreationSucceededEvent>().Publish(result.Item2);
                            logger?.LogMessage($"Created user {result.Item2.Id}.");
                        });
                        break;
                    case 1: // Epistle backend connectivity issues
                        var errorMsg = localization["ConnectionToServerFailedErrorMsg"];
                        logger?.LogError(errorMsg);
                        ErrorMessage = errorMsg;
                        break;
                    case 2: // RSA failure
                        errorMsg = localization["KeyGenerationFailed"];
                        logger?.LogError(errorMsg);
                        ErrorMessage = errorMsg;
                        break;
                    case 3: // Server-side failure
                        logger?.LogError(localization["UserCreationFailedServerSide"]);
                        ErrorMessage = localization["UserCreationFailedServerSide"];
                        break;
                    case 4: // Client-side failure
                        errorMsg = localization["UserCreationFailedClientSideButSucceededServerSide"];
                        logger?.LogError(errorMsg);
                        ErrorMessage = errorMsg;
                        break;
                }

                ExecUI(() => 
                {
                    ValidateForm();
                    PendingAttempt = false;
                    Password = PasswordConfirmation = null;
                });
            });
        }

        private void ValidateForm()
        {
            FormValid = Username.NotNullNotEmpty() &&
                        Password.NotNullNotEmpty() &&
                        PasswordConfirmation.NotNullNotEmpty() &&
                        Password == PasswordConfirmation &&
                        Password.Length > 7;
        }
    }
}
