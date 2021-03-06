﻿/*
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
using System.Windows.Input;
using GlitchedPolygons.ExtensionMethods;
using GlitchedPolygons.Services.MethodQ;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Commands;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels.Interfaces;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Logging;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Settings;
using Plugin.FilePicker;
using Plugin.FilePicker.Abstractions;
using Plugin.SimpleAudioPlayer;
using Plugin.SimpleAudioRecorder;
using Xamarin.Forms;
using Xamarin.Essentials;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels
{
    public class RecordVoiceMessageViewModel : ViewModel, IOnAppearingListener, IOnDisappearingListener
    {
        public byte[] Result { get; private set; }

        #region Constants

        private readonly ILogger logger;
        private readonly IMethodQ methodQ;
        private readonly IAppSettings appSettings;
        private readonly ILocalization localization;
        private readonly ISilenceDetector silenceDetector;

        #endregion

        #region Commands

        public ICommand SendCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand StartRecordingCommand { get; }
        public ICommand StopRecordingCommand { get; }
        public ICommand AudioThumbDraggedCommand { get; }
        public ICommand ClickedOnPlayAudioAttachmentCommand { get; }

        #endregion

        #region UI Bindings

        private string duration = "00:00";
        public string Duration
        {
            get => duration;
            set => Set(ref duration, value);
        }

        private double audioThumbPos = 0.0;
        public double AudioThumbPos
        {
            get => audioThumbPos;
            set => Set(ref audioThumbPos, value < 0 ? 0 : value > 1 ? 1 : value);
        }

        private bool isRecording = false;
        public bool IsRecording
        {
            get => isRecording;
            set => Set(ref isRecording, value);
        }

        private bool isAudioPlaying = false;
        public bool IsAudioPlaying
        {
            get => isAudioPlaying;
            set => Set(ref isAudioPlaying, value);
        }

        private bool audioLoadFailed = false;
        public bool AudioLoadFailed
        {
            get => audioLoadFailed;
            set => Set(ref audioLoadFailed, value);
        }

        private bool done = false;
        public bool Done
        {
            get => done;
            set => Set(ref done, value);
        }

        private bool askForConfirmation = true;
        public bool AskForConfirmation
        {
            get => askForConfirmation;
            set => Set(ref askForConfirmation, value);
        }

        private bool cancelButtonEnabled = true;
        public bool CancelButtonEnabled
        {
            get => cancelButtonEnabled;
            set => Set(ref cancelButtonEnabled, value);
        }

        private Color recordingCircleColor = Color.Transparent;
        public Color RecordingCircleColor
        {
            get => recordingCircleColor;
            set => Set(ref recordingCircleColor, value);
        }

        #endregion

        private ulong? counter;
        private ulong? thumbUpdater;
        private ulong? durationClamper;
        private ulong? recordingCirclePulse;
        private volatile int seconds;
        private ISimpleAudioPlayer audioPlayer;
        private ISimpleAudioRecorder audioRecorder;

        public RecordVoiceMessageViewModel(ILogger logger, IMethodQ methodQ, IAppSettings appSettings)
        {
            this.logger = logger;
            this.methodQ = methodQ;
            this.appSettings = appSettings;

            localization = DependencyService.Get<ILocalization>();
            silenceDetector = DependencyService.Get<ISilenceDetector>();
            audioRecorder = DependencyService.Get<ISimpleAudioRecorder>();

            SendCommand = new DelegateCommand(OnSend);
            ResetCommand = new DelegateCommand(OnReset);
            CancelCommand = new DelegateCommand(OnCancel);
            StartRecordingCommand = new DelegateCommand(OnStartRecording);
            StopRecordingCommand = new DelegateCommand(OnStopRecording);
            AudioThumbDraggedCommand = new DelegateCommand(OnAudioThumbDragged);
            ClickedOnPlayAudioAttachmentCommand = new DelegateCommand(OnClickedPlay);
        }

        public async void OnAppearing()
        {
            AskForConfirmation = appSettings["AskForConfirmationBeforeSendingVoiceMsg", true];

            if (!audioRecorder.CanRecordAudio)
            {
                await Application.Current.MainPage.DisplayAlert(localization["CannotRecordAudioErrorMessageTitle"], localization["CannotRecordAudioErrorMessageText"], "OK");
                OnCancel(null);
            }
        }

        public void OnDisappearing()
        {
            StopCounter();
            audioRecorder?.StopAsync();
            DeviceDisplay.KeepScreenOn = false;
            appSettings["AskForConfirmationBeforeSendingVoiceMsg"] = AskForConfirmation.ToString();
        }

        private void OnClickedPlay(object commandParam)
        {
            if (audioPlayer is null)
            {
                audioPlayer = CrossSimpleAudioPlayer.CreateSimpleAudioPlayer();
                AudioLoadFailed = !audioPlayer.Load(new MemoryStream(Result));
                audioPlayer.Loop = false;
                OnAudioThumbDragged(null);
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
                if (AudioThumbPos >= 0.99d)
                {
                    audioPlayer.Seek(0);
                }

                audioPlayer.Play();

                thumbUpdater = methodQ.Schedule(() =>
                {
                    AudioThumbPos = audioPlayer.CurrentPosition / audioPlayer.Duration;

                    if (AudioThumbPos >= 0.99d)
                    {
                        OnClickedPlay(null);
                    }
                }, TimeSpan.FromMilliseconds(420));
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

        private void OnAudioThumbDragged(object commandParam)
        {
            if (audioPlayer is null || !audioPlayer.CanSeek)
            {
                return;
            }

            audioPlayer.Seek(AudioThumbPos * audioPlayer.Duration);
        }

        private void OnStartRecording(object commandParam)
        {
            if (IsRecording)
            {
                return;
            }
            
            StopCounter();
            StartCounter();
            audioRecorder.RecordAsync();
            IsRecording = true;
            DeviceDisplay.KeepScreenOn = true;
            
            if (durationClamper.HasValue)
                methodQ.Cancel(durationClamper.Value);

            durationClamper = methodQ.Schedule(() =>
            {
                OnStopRecording(null);
                
                if (appSettings["Vibration", true] && !silenceDetector.IsAudioMuted())
                {
                    try
                    {
                        Vibration.Vibrate(TimeSpan.FromMilliseconds(120));
                    }
                    catch
                    {
                        appSettings["Vibration"] = "false";
                    }
                }
            }, DateTime.UtcNow.AddMinutes(30));
        }

        private async void OnStopRecording(object commandParam)
        {
            StopCounter();
            
            if (durationClamper.HasValue)
            {
                methodQ.Cancel(durationClamper.Value);
                durationClamper = null;
            }

            try
            {
                AudioRecording r = await audioRecorder.StopAsync();
                Result = File.ReadAllBytes(r.GetFilePath());
                Done = true;
                IsRecording = false;
            }
            catch (Exception e)
            {
                logger.LogError($"{nameof(RecordVoiceMessageViewModel)}::{nameof(OnStopRecording)}: Failed to record voice message. Thrown exception: {e.Message}");
                Reset();
            }
            DeviceDisplay.KeepScreenOn = false;
        }

        private async void OnReset(object commandParam)
        {
            if (IsAudioPlaying)
            {
                OnClickedPlay(null);
            }
            
            if (!await Application.Current.MainPage.DisplayAlert(localization["AreYouSure"], localization["ConfirmVoiceMessageReset"], localization["Yes"],localization["No"]))
            {
                return;
            }
            
            Reset();
        }

        private void Reset()
        {
            StopCounter();

            audioPlayer?.Stop();
            audioPlayer = null;

            Result = null;
            Duration = "00:00";
            Done = IsRecording = false;
            AudioThumbPos = seconds = 0;
        }

        private async void OnSend(object commandParam)
        {
            if (Result is null)
            {
                return;
            }

            if (IsAudioPlaying)
            {
                OnClickedPlay(null);
            }

            if (AskForConfirmation)
            {
                if (!await Application.Current.MainPage.DisplayAlert(localization["AreYouSure"], localization["ConfirmSendingVoiceMessageDialogText"], localization["Yes"], localization["No"]))
                {
                    return;
                }
            }

            StopCounter();

            await Application.Current.MainPage.Navigation.PopModalAsync();
        }

        private async void OnCancel(object commandParam)
        {
            if (IsAudioPlaying)
            {
                OnClickedPlay(null);
            }
            
            if (IsRecording || Done)
            {
                if (!await Application.Current.MainPage.DisplayAlert(localization["AreYouSure"], localization["ConfirmVoiceMessageCancellation"], localization["Yes"],localization["No"]))
                {
                    return;
                }
            }

            if (IsRecording)
            {
                OnStopRecording(null);
            }

            Reset();
            
            await Application.Current.MainPage.Navigation.PopModalAsync();
        }

        private void StopCounter()
        {
            if (recordingCirclePulse.HasValue)
            {
                methodQ.Cancel(recordingCirclePulse.Value);
                recordingCirclePulse = null;
            }

            RecordingCircleColor = Color.Transparent;

            if (counter.HasValue)
            {
                methodQ.Cancel(counter.Value);
                counter = null;
            }
        }

        private void StartCounter()
        {
            counter = methodQ.Schedule(() => Duration = TimeSpan.FromSeconds(++seconds).ToString(@"mm\:ss"), TimeSpan.FromMilliseconds(1000));
            recordingCirclePulse = methodQ.Schedule(() => RecordingCircleColor = RecordingCircleColor == Color.Transparent ? Color.Red : Color.Transparent, TimeSpan.FromMilliseconds(500));
        }
    }
}