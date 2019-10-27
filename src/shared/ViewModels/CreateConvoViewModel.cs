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

using GlitchedPolygons.ExtensionMethods;
using GlitchedPolygons.GlitchedEpistle.Client.Models;
using GlitchedPolygons.GlitchedEpistle.Client.Models.DTOs;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Commands;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.PubSubEvents;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels.Interfaces;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Logging;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Settings;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Web.Convos;
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
        private readonly IEventAggregator eventAggregator;
        private readonly IAsymmetricCryptographyRSA crypto;
        private readonly IConvoPasswordProvider convoPasswordProvider;
        
        #endregion

        #region Commands

        public ICommand CreateCommand { get; }
        public ICommand CancelCommand { get; }

        #endregion

        #region UI Bindings

        private string title;
        public string Title
        {
            get => title;
            set
            {
                Set(ref title, value);
                UpdateCreateButton();
            }
        }
        
        private string desc;
        public string Description
        {
            get => desc;
            set => Set(ref desc, value);
        }

        private DateTime exp;
        public DateTime ExpirationUTC
        {
            get => exp;
            set => Set(ref exp, value);
        }
        
        public DateTime MinExpirationUTC => DateTime.UtcNow.AddDays(2);

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

        private bool createButtonEnabled = false;
        public bool CreateButtonEnabled
        {
            get => createButtonEnabled;
            set => Set(ref createButtonEnabled, value);
        }

        #endregion
        
        public CreateConvoViewModel(User user, ILogger logger, IConvoService convoService, IConvoPasswordProvider convoPasswordProvider, IAsymmetricCryptographyRSA crypto, IEventAggregator eventAggregator, IAppSettings appSettings)
        {
            localization = DependencyService.Get<ILocalization>();
            
            this.user = user;
            this.logger = logger;
            this.crypto = crypto;
            this.appSettings = appSettings;
            this.convoService = convoService;
            this.eventAggregator = eventAggregator;
            this.convoPasswordProvider = convoPasswordProvider;

            CancelCommand = new DelegateCommand(OnCancel);
            CreateCommand = new DelegateCommand(OnClickedCreate);
        }
        
        public void OnAppearing()
        {
            UpdateCreateButton();
            ShowTotpField = !appSettings["SaveTotpSecret", false];
            AutoCopyId = appSettings["AutoCopyConvoIdAfterSuccessfulCreation", false];
        }
        
        private void UpdateCreateButton()
        {
            CreateButtonEnabled = Title.NotNullNotEmpty() && ConvoPassword.NotNullNotEmpty();
        }

        private void OnClickedCreate(object commandParam)
        {
            if (Title.NullOrEmpty())
            {
                ErrorMessage = localization["EmptyConvoTitleFieldErrorMessage"];
                return;
            }
            
            // TODO: impl
        }
        
        private async void OnCancel(object commandParam)
        {
            CreateButtonEnabled = CancelButtonEnabled = false;
            Title = Description = ConvoPassword = ConvoPasswordConfirmation = null;
            await Application.Current.MainPage.Navigation.PopModalAsync();
        }
    }
}
