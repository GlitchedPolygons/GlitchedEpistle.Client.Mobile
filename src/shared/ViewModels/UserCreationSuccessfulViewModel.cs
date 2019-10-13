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

using System;
using System.Text;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Collections.Generic;

using GlitchedPolygons.GlitchedEpistle.Client.Models;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Commands;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.PubSubEvents;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Web.Users;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels
{
    public class UserCreationSuccessfulViewModel : ViewModel
    {
        #region Constants
        private readonly User user;
        private readonly IUserService userService;
        private readonly ILocalization localization;
        private readonly IEventAggregator eventAggregator;
        #endregion

        #region Commands
        public ICommand VerifyCommand { get; }
        public ICommand ExportBackupCodesCommand { get; }
        #endregion

        #region UI Bindings
        private string totp = string.Empty;
        public string Totp
        {
            get => totp;
            set => Set(ref totp, value);
        }

        private string secret = string.Empty;
        public string Secret
        {
            get => secret;
            set => Set(ref secret, value);
        }

        private string qr;
        public string QR
        {
            get => qr;
            set => Set(ref qr, value);
        }
        #endregion

        private volatile bool pendingAttempt;

        public List<string> BackupCodes { get; set; }

        public UserCreationSuccessfulViewModel(IUserService userService, IEventAggregator eventAggregator, User user)
        {
            this.user = user;
            this.userService = userService;
            this.eventAggregator = eventAggregator;

            localization = DependencyService.Get<ILocalization>();

            VerifyCommand = new DelegateCommand(OnClickedVerify);
            ExportBackupCodesCommand = new DelegateCommand(OnClickedExport);
        }

        private void OnClickedExport(object commandParam)
        {
            string backup = GetBackupString();
            // TODO: copy or dl?
        }

        private void OnClickedVerify(object commandParam)
        {
            if (pendingAttempt)
            {
                return;
            }

            pendingAttempt = true;

            Task.Run(() => userService.Validate2FA(user.Id, Totp)).ContinueWith(validationTask =>
            {
                if (validationTask.Result == true)
                {
                    ExecUI(() => eventAggregator.GetEvent<UserCreationVerifiedEvent>().Publish());
                }
                else
                {
                    Totp = string.Empty;
                    ErrorMessage = localization["2FA_Failure"];
                }

                pendingAttempt = false;

            }, TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        private string GetBackupString()
        {
            var sb = new StringBuilder(512);

            sb.AppendLine("Glitched Epistle 2FA Backup Codes\n");
            sb.AppendLine($"User: {user.Id}");
            sb.AppendLine($"Export timestamp: {DateTime.UtcNow:s} (UTC)\n");

            for (var i = 0; i < BackupCodes.Count; i++)
            {
                sb.AppendLine(BackupCodes[i]);
            }

            return sb.ToString();
        }
    }
}
