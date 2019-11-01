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
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Alerts;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;
using Xamarin.Forms.Xaml;
using Rg.Plugins.Popup.Pages;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Views.Popups
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ParticipantContextMenuPopupPage : PopupPage
    {
        private readonly string userId;
        private readonly IAlertService alertService;
        private readonly ILocalization localization;
        
        // TODO: define some sort of result to give back to whoever opened this popup page
        
        public ParticipantContextMenuPopupPage(string userId)
        {
            this.userId = userId;
            alertService = DependencyService.Get<IAlertService>();
            localization = DependencyService.Get<ILocalization>();
            
            InitializeComponent();
        }

        private void CancelButton_OnClicked(object sender, EventArgs e)
        {
            // TODO: implement this
            throw new NotImplementedException();
        }
        
        private void CopyButton_OnClicked(object sender, EventArgs e)
        {
            Clipboard.SetTextAsync(userId);
            alertService.AlertShort(localization["Copied"]);
        }
        
        private void MakeAdmin_OnClicked(object sender, EventArgs e)
        {
            // TODO: implement this
            throw new NotImplementedException();
        }
        
        private void KickAndBan_OnClicked(object sender, EventArgs e)
        {
            // TODO: implement this
            throw new NotImplementedException();
        }
    }
}
