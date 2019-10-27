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
using System.Windows.Input;
using System.Threading.Tasks;
using GlitchedPolygons.ExtensionMethods;
using GlitchedPolygons.GlitchedEpistle.Client.Models;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Commands;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Logging;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels.Interfaces;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels
{
    public class JoinConvoViewModel : ViewModel, IOnAppearingListener
    {
        #region Constants

        // Injections:
        private readonly User user;
        private readonly ILogger logger;
        private readonly ILocalization localization;
        
        #endregion

        #region Commands

        public ICommand SubmitCommand { get; }
        public ICommand CancelCommand { get; }

        #endregion

        #region UI Bindings

        private string convoId;
        public string ConvoId
        {
            get => convoId;
            set
            {
                Set(ref convoId, value);
                UpdateJoinButton();
            }
        }

        private string convoPassword;
        public string ConvoPassword
        {
            get => convoPassword;
            set
            {
                Set(ref convoPassword, value);
                UpdateJoinButton();
            }
        }
        
        private bool cancelButtonEnabled = true;
        public bool CancelButtonEnabled
        {
            get => cancelButtonEnabled;
            set => Set(ref cancelButtonEnabled, value);
        }

        private bool joinButtonEnabled = false;
        public bool JoinButtonEnabled
        {
            get => joinButtonEnabled;
            set => Set(ref joinButtonEnabled, value);
        }

        #endregion
        
        public JoinConvoViewModel(User user, ILogger logger)
        {
            localization = DependencyService.Get<ILocalization>();
            
            this.user = user;
            this.logger = logger;

            SubmitCommand = new DelegateCommand(OnSubmit);
            CancelCommand = new DelegateCommand(OnCancel);
        }
        
        public void OnAppearing()
        {
            UpdateJoinButton();
        }
        
        private void UpdateJoinButton()
        {
            JoinButtonEnabled = ConvoId.NotNullNotEmpty() && ConvoPassword.NotNullNotEmpty();
        }

        private void OnSubmit(object commandParam)
        {
            Task.Run(async () =>
            {
                // TODO: attempt join here
            });
        }
        
        private async void OnCancel(object commandParam)
        {
            JoinButtonEnabled = CancelButtonEnabled = false;
            ConvoId = ConvoPassword = null;
            await Application.Current.MainPage.Navigation.PopModalAsync();
        }
    }
}
