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

using System.Windows.Input;
using System.Globalization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Prism.Events;
using Xamarin.Forms;
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

        private string language = "en";
        public string Language
        {
            get => language;
            set
            {
                Set(ref language, value);
                localization.SetCurrentCultureInfo(new CultureInfo(value));
            }
        }

        public ObservableCollection<string> Themes { get; }
        public ObservableCollection<string> Languages { get; }

        #endregion

        public SettingsViewModel(IAppSettings appSettings, IEventAggregator eventAggregator, IUserSettings userSettings, User user)
        {
            this.user = user;
            this.appSettings = appSettings;
            this.userSettings = userSettings;
            this.eventAggregator = eventAggregator;
            this.localization = DependencyService.Get<ILocalization>();

            CloseCommand = new DelegateCommand(OnClickedClose);
            RevertCommand = new DelegateCommand(OnClickedRevert);

            Themes = new ObservableCollection<string>(new List<string>
            {
                Constants.Themes.LIGHT_THEME,
                Constants.Themes.DARK_THEME,
                Constants.Themes.OLED_THEME
            });

            Languages = new ObservableCollection<string>(new List<string>
            {
                localization["English"] + " (English)",
                localization["German"] + " (Deutsch)",
                localization["SwissGerman"] + " (Schwiizerdütsch)",
                localization["Italian"] + " (Italiano)"
            });
        }

        public void OnAppearing()
        {
            Username = userSettings.Username;
            //Language = appSettings["Language"];
            Theme = appSettings["Theme", Constants.Themes.DARK_THEME];
        }

        public void OnDisappearing()
        {
            userSettings.Username = Username;
        }

        private async void OnClickedClose(object commandParam)
        {
            await Application.Current.MainPage.Navigation.PopModalAsync();
        }

        private async void OnClickedRevert(object commandParam)
        {
            var view = new BooleanPopupPage(
                title: localization["AreYouSure"],
                description: localization["SettingsAutoSaveReminder"],
                trueButtonLabel: localization["Yes"],
                falseButtonLabel: localization["No"]
            );

            view.Disappearing += (sender, e) =>
            {
                if (view.Result == true)
                {
                    ExecUI(() =>
                    {
                        Username = user?.Id ?? "user";
                        Theme = Constants.Themes.DARK_THEME;
                        Language = localization["English"] + " (English)";
                    });
                }
            };

            await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(view);
        }
    }
}