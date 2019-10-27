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
using Xamarin.Essentials;

using System;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel;
using GlitchedPolygons.ExtensionMethods;
using GlitchedPolygons.GlitchedEpistle.Client.Models;
using GlitchedPolygons.GlitchedEpistle.Client.Models.DTOs;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Commands;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.PubSubEvents;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Alerts;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Totp;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels.Interfaces;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Logging;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Settings;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Web.Convos;
using GlitchedPolygons.Services.CompressionUtility;
using GlitchedPolygons.Services.Cryptography.Asymmetric;

using Prism.Events;
using Newtonsoft.Json;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels
{
    public class CreateConvoViewModel : ViewModel, IOnAppearingListener
    {
        #region Constants

        // Injections:
        private readonly User user;
        private readonly ILogger logger;
        private readonly IAppSettings appSettings;
        private readonly IConvoService convoService;
        private readonly ILocalization localization;
        private readonly IAlertService alertService;
        private readonly ITotpProvider totpProvider;
        private readonly ICompressionUtilityAsync gzip;
        private readonly IEventAggregator eventAggregator;
        private readonly IAsymmetricCryptographyRSA crypto;
        private readonly IConvoPasswordProvider convoPasswordProvider;

        #endregion

        #region Commands

        public ICommand CreateCommand { get; }
        public ICommand CancelCommand { get; }

        #endregion

        #region UI Bindings

        private string name;
        public string Name
        {
            get => name;
            set => Set(ref name, value);
        }

        private string desc;
        public string Description
        {
            get => desc;
            set => Set(ref desc, value);
        }

        private DateTime exp = DateTime.UtcNow.AddDays(3);
        public DateTime ExpirationUTC
        {
            get => exp;
            set => Set(ref exp, value);
        }

        public DateTime MinExpirationUTC => DateTime.UtcNow.AddDays(2);

        private TimeSpan expirationTime;
        public TimeSpan ExpirationTime
        {
            get => expirationTime;
            set => Set(ref expirationTime, value);
        }

        private string convoPassword;
        public string ConvoPassword
        {
            get => convoPassword;
            set => Set(ref convoPassword, value);
        }

        private string convoPasswordConfirmation;
        public string ConvoPasswordConfirmation
        {
            get => convoPasswordConfirmation;
            set => Set(ref convoPasswordConfirmation, value);
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

        private bool autoCopyId = false;
        public bool AutoCopyId
        {
            get => autoCopyId;
            set
            {
                Set(ref autoCopyId, value);
                appSettings["AutoCopyConvoIdAfterSuccessfulCreation"] = value.ToString();
            }
        }

        private bool cancelButtonEnabled = true;
        public bool CancelButtonEnabled
        {
            get => cancelButtonEnabled;
            set => Set(ref cancelButtonEnabled, value);
        }

        private bool createButtonEnabled = true;
        public bool CreateButtonEnabled
        {
            get => createButtonEnabled;
            set => Set(ref createButtonEnabled, value);
        }

        #endregion

        public CreateConvoViewModel(User user, ILogger logger, IConvoService convoService, IConvoPasswordProvider convoPasswordProvider, IAsymmetricCryptographyRSA crypto, IEventAggregator eventAggregator, IAppSettings appSettings, ICompressionUtilityAsync gzip, ITotpProvider totpProvider)
        {
            localization = DependencyService.Get<ILocalization>();
            alertService = DependencyService.Get<IAlertService>();

            this.user = user;
            this.gzip = gzip;
            this.logger = logger;
            this.crypto = crypto;
            this.appSettings = appSettings;
            this.totpProvider = totpProvider;
            this.convoService = convoService;
            this.eventAggregator = eventAggregator;
            this.convoPasswordProvider = convoPasswordProvider;

            CancelCommand = new DelegateCommand(OnCancel);
            CreateCommand = new DelegateCommand(OnClickedCreate);
        }

        public void OnAppearing()
        {
            ShowTotpField = !appSettings["SaveTotpSecret", false];
            AutoCopyId = appSettings["AutoCopyConvoIdAfterSuccessfulCreation", false];
        }

        private void OnClickedCreate(object commandParam)
        {
            Task.Run(async () =>
            {
                CreateButtonEnabled = CancelButtonEnabled = false;

                if (Name.NullOrEmpty())
                {
                    ErrorMessage = localization["EmptyConvoTitleFieldErrorMessage"];
                    CreateButtonEnabled = CancelButtonEnabled = true;
                    return;
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
                    CreateButtonEnabled = CancelButtonEnabled = true;
                    return;
                }

                if (ConvoPassword != ConvoPasswordConfirmation)
                {
                    ErrorMessage = localization["PasswordsDontMatchErrorMessage"];
                    CreateButtonEnabled = CancelButtonEnabled = true;
                    return;
                }

                if (ConvoPassword.Length < 5)
                {
                    ErrorMessage = string.Format(localization["PasswordTooWeakErrorMessage"], 5);
                    CreateButtonEnabled = CancelButtonEnabled = true;
                    return;
                }

                string sha512 = ConvoPassword.SHA512();
                DateTime expirationUTC = ExpirationUTC;

                if (ExpirationTime > TimeSpan.Zero && ExpirationTime < TimeSpan.FromHours(24))
                {
                    expirationUTC += TimeSpan.FromTicks(new DateTime(ExpirationTime.Ticks).ToUniversalTime().Ticks);
                }

                var convoCreationDto = new ConvoCreationRequestDto
                {
                    Totp = Totp,
                    Name = Name,
                    Description = Description,
                    ExpirationUTC = expirationUTC,
                    PasswordSHA512 = sha512
                };

                var body = new EpistleRequestBody
                {
                    UserId = user.Id,
                    Auth = user.Token.Item2,
                    Body = await gzip.Compress(JsonConvert.SerializeObject(convoCreationDto))
                };

                string id = await convoService.CreateConvo(body.Sign(crypto, user.PrivateKeyPem));

                if (id.NotNullNotEmpty())
                {
                    // Create the convo model object and feed it into the convo provider.
                    var convo = new Convo
                    {
                        Id = id,
                        Name = Name,
                        CreatorId = user.Id,
                        CreationUTC = DateTime.UtcNow,
                        Description = Description,
                        ExpirationUTC = expirationUTC,
                        Participants = new List<string> {user.Id}
                    };

                    convoPasswordProvider.SetPasswordSHA512(convo.Id, sha512);

                    // Raise the convo created event application-wide
                    // (the main view will subscribe to this to update its list).
                    ExecUI(() =>
                    {
                        if (AutoCopyId)
                        {
                            Clipboard.SetTextAsync(convo.Id);
                            alertService.AlertShort(localization["Copied"]);
                        }

                        // Display success message and keep UI disabled.
                        OnCancel(null);
                        eventAggregator.GetEvent<ConvoCreationSucceededEvent>().Publish(id);
                    });

                    // Record the convo creation into the user log.
                    logger.LogMessage($"Convo \"{Name}\" created successfully under the id {id}.");
                }
                else
                {
                    // If convo creation failed for some reason, the returned
                    // id string is null and the user is notified accordingly.
                    CreateButtonEnabled = CancelButtonEnabled = true;
                    ErrorMessage = string.Format(localization["ConvoCreationFailedErrorMessage"], Name);
                    logger.LogError($"Convo \"{Name}\" couldn't be created. Probably 2FA token (\"{Totp}\") wrong, already obliterated or expired.");
                }
            });
        }

        private async void OnCancel(object commandParam)
        {
            CreateButtonEnabled = CancelButtonEnabled = false;
            Name = Description = ConvoPassword = ConvoPasswordConfirmation = null;
            await Application.Current.MainPage.Navigation.PopModalAsync();
        }
    }
}
