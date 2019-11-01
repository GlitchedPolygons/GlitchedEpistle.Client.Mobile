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
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Xamarin.Essentials;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Alerts;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Views.Popups
{
    public enum ParticipantContextAction : int
    {
        None = 0,
        MakeAdmin = 1,
        KickAndBan = 2
    }
    
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ParticipantContextMenuPopupPage : PopupPage
    {
        private readonly string userId;
        private readonly IAlertService alertService;
        private readonly ILocalization localization;

        public ParticipantContextAction Result { get; set; } = ParticipantContextAction.None;
        
        public ParticipantContextMenuPopupPage(string userId, bool asAdmin)
        {
            this.userId = userId;
            
            alertService = DependencyService.Get<IAlertService>();
            localization = DependencyService.Get<ILocalization>();
            
            InitializeComponent();

            MakeAdminButton.IsEnabled = MakeAdminButton.IsVisible = asAdmin;
            KickAndBanButton.IsEnabled = KickAndBanButton.IsVisible = asAdmin;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ParticipantContextMenuDescriptionLabel.Text = $"{localization["User"]}: {userId}";
        }

        private async void CancelButton_OnClicked(object sender, EventArgs e)
        {
            Result = ParticipantContextAction.None;
            await PopupNavigation.Instance.PopAsync();
        }
        
        private void CopyButton_OnClicked(object sender, EventArgs e)
        {
            Clipboard.SetTextAsync(userId);
            alertService.AlertShort(localization["Copied"]);
        }
        
        private async void MakeAdmin_OnClicked(object sender, EventArgs e)
        {
            Result = ParticipantContextAction.MakeAdmin;
            await PopupNavigation.Instance.PopAsync();
        }
        
        private async void KickAndBan_OnClicked(object sender, EventArgs e)
        {
            Result = ParticipantContextAction.KickAndBan;
            await PopupNavigation.Instance.PopAsync();
        }
    }
}
