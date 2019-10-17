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
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using GlitchedPolygons.ExtensionMethods;
using GlitchedPolygons.Services.Cryptography.Asymmetric;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Commands;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Constants;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.PubSubEvents;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;
using GlitchedPolygons.GlitchedEpistle.Client.Models;
using GlitchedPolygons.GlitchedEpistle.Client.Models.DTOs;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Logging;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Web.Convos;

using Prism.Events;
using Newtonsoft.Json;

using Xamarin.Forms;
using Xamarin.Essentials;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels.MasterDetail
{
    public class ConvosViewModel : ViewModel
    {
        #region Constants
        private readonly User user;
        private readonly ILogger logger;
        private readonly ILocalization localization;
        private readonly IConvoService convoService;
        private readonly IConvoPasswordProvider convoPasswordProvider;
        private readonly IEventAggregator eventAggregator;
        private readonly IAsymmetricCryptographyRSA crypto;
        #endregion

        #region Commands
        public ICommand OpenConvoCommand { get; }
        public ICommand EditConvoCommand { get; }
        public ICommand CopyConvoIdCommand { get; }
        #endregion

        #region UI Bindings
        private ObservableCollection<Convo> convos;
        public ObservableCollection<Convo> Convos
        {
            get => convos;
            set => Set(ref convos, value);
        }

        private bool canJoin = true;
        public bool CanJoin
        {
            get => canJoin;
            set => Set(ref canJoin, value);
        }
        #endregion

        public ConvosViewModel(User user, IConvoService convoService, IConvoPasswordProvider convoPasswordProvider, IEventAggregator eventAggregator, IAsymmetricCryptographyRSA crypto, ILogger logger)
        {
            localization = DependencyService.Get<ILocalization>();
            
            this.user = user;
            this.crypto = crypto;
            this.logger = logger;
            this.convoService = convoService;
            this.eventAggregator = eventAggregator;
            this.convoPasswordProvider = convoPasswordProvider;
            
            if (user.Id.NotNullNotEmpty())
            {
                UpdateList();
            }

            OpenConvoCommand = new DelegateCommand(OnClickedOnConvo);
            EditConvoCommand = new DelegateCommand(OnClickedEditConvo);
            CopyConvoIdCommand = new DelegateCommand(OnClickedCopyConvoIdToClipboard);

            eventAggregator.GetEvent<LoginSucceededEvent>().Subscribe(UpdateList);
            eventAggregator.GetEvent<UpdatedUserConvosEvent>().Subscribe(UpdateList);
            eventAggregator.GetEvent<JoinedConvoEvent>().Subscribe(_ => UpdateList());
            eventAggregator.GetEvent<DeletedConvoEvent>().Subscribe(_ => UpdateList());
            eventAggregator.GetEvent<ChangedConvoMetadataEvent>().Subscribe(_ => UpdateList());
            eventAggregator.GetEvent<ConvoCreationSucceededEvent>().Subscribe(_ => UpdateList());
        }
        
        private async void UpdateList()
        {
            string convosJson = await SecureStorage.GetAsync(user.Id + ":convos");

            try
            {
                Convos = new ObservableCollection<Convo>(
                    JsonConvert.DeserializeObject<Convo[]>(convosJson ?? "{}")
                        .Where(convo => !convo.IsExpired())
                        .OrderByDescending(convo => convo.ExpirationUTC)
                        .ThenBy(convo => convo.Name.ToUpper())
                );
            }
            catch (Exception e)
            {
                logger?.LogError($"{nameof(ConvosViewModel)}::{nameof(UpdateList)}: Couldn't deserialize convos list found on device's {nameof(SecureStorage)}. Thrown exception: {e.ToString()}''");
                Convos = new ObservableCollection<Convo>();
            }
        }

        private void OnClickedOnConvo(object commandParam)
        {
            var _convo = commandParam as Convo;
            if (_convo is null || !CanJoin)
            {
                return;
            }

            CanJoin = false;
            string cachedPwSHA512 = convoPasswordProvider.GetPasswordSHA512(_convo.Id);

            if (cachedPwSHA512.NotNullNotEmpty())
            {
                Task.Run(async () =>
                {
                    var dto = new ConvoJoinRequestDto
                    {
                        ConvoId = _convo.Id,
                        ConvoPasswordSHA512 = cachedPwSHA512
                    };

                    var body = new EpistleRequestBody
                    {
                        UserId = user.Id,
                        Auth = user.Token.Item2,
                        Body = JsonConvert.SerializeObject(dto)
                    };

                    if (!await convoService.JoinConvo(body.Sign(crypto, user.PrivateKeyPem)))
                    {
                        convoPasswordProvider.RemovePasswordSHA512(_convo.Id);

                        ExecUI(() =>
                        {
                            CanJoin = true;
                            Application.Current.MainPage.DisplayAlert(localization["Error"], localization["JoinConvoFailed"], "OK");
                        });
                        return;
                    }

                    ConvoMetadataDto metadata = await convoService.GetConvoMetadata(_convo.Id, cachedPwSHA512, user.Id, user.Token.Item2);
                    if (metadata is null)
                    {
                        return;
                    }

                    ExecUI(() =>
                    {
                        CanJoin = true;
                        eventAggregator.GetEvent<JoinedConvoEvent>().Publish(metadata);
                    });
                });
            }
            else // Password not yet stored in session's IConvoPasswordProvider
            {
//                var view = windowFactory.Create<JoinConvoDialogView>(true);
//                var viewModel = viewModelFactory.Create<JoinConvoDialogViewModel>();
//                viewModel.ConvoId = _convo.Id;
//
//                if (view.DataContext is null)
//                {
//                    view.DataContext = viewModel;
//                }
//
//                view.ShowDialog();
//                CanJoin = true;
            }
        }

        private void OnClickedEditConvo(object commandParam)
        {
//            var convo = commandParam as Convo;
//            if (convo is null)
//            {
//                return;
//            }
//
//            var view = windowFactory.Create<EditConvoMetadataView>(true);
//            var viewModel = viewModelFactory.Create<EditConvoMetadataViewModel>();
//            viewModel.Convo = convo;
//            view.DataContext = viewModel;
//
//            view.Show();
//            view.Focus();
        }

        private void OnClickedCopyConvoIdToClipboard(object commandParam)
        {
            if (commandParam is Convo convo)
            {
                Clipboard.SetTextAsync(convo.Id);
            }
        }
    }
}
