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

#define PARALLEL_LOAD
// Comment out the above line to load/decrypt messages synchronously.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using GlitchedPolygons.ExtensionMethods;
using GlitchedPolygons.Services.MethodQ;
using GlitchedPolygons.GlitchedEpistle.Client.Models;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Commands;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.PubSubEvents;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Alerts;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Factories;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels.Interfaces;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Logging;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Settings;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Web.Users;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Web.Convos;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Cryptography.Messages;
using Prism.Events;
using Newtonsoft.Json.Linq;
using Xamarin.Forms;
using Xamarin.Essentials;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels
{
    public class ActiveConvoViewModel : ViewModel, IOnAppearingListener, IOnDisappearingListener, IDisposable
    {
        #region Constants

        private const int MSG_COLLECTION_SIZE = 20;
        private const string MSG_TIMESTAMP_FORMAT = "dd.MM.yyyy HH:mm";
        private static readonly TimeSpan METADATA_PULL_FREQUENCY = TimeSpan.FromMilliseconds(30000);

        // Injections:
        private readonly User user;
        private readonly ILogger logger;
        private readonly IMethodQ methodQ;
        private readonly ILocalization localization;
        private readonly IAlertService alertService;
        private readonly IConvoService convoService;
        private readonly IUserService userService;
        private readonly IAppSettings appSettings;
        private readonly IUserSettings userSettings;
        private readonly IMessageCryptography crypto;
        private readonly IMessageSender messageSender;
        private readonly IMessageFetcher messageFetcher;
        private readonly IEventAggregator eventAggregator;
        private readonly IViewModelFactory viewModelFactory;
        private readonly IConvoPasswordProvider convoPasswordProvider;

        #endregion

        #region Commands

        public ICommand CopyConvoIdToClipboardCommand { get; }

        #endregion

        #region UI Bindings

        private bool canSend;
        public bool CanSend
        {
            get => canSend;
            set => Set(ref canSend, value);
        }

        private string text;
        public string Text
        {
            get => text;
            set => Set(ref text, value);
        }

        private string name;
        public string Name
        {
            get => name;
            set
            {
                // Convo expiration check 
                // (adapt the title label accordingly).
                DateTime? exp = ActiveConvo?.ExpirationUTC;
                if (exp.HasValue)
                {
                    if (DateTime.UtcNow > exp.Value)
                    {
                        value += localization["Expired"];
                    }
                    else if ((exp.Value - DateTime.UtcNow).TotalDays < 3)
                    {
                        value += localization["ExpiresSoon"];
                    }
                }

                Set(ref name, value);
            }
        }
        
        private bool loading = true;
        public bool Loading
        {
            get => loading;
            set => Set(ref loading, value);
        }

        private bool clipboardTickVisible = false;
        public bool ClipboardTickVisible
        {
            get => clipboardTickVisible;
            set => Set(ref clipboardTickVisible, value);
        }

        private ObservableCollection<MessageViewModel> messages = new ObservableCollection<MessageViewModel>();
        public ObservableCollection<MessageViewModel> Messages
        {
            get => messages;
            set => Set(ref messages, value);
        }

        #endregion

        private volatile bool disposed;
        private volatile int pageIndex;
        private volatile CancellationTokenSource autoFetch;
        private volatile CancellationTokenSource metadataUpdater;

        private ulong? scheduledHideGreenTickIcon;

        private Convo activeConvo;
        public Convo ActiveConvo
        {
            get => activeConvo;
            set
            {
                activeConvo = value;
                Name = value.Name;
            }
        }

        public ActiveConvoViewModel(User user, IConvoService convoService, IConvoPasswordProvider convoPasswordProvider, IEventAggregator eventAggregator, ILogger logger, IUserService userService, IUserSettings userSettings, IMethodQ methodQ, IViewModelFactory viewModelFactory, IAppSettings appSettings, IMessageCryptography crypto, IMessageSender messageSender, IMessageFetcher messageFetcher)
        {
            localization = DependencyService.Get<ILocalization>();
            alertService = DependencyService.Get<IAlertService>();

            this.user = user;
            this.logger = logger;
            this.crypto = crypto;
            this.methodQ = methodQ;
            this.userService = userService;
            this.convoService = convoService;
            this.appSettings = appSettings;
            this.userSettings = userSettings;
            this.messageSender = messageSender;
            this.messageFetcher = messageFetcher;
            this.eventAggregator = eventAggregator;
            this.viewModelFactory = viewModelFactory;
            this.convoPasswordProvider = convoPasswordProvider;

            CopyConvoIdToClipboardCommand = new DelegateCommand(OnClickedCopyConvoIdToClipboard);
            
            eventAggregator.GetEvent<ChangedConvoMetadataEvent>().Subscribe(OnChangedConvoMetadata);
        }

        ~ActiveConvoViewModel()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            StopAutomaticPulling();
        }

        public void OnAppearing()
        {
            if (ActiveConvo is null || ActiveConvo.Id.NullOrEmpty())
            {
                throw new NullReferenceException($"{nameof(ActiveConvoViewModel)}::{nameof(OnAppearing)}: Tried to initialize a convo viewmodel without assigning it an {nameof(ActiveConvo)} first. Please assign that before calling init.");
            }

            CanSend = false;
            StopAutomaticPulling();

            Name = ActiveConvo.Name;
            Messages = new ObservableCollection<MessageViewModel>();
            
            StartAutomaticPulling();
            CanSend = true;
        }

        public void OnDisappearing()
        {
            Dispose();
        }

        private void StopAutomaticPulling()
        {
            autoFetch?.Cancel();
            autoFetch = null;

            metadataUpdater?.Cancel();
            metadataUpdater = null;
        }

        private void StartAutomaticPulling()
        {
            StopAutomaticPulling();

            long tailId = 0;
            if (Messages.NotNullNotEmpty())
            {
                long.TryParse(Messages?.Last()?.Id ?? "0", out tailId);
            }
            
            autoFetch = messageFetcher.StartAutoFetchingMessages(
                tailId: tailId,
                callback: OnFetchedNewMessages,
                convoId: ActiveConvo.Id,
                convoPasswordSHA512: convoPasswordProvider.GetPasswordSHA512(ActiveConvo.Id)
            );

            metadataUpdater = new CancellationTokenSource();

            var task = Task.Run(async () =>
            {
                while (!metadataUpdater.IsCancellationRequested)
                {
                    await PullConvoMetadata();
                    Thread.Sleep(METADATA_PULL_FREQUENCY);
                }
            }, metadataUpdater.Token);
        }
        
        private void OnSendText(object commandParam)
        {
            if (Text.NullOrEmpty())
            {
                return;
            }

            Task.Run(async () =>
            {
                if (!await messageSender.PostText(ActiveConvo, Text))
                {
                    ExecUI(() => Application.Current.MainPage.DisplayAlert(localization["MessageUploadFailureTitle"], localization["MessageUploadFailureMessage"], "OK"));
                }
            });
        }
        
        public void OnSendFile(object commandParam)
        {
            string filePath = ""; // TODO: open file picker dialog here!
            
            if (filePath.NullOrEmpty())
            {
                return;
            }

            Task.Run(async () =>
            {
                byte[] fileBytes = File.ReadAllBytes(filePath);

                if (fileBytes.LongLength < MessageSender.MAX_FILE_SIZE_BYTES)
                {
                    if (!await messageSender.PostFile(ActiveConvo, Path.GetFileName(filePath), fileBytes))
                    {
                        ExecUI(() => Application.Current.MainPage.DisplayAlert(localization["MessageUploadFailureTitle"], localization["MessageUploadFailureMessage"], "OK"));
                    }
                }
                else
                {
                    ExecUI(() => Application.Current.MainPage.DisplayAlert(localization["MessageTooLargeFailureTitle"], localization["MessageTooLargeFailureMessage"], "OK"));
                }
            });
        }
        
        private void OnChangedConvoMetadata(string convoId)
        {
            if (ActiveConvo.Id != convoId)
            {
                return;
            }
            
            Name = ActiveConvo?.Name ?? "Convo";
        }

        /// <summary>
        /// Callback method for the auto-fetching routine.<para> </para>
        /// Gets called when there were new messages in the <see cref="Convo"/> server-side.<para> </para>
        /// Also truncates the view's message collection size when needed.
        /// </summary>
        /// <param name="fetchedMessages">The messages that were fetched from the backend.</param>
        private void OnFetchedNewMessages(IEnumerable<Message> fetchedMessages)
        {
            if (fetchedMessages is null)
            {
                return;
            }

            // Decrypt and add the retrieved messages to the chatroom UI.
            var newMessages = Messages.ToList();
            newMessages.AddRange(DecryptMessages(fetchedMessages).OrderBy(m => m?.TimestampDateTimeUTC));

            if (Loading)
            {
                Loading = false;
            }
            
            ExecUI(() =>
            {
                Messages = new ObservableCollection<MessageViewModel>(newMessages);

                if (pageIndex == 0)
                {
                    TruncateMessagesCollection();
                }
            });
        }

        /// <summary>
        /// Decrypts a single <see cref="Message"/> into a <see cref="MessageViewModel"/>.
        /// </summary>
        /// <param name="message">The <see cref="Message"/> to decrypt.</param>
        /// <returns>The decrypted <see cref="MessageViewModel"/>, ready to be added to the view.</returns>
        private MessageViewModel DecryptMessage(Message message)
        {
            if (message is null)
            {
                return null;
            }

            string decryptedJson = crypto.DecryptMessage(message.Body, user.PrivateKeyPem);
            JToken json = JToken.Parse(decryptedJson);

            if (json == null)
            {
                return null;
            }

            var messageViewModel = new MessageViewModel(methodQ)
            {
                Id = message.Id.ToString(),
                SenderId = message.SenderId,
                SenderName = message.SenderName,
                TimestampDateTimeUTC = message.TimestampUTC,
                Timestamp = message.TimestampUTC.ToLocalTime().ToString(MSG_TIMESTAMP_FORMAT),
                Text = json["text"]?.Value<string>(),
                FileName = json["fileName"]?.Value<string>(),
                IsOwn = message.SenderId.Equals(user.Id),
            };

            string fileBase64 = json["fileBase64"]?.Value<string>();
            messageViewModel.FileBytes = string.IsNullOrEmpty(fileBase64) ? null : Convert.FromBase64String(fileBase64);

            return messageViewModel;
        }

        /// <summary>
        /// Decrypts multiple <see cref="Message"/>s. Does not guarantee correct order!
        /// </summary>
        /// <param name="encryptedMessages">The <see cref="Message"/>s to decrypt.</param>
        /// <returns>The decrypted <see cref="MessageViewModel"/>s, ready to be added to the view.</returns>
        private IEnumerable<MessageViewModel> DecryptMessages(IEnumerable<Message> encryptedMessages)
        {
            var decryptedMessages = new ConcurrentBag<MessageViewModel>();

#if PARALLEL_LOAD
            Parallel.ForEach(encryptedMessages, message =>
            {
                try
                {
                    var decryptedMessage = DecryptMessage(message);
                    decryptedMessages.Add(decryptedMessage);
                }
                catch (Exception e)
                {
                    logger.LogError($"{nameof(ActiveConvoViewModel)}::{nameof(DecryptMessages)}: Failed to decrypt message {message?.Id}. Error message: {e}");
                }
            });
#else
            foreach (var message in encryptedMessages)
            {
                try
                {
                    decryptedMessages.Add(DecryptMessage(message));
                }
                catch (Exception e)
                {
                    logger.LogError($"{nameof(ActiveConvoViewModel)}::{nameof(DecryptMessages)}: Failed to decrypt message {message?.Id}. Error message: {e}");
                }
            }
#endif
            return decryptedMessages;
        }

        /// <summary>
        /// Pulls the convo's metadata and updates the local copy if something changed.<para> </para>
        /// </summary>
        private async Task PullConvoMetadata()
        {
            var metadataDto = await convoService.GetConvoMetadata(ActiveConvo.Id, convoPasswordProvider.GetPasswordSHA512(ActiveConvo.Id), user.Id, user.Token.Item2);

            if (metadataDto is null || ActiveConvo.Equals(metadataDto))
            {
                return;
            }

            ActiveConvo.Name = metadataDto.Name;
            ActiveConvo.CreatorId = metadataDto.CreatorId;
            ActiveConvo.Description = metadataDto.Description;
            ActiveConvo.ExpirationUTC = metadataDto.ExpirationUTC;
            ActiveConvo.CreationUTC = metadataDto.CreationUTC;
            ActiveConvo.BannedUsers = metadataDto.BannedUsers.Split(',').ToList();
            ActiveConvo.Participants = metadataDto.Participants.Split(',').ToList();

            ExecUI(() => eventAggregator.GetEvent<ChangedConvoMetadataEvent>().Publish(ActiveConvo.Id));
        }

        private void OnClickedCopyConvoIdToClipboard(object commandParam)
        {
            Clipboard.SetTextAsync(ActiveConvo.Id);
            ClipboardTickVisible = true;

            if (scheduledHideGreenTickIcon.HasValue)
            {
                methodQ.Cancel(scheduledHideGreenTickIcon.Value);
            }

            scheduledHideGreenTickIcon = methodQ.Schedule(delegate
            {
                ClipboardTickVisible = false;
                scheduledHideGreenTickIcon = null;
            }, DateTime.UtcNow.AddSeconds(3));
        }

        /// <summary>
        /// If there are too many messages loaded into the view,
        /// truncate the messages collection to the latest ones.<para> </para>
        /// </summary>
        private void TruncateMessagesCollection()
        {
            int messageCount = Messages.Count;
            if (messageCount > MSG_COLLECTION_SIZE * 2)
            {
                Messages = new ObservableCollection<MessageViewModel>(Messages.SkipWhile((msg, i) => i < messageCount - MSG_COLLECTION_SIZE).ToArray());
            }
        }
    }
}
