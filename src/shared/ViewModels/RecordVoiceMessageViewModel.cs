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
using System;
using System.Windows.Input;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Commands;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels.Interfaces;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Logging;
using GlitchedPolygons.Services.MethodQ;
using Plugin.SimpleAudioRecorder;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels
{
    public class RecordVoiceMessageViewModel : ViewModel, IOnAppearingListener, IOnDisappearingListener
    {
        public AudioRecording Result { get; set; }

        #region Constants

        private readonly ILogger logger;
        private readonly IMethodQ methodQ;
        private readonly ILocalization localization;
        private readonly ISimpleAudioRecorder audioRecorder;

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

        #endregion

        private ulong? counter = null;
        private volatile int seconds = 0;

        public RecordVoiceMessageViewModel(ILogger logger, IMethodQ methodQ)
        {
            this.logger = logger;
            this.methodQ = methodQ;

            localization = DependencyService.Get<ILocalization>();
            audioRecorder = CrossSimpleAudioRecorder.CreateSimpleAudioRecorder();

            SendCommand = new DelegateCommand(OnSend);
            ResetCommand = new DelegateCommand(OnReset);
            CancelCommand = new DelegateCommand(OnCancel);
            StartRecordingCommand = new DelegateCommand(OnStartRecording);
            StopRecordingCommand = new DelegateCommand(OnStopRecording);
            AudioThumbDraggedCommand = new DelegateCommand(OnAudioThumbDragged);
            ClickedOnPlayAudioAttachmentCommand = new DelegateCommand(OnClickedOnPlay);
        }

        public async void OnAppearing()
        {
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
        }

        private void OnClickedOnPlay(object commandParam)
        {
            throw new NotImplementedException();
        }

        private void OnAudioThumbDragged(object commandParam)
        {
            throw new NotImplementedException();
        }

        private void OnStartRecording(object commandParam)
        {
            StopCounter();
            StartCounter();
            audioRecorder.RecordAsync();
            IsRecording = true;
        }

        private void OnStopRecording(object commandParam)
        {
            StopCounter();
            audioRecorder.StopAsync();
            Done = true;
            IsRecording = false;
        }

        private void OnReset(object commandParam)
        {
            StopCounter();
        }

        private void OnSend(object commandParam)
        {
            throw new NotImplementedException();
        }

        private void OnCancel(object commandParam)
        {
            throw new NotImplementedException();
        }

        private void StopCounter()
        {
            if (counter.HasValue)
            {
                methodQ.Cancel(counter.Value);
                counter = null;
            }
        }

        private void StartCounter()
        {
            counter = methodQ.Schedule(() => Duration = TimeSpan.FromSeconds(++seconds).ToString(@"mm\:ss"), TimeSpan.FromSeconds(1));
        }
    }
}