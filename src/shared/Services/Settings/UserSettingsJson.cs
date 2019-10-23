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

using System;
using System.IO;
using GlitchedPolygons.ExtensionMethods;
using GlitchedPolygons.GlitchedEpistle.Client.Models;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Logging;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Settings;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Views;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Constants;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.PubSubEvents;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Factories;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Views.Popups;
using Prism.Events;
using Xamarin.Forms;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Settings
{
    /// <summary>
    /// Per-user, account-level settings that change for every user account
    /// (when you log out and then in again with another account, these should be reloaded!).
    /// </summary>
    /// <seealso cref="ISettings"/>
    public class UserSettingsJson : SettingsJson, IUserSettings
    {
        private readonly User user;
        private readonly ILogger logger;
        private readonly ILocalization localization;
        private readonly IEventAggregator eventAggregator;
        private readonly IViewModelFactory viewModelFactory;

        public UserSettingsJson(User user, ILogger logger, IEventAggregator eventAggregator, IViewModelFactory viewModelFactory) : base(logger, null)
        {
            this.user = user;
            this.logger = logger;
            this.eventAggregator = eventAggregator;
            this.viewModelFactory = viewModelFactory;
            this.localization = DependencyService.Get<ILocalization>();

            eventAggregator.GetEvent<LoginSucceededEvent>().Subscribe(OnLoginOrRegister);
            eventAggregator.GetEvent<UserCreationSucceededEvent>().Subscribe(_ => OnLoginOrRegister());
        }

        // Reload user-specific settings on login or register (e.g. when switching accounts).
        private async void OnLoginOrRegister()
        {
            if (user is null || user.Id.NullOrEmpty())
            {
                var msg = "A login (or registration) request succeeded but then the session user's ID was null or empty.";
                logger?.LogError(msg);
                throw new ApplicationException(msg);
            }

            FilePath = Path.Combine(Paths.GetUserDirectory(user.Id), "Settings.json");

            Reload();

            if (Username.NullOrEmpty())
            {
                var view = new TextPromptPopupPage(localization["PleaseEnterUsernameDialogTitleLabel"], localization["PleaseEnterUsernameDialogTextLabel"], allowCancel:false);
                view.Disappearing += (sender, e) => Username = view.Text;
                await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(view);
            }

            Username = Username; // Triggers the UsernameChangedEvent and refreshes the main view's username label...
        }

        /// <summary>The username to use for sending messages.</summary>
        public string Username
        {
            get => this["Username"];
            set
            {
                this["Username"] = value;
                Device.BeginInvokeOnMainThread(() => eventAggregator.GetEvent<UsernameChangedEvent>().Publish(value));
            }
        }
    }
}