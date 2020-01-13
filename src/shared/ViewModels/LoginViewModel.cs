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

using System;
using System.Linq;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using GlitchedPolygons.ExtensionMethods;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Settings;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Web.Users;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Commands;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.PubSubEvents;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Totp;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Alerts;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels.Interfaces;
using GlitchedPolygons.Services.MethodQ;
using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;
using Prism.Events;
using Xamarin.Forms;
using Xamarin.Essentials;
using OtpNet;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels
{
    public class LoginViewModel : ViewModel, IOnAppearingListener, IOnDisappearingListener
    {
        #region Constants

        // Injections:
        private readonly IMethodQ methodQ;
        private readonly IAppSettings appSettings;
        private readonly ILocalization localization;
        private readonly ILoginService loginService;
        private readonly ITotpProvider totpProvider;
        private readonly IEventAggregator eventAggregator;

        private static readonly AuthenticationRequestConfiguration FINGERPRINT_CONFIG = new AuthenticationRequestConfiguration("Glitched Epistle - Biom. Login") { UseDialog = false };

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

        private volatile int failedAttempts;
        private volatile bool pendingAttempt;
        private volatile bool initialized = false;

        public bool AutoPromptForFingerprint { get; set; } = true;

        public LoginViewModel(IAppSettings appSettings, ILoginService loginService, IEventAggregator eventAggregator, ITotpProvider totpProvider, IMethodQ methodQ)
        {
            localization = DependencyService.Get<ILocalization>();

            this.methodQ = methodQ;
            this.appSettings = appSettings;
            this.loginService = loginService;
            this.totpProvider = totpProvider;
            this.eventAggregator = eventAggregator;

            LoginCommand = new DelegateCommand(OnClickedLogin);

            RegisterCommand = new DelegateCommand(_ => { eventAggregator.GetEvent<ClickedRegisterButtonEvent>().Publish(); });

            EditServerUrlCommand = new DelegateCommand(_ => { eventAggregator.GetEvent<ClickedConfigureServerUrlButtonEvent>().Publish(); });
        }

        public async void OnAppearing()
        {
            UserId = appSettings.LastUserId;

            Languages = new ObservableCollection<Tuple<string, string>>(new List<Tuple<string, string>>
            {
                new Tuple<string, string>("en", localization["English"] + " (English)"),
                new Tuple<string, string>("de", localization["German"] + " (Deutsch)"),
                new Tuple<string, string>("gsw", localization["SwissGerman"] + " (Schwiizerdütsch)"),
                new Tuple<string, string>("it", localization["Italian"] + " (Italiano)")
            });

            Language = Languages.FirstOrDefault(tuple => tuple.Item1 == appSettings["Language", "en"]) ?? Languages[0];

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

            methodQ.Schedule(() => initialized = true, DateTime.UtcNow.AddMilliseconds(420));
        }

        public void OnDisappearing()
        {
            appSettings["Language"] = Language.Item1;
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
                if (appSettings["UseFingerprint", false])
                {
                    if (await CrossFingerprint.Current.IsAvailableAsync())
                    {
                        var fingerprintAuthenticationResult = await CrossFingerprint.Current.AuthenticateAsync(FINGERPRINT_CONFIG);
                        if (!fingerprintAuthenticationResult.Authenticated)
                        {
                            UIEnabled = true;
                            pendingAttempt = false;
                            return;
                        }
                    }
                    else
                    {
                        appSettings["UseFingerprint"] = "false";
                    }
                }

                if (appSettings["SaveTotpSecret", false])
                {
                    Totp = await totpProvider.GetTotp(await SecureStorage.GetAsync("totp:" + UserId));

                    if (Totp.NullOrEmpty())
                    {
                        ShowTotpField = true;
                        appSettings["SaveTotpSecret"] = "false";
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
                        SecureStorage.Remove("pw:" + UserId);
                        ErrorMessage = localization["InvalidUserIdPwOrTOTP"];
                        if (++failedAttempts >= 2)
                        {
                            ExecUI(() => Application.Current.MainPage.DisplayAlert(localization["Error"], localization["InvalidUserIdPwOrTOTP"] + "\n\n" + localization["LoginMultiFailedAttemptsErrorMessage"], "OK"));
                        }

                        break;
                    case 3: // Login failed client-side.
                        failedAttempts++;
                        SecureStorage.Remove("pw:" + UserId);
                        ErrorMessage = localization["LoginFailedClientSide"];
                        break;
                }

                UIEnabled = true;
                pendingAttempt = false;
            });
        }
    }
}
