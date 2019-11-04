﻿/*
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
using System.Globalization;
using System.Windows.Input;
using GlitchedPolygons.ExtensionMethods;
using GlitchedPolygons.Services.MethodQ;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Commands;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Alerts;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;
using Xamarin.Forms;
using Xamarin.Essentials;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels
{
    public class MessageViewModel : ViewModel
    {
        #region Constants

        private readonly ILocalization localization = DependencyService.Get<ILocalization>();
        private readonly IAlertService alertService = DependencyService.Get<IAlertService>();

        // Injections:
        private readonly IMethodQ methodQ;

        #endregion

        #region Commands

        public ICommand DownloadAttachmentCommand { get; }
        public ICommand CopyUserIdToClipboardCommand { get; }
        public ICommand ClickedOnImageAttachmentCommand { get; }

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
                if (value != null && IsImage())
                {
                    try
                    {
                        var img = new BitmapImage();
                        img.BeginInit();
                        img.DecodePixelWidth = 256;
                        img.StreamSource = FileBytesStream;
                        img.EndInit();
                        img.Freeze();
                        Image = img;
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        private MemoryStream fileBytesStream;
        public MemoryStream FileBytesStream
        {
            get
            {
                if (fileBytesStream is null)
                {
                    fileBytesStream = new MemoryStream(FileBytes ?? new byte[0]);
                }

                return fileBytesStream;
            }
        }

        private bool isOwn;
        public bool IsOwn
        {
            get => isOwn;
            set => Set(ref isOwn, value);
        }

        public string FileSize => $"({FileBytes.GetFileSizeString()})";

        public BitmapImage Image { get; set; }

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

        public bool GifVisibility => IsGif();
        public bool ImageVisibility => IsImage();
        public bool AttachmentButtonVisibility => HasAttachment();

        #endregion

        public string Id { get; set; }

        private ulong? scheduledHideGreenTickIcon;

        public MessageViewModel(IMethodQ methodQ)
        {
            this.methodQ = methodQ;

            DownloadAttachmentCommand = new DelegateCommand(OnDownloadAttachment);
            CopyUserIdToClipboardCommand = new DelegateCommand(OnCopyUserIdToClipboard);
            ClickedOnImageAttachmentCommand = new DelegateCommand(OnClickedImagePreview);
        }

        ~MessageViewModel()
        {
            Text = FileName = null;
            if (FileBytes != null)
            {
                for (var i = 0; i < FileBytes.Length; i++)
                {
                    FileBytes[i] = 0;
                }
            }
        }

        private void OnDownloadAttachment(object commandParam)
        {
            string ext = Path.GetExtension(FileName) ?? string.Empty;

            var dialog = new SaveFileDialog
            {
                Title = "Download attachment",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                FileName = FileName,
                DefaultExt = ext,
                AddExtension = true,
                OverwritePrompt = true,
                Filter = $"Epistle Message Attachment|*{ext}"
            };

            dialog.FileOk += (sender, e) =>
            {
                if (sender is SaveFileDialog _dialog)
                {
                    File.WriteAllBytes(_dialog.FileName, FileBytes);
                }
            };

            dialog.ShowDialog();
        }

        private void OnClickedImagePreview(object commandParam)
        {
            var viewModel = new ImageViewerViewModel {ImageBytes = FileBytes};
            var view = new ImageViewerView {BindingContext = viewModel};
            view.ShowDialog();
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

        private bool HasAttachment()
        {
            return FileName.NotNullNotEmpty() && FileBytes != null && FileBytes.Length > 0;
        }

        private bool IsImage()
        {
            if (!HasAttachment())
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
            if (!HasAttachment())
            {
                return false;
            }

            return FileName.EndsWith(".gif", true, CultureInfo.InvariantCulture);
        }

        private bool IsAudio()
        {
            if (!HasAttachment())
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