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
using Xamarin.Essentials;

using System;
using System.Linq;
using System.Windows.Input;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using GlitchedPolygons.ExtensionMethods;
using GlitchedPolygons.Services.MethodQ;
using GlitchedPolygons.GlitchedEpistle.Client.Models;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Commands;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Alerts;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels.Interfaces;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Permissions;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Views.Popups;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Settings;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Web.Users;
using OtpNet;
using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;
using Plugin.Permissions.Abstractions;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels
{
    /// <summary>
    /// View-Model for the Settings page.
    /// </summary>
    public class SettingsViewModel : ViewModel, IOnAppearingListener, IOnDisappearingListener
    {
        #region Constants

        // Injections:
        private readonly User user;
        private readonly IMethodQ methodQ;
        private readonly IAppSettings appSettings;
        private readonly IUserService userService;
        private readonly IUserSettings userSettings;
        private readonly ILocalization localization;
        private readonly IAlertService alertService;
        private readonly IEventAggregator eventAggregator;
        private readonly IPermissionChecker permissionChecker;

        private static readonly AuthenticationRequestConfiguration FINGERPRINT_CONFIG = new AuthenticationRequestConfiguration("Glitched Epistle - Config") {UseDialog = false};
        #endregion

        #region Commands

        public ICommand AboutCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand RevertCommand { get; }

        #endregion

        #region UI Bindings

        private string username = string.Empty;
        public string Username
        {
            get => username;
            set
            {
                if (Set(ref username, value))
                    userSettings.Username = Username;
            }
        }

        private bool saveConvoPasswords = true;
        public bool SaveConvoPasswords
        {
            get => saveConvoPasswords;
            set
            {
                if (Set(ref saveConvoPasswords, value))
                    appSettings["SaveConvoPasswords"] = value.ToString();
            }
        }

        private bool saveUserPassword = true;
        public bool SaveUserPassword
        {
            get => saveUserPassword;
            set
            {
                if (Set(ref saveUserPassword, value))
                    appSettings["SaveUserPassword"] = value.ToString();
            }
        }
        
        private bool saveTotpSecret = true;
        public bool SaveTotpSecret
        {
            get => saveTotpSecret;
            set
            {
                // When the setter is not invoked by user but by the settings page
                // during construction, set the initial state without asking questions.
                if (!initialized)
                {
                    Set(ref saveTotpSecret, value, force: true);
                    return;
                }
                
                // Disabling this option should always be possible.
                if (value == false)
                {
                    Set(ref saveTotpSecret, false, force: true);
                    appSettings["SaveTotpSecret"] = "false";
                    return;
                }

                // Enabling requires the device to already have the user's TOTP secret in its SecureStorage.
                // If this is the case, fine, enable this option... but if not, prompt the user to enter his 2FA secret.
                string totpSecret = SecureStorage.GetAsync("totp:" + user.Id).GetAwaiter().GetResult();
                if (totpSecret.NotNullNotEmpty())
                {
                    Set(ref saveTotpSecret, true, force: true);
                    appSettings["SaveTotpSecret"] = "true";
                    return;
                }

                var view = new TextPromptPopupPage(
                    title: localization["PleaseEnterTotpSecretDialogTitleLabel"],
                    description: localization["PleaseEnterTotpSecretDialogTextLabel"],
                    allowCancel: true, 
                    allowNullOrEmptyString: true
                );
                
                view.Disappearing += async (sender, e) =>
                {
                    string input = view.Text;
                    if (input.NullOrEmpty())
                    {
                        alertService.AlertShort(localization["Cancelled"]);
                        SaveTotpSecret = false;
                        return;
                    }        
                    
                    var totp = new Totp(Base32Encoding.ToBytes(input));
                    if (totp.RemainingSeconds() < 2)
                    {
                        await Task.Delay(1250);
                    }
                            
                    bool success = await userService.Validate2FA(user.Id, totp.ComputeTotp());
                    if (!success)
                    {
                        alertService.AlertShort(localization["TwoFactorAuthTokenVerificationFailed"]);
                        SaveTotpSecret = false;
                        return;
                    }
                            
                    alertService.AlertShort(localization["ActivationSuccessful"]);
                    
                    Set(ref saveTotpSecret, true, force: true);
                    appSettings["SaveTotpSecret"] = "true";
                    await SecureStorage.SetAsync("totp:" + user.Id, input);
                };
                
                Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(view);
            }
        }

        private bool useFingerprint = false;
        public bool UseFingerprint
        {
            get => useFingerprint;
            set
            {
                if (!initialized)
                {
                    Set(ref useFingerprint, value, force: true);
                    appSettings["UseFingerprint"] = value.ToString();    
                    return;
                }

                Task.Run(async () =>
                {
                    bool prev = useFingerprint;
                    
                    FingerprintAuthenticationResult auth = await CrossFingerprint.Current.AuthenticateAsync(FINGERPRINT_CONFIG);

                    switch (auth.Status)
                    {
                        case FingerprintAuthenticationResultStatus.Succeeded:
                            Set(ref useFingerprint, value, force: true);
                            appSettings["UseFingerprint"] = value.ToString();
                            return;
                        case FingerprintAuthenticationResultStatus.NotAvailable:
                            alertService.AlertShort(localization["FingerprintNotAvailable"]);
                            Set(ref useFingerprint, false, force: true);
                            appSettings["UseFingerprint"] = "false";
                            return;
                        default:
                            alertService.AlertShort(localization["Cancelled"]);
                            Set(ref useFingerprint, prev, force: true);
                            appSettings["UseFingerprint"] = prev.ToString();
                            return;
                    }
                });
            }
        }

        private Tuple<string, string> theme;
        public Tuple<string, string> Theme
        {
            get => theme;
            set
            {
                if (Set(ref theme, value))
                    appSettings["Theme"] = value.Item1;
                
                (Application.Current as App)?.ChangeTheme(value.Item1);
            }
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

        private bool showLanguageRestartRequiredWarning = false;
        public bool ShowLanguageRestartRequiredWarning
        {
            get => showLanguageRestartRequiredWarning;
            set => Set(ref showLanguageRestartRequiredWarning, value);
        }

        private ObservableCollection<Tuple<string, string>> themes;
        public ObservableCollection<Tuple<string, string>> Themes
        {
            get => themes;
            set => Set(ref themes, value);
        }

        private ObservableCollection<Tuple<string, string>> languages;
        public ObservableCollection<Tuple<string, string>> Languages
        {
            get => languages;
            set => Set(ref languages, value);
        }

        #endregion
        
        private volatile bool initialized = false;

        public SettingsViewModel(User user, IAppSettings appSettings, IEventAggregator eventAggregator, IUserSettings userSettings, IMethodQ methodQ, IUserService userService)
        {
            this.user = user;
            this.methodQ = methodQ;
            this.userService = userService;
            this.appSettings = appSettings;
            this.userSettings = userSettings;
            this.eventAggregator = eventAggregator;
            this.alertService = DependencyService.Get<IAlertService>();
            this.localization = DependencyService.Get<ILocalization>();
            this.permissionChecker = DependencyService.Get<IPermissionChecker>();

            AboutCommand = new DelegateCommand(OnClickedAbout);
            CloseCommand = new DelegateCommand(OnClickedClose);
            RevertCommand = new DelegateCommand(OnClickedRevert);
        }

        public void OnAppearing()
        {
            Themes = new ObservableCollection<Tuple<string, string>>(new List<Tuple<string, string>>
            {
                new Tuple<string, string>(Constants.Themes.LIGHT_THEME, localization["LightTheme"]),
                new Tuple<string, string>(Constants.Themes.DARK_THEME, localization["DarkTheme"]),
                new Tuple<string, string>(Constants.Themes.OLED_THEME, localization["OLEDTheme"]),
            });

            Languages = new ObservableCollection<Tuple<string, string>>(new List<Tuple<string, string>>
            {
                new Tuple<string, string>("en", localization["English"] + " (English)"),
                new Tuple<string, string>("de", localization["German"] + " (Deutsch)"),
                new Tuple<string, string>("gsw", localization["SwissGerman"] + " (Schwiizerdütsch)"),
                new Tuple<string, string>("it", localization["Italian"] + " (Italiano)")
            });
            
            Username = userSettings.Username;
            SaveUserPassword = appSettings["SaveUserPassword", true];
            SaveTotpSecret = appSettings["SaveTotpSecret", false];
            SaveConvoPasswords = appSettings["SaveConvoPasswords", true];
            UseFingerprint = appSettings["UseFingerprint", false];
            Language = Languages.FirstOrDefault(tuple => tuple.Item1 == appSettings["Language", "en"]) ?? Languages[0];
            Theme = Themes.FirstOrDefault(tuple => tuple.Item1 == appSettings["Theme", Constants.Themes.DARK_THEME]);

            methodQ.Schedule(() => initialized = true, DateTime.UtcNow.AddMilliseconds(420));
        }

        public void OnDisappearing()
        {
            userSettings.Username = Username;
        }

        private async void OnClickedClose(object commandParam)
        {
            await Application.Current.MainPage.Navigation.PopModalAsync();
        }

        private void OnClickedAbout(object obj)
        {
            Application.Current.MainPage.DisplayAlert(
                title: localization["AboutButton"],
                message: string.Format(localization["AboutText"], App.Version),
                cancel: localization["Dismiss"]
            );
        }

        private async void OnClickedRevert(object commandParam)
        {
            bool confirmed = await Application.Current.MainPage.DisplayAlert(
                title: localization["AreYouSure"],
                message: localization["SettingsAutoSaveReminder"],
                cancel: localization["No"],
                accept: localization["Yes"]
            );

            if (confirmed)
            {
                Username = user?.Id ?? "user";
                SaveUserPassword = true;
                SaveConvoPasswords = true;
                UseFingerprint = false;
                Theme = Themes[1];
                Language = Languages[0];
            }
        }
    }
}