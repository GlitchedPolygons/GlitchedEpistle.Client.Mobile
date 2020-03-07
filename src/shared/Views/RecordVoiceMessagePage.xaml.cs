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

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using FFImageLoading.Transformations;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels.Interfaces;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RecordVoiceMessagePage : ContentPage
    {
        private readonly TintTransformation tint;
        
        public RecordVoiceMessagePage()
        {
            InitializeComponent();
            
            Application.Current.Resources.TryGetValue("HeaderButtonIdleColorHex", out var idleColorHex);
            tint = new TintTransformation(idleColorHex?.ToString() ?? "#ffffff") { EnableSolidColor = true };
        }
        
        protected override void OnAppearing()
        {
            base.OnAppearing();
            (BindingContext as IOnAppearingListener)?.OnAppearing();
            
            PlayIconCachedImage.Transformations.Add(tint);
            PlayIconCachedImage.ReloadImage();
            
            PauseIconCachedImage.Transformations.Add(tint);
            PauseIconCachedImage.ReloadImage();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            (BindingContext as IOnDisappearingListener)?.OnDisappearing();
        }
    }
}
