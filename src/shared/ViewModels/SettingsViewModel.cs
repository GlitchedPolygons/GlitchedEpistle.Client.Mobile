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
using System.Windows.Input;
using System.Globalization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using GlitchedPolygons.GlitchedEpistle.Client.Models;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Commands;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Views.Popups;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels.Interfaces;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Settings;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels
{
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
            set => Set(ref username, value);
        }

        private bool saveConvoPasswords = true;
        public bool SaveConvoPasswords
        {
            get => saveConvoPasswords;
            set => Set(ref saveConvoPasswords, value);
        }

        private string theme = Constants.Themes.DARK_THEME;
        public string Theme
        {
            get => theme;
            set
            {
                Set(ref theme, value);

                var app = Application.Current as App;
                if (app is null)
                {
                    return;
                }

                if (app.ChangeTheme(value))
                {
                    appSettings["Theme"] = value;
                }
            }
        }

        private string language;
        public string Language
        {
            get => language;
            set
            {
                Set(ref language, value);
                localization.SetCurrentCultureInfo(LanguageLabelToCultureInfo(value));
                UpdateLocalizedLabels();
            }
        }

        private ObservableCollection<string> themes;
        public ObservableCollection<string> Themes
        {
            get => themes;
            set => Set(ref themes, value);
        }

        private ObservableCollection<string> languages;
        public ObservableCollection<string> Languages
        {
            get => languages;
            set => Set(ref languages, value);
        }

        #endregion

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

            UpdateLocalizedLabels();
        }

        public void OnAppearing()
        {
            Username = userSettings.Username;
            //Language = appSettings["Language"];
            Theme = appSettings["Theme", Constants.Themes.DARK_THEME];
            SaveConvoPasswords = appSettings["SaveConvoPasswords", "true"]?.ToLowerInvariant() == "true";
        }

        public void OnDisappearing()
        {
            userSettings.Username = Username;
            appSettings["SaveConvoPasswords"] = SaveConvoPasswords.ToString();
        }
        
        private void UpdateLocalizedLabels()
        {
            Themes = new ObservableCollection<string>(new List<string>
            {
                localization["LightTheme"],
                localization["DarkTheme"],
                localization["OLEDTheme"],
            });

            Languages = new ObservableCollection<string>(new List<string>
            {
                localization["English"] + " (English)",
                localization["German"] + " (Deutsch)",
                localization["SwissGerman"] + " (Schwiizerdütsch)",
                localization["Italian"] + " (Italiano)"
            });
        }

        private CultureInfo LanguageLabelToCultureInfo(string language)
        {
            language = this.language.ToLowerInvariant();
            
            if (language.Contains("deutsch"))
            {
                return new CultureInfo("de");
            }
            if (language.Contains("schwiizerdütsch"))
            {
                return new CultureInfo("gsw");
            }
            if (language.Contains("italiano"))
            {
                return new CultureInfo("it");
            }
            
            return new CultureInfo("en");
        }

        private async void OnClickedClose(object commandParam)
        {
            await Application.Current.MainPage.Navigation.PopModalAsync();
        }

        private void OnClickedAbout(object obj)
        {
            Application.Current.MainPage.DisplayAlert(localization["AboutButton"], localization["AboutText"], localization["Dismiss"]);
        }

        private async void OnClickedRevert(object commandParam)
        {
            if (await Application.Current.MainPage.DisplayAlert(localization["AreYouSure"], localization["SettingsAutoSaveReminder"], localization["Yes"], localization["No"]))
            {
                Username = user?.Id ?? "user";
                SaveConvoPasswords = true;
                Theme = Constants.Themes.DARK_THEME;
                Language = localization["English"] + " (English)";
            }
        }
    }
}
