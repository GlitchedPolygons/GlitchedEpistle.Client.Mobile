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
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
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
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Views.Popups;
using GlitchedPolygons.GlitchedEpistle.Client.Models.DTOs;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Web.Convos;
using GlitchedPolygons.Services.CompressionUtility;
using GlitchedPolygons.Services.Cryptography.Asymmetric;
using GlitchedPolygons.Services.MethodQ;
using Prism.Events;
using Newtonsoft.Json;
using Xamarin.Forms;
using Xamarin.Essentials;
using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels
{
    public class ConvoMetadataViewModel : ViewModel, IOnAppearingListener
    {
        #region Constants

        // Injections:
        private readonly User user;
        private readonly IMethodQ methodQ;
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

        public ICommand LeaveCommand { get; }
        public ICommand SubmitCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand CopyConvoIdToClipboardCommand { get; }

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

        private string oldConvoPassword;
        public string OldConvoPassword
        {
            get => oldConvoPassword;
            set => Set(ref oldConvoPassword, value);
        }

        private string newConvoPassword;
        public string NewConvoPassword
        {
            get => newConvoPassword;
            set => Set(ref newConvoPassword, value);
        }

        private string newConvoPasswordConfirmation;
        public string NewConvoPasswordConfirmation
        {
            get => newConvoPasswordConfirmation;
            set => Set(ref newConvoPasswordConfirmation, value);
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

        public string ExpirationLabel => (ExpirationUTC.Date + ExpirationTime).ToString(CultureInfo.CurrentCulture);

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
                    ExpirationUTC = convo.ExpirationUTC.Date;
                    ExpirationTime = convo.ExpirationUTC.TimeOfDay;
                    RefreshParticipantLists();
                }
            }
        }

        public string ConvoId => Convo?.Id;

        private bool convoIdCopiedTickVisible;
        public bool ConvoIdCopiedTickVisible
        {
            get => convoIdCopiedTickVisible;
            set => Set(ref convoIdCopiedTickVisible, value);
        }

        #endregion

        private volatile bool pendingAttempt = false;

        public ConvoMetadataViewModel(User user, IAppSettings appSettings, IEventAggregator eventAggregator, ITotpProvider totpProvider, ICompressionUtilityAsync gzip, IConvoService convoService, IAsymmetricCryptographyRSA crypto, IConvoPasswordProvider convoPasswordProvider, IMethodQ methodQ)
        {
            localization = DependencyService.Get<ILocalization>();
            alertService = DependencyService.Get<IAlertService>();

            this.user = user;
            this.gzip = gzip;
            this.crypto = crypto;
            this.methodQ = methodQ;
            this.appSettings = appSettings;
            this.convoService = convoService;
            this.totpProvider = totpProvider;
            this.eventAggregator = eventAggregator;
            this.convoPasswordProvider = convoPasswordProvider;

            LeaveCommand = new DelegateCommand(OnLeave);
            CancelCommand = new DelegateCommand(OnClickedCancel);
            SubmitCommand = new DelegateCommand(OnClickedSubmit);
            CopyConvoIdToClipboardCommand = new DelegateCommand(_ =>
            {
                Clipboard.SetTextAsync(ConvoId);
                ConvoIdCopiedTickVisible = true;
                methodQ.Schedule(() => ConvoIdCopiedTickVisible = false, DateTime.UtcNow.AddSeconds(2.5));
                ExecUI(() => alertService.AlertLong(localization["Copied"]));
            });
        }

        public void OnAppearing()
        {
            RefreshParticipantLists();
            ShowTotpField = !appSettings["SaveTotpSecret", false];
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

        private async void OnLeave(object commandParam)
        {
            if (pendingAttempt)
            {
                return;
            }
            
            bool confirmed = await Application.Current.MainPage.DisplayAlert(
                title: localization["LeaveConvoDialogTitle"], 
                message: string.Format(localization["LeaveConvoDialogMessage"], Convo.Name), 
                accept: localization["Yes"], 
                cancel: localization["No"]
            );
            
            if (!confirmed)
            {
                return;
            }

            pendingAttempt = true;
            UIEnabled = false;
            
            var _=Task.Run(async()=>
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

                var dto = new ConvoLeaveRequestDto
                {
                    ConvoId = Convo.Id,
                    Totp = Totp
                };

                var body = new EpistleRequestBody
                {
                    UserId = user.Id,
                    Auth = user.Token.Item2,
                    Body = JsonConvert.SerializeObject(dto)
                };

                bool success = await convoService.LeaveConvo(body.Sign(crypto, user.PrivateKeyPem));

                if (!success)
                {
                    UIEnabled = true;
                    pendingAttempt = false;
                    ErrorMessage = localization["RequestFailedServerSide"];
                    return;
                }

                ExecUI(() =>
                {
                    UIEnabled = false;
                    pendingAttempt = true;
                    alertService.AlertLong(localization["LeftConvoSuccessfully"]);
                    eventAggregator.GetEvent<DeletedConvoEvent>().Publish(Convo.Id);
                    OnClickedCancel(null);
                });
            });
        }
        
        private void OnMakeAdmin(object commandParam)
        {
            if (OldConvoPassword.NullOrEmpty())
            {
                PrintMessage("Please authenticate your request by providing the current convo's password (at the top of the form).", true);
                return;
            }

            if (Totp.NullOrEmpty())
            {
                PrintMessage("Please authenticate your request by providing your two-factor authentication token.", true);
                return;
            }

            if (commandParam is string newAdminUserId)
            {
                bool? confirmed = new ConfirmChangeConvoAdminView().ShowDialog();
                
                if (confirmed == true)
                {
                    Task.Run(async() =>
                    {
                        var dto = new ConvoChangeMetadataRequestDto
                        {
                            Totp = Totp,
                            ConvoId = Convo.Id,
                            ConvoPasswordSHA512 = OldConvoPassword.SHA512(),
                            NewConvoPasswordSHA512 = null,
                            Name = null,
                            Description = null,
                            ExpirationUTC = null,
                            CreatorId = newAdminUserId
                        };

                        var body = new EpistleRequestBody
                        {
                            UserId = user.Id,
                            Auth = user.Token.Item2,
                            Body = await gzip.Compress(JsonConvert.SerializeObject(dto))
                        };

                        bool success = await convoService.ChangeConvoMetadata(body.Sign(crypto, user.PrivateKeyPem));
                        if (!success)
                        {
                            PrintMessage("The convo admin change request was rejected server-side! Perhaps double-check the provided convo password?",true);
                            return;
                        }

                        if (convo != null)
                        {
                            convo.CreatorId = newAdminUserId;
                            
                            if (await convoProvider.Update(convo))
                            {
                                PrintMessage($"Success! The user {newAdminUserId} is now the new convo admin. You can now close this window...", false);
                            }
                            else
                            {
                                PrintMessage("The convo admin change request was accepted server-side but couldn't be applied locally. Try re-syncing the convo...", true);
                            }
                        }

                        ExecUI(() =>
                        {
                            UIEnabled = false;
                            eventAggregator.GetEvent<ChangedConvoMetadataEvent>().Publish(Convo.Id);
                        });
                    });
                }
            }
        }

        private void OnKickAndBanUser(object commandParam)
        {
            if (OldConvoPassword.NullOrEmpty())
            {
                PrintMessage("Please authenticate your request by providing the current convo's password (at the top of the form).", true);
                return;
            }

            if (Totp.NullOrEmpty())
            {
                PrintMessage("Please authenticate your request by providing your two-factor authentication token.", true);
                return;
            }

            if (commandParam is string userIdToKick)
            {
                bool? confirmed = new ConfirmKickUserView().ShowDialog();
                
                if (confirmed == true)
                {
                    Task.Run(async () =>
                    {
                        var dto = new ConvoKickUserRequestDto
                        {
                            ConvoId = Convo.Id,
                            ConvoPasswordSHA512 = OldConvoPassword.SHA512(),
                            UserIdToKick = userIdToKick,
                            PermaBan = true,
                            Totp = Totp
                        };

                        var body = new EpistleRequestBody
                        {
                            UserId = user.Id,
                            Auth = user.Token.Item2,
                            Body = await gzip.Compress(JsonConvert.SerializeObject(dto))
                        };

                        bool success = await convoService.KickUser(body.Sign(crypto, user.PrivateKeyPem));
                        if (!success)
                        {
                            PrintMessage($"The user \"{userIdToKick}\" could not be kicked and banned from the convo. Perhaps double-check the provided convo password?", true);
                            return;
                        }

                        PrintMessage($"The user \"{userIdToKick}\" has been kicked out of the convo.", false);

                        if (convo != null)
                        {
                            convo.BannedUsers.Add(userIdToKick);
                            convo.Participants.Remove(userIdToKick);
                        }
                        
                        RefreshParticipantLists();
                        ExecUI(() => eventAggregator.GetEvent<ChangedConvoMetadataEvent>().Publish(Convo.Id));
                    });
                }
            }
        }

        private void OnDelete(object commandParam)
        {
            CanSubmit = false;

            bool? confirmed = dialog.ShowDialog();
            
            if (confirmed == true)
            {
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
                        pendingAttempt = false;
                        UIEnabled = true;
                        return;
                    }
                    
                    var dto = new ConvoDeletionRequestDto
                    {
                        ConvoId = Convo.Id,
                        Totp = Totp
                    };

                    var body = new EpistleRequestBody
                    {
                        UserId = user.Id,
                        Auth = user.Token.Item2,
                        Body = JsonConvert.SerializeObject(dto)
                    };

                    bool success = await convoService.DeleteConvo(body.Sign(crypto, user.PrivateKeyPem));
                    
                    if (!success)
                    {
                        ErrorMessage = localization["RequestFailedServerSide"];
                        pendingAttempt = false;
                        UIEnabled = true;
                        return;
                    }
                    
                    ExecUI(() =>
                    {
                        alertService.AlertLong(localization["DeletedConvoSuccessfully"]);
                        eventAggregator.GetEvent<DeletedConvoEvent>().Publish(Convo.Id);
                        OnClickedCancel(null);
                    });
                });
            }
        }

        private void OnClickedSubmit(object commandParam)
        {
            if (pendingAttempt)
            {
                return;
            }

            if (OldConvoPassword.NullOrEmpty())
            {
                ErrorMessage = localization["ConvoMetadataChangeRequestMissingPassword"];
                return;
            }

            pendingAttempt = true;
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

                var dto = new ConvoChangeMetadataRequestDto
                {
                    Totp = this.Totp,
                    ConvoId = Convo.Id,
                    ConvoPasswordSHA512 = OldConvoPassword.SHA512(),
                };

                if (ExpirationUTC.Date != Convo.ExpirationUTC.Date || ExpirationTime != Convo.ExpirationUTC.TimeOfDay)
                {
                    dto.ExpirationUTC = ExpirationUTC.Date;
                    if (ExpirationTime > TimeSpan.Zero && ExpirationTime < TimeSpan.FromHours(24))
                    {
                        dto.ExpirationUTC += TimeSpan.FromTicks(new DateTime(ExpirationTime.Ticks).ToUniversalTime().Ticks);
                    }
                }

                if (Name.NotNullNotEmpty() && Name != Convo.Name)
                {
                    dto.Name = Name;
                }

                if (Description.NotNullNotEmpty() && Description != Convo.Description)
                {
                    dto.Description = Description;
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
                }

                ExecUI(delegate
                {
                    eventAggregator.GetEvent<ChangedConvoMetadataEvent>().Publish(Convo.Id);
                    alertService.AlertLong(localization["ConvoMetadataChangedSuccessfully"]);
                    OnClickedCancel(null);
                });
            });
        }

        private async void OnClickedCancel(object commandParam)
        {
            UIEnabled = false;
            pendingAttempt = true;
            Participants = Banned = null;
            Name = Description = Totp = null;
            await Application.Current.MainPage.Navigation.PopModalAsync();
        }
    }
}