﻿/*
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

        #endregion

        private volatile int failedAttempts;
        private volatile bool pendingAttempt;

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

            if (Password.NotNullNotEmpty() /*&& appSettings["ReplaceTotpWithFingerprint", false]*/)
            {
                var fingerprintAuthenticationResult = await CrossFingerprint.Current.AuthenticateAsync(new AuthenticationRequestConfiguration("Glitched Epistle - Biom. Login") { UseDialog = false });
                if (fingerprintAuthenticationResult.Authenticated)
                {
                    // TODO: finish this!
                }
            }
        }

        private void OnClickedLogin(object commandParam)
        {
            if (pendingAttempt || UserId.NullOrEmpty() || Password.NullOrEmpty() || Totp.NullOrEmpty())
            {
                return;
            }

            pendingAttempt = true;
            UIEnabled = false;

            Task.Run(async () =>
            {
                int result = await loginService.Login(UserId, Password, Totp);

                switch (result)
                {
                    case 0: // Login succeeded.
                        failedAttempts = 0;
                        if (appSettings["SaveUserPassword", true])
                        {
                            var _ = SecureStorage.SetAsync("pw:" + UserId, Password);
                        }

                        ExecUI(() => eventAggregator.GetEvent<LoginSucceededEvent>().Publish());
                        break;
                    case 1: // Connection to server failed.
                        ExecUI(() =>
                        {
                            pendingAttempt = false;
                            UIEnabled = true;
                            Application.Current.MainPage.DisplayAlert(localization["Error"], localization["ConnectionToServerFailed"], "OK");
                        });
                        break;
                    case 2: // Login failed server-side.
                        failedAttempts++;
                        SecureStorage.Remove("pw:" + UserId);
                        ErrorMessage = "Error! Invalid user id, password or 2FA.";
                        if (failedAttempts > 3)
                        {
                            ErrorMessage += "\nNote that if your credentials are correct but login fails nonetheless, it might be that you're locked out due to too many failed attempts!\nPlease try again in 15 minutes.";
                        }

                        break;
                    case 3: // Login failed client-side.
                        failedAttempts++;
                        SecureStorage.Remove("pw:" + UserId);
                        ErrorMessage = "Unexpected ERROR! Login succeeded server-side, but the returned response couldn't be handled properly (probably key decryption failure).";
                        break;
                }

                ExecUI(() =>
                {
                    pendingAttempt = false;
                    UIEnabled = true;
                });
            });
        }
    }
}