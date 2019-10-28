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
using System.Globalization;
using System.Threading.Tasks;
using GlitchedPolygons.ExtensionMethods;
using GlitchedPolygons.GlitchedEpistle.Client.Models;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Settings;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Web.Users;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Commands;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.PubSubEvents;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Alerts;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Totp;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels.Interfaces;
using GlitchedPolygons.GlitchedEpistle.Client.Models.DTOs;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Web.Convos;
using GlitchedPolygons.Services.CompressionUtility;
using GlitchedPolygons.Services.Cryptography.Asymmetric;
using Newtonsoft.Json;
using Plugin.Fingerprint;
using Prism.Events;
using Xamarin.Forms;
using Plugin.Fingerprint.Abstractions;
using Xamarin.Essentials;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels
{
    public class ConvoMetadataViewModel : ViewModel, IOnAppearingListener
    {
        #region Constants

        // Injections:
        private readonly User user;
        private readonly IAppSettings appSettings;
        private readonly ILocalization localization;
        private readonly IConvoService convoService;
        private readonly ITotpProvider totpProvider;
        private readonly IAlertService alertService;
        private readonly ICompressionUtilityAsync gzip;
        private readonly IEventAggregator eventAggregator;
        private readonly IAsymmetricCryptographyRSA crypto;
        private readonly IConvoPasswordProvider convoPasswordProvider;

        private static readonly AuthenticationRequestConfiguration FINGERPRINT_CONFIG = new AuthenticationRequestConfiguration("Glitched Epistle - Metadata Mod.") {UseDialog = false};

        #endregion

        #region Commands

        public ICommand SubmitCommand { get; }
        public ICommand CancelCommand { get; }

        #endregion

        #region UI Bindings

        public string UserId => user?.Id;

        private string name;
        public string Name { get => name; set => Set(ref name, value); }

        private string totp;
        public string Totp { get => totp; set => Set(ref totp, value); }

        private bool showTotpField = true;
        public bool ShowTotpField { get => showTotpField; set => Set(ref showTotpField, value); }

        private string description;
        public string Description { get => description; set => Set(ref description, value); }
        
        private string oldConvoPassword;
        public string OldConvoPassword { get => oldConvoPassword; set => Set(ref oldConvoPassword, value); }

        private string newConvoPassword;
        public string NewConvoPassword { get => newConvoPassword; set => Set(ref newConvoPassword, value); }

        private string newConvoPasswordConfirmation;
        public string NewConvoPasswordConfirmation { get => newConvoPasswordConfirmation; set => Set(ref newConvoPasswordConfirmation, value); }

        private DateTime minExpirationUTC = DateTime.UtcNow.AddDays(2);
        public DateTime MinExpirationUTC { get => minExpirationUTC; set => Set(ref minExpirationUTC, value); }

        private DateTime expirationUTC = DateTime.UtcNow.AddDays(14);
        public DateTime ExpirationUTC { get => expirationUTC; set => Set(ref expirationUTC, value); }

        private TimeSpan expirationTime;
        public TimeSpan ExpirationTime { get => expirationTime; set => Set(ref expirationTime, value); }

        public string ExpirationLabel => (ExpirationUTC.Date + ExpirationTime).ToString(CultureInfo.CurrentCulture);

        private bool uiEnabled = true;
        public bool UIEnabled { get => uiEnabled; set => Set(ref uiEnabled, value); }

        public bool IsAdmin
        {
            get
            {
                bool? isAdmin = Convo?.CreatorId.Equals(user?.Id);
                return isAdmin ?? false;
            }
        }

        public bool CanLeave => !IsAdmin;

        private ObservableCollection<string> participants = new ObservableCollection<string>();
        public ObservableCollection<string> Participants { get => participants; set => Set(ref participants, value); }

        public bool OtherParticipantsListVisibility => Participants.Count > 0;

        private ObservableCollection<string> banned = new ObservableCollection<string>();
        public ObservableCollection<string> Banned { get => banned; set => Set(ref banned, value); }

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
                    ExpirationTime = convo.ExpirationUTC.TimeOfDay;
                    RefreshParticipantLists();
                }
            }
        }

        #endregion

        private volatile bool pendingAttempt;

        public ConvoMetadataViewModel(User user, IAppSettings appSettings, IEventAggregator eventAggregator, ITotpProvider totpProvider, ICompressionUtilityAsync gzip, IConvoService convoService, IAsymmetricCryptographyRSA crypto, IConvoPasswordProvider convoPasswordProvider)
        {
            localization = DependencyService.Get<ILocalization>();
            alertService = DependencyService.Get<IAlertService>();
            
            this.user = user;
            this.gzip = gzip;
            this.crypto = crypto;
            this.appSettings = appSettings;
            this.convoService = convoService;
            this.totpProvider = totpProvider;
            this.eventAggregator = eventAggregator;
            this.convoPasswordProvider = convoPasswordProvider;

            CancelCommand = new DelegateCommand(OnClickedCancel);
            SubmitCommand = new DelegateCommand(OnClickedSubmit);
        }

        public void OnAppearing()
        {
            RefreshParticipantLists();
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
            if (pendingAttempt)
            {
                return;
            }

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
                    UIEnabled = true;
                    pendingAttempt = false;
                    return;
                }

                if (NewConvoPassword.NotNullNotEmpty() || NewConvoPasswordConfirmation.NotNullNotEmpty())
                {
                    if (NewConvoPassword != NewConvoPasswordConfirmation)
                    {
                        ErrorMessage = localization["PasswordsDontMatchErrorMessage"];
                        UIEnabled = true;
                        pendingAttempt = false;
                        return;
                    }

                    if (NewConvoPassword.Length < 5)
                    {
                        ErrorMessage = string.Format(localization["PasswordTooWeakErrorMessage"], 5);
                        UIEnabled = true;
                        pendingAttempt = false;
                        return;
                    }
                }

                DateTime expirationUTC = ExpirationUTC;

                if (ExpirationTime > TimeSpan.Zero && ExpirationTime < TimeSpan.FromHours(24))
                {
                    expirationUTC += TimeSpan.FromTicks(new DateTime(ExpirationTime.Ticks).ToUniversalTime().Ticks);
                }

                var dto = new ConvoChangeMetadataRequestDto
                {
                    Totp = Totp,
                    ConvoId = Convo.Id,
                    ExpirationUTC = expirationUTC,
                    ConvoPasswordSHA512 = OldConvoPassword.SHA512(),
                };

                if (Name.NotNullNotEmpty() && Name != Convo.Name)
                {
                    dto.Name = Name;
                }

                if (Description.NotNullNotEmpty() && Description != Convo.Description)
                {
                    dto.Description = Description;
                }

                if (ExpirationUTC != Convo.ExpirationUTC)
                {
                    dto.ExpirationUTC = ExpirationUTC;
                }

                if (NewConvoPassword.NotNullNotEmpty())
                {
                    dto.NewConvoPasswordSHA512 = NewConvoPassword.SHA512();
                }

                var body = new EpistleRequestBody
                {
                    UserId = user.Id,
                    Auth = user.Token.Item2,
                    Body = await gzip.Compress(JsonConvert.SerializeObject(dto))
                };

                bool successful = await convoService.ChangeConvoMetadata(body.Sign(crypto, user.PrivateKeyPem));

                if (!successful)
                {
                    ErrorMessage = localization["ConvoMetadataChangeRequestRejected"];
                    UIEnabled = true;
                    pendingAttempt = false;
                    return;
                }

                alertService.AlertLong(localization["ConvoMetadataChangedSuccessfully"]);

                var convo = await convoProvider.Get(Convo.Id);
                if (convo != null)
                {
                    if (dto.Name.NotNullNotEmpty())
                    {
                        convo.Name = dto.Name;
                    }

                    if (dto.Description.NotNullNotEmpty())
                    {
                        convo.Description = dto.Description;
                    }

                    if (dto.ExpirationUTC.HasValue)
                    {
                        convo.ExpirationUTC = dto.ExpirationUTC.Value;
                    }

                    if (dto.NewConvoPasswordSHA512.NotNullNotEmpty())
                    {
                        convoPasswordProvider.SetPasswordSHA512(convo.Id, dto.NewConvoPasswordSHA512);
                    }

                    await convoProvider.Update(convo);
                }

                UIEnabled = false;
                ExecUI(() => eventAggregator.GetEvent<ChangedConvoMetadataEvent>().Publish(Convo.Id));
            });
        }

        private async void OnClickedCancel(object commandParam)
        {
            UIEnabled = false;
            Participants = Banned = null;
            Name = Description = Totp = null;
            await Application.Current.MainPage.Navigation.PopModalAsync();
        }
    }
}