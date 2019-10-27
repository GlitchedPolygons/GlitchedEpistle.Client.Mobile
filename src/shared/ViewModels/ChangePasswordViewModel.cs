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
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Totp;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels.Interfaces;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Settings;
using OtpNet;
using Xamarin.Essentials;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels
{
    public class ChangePasswordViewModel : ViewModel, IOnAppearingListener
    {
        #region Constants

        // Injections:
        private readonly User user;
        private readonly ILogger logger;
        private readonly IAppSettings appSettings;
        private readonly ILocalization localization;
        private readonly IAlertService alertService;
        private readonly ITotpProvider totpProvider;
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
            set
            {
                Set(ref currentPassword, value);
                UpdateSubmitButton();
            }
        }

        private string newPassword;
        public string NewPassword
        {
            get => newPassword;
            set
            {
                Set(ref newPassword, value);
                UpdateSubmitButton();
            }
        }

        private string newPasswordConfirmation;
        public string NewPasswordConfirmation
        {
            get => newPasswordConfirmation;
            set
            {
                Set(ref newPasswordConfirmation, value);
                UpdateSubmitButton();
            }
        }

        private string totp;
        public string Totp
        {
            get => totp;
            set => Set(ref totp, value);
        }

        private bool showTotpField = true;
        public bool ShowTotpField
        {
            get => showTotpField;
            set => Set(ref showTotpField, value);
        }
        
        private bool cancelButtonEnabled = true;
        public bool CancelButtonEnabled
        {
            get => cancelButtonEnabled;
            set => Set(ref cancelButtonEnabled, value);
        }

        private bool submitButtonEnabled = false;
        public bool SubmitButtonEnabled
        {
            get => submitButtonEnabled;
            set => Set(ref submitButtonEnabled, value);
        }

        #endregion
        
        public ChangePasswordViewModel(User user, ILogger logger, IPasswordChanger passwordChanger, IAppSettings appSettings, ITotpProvider totpProvider)
        {
            localization = DependencyService.Get<ILocalization>();
            alertService = DependencyService.Get<IAlertService>();
            
            this.user = user;
            this.logger = logger;
            this.appSettings = appSettings;
            this.totpProvider = totpProvider;
            this.passwordChanger = passwordChanger;

            SubmitCommand = new DelegateCommand(OnSubmit);
            CancelCommand = new DelegateCommand(OnCancel);
        }
        
        public void OnAppearing()
        {
            ShowTotpField = !appSettings["SaveTotpSecret", false];
            UpdateSubmitButton();
        }
        
        private void UpdateSubmitButton()
        {
            SubmitButtonEnabled = CurrentPassword.NotNullNotEmpty() && NewPassword.NotNullNotEmpty();
        }

        private void OnSubmit(object commandParam)
        {
            SubmitButtonEnabled = CancelButtonEnabled = false;
            
            Task.Run(async () =>
            {
                if (appSettings["SaveTotpSecret", false] && await SecureStorage.GetAsync("totp:" + user.Id) is string totpSecret)
                {
                    Totp = await totpProvider.GetTotp(totpSecret);
                    
                    if (Totp.NullOrEmpty())
                    {
                        ShowTotpField = true;
                        appSettings["SaveTotpSecret"] = "false";
                    }
                }

                if (Totp.NullOrEmpty())
                {
                    ErrorMessage = localization["NoTotpProvidedErrorMessage"];
                    SubmitButtonEnabled = CancelButtonEnabled = true;
                    return;
                }
                
                if (CurrentPassword.NullOrEmpty() || NewPassword.NullOrEmpty() || NewPassword != NewPasswordConfirmation)
                {
                    ErrorMessage = localization["PasswordFieldsInvalid"];
                    SubmitButtonEnabled = CancelButtonEnabled = true;
                    return;
                }

                if (NewPassword.Length < 7)
                {
                    ErrorMessage = string.Format(localization["PasswordTooWeakErrorMessage"], 7);
                    SubmitButtonEnabled = CancelButtonEnabled = true;
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
                    if (appSettings["SaveUserPassword", true])
                    {
                        await SecureStorage.SetAsync("pw:" + user.Id, NewPassword);
                    }
                    
                    CurrentPassword = NewPassword = NewPasswordConfirmation = Totp = null;
                    alertService.AlertLong(localization["PasswordChangedSuccessfully"]);
                    
                    OnCancel(null);
                }
                else
                {
                    ErrorMessage = localization["PasswordChangeRequestFailedServerSideErrorMessage"];
                    SubmitButtonEnabled = CancelButtonEnabled = true;
                }
            });
        }
        
        private async void OnCancel(object commandParam)
        {
            SubmitButtonEnabled = CancelButtonEnabled = false;
            CurrentPassword = NewPassword = NewPasswordConfirmation = Totp = null;
            await Application.Current.MainPage.Navigation.PopModalAsync();
        }
    }
}
