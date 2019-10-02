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
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Commands;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.PubSubEvents;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels
{
    public class RegisterViewModel : ViewModel
    {
        #region Injections
        private readonly IEventAggregator eventAggregator;
        #endregion

        #region UI Bindings
        private string username = string.Empty;
        public string Username
        {
            get => username;
            set => Set(ref username, value);
        }

        private string userCreationSecret = string.Empty;
        public string UserCreationSecret
        {
            get => userCreationSecret;
            set => Set(ref userCreationSecret, value);
        }

        private string password = string.Empty;
        public string Password
        {
            get => password;
            set => Set(ref password, value);
        }

        private string passwordConfirmation = string.Empty;
        public string PasswordConfirmation
        {
            get => passwordConfirmation;
            set => Set(ref passwordConfirmation, value);
        }

        private bool generatingKeys;
        public bool GeneratingKeys
        {
            get => generatingKeys;
            set => Set(ref generatingKeys, value);
        }
        #endregion

        #region Commands
        public ICommand CancelCommand { get; }
        public ICommand RegisterCommand { get; }
        public ICommand EditServerUrlCommand { get; }
        #endregion

        public RegisterViewModel(IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;

            CancelCommand = new DelegateCommand(OnClickedCancel);
            RegisterCommand = new DelegateCommand(OnClickedRegister);
            EditServerUrlCommand = new DelegateCommand(_ =>
            {
                eventAggregator.GetEvent<ClickedConfigureServerUrlButtonEvent>().Publish();
            });
        }

        private void OnClickedCancel(object commandParam)
        {
            eventAggregator?.GetEvent<LogoutEvent>()?.Publish();
        }

        private void OnClickedRegister(object commandParam)
        {
            // TODO: handle user registration here
        }
    }
}
