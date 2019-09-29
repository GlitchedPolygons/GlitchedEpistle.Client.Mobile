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
using System.Windows.Input;
using System.Threading.Tasks;
using GlitchedPolygons.ExtensionMethods;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Commands;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.PubSubEvents;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Settings;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Web.Users;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels
{
    public class LoginViewModel : ViewModel
    {
        #region Constants
        // Injections:
        private readonly ILoginService loginService;
        private readonly IEventAggregator eventAggregator;
        #endregion

        #region Commands
        public ICommand LoginCommand { get; }
        public ICommand RegisterCommand { get; }
        public ICommand EditServerUrlCommand { get; }
        #endregion

        #region UI Bindings
        private string userId = string.Empty;
        public string UserId { get => userId; set => Set(ref userId, value); }

        private string password = string.Empty;
        public string Password { get => password; set => Set(ref password, value); }

        private string totp = string.Empty;
        public string Totp { get => totp; set => Set(ref totp, value); }

        private bool uiEnabled = true;
        public bool UIEnabled { get => uiEnabled; set => Set(ref uiEnabled, value); }
        #endregion

        private volatile int failedAttempts;
        private volatile bool pendingAttempt;

        public LoginViewModel(IAppSettings settings, ILoginService loginService, IEventAggregator eventAggregator)
        {
            this.loginService = loginService;
            this.eventAggregator = eventAggregator;

            LoginCommand = new DelegateCommand(OnClickedLogin);

            RegisterCommand = new DelegateCommand(_ =>
            {
                eventAggregator.GetEvent<ClickedRegisterButtonEvent>().Publish();
            });

            EditServerUrlCommand = new DelegateCommand(_ =>
            {
                eventAggregator.GetEvent<ClickedConfigureServerUrlButtonEvent>().Publish();
            });

            UserId = settings.LastUserId;
        }

        private void OnClickedLogin(object commandParam)
        {
            if (pendingAttempt || UserId.NullOrEmpty() || Password.NullOrEmpty() || Totp.NullOrEmpty())
            {
                return;
            }

            pendingAttempt = true;
            UIEnabled = false;

            Task.Run(async () =>
            {
                int result = await loginService.Login(UserId, Password, Totp);

                switch (result)
                {
                    case 0: // Login succeeded.
                        failedAttempts = 0;
                        ExecUI(() => eventAggregator.GetEvent<LoginSucceededEvent>().Publish());
                        break;
                    case 1: // Connection to server failed.
                        ExecUI(() =>
                        {
                            pendingAttempt = false;
                            UIEnabled = true;
                            // TODO: notify the user via some dialog that connection to the epistle server failed!
                            //var errorView = new InfoDialogView { DataContext = new InfoDialogViewModel { OkButtonText = "Okay :/", Text = "ERROR: The Glitched Epistle server is unresponsive. It might be under maintenance, please try again later! Sorry.", Title = "Epistle Server Unresponsive" } };
                            //errorView.ShowDialog();
                        });
                        break;
                    case 2: // Login failed server-side.
                        failedAttempts++;
                        ErrorMessage = "Error! Invalid user id, password or 2FA.";
                        if (failedAttempts > 3)
                        {
                            ErrorMessage += "\nNote that if your credentials are correct but login fails nonetheless, it might be that you're locked out due to too many failed attempts!\nPlease try again in 15 minutes.";
                        }
                        break;
                    case 3: // Login failed client-side.
                        failedAttempts++;
                        ErrorMessage = "Unexpected ERROR! Login succeeded server-side, but the returned response couldn't be handled properly (probably key decryption failure).";
                        break;
                }

                ExecUI(() =>
                {
                    pendingAttempt = false;
                    UIEnabled = true;
                });
            });
        }
    }
}
