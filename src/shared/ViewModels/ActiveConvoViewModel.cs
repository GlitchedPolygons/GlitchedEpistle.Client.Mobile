/*
    Glitched Epistle - Mobile Client
    Copyright (C) 2020 Raphael Beck

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
using System.Text;
using System.Windows.Input;
using GlitchedPolygons.ExtensionMethods;
using GlitchedPolygons.Services.MethodQ;
using GlitchedPolygons.GlitchedEpistle.Client.Models;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Models;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Commands;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.PubSubEvents;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Alerts;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Factories;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Permissions;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels.Interfaces;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Views;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Logging;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Settings;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Web.Users;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Web.Convos;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Cryptography.Messages;
using Prism.Events;
using Plugin.FilePicker;
using Plugin.FilePicker.Abstractions;
using Plugin.Permissions.Abstractions;
using Xamarin.Forms;
using Xamarin.Essentials;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels
{
    public class ActiveConvoViewModel : ViewModel, IOnAppearingListener, IOnDisappearingListener, IScrollToBottom, IDisposable
    {
        #region Constants

        private const int MSG_COLLECTION_SIZE = 10;
        private const string MSG_TIMESTAMP_FORMAT = "dd.MM.yyyy HH:mm";
        private const int MSG_PULL_FREQUENCY_ACTIVE = 314;
        private const int MSG_PULL_FREQUENCY_PASSIVE = 5000;
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
        private readonly ISilenceDetector silenceDetector;
        private readonly IEventAggregator eventAggregator;
        private readonly IViewModelFactory viewModelFactory;
        private readonly IConvoPasswordProvider convoPasswordProvider;
        private readonly IPermissionChecker permissionChecker;
        private readonly IGenericMessageNotification notification;

        #endregion

        #region Events

        public event EventHandler<ScrollToBottomEventArgs> ScrollToBottom;

        #endregion

        #region Commands

        public ICommand SendTextCommand { get; }
        public ICommand SendAudioCommand { get; }
        public ICommand SendFileCommand { get; }
        public ICommand EditConvoCommand { get; }
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
                DateTime? exp = ActiveConvo?.ExpirationUTC.FromUnixTimeSeconds();
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

        private bool hasNewMessages;
        public bool HasNewMessages
        {
            get => hasNewMessages;
            set => Set(ref hasNewMessages, value);
        }

        #endregion

        private volatile bool disposed;
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
            silenceDetector = DependencyService.Get<ISilenceDetector>();
            permissionChecker = DependencyService.Get<IPermissionChecker>();
            notification = DependencyService.Get<IGenericMessageNotification>();

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

            SendTextCommand = new DelegateCommand(OnSendText);
            SendFileCommand = new DelegateCommand(OnSendFile);
            SendAudioCommand = new DelegateCommand(OnSendAudio);
            EditConvoCommand = new DelegateCommand(OnEditConvo);
            CopyConvoIdToClipboardCommand = new DelegateCommand(OnClickedCopyConvoIdToClipboard);

            eventAggregator.GetEvent<DeletedConvoEvent>().Subscribe(OnDeletedConvo);
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
            CanSend = false;
            StopAutomaticPulling();

            foreach (var msg in Messages)
            {
                msg.Dispose();
            }
        }

        public async void OnAppearing()
        {
            if (ActiveConvo is null || ActiveConvo.Id.NullOrEmpty())
            {
                throw new NullReferenceException($"{nameof(ActiveConvoViewModel)}::{nameof(OnAppearing)}: Tried to initialize a convo viewmodel without assigning it an {nameof(ActiveConvo)} first. Please assign that before calling init.");
            }

            CanSend = false;
            StopAutomaticPulling();

            Name = ActiveConvo.Name;

            if (Messages.NullOrEmpty())
            {
                var lastMessages = await convoService.GetLastConvoMessages(ActiveConvo.Id, convoPasswordProvider.GetPasswordSHA512(ActiveConvo.Id), user.Id, user.Token.Item2, MSG_COLLECTION_SIZE);

                if (lastMessages.NotNullNotEmpty())
                {
                    var decryptedMessages = DecryptMessages(lastMessages).ToArray();
                    Messages = decryptedMessages.NotNullNotEmpty() ? new ObservableCollection<MessageViewModel>(decryptedMessages.Distinct().OrderBy(_ => _.TimestampDateTimeUTC)) : new ObservableCollection<MessageViewModel>();
                }

                if (Loading)
                {
                    Loading = false;
                }
            }

            StartAutomaticPulling(MSG_PULL_FREQUENCY_ACTIVE);

            CanSend = true;

            ScrollToBottom?.Invoke(this, new ScrollToBottomEventArgs { Animated = false });

            methodQ.Schedule(() => Loading = false, DateTime.UtcNow.AddSeconds(4.20));

            var lastMsg = Messages.LastOrDefault();
            var lastSeenMsg = await SecureStorage.GetAsync("last_msg:" + ActiveConvo.Id);
            
            if (lastMsg is null || lastSeenMsg.NullOrEmpty())
            {
                HasNewMessages = false;
            }
            else
            {
                HasNewMessages = lastMsg.Id != lastSeenMsg;
            }
            
            notification.Pop();
        }

        public async void OnDisappearing()
        {
            StopAutomaticPulling();
            StartAutomaticPulling(MSG_PULL_FREQUENCY_PASSIVE);

            foreach (var msg in Messages)
            {
                if (msg.IsAudioPlaying)
                {
                    msg.ClickedOnPlayAudioAttachmentCommand.Execute(null);
                }
            }

            HasNewMessages = false;
            await SecureStorage.SetAsync("last_msg:" + ActiveConvo.Id, Messages.LastOrDefault()?.Id ?? "0");
        }

        public Task LoadPreviousMessages()
        {
            if (Loading)
            {
                return Task.CompletedTask;
            }

            Loading = true;

            return Task.Run(async () =>
            {
                var top = Messages.FirstOrDefault();
                if (top is null)
                {
                    return;
                }

                if (!long.TryParse(top.Id, out long topMsgId))
                {
                    return;
                }

                var decryptedMessages = DecryptMessages(await convoService.GetPreviousMessages(
                    userId: user.Id,
                    auth: user.Token.Item2,
                    convoId: ActiveConvo.Id,
                    convoPasswordSHA512: convoPasswordProvider.GetPasswordSHA512(ActiveConvo.Id),
                    fromId: topMsgId,
                    n: MSG_COLLECTION_SIZE
                ));

                ExecUI(() =>
                {
                    foreach (var msg in decryptedMessages.OrderByDescending(_ => _.TimestampDateTimeUTC))
                    {
                        Messages.Insert(0, msg);
                    }

                    Loading = false;
                });
            });
        }

        private void StopAutomaticPulling()
        {
            autoFetch?.Cancel();
            autoFetch = null;

            metadataUpdater?.Cancel();
            metadataUpdater = null;
        }

        private void StartAutomaticPulling(int intervalMilliseconds)
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
                fetchTimeoutMilliseconds: intervalMilliseconds,
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

        private async void OnEditConvo(object obj)
        {
            var view = new ConvoMetadataPage();
            var viewModel = viewModelFactory.Create<ConvoMetadataViewModel>();
            viewModel.Convo = ActiveConvo;
            view.BindingContext = viewModel;

            await Application.Current.MainPage.Navigation.PushModalAsync(view);
        }

        private void OnSendText(object commandParam)
        {
            if (Text.NullOrEmpty())
            {
                return;
            }

            CanSend = false;

            Task.Run(async () =>
            {
                if (await messageSender.PostText(ActiveConvo, Text))
                {
                    Text = null;
                    ScrollToBottom?.Invoke(this, new ScrollToBottomEventArgs());
                }
                else
                {
                    ExecUI(() => Application.Current.MainPage.DisplayAlert(localization["MessageUploadFailureTitle"], localization["MessageUploadFailureMessage"], "OK"));
                }

                CanSend = true;
            });
        }

        private async void OnSendAudio(object commandParam)
        {
            if (!await permissionChecker.CheckPermission(Permission.Microphone, localization["MicrophonePermissionNeededForRecordingAudioAttachmentTitle"], localization["MicrophonePermissionNeededForRecordingAudioAttachmentText"], localization["AbortedDueToMicrophonePermissionDeclined"]))
            {
                return;
            }

            var viewModel = viewModelFactory.Create<RecordVoiceMessageViewModel>();
            var view = new RecordVoiceMessagePage { BindingContext = viewModel };

            view.Disappearing += (sender, e) =>
            {
                if (viewModel.Result != null)
                {
                    SendFile($"{DateTime.UtcNow:yyyy-MM-dd-HHmm-}{ActiveConvo.Id.Substring(0, 4)}.aac", viewModel.Result);
                }
            };

            await Application.Current.MainPage.Navigation.PushModalAsync(view);
        }

        private async void OnSendFile(object commandParam)
        {
            FileData pickerResult = await CrossFilePicker.Current.PickFile();

            if (pickerResult is null || pickerResult.FilePath.NullOrEmpty())
            {
                return;
            }

            if (!await Application.Current.MainPage.DisplayAlert(localization["ConfirmAttachmentUploadDialogTitle"], string.Format(localization["ConfirmAttachmentUploadDialogText"], pickerResult.FileName), localization["Yes"], localization["No"]))
            {
                return;
            }

            SendFile(pickerResult.FileName, pickerResult.DataArray);
        }

        private void SendFile(string fileName, byte[] fileBytes)
        {
            var _ = Task.Run(async () =>
            {
                CanSend = false;

                if (fileBytes.LongLength < MessageSender.MAX_FILE_SIZE_BYTES)
                {
                    if (!await messageSender.PostFile(ActiveConvo, fileName, fileBytes))
                    {
                        ExecUI(() => Application.Current.MainPage.DisplayAlert(localization["MessageUploadFailureTitle"], localization["MessageUploadFailureMessage"], "OK"));
                    }
                }
                else
                {
                    ExecUI(() => Application.Current.MainPage.DisplayAlert(localization["MessageTooLargeFailureTitle"], localization["MessageTooLargeFailureMessage"], "OK"));
                }

                CanSend = true;
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

            var decryptedMessages = DecryptMessages(fetchedMessages.Distinct()).OrderBy(m => m?.TimestampDateTimeUTC);

            if (Loading)
            {
                Loading = false;
            }

            ExecUI(() =>
            {
                // Decrypt and add the retrieved messages to the chatroom UI.
                foreach (var msg in decryptedMessages)
                {
                    Messages.Add(msg);
                }

                ScrollToBottom?.Invoke(this, new ScrollToBottomEventArgs());
            });

            bool sleeping = ((App)Application.Current).Sleeping;
       
#if !DEBUG
            if (decryptedMessages.Any(m => m?.SenderId != user.Id))
#endif
            {
                if (!sleeping && appSettings["Vibration", true] && !silenceDetector.IsAudioMuted())
                {
                    try
                    {
                        Vibration.Vibrate(TimeSpan.FromMilliseconds(250));
                    }
                    catch
                    {
                        appSettings["Vibration"] = "false";
                        alertService.AlertLong(localization["VibrationNotAvailable"]);
                    }
                }

                if (sleeping && appSettings["Notifications", true])
                {
                    notification?.Push();
                }
                
                HasNewMessages = true;
            }
            
            eventAggregator.GetEvent<FetchedNewMessagesEvent>().Publish(ActiveConvo.Id);
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

            try
            {
                var messageViewModel = new MessageViewModel(methodQ)
                {
                    Id = message.Id.ToString(),
                    SenderId = message.SenderId,
                    SenderName = message.SenderName,
                    IsFromServer = message.IsFromServer(),
                    TimestampDateTimeUTC = message.TimestampUTC.FromUnixTimeSeconds(),
                    Timestamp = message.TimestampUTC.FromUnixTimeSeconds().ToLocalTime().ToString(MSG_TIMESTAMP_FORMAT),
                    IsOwn = message.SenderId.Equals(user.Id),
                };

                if (message.IsFromServer())
                {
                    string[] split = message.Body.Split(':');
                    if (split.Length != 3 || !int.TryParse(split[1], out int messageType))
                    {
                        logger.LogError($"{nameof(ActiveConvoViewModel)}::{nameof(DecryptMessage)}: Broadcast message from the backend was submitted to the convo '{activeConvo.Id}' in an invalid format: was the server compromised?!");
                        return null;
                    }

                    switch (messageType)
                    {
                        case 0:
                            messageViewModel.Text = string.Format(localization["UserJoinedConvo"], split[2]);
                            break;
                        case 1:
                            messageViewModel.Text = string.Format(localization["UserLeftConvo"], split[2]);
                            break;
                        case 2:
                            messageViewModel.Text = string.Format(localization["UserWasKickedFromConvo"], split[2]);
                            break;
                        case 3:
                            messageViewModel.Text = localization["ConvoAboutToExpire"];
                            break;
                        case 4:
                            if (!int.TryParse(split[2], out int change))
                            {
                                logger.LogError($"{nameof(ActiveConvoViewModel)}::{nameof(DecryptMessage)}: Broadcast message from the backend was submitted to the convo '{activeConvo.Id}' in an invalid format: was the server compromised?!");
                                return null;
                            }

                            var sb = new StringBuilder(localization["ConvoMetadataChanged"]).Append(' ');

                            if ((change & 1 << 0) > 0)
                            {
                                sb.Append(localization["ConvoAdmin"]).Append(", ");
                            }

                            if ((change & 1 << 1) > 0)
                            {
                                sb.Append(localization["ConvoTitle"]).Append(", ");
                            }

                            if ((change & 1 << 2) > 0)
                            {
                                sb.Append(localization["ConvoDescription"]).Append(", ");
                            }

                            if ((change & 1 << 3) > 0)
                            {
                                sb.Append(localization["ConvoExpiration"]).Append(", ");
                            }

                            if ((change & 1 << 4) > 0)
                            {
                                sb.Append(localization["ConvoPassword"]).Append(", ");
                            }

                            sb.Length -= 2;
                            messageViewModel.Text = sb.ToString();
                            break;
                    }

                    messageViewModel.FileName = null;
                    messageViewModel.FileBytes = null;
                }
                else
                {
                    string decryptedMessage = crypto.DecryptMessage(message.Body, user.PrivateKeyPem);

                    if (decryptedMessage.StartsWith("TEXT="))
                    {
                        messageViewModel.Text = decryptedMessage.Substring(5);
                    }
                    else if (decryptedMessage.StartsWith("FILE="))
                    {
                        int base64 = decryptedMessage.IndexOf("///BASE64=", StringComparison.Ordinal);
                        if (base64 == -1)
                        {
                            logger?.LogError($"{nameof(ActiveConvoViewModel)}::{nameof(DecryptMessage)}: Decryption succeeded but message has invalid format: a \"FILE=\" name was specified but then there was no \"///BASE64=\"  token with the actual file's content...");
                            return null;
                        }

                        messageViewModel.FileName = decryptedMessage.Substring(5, base64 - 5);
                        messageViewModel.FileBytes = Convert.FromBase64String(decryptedMessage.Substring(base64 + 10, decryptedMessage.Length - base64 - 10));
                    }
                    else
                    {
                        logger?.LogError($"{nameof(ActiveConvoViewModel)}::{nameof(DecryptMessage)}: Decryption succeeded but message has invalid format!");
                        return null;
                    }
                }

                return messageViewModel;
            }
            catch
            {
                return null;
            }
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
                    logger.LogError($"{nameof(ActiveConvoViewModel)}::{nameof(DecryptMessages)}: Failed to decrypt message {message?.Id}. Error message: {e.Message}");
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
        
        private void OnDeletedConvo(string convoId)
        {
            if (ActiveConvo?.Id == convoId)
            {
                Application.Current.MainPage.Navigation.PopModalAsync();
            }
        }
    }
}