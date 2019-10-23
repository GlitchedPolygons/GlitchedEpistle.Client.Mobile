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
using System.Windows.Input;
using System.Threading.Tasks;
using GlitchedPolygons.ExtensionMethods;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Settings;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Web.Users;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Commands;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.PubSubEvents;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels.Interfaces;
using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;
using Xamarin.Forms;
using Xamarin.Essentials;
using OtpNet;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels
{
    public class LoginViewModel : ViewModel, IOnAppearingListener
    {
        #region Constants

        // Injections:
        private readonly IAppSettings appSettings;
        private readonly ILocalization localization;
        private readonly ILoginService loginService;
        private readonly IEventAggregator eventAggregator;

        private static readonly AuthenticationRequestConfiguration FINGERPRINT_CONFIG = new AuthenticationRequestConfiguration("Glitched Epistle - Biom. Login") {UseDialog = false};

        #endregion

        #region Commands

        public ICommand LoginCommand { get; }
        public ICommand RegisterCommand { get; }
        public ICommand EditServerUrlCommand { get; }

        #endregion

        #region UI Bindings

        private string userId = string.Empty;
        public string UserId
        {
            get => userId;
            set => Set(ref userId, value);
        }

        private string password = string.Empty;
        public string Password
        {
            get => password;
            set => Set(ref password, value);
        }

        private string totp = string.Empty;
        public string Totp
        {
            get => totp;
            set => Set(ref totp, value);
        }

        private bool uiEnabled = true;
        public bool UIEnabled
        {
            get => uiEnabled;
            set => Set(ref uiEnabled, value);
        }
        
        private bool showTotpField = true;
        public bool ShowTotpField
        {
            get => showTotpField;
            set => Set(ref showTotpField, value);
        }

        #endregion

        private volatile int failedAttempts;
        private volatile bool pendingAttempt;
        
        public bool AutoPromptForFingerprint { get; set; } = true;

        public LoginViewModel(IAppSettings appSettings, ILoginService loginService, IEventAggregator eventAggregator)
        {
            localization = DependencyService.Get<ILocalization>();

            this.appSettings = appSettings;
            this.loginService = loginService;
            this.eventAggregator = eventAggregator;

            LoginCommand = new DelegateCommand(OnClickedLogin);

            RegisterCommand = new DelegateCommand(_ => { eventAggregator.GetEvent<ClickedRegisterButtonEvent>().Publish(); });

            EditServerUrlCommand = new DelegateCommand(_ => { eventAggregator.GetEvent<ClickedConfigureServerUrlButtonEvent>().Publish(); });
        }

        public async void OnAppearing()
        {
            UserId = appSettings.LastUserId;

            if (appSettings["SaveUserPassword", true])
            {
                var storedPw = await SecureStorage.GetAsync("pw:" + UserId);
                Password = storedPw.NotNullNotEmpty() ? storedPw : null;
            }

            if (!await CrossFingerprint.Current.IsAvailableAsync())
            {
                appSettings["UseFingerprint"] = "false";
            }

            bool saveTotpSecret = appSettings["SaveTotpSecret", false];
            
            ShowTotpField = !saveTotpSecret;

            if (AutoPromptForFingerprint && saveTotpSecret && appSettings["UseFingerprint", false])
            {
                OnClickedLogin(null);
            }
        }

        private void OnClickedLogin(object commandParam)
        {
            if (pendingAttempt || UserId.NullOrEmpty() || Password.NullOrEmpty())
            {
                return;
            }

            pendingAttempt = true;
            UIEnabled = false;
            
            Task.Run(async () =>
            {
                if (appSettings["SaveTotpSecret", false])
                {
                    string totpSecret = await SecureStorage.GetAsync("totp:" + UserId);
                    
                    if (totpSecret.NotNullNotEmpty())
                    {
                        Totp = new Totp(Base32Encoding.ToBytes(totpSecret)).ComputeTotp();
                    }
                    else
                    {
                        appSettings["SaveTotpSecret"] = "false";
                    }
                }
                
                if (appSettings["UseFingerprint", false])
                {
                    if (!await CrossFingerprint.Current.IsAvailableAsync())
                    {
                        appSettings["UseFingerprint"] = "false";
                    }
                    
                    var fingerprintAuthenticationResult = await CrossFingerprint.Current.AuthenticateAsync(FINGERPRINT_CONFIG);
                    if (!fingerprintAuthenticationResult.Authenticated)
                    {
                        goto end;
                    }
                }
                
                int result = await loginService.Login(UserId, Password, Totp);

                switch (result)
                {
                    case 0: // Login succeeded.
                        failedAttempts = 0;
                        if (appSettings["SaveUserPassword", true])
                        {
                            var saveUserPwTask = SecureStorage.SetAsync("pw:" + UserId, Password);
                        }
                        ExecUI(() => eventAggregator.GetEvent<LoginSucceededEvent>().Publish());
                        break;
                    case 1: // Connection to server failed.
                        pendingAttempt = false;
                        UIEnabled = true;
                        var serverSideFailureAlertTask = Application.Current.MainPage.DisplayAlert(localization["Error"], localization["ConnectionToServerFailed"], "OK");
                        break;
                    case 2: // Login failed server-side.
                        failedAttempts++;
                        SecureStorage.Remove("pw:" + UserId);
                        ErrorMessage = localization["InvalidUserIdPwOrTOTP"];
                        if (failedAttempts > 3)
                        {
                            ErrorMessage += "\n" + localization["LoginMultiFailedAttemptsErrorMessage"];
                        }
                        break;
                    case 3: // Login failed client-side.
                        failedAttempts++;
                        SecureStorage.Remove("pw:" + UserId);
                        ErrorMessage = localization["LoginFailedClientSide"];
                        break;
                }

                end:
                UIEnabled = true;
                pendingAttempt = false;
            });
        }
    }
}
