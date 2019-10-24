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

using Xamarin.Forms;

using System;
using System.Windows.Input;
using System.Threading.Tasks;

using Plugin.Fingerprint.Abstractions;
using GlitchedPolygons.ExtensionMethods;
using GlitchedPolygons.GlitchedEpistle.Client.Models;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Commands;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Alerts;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Logging;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Web.Users;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Settings;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels
{
    public class ChangePasswordViewModel : ViewModel
    {
        #region Constants

        // Injections:
        private readonly User user;
        private readonly ILogger logger;
        private readonly IAppSettings appSettings;
        private readonly ILocalization localization;
        private readonly IAlertService alertService;
        private readonly IPasswordChanger passwordChanger;
        
        private static readonly AuthenticationRequestConfiguration FINGERPRINT_CONFIG = new AuthenticationRequestConfiguration("Glitched Epistle - Password Mod") {UseDialog = false};

        #endregion

        #region Commands

        public ICommand SubmitCommand { get; }
        public ICommand CancelCommand { get; }

        #endregion

        #region UI Bindings

        private string currentPassword;
        public string CurrentPassword
        {
            get => currentPassword;
            set => Set(ref currentPassword, value);
        }

        private string newPassword;
        public string NewPassword
        {
            get => newPassword;
            set => Set(ref newPassword, value);
        }

        private string newPasswordConfirmation;
        public string NewPasswordConfirmation
        {
            get => newPasswordConfirmation;
            set => Set(ref newPasswordConfirmation, value);
        }

        private string totp;
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
        
        public ChangePasswordViewModel(User user, ILogger logger, IPasswordChanger passwordChanger, IAppSettings appSettings)
        {
            localization = DependencyService.Get<ILocalization>();
            alertService = DependencyService.Get<IAlertService>();
            
            this.user = user;
            this.logger = logger;
            this.appSettings = appSettings;
            this.passwordChanger = passwordChanger;

            SubmitCommand = new DelegateCommand(OnSubmit);
            CancelCommand = new DelegateCommand(OnCancel);
        }

        private void OnSubmit(object commandParam)
        {
            if (Totp.NullOrEmpty())
            {
                ErrorMessage = localization["NoTotpProvidedErrorMessage"];
                return;
            }
            
            UIEnabled = false;
            
            Task.Run(async () =>
            {
                if (CurrentPassword.NullOrEmpty() || NewPassword.NullOrEmpty() || NewPassword != NewPasswordConfirmation)
                {
                    ErrorMessage = localization["PasswordFieldsInvalid"];
                    UIEnabled = true;
                    return;
                }

                if (NewPassword.Length < 7)
                {
                    ErrorMessage = localization["PasswordTooWeakErrorMessage"];
                    UIEnabled = true;
                    return;
                }

                if (user.PrivateKeyPem.NullOrEmpty())
                {
                    string msg = "The user's in-memory private key seems to be null or empty; can't change passwords without re-encrypting a new copy of the user key!";
                    logger?.LogError(msg);
                    throw new ApplicationException(msg);
                }

                bool success = await passwordChanger.ChangePassword(CurrentPassword, NewPassword, totp);

                if (success)
                {
                    CurrentPassword = NewPassword = NewPasswordConfirmation = Totp = null;
                    alertService.AlertLong(localization["PasswordChangedSuccessfully"]);
                    OnCancel(null);
                }
                else
                {
                    ErrorMessage = localization["PasswordChangeRequestFailedServerSideErrorMessage"];
                    UIEnabled = true;
                }
            });
        }
        
        private async void OnCancel(object commandParam)
        {
            UIEnabled = false;
            CurrentPassword = NewPassword = NewPasswordConfirmation = Totp = null;
            await Application.Current.MainPage.Navigation.PopModalAsync();
        }
    }
}
