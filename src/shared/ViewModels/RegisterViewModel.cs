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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels.Interfaces;
using GlitchedPolygons.Services.MethodQ;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels
{
    public class RegisterViewModel : ViewModel, IOnAppearingListener, IOnDisappearingListener
    {
        #region Injections

        private readonly User user;
        private readonly ILogger logger;
        private readonly IMethodQ methodQ;
        private readonly IAppSettings appSettings;
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

        private bool showUserCreationSecretField = true;
        public bool ShowUserCreationSecretField
        {
            get => showUserCreationSecretField;
            set => Set(ref showUserCreationSecretField, value);
        }

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

        #endregion

        #region Commands

        public ICommand CancelCommand { get; }
        public ICommand RegisterCommand { get; }
        public ICommand EditServerUrlCommand { get; }

        #endregion

        private volatile bool initialized = false;

        public RegisterViewModel(IEventAggregator eventAggregator, IRegistrationService registrationService, IUserSettings userSettings, ILogger logger, User user, IAppSettings appSettings, IMethodQ methodQ)
        {
            this.user = user;
            this.logger = logger;
            this.methodQ = methodQ;
            this.appSettings = appSettings;
            this.userSettings = userSettings;
            this.eventAggregator = eventAggregator;
            this.registrationService = registrationService;

            localization = DependencyService.Get<ILocalization>();

            bool onOfficialServer = appSettings.ServerUrl.Contains("epistle.glitchedpolygons.com");

            ShowUserCreationSecretField = !onOfficialServer;

            if (onOfficialServer)
            {
                UserCreationSecret = "Freedom";
            }

            CancelCommand = new DelegateCommand(OnClickedCancel);
            RegisterCommand = new DelegateCommand(OnClickedRegister);
            EditServerUrlCommand = new DelegateCommand(_ => { eventAggregator.GetEvent<ClickedConfigureServerUrlButtonEvent>().Publish(); });
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

                ValidateForm();
                PendingAttempt = false;
                Password = PasswordConfirmation = null;
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