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

using System;
using System.IO;
using System.Globalization;
using System.Windows.Input;
using GlitchedPolygons.ExtensionMethods;
using GlitchedPolygons.Services.MethodQ;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Commands;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Alerts;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Paths;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Permissions;
using Plugin.Permissions.Abstractions;
using Plugin.SimpleAudioPlayer;
using Xamarin.Forms;
using Xamarin.Essentials;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels
{
    public class MessageViewModel : ViewModel, IDisposable
    {
        #region Constants

        private readonly IFileOpener fileOpener = DependencyService.Get<IFileOpener>();
        private readonly ILocalization localization = DependencyService.Get<ILocalization>();
        private readonly IAlertService alertService = DependencyService.Get<IAlertService>();
        private readonly IDownloadPath downloadPath = DependencyService.Get<IDownloadPath>();
        private readonly IPermissionChecker permissionChecker = DependencyService.Get<IPermissionChecker>();

        // Injections:
        private readonly IMethodQ methodQ;

        #endregion

        #region Commands

        public ICommand DownloadAttachmentCommand { get; }
        public ICommand CopyUserIdToClipboardCommand { get; }
        public ICommand ClickedOnImageAttachmentCommand { get; }
        public ICommand ClickedOnPlayAudioAttachmentCommand { get; }
        public ICommand LongPressedMessageTextCommand { get; }
        public ICommand AudioThumbDraggedCommand { get; }

        #endregion

        #region UI Bindings

        private string senderId = string.Empty;
        public string SenderId
        {
            get => senderId;
            set => Set(ref senderId, value);
        }

        private string senderName = string.Empty;
        public string SenderName
        {
            get => senderName;
            set => Set(ref senderName, value);
        }

        private string text = string.Empty;
        public string Text
        {
            get => text;
            set => Set(ref text, value);
        }

        private string fileName = string.Empty;
        public string FileName
        {
            get => fileName;
            set => Set(ref fileName, value);
        }

        private byte[] fileBytes;
        public byte[] FileBytes
        {
            get => fileBytes;
            set
            {
                fileBytes = value;
                
                if (value != null && ImageVisibility)
                {
                    try
                    {
                        ImageSource = null;
                        ImageSource = ImageSource.FromStream(FileBytesStream);
                    }
                    catch{}
                }
                
                if (value != null && IsAudio())
                {
                    audioPlayer = DependencyService.Get<ISimpleAudioPlayer>(DependencyFetchTarget.NewInstance);
                    AudioLoadFailed = !audioPlayer.Load(FileBytesStream());
                    audioPlayer.Loop = false;
                    audioPlayer.PlaybackEnded += OnAudioPlaybackEnded;
                    OnAudioThumbDragged(null);
                    AudioDuration = TimeSpan.FromSeconds(audioPlayer.Duration).ToString(@"mm\:ss");
                }
            }
        }

        private MemoryStream fileBytesStream;
        public MemoryStream FileBytesStream()
        {
            if (fileBytesStream != null)
            {
                fileBytesStream.Dispose();
                fileBytesStream = null;
            }

            return fileBytesStream = new MemoryStream(FileBytes ?? new byte[0]);
        }

        private bool isOwn;
        public bool IsOwn
        {
            get => isOwn;
            set => Set(ref isOwn, value);
        }
        
        private bool isAudioPlaying;
        public bool IsAudioPlaying
        {
            get => isAudioPlaying;
            set => Set(ref isAudioPlaying, value);
        }

        public string FileSize => $"({FileBytes.GetFileSizeString()})";

        private ImageSource imageSource;
        public ImageSource ImageSource
        {
            get => imageSource;
            set => Set(ref imageSource, value);
        }

        private string timestamp = string.Empty;
        public string Timestamp
        {
            get => timestamp;
            set => Set(ref timestamp, value);
        }

        public DateTime TimestampDateTimeUTC { get; set; }

        private bool clipboardTickVisible = false;
        public bool ClipboardTickVisible
        {
            get => clipboardTickVisible;
            set => Set(ref clipboardTickVisible, value);
        }
        
        private bool audioLoadFailed = false;
        public bool AudioLoadFailed
        {
            get => audioLoadFailed;
            set => Set(ref audioLoadFailed, value);
        }

        private double audioThumbPos = 0.0;
        public double AudioThumbPos
        {
            get => audioThumbPos;
            set => Set(ref audioThumbPos, value < 0 ? 0 : value > 1 ? 1 : value);
        }

        private string audioDuration = "00:00";
        public string AudioDuration
        {
            get => audioDuration;
            set => Set(ref audioDuration, value);
        }

        public bool GifVisibility => IsGif();
        public bool AudioVisibility => IsAudio();
        public bool ImageVisibility => IsImage() || IsGif();
        public bool HasAttachment => FileName.NotNullNotEmpty() && FileBytes != null && FileBytes.Length > 0;

        #endregion

        public string Id { get; set; }

        private bool disposed;
        private ulong? thumbUpdater;
        private ulong? scheduledHideGreenTickIcon;
        private ISimpleAudioPlayer audioPlayer;

        public MessageViewModel(IMethodQ methodQ)
        {
            this.methodQ = methodQ;

            DownloadAttachmentCommand = new DelegateCommand(OnDownloadAttachment);
            AudioThumbDraggedCommand = new DelegateCommand(OnAudioThumbDragged);
            CopyUserIdToClipboardCommand = new DelegateCommand(OnCopyUserIdToClipboard);
            ClickedOnImageAttachmentCommand = new DelegateCommand(OnClickedImagePreview);
            LongPressedMessageTextCommand = new DelegateCommand(OnLongPressedMessageText);
            ClickedOnPlayAudioAttachmentCommand = new DelegateCommand(OnClickedPlayAudioAttachment);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            if (audioPlayer != null)
            {
                audioPlayer.Stop();
                audioPlayer.PlaybackEnded -= OnAudioPlaybackEnded;
                audioPlayer.Dispose(); // TODO: check dis iz bueno ricky
            }

            Text = FileName = null;

            if (FileBytes != null)
            {
                for (var i = 0; i < FileBytes.Length; i++)
                {
                    FileBytes[i] = 0;
                }
            }
        }

        private async void OnDownloadAttachment(object commandParam)
        {
            if (FileName.NullOrEmpty())
            {
                return;
            }

            if (!await permissionChecker.CheckPermission(Permission.Storage,localization["StoragePermissionNeededForDownloadingAttachmentTitle"], localization["StoragePermissionNeededForDownloadingAttachmentText"], localization["AbortedDueToStoragePermissionDeclined"]))
            {
                return;
            }

            string path = Path.Combine(await downloadPath.GetDownloadDirectoryPath(), FileName);

            if (path.NullOrEmpty())
            {
                return;
            }

            if (!await Application.Current.MainPage.DisplayAlert(localization["DownloadAttachmentDialogTitle"], string.Format(localization["DownloadAttachmentDialogText"], FileSize.Replace("(", string.Empty).Replace(")", string.Empty), path), localization["Yes"], localization["No"]))
            {
                return;
            }

            if (File.Exists(path) && !await Application.Current.MainPage.DisplayAlert(localization["FileAlreadyExistsDialogTitle"], string.Format(localization["FileAlreadyExistsDialogText"], Path.GetFileName(path)), localization["Yes"], localization["No"]))
            {
                return;
            }

            try
            {
                File.WriteAllBytes(path, FileBytes);
                alertService.AlertLong(string.Format(localization["DownloadCompleteSuccessMessage"], path));
            }
            catch (Exception)
            {
                alertService.AlertShort(localization["DownloadFailedErrorMessage"]);
                return;
            }

            OpenAttachment(path);
        }

        private async void OnLongPressedMessageText(object commandParam)
        {
            bool copyConfirmed = await Application.Current.MainPage.DisplayAlert(
                title: localization["LongPressedMessageTextTitle"],
                message: localization["LongPressedMessageTextDescription"],
                accept: localization["Copy"],
                cancel: localization["CancelButton"]
            );

            if (!copyConfirmed)
            {
                return;
            }

            await Clipboard.SetTextAsync(Text);
            alertService.AlertShort(localization["Copied"]);
        }

        private async void OnClickedImagePreview(object commandParam)
        {
            if (FileName.NullOrEmpty())
            {
                return;
            }

            if (!await permissionChecker.CheckPermission(Permission.Storage,localization["StoragePermissionNeededForDownloadingAttachmentTitle"], localization["StoragePermissionNeededForDownloadingAttachmentText"], localization["AbortedDueToStoragePermissionDeclined"]))
            {
                return;
            }

            string path = Path.Combine(await downloadPath.GetDownloadDirectoryPath(), FileName);

            if (!File.Exists(path))
            {
                OnDownloadAttachment(null);
                return;
            }

            OpenAttachment(path);
        }

        private async void OpenAttachment(string filePath)
        {
            if (filePath.NullOrEmpty() || !File.Exists(filePath) || !await Application.Current.MainPage.DisplayAlert(localization["OpenAttachmentDialogTitle"], string.Format(localization["OpenAttachmentDialogText"], Path.GetFileName(FileName)), localization["Yes"], localization["No"]))
            {
                return;
            }

            fileOpener.OpenFile(filePath);
        }

        private void OnClickedPlayAudioAttachment(object commandParam)
        {
            if (!IsAudio())
            {
                return;
            }

            if (AudioLoadFailed)
            {
                Application.Current.MainPage.DisplayAlert(localization["AudioLoadFailedErrorMessageTitle"], localization["AudioLoadFailedErrorMessageText"], "OK");
                return;
            }

            if (audioPlayer is null)
            {
                return;
            }
            
            IsAudioPlaying = !IsAudioPlaying;

            if (thumbUpdater.HasValue)
            {
                methodQ.Cancel(thumbUpdater.Value);
                thumbUpdater = null;
            }
            
            if (IsAudioPlaying)
            {
                audioPlayer.Play();

                thumbUpdater = methodQ.Schedule(() => AudioThumbPos = audioPlayer.CurrentPosition / audioPlayer.Duration, TimeSpan.FromMilliseconds(420.69d));
            }
            else
            {
                audioPlayer.Pause();
                
                if (thumbUpdater.HasValue)
                {
                    methodQ.Cancel(thumbUpdater.Value);
                    thumbUpdater = null;
                }
            }
        }

        private void OnAudioPlaybackEnded(object sender, EventArgs e)
        {
            AudioThumbPos = 0;
            audioPlayer.Stop();
            IsAudioPlaying = false;
        }

        private void OnAudioThumbDragged(object commandParam)
        {
            if (audioPlayer is null || !audioPlayer.CanSeek)
            {
                return;
            }

            audioPlayer.Seek(AudioThumbPos * audioPlayer.Duration);
        }

        private void OnCopyUserIdToClipboard(object commandParam)
        {
            Clipboard.SetTextAsync(SenderId);
            ClipboardTickVisible = true;

            if (scheduledHideGreenTickIcon.HasValue)
            {
                methodQ.Cancel(scheduledHideGreenTickIcon.Value);
            }

            alertService?.AlertShort(localization["Copied"] ?? "OK!");

            scheduledHideGreenTickIcon = methodQ.Schedule(() =>
            {
                ClipboardTickVisible = false;
                scheduledHideGreenTickIcon = null;
            }, DateTime.UtcNow.AddSeconds(3));
        }

        private bool IsImage()
        {
            if (!HasAttachment)
            {
                return false;
            }

            return FileName.EndsWith(".png", true, CultureInfo.InvariantCulture)
                   || FileName.EndsWith(".jpg", true, CultureInfo.InvariantCulture)
                   || FileName.EndsWith(".jpeg", true, CultureInfo.InvariantCulture)
                   || FileName.EndsWith(".tif", true, CultureInfo.InvariantCulture);
        }

        private bool IsGif()
        {
            if (!HasAttachment)
            {
                return false;
            }

            return FileName.EndsWith(".gif", true, CultureInfo.InvariantCulture);
        }

        private bool IsAudio()
        {
            if (!HasAttachment)
            {
                return false;
            }

            return FileName.EndsWith(".mp3", true, CultureInfo.InvariantCulture)
                   || FileName.EndsWith(".wav", true, CultureInfo.InvariantCulture)
                   || FileName.EndsWith(".aac", true, CultureInfo.InvariantCulture)
                   || FileName.EndsWith(".wma", true, CultureInfo.InvariantCulture);
        }
    }
}