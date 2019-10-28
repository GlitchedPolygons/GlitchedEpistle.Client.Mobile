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
using System.Windows.Input;
using System.Collections.ObjectModel;

using GlitchedPolygons.ExtensionMethods;
using GlitchedPolygons.GlitchedEpistle.Client.Models;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Settings;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Web.Users;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Commands;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Totp;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels.Interfaces;

using Prism.Events;
using Xamarin.Forms;
using Plugin.Fingerprint.Abstractions;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels
{
    public class EditConvoMetadataViewModel : ViewModel, IOnAppearingListener
    {
        #region Constants

        // Injections:
        private readonly User user;
        private readonly IAppSettings appSettings;
        private readonly ILocalization localization;
        private readonly ILoginService loginService;
        private readonly ITotpProvider totpProvider;
        private readonly IEventAggregator eventAggregator;

        private static readonly AuthenticationRequestConfiguration FINGERPRINT_CONFIG = new AuthenticationRequestConfiguration("Glitched Epistle - Metadata Mod.") {UseDialog = false};

        #endregion

        #region Commands

        public ICommand SubmitCommand { get; }
        public ICommand CancelCommand { get; }

        #endregion

        #region UI Bindings

        public string UserId => user?.Id;

        private string name;
        public string Name
        {
            get => name;
            set => Set(ref name, value);
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

        private string description;
        public string Description
        {
            get => description;
            set => Set(ref description, value);
        }

        private DateTime minExpirationUTC = DateTime.UtcNow.AddDays(2);
        public DateTime MinExpirationUTC
        {
            get => minExpirationUTC;
            set => Set(ref minExpirationUTC, value);
        }

        private DateTime expirationUTC = DateTime.UtcNow.AddDays(14);
        public DateTime ExpirationUTC
        {
            get => expirationUTC;
            set => Set(ref expirationUTC, value);
        }

        private TimeSpan expirationTime;
        public TimeSpan ExpirationTime
        {
            get => expirationTime;
            set => Set(ref expirationTime, value);
        }

        private bool uiEnabled = true;
        public bool UIEnabled
        {
            get => uiEnabled;
            set => Set(ref uiEnabled, value);
        }

        public bool IsAdmin
        {
            get
            {
                bool? isAdmin = Convo?.CreatorId.Equals(user?.Id);
                return isAdmin ?? false;
            }
        }

        private bool canSubmit = true;
        public bool CanSubmit
        {
            get => canSubmit;
            set => Set(ref canSubmit, value);
        }

        public bool CanLeave => !IsAdmin;

        private ObservableCollection<string> participants = new ObservableCollection<string>();
        public ObservableCollection<string> Participants
        {
            get => participants;
            set => Set(ref participants, value);
        }

        public bool OtherParticipantsListVisibility => Participants.Count > 0;

        private ObservableCollection<string> banned = new ObservableCollection<string>();
        public ObservableCollection<string> Banned
        {
            get => banned;
            set => Set(ref banned, value);
        }

        public bool BannedListVisibility => Banned.Count > 0;

        private Convo convo;
        public Convo Convo
        {
            get => convo;
            set
            {
                convo = value;
                if (convo != null)
                {
                    Name = convo.Name;
                    Description = convo.Description;
                    ExpirationUTC = convo.ExpirationUTC;
                    RefreshParticipantLists();
                }
            }
        }

        #endregion

        private volatile int failedAttempts;
        private volatile bool pendingAttempt;

        public bool AutoPromptForFingerprint { get; set; } = true;

        public EditConvoMetadataViewModel(IAppSettings appSettings, ILoginService loginService, IEventAggregator eventAggregator, ITotpProvider totpProvider, User user)
        {
            localization = DependencyService.Get<ILocalization>();

            this.appSettings = appSettings;
            this.loginService = loginService;
            this.totpProvider = totpProvider;
            this.user = user;
            this.eventAggregator = eventAggregator;

            CancelCommand = new DelegateCommand(OnClickedCancel);
            SubmitCommand = new DelegateCommand(OnClickedSubmit);
        }

        public void OnAppearing()
        {
            
        }

        private void RefreshParticipantLists()
        {
            Participants = new ObservableCollection<string>();
            foreach (string userId in convo.Participants)
            {
                if (userId.Equals(this.user.Id))
                    continue;

                Participants.Add(userId);
            }

            Banned = new ObservableCollection<string>();
            foreach (string bannedUserId in convo.BannedUsers)
            {
                if (bannedUserId.NullOrEmpty())
                    continue;

                Banned.Add(bannedUserId);
            }
        }

        private void OnClickedSubmit(object commandParam)
        {
            // TODO: handle metadata submission
        }

        private async void OnClickedCancel(object commandParam)
        {
            UIEnabled = false;
            Name = Description = null;
            await Application.Current.MainPage.Navigation.PopModalAsync();
        }
    }
}
