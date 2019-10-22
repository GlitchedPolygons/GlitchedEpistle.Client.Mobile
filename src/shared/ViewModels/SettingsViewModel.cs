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
using Plugin.Fingerprint;
using System;
using System.Windows.Input;
using System.Globalization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GlitchedPolygons.ExtensionMethods;
using GlitchedPolygons.GlitchedEpistle.Client.Models;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Commands;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels.Interfaces;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Settings;

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
        private readonly IAppSettings appSettings;
        private readonly IUserSettings userSettings;
        private readonly ILocalization localization;
        private readonly IEventAggregator eventAggregator;

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
                if(Set(ref saveUserPassword, value))
                    appSettings["SaveUserPassword"] = SaveUserPassword.ToString();
            }
        }

        private bool replaceTotpWithFingerprint = false;
        public bool ReplaceTotpWithFingerprint
        {
            get => replaceTotpWithFingerprint;
            set
            {
                if (value is true)
                {
                    // TODO: check if TOTP secret is in SecureStorage (if not, prompt user) and then verify setting change by asking user for his fingerprint.
                }

                Set(ref replaceTotpWithFingerprint, value);
            }
        }

        public bool FingerprintAvailable => CrossFingerprint.Current.IsAvailableAsync().GetAwaiter().GetResult();

        private Tuple<string, string> theme;
        public Tuple<string, string> Theme
        {
            get => theme;
            set
            {
                Set(ref theme, value);
                (Application.Current as App)?.ChangeTheme(value.Item1);
                appSettings["Theme"] = value.Item1;
            }
        }

        private Tuple<string, string> language;
        public Tuple<string, string> Language
        {
            get => language;
            set
            {
                Set(ref language, value);
                appSettings["Language"] = value.Item1;
                if (initialized) ShowLanguageRestartRequiredWarning = true;
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
        
        private bool initialized = false;

        public SettingsViewModel(IAppSettings appSettings, IEventAggregator eventAggregator, IUserSettings userSettings, User user)
        {
            this.user = user;
            this.appSettings = appSettings;
            this.userSettings = userSettings;
            this.eventAggregator = eventAggregator;
            this.localization = DependencyService.Get<ILocalization>();

            AboutCommand = new DelegateCommand(OnClickedAbout);
            CloseCommand = new DelegateCommand(OnClickedClose);
            RevertCommand = new DelegateCommand(OnClickedRevert);

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
        }

        public void OnAppearing()
        {
            Username = userSettings.Username;
            SaveUserPassword = appSettings["SaveUserPassword", true];
            SaveConvoPasswords = appSettings["SaveConvoPasswords", true];
            ReplaceTotpWithFingerprint = appSettings["ReplaceTotpWithFingerprint", false];
            Language = Languages.FirstOrDefault(tuple => tuple.Item1 == appSettings["Language", "en"]);
            Theme = Themes.FirstOrDefault(tuple => tuple.Item1 == appSettings["Theme", Constants.Themes.DARK_THEME]);

            initialized = true;
        }

        public void OnDisappearing()
        {
            userSettings.Username = Username;
            appSettings["Theme"] = Theme.Item1;
            appSettings["Language"] = Language.Item1;
            appSettings["SaveUserPassword"] = SaveUserPassword.ToString();
            appSettings["SaveConvoPasswords"] = SaveConvoPasswords.ToString();
            appSettings["ReplaceTotpWithFingerprint"] = ReplaceTotpWithFingerprint.ToString();
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
                ReplaceTotpWithFingerprint = false;
                Theme = Themes[1];
                Language = Languages[0];
            }
        }
    }
}