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
using GlitchedPolygons.GlitchedEpistle.Client.Services.Web.Users;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels
{
    public class LoginViewModel : ViewModel
    {
        #region Constants
        // Injections:
        private readonly ILoginService loginService;
        //private readonly IEventAggregator eventAggregator;
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

        public LoginViewModel(ILoginService loginService)
        {
            this.loginService = loginService;
        }
    }
}
