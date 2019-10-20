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
using FFImageLoading.Forms;
using FFImageLoading.Transformations;
using GlitchedPolygons.Services.MethodQ;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ConvosPage : ContentPage
    {
        private readonly IMethodQ methodQ = new MethodQ();

        public ConvosPage()
        {
            InitializeComponent();
        }

        private void HeaderButtonTapGestureRecognizer_OnTapped(object sender, EventArgs e)
        {
            var cachedImage = sender as CachedImage;
            if (cachedImage is null) return;

            var _ = cachedImage.Transformations[0];
            cachedImage.Transformations.Clear();
            cachedImage.Transformations.Add(new TintTransformation("#00b4dd") {EnableSolidColor = true});
            cachedImage.ReloadImage();
            
            methodQ.Schedule(() =>
            {
                cachedImage.Transformations.Clear();
                cachedImage.Transformations.Add(_);
                cachedImage.ReloadImage();
            }, DateTime.UtcNow.AddSeconds(0.2));
        }

        private void LogoutHeaderButtonTapGestureRecognizer_OnTapped(object sender, EventArgs e)
        {
            var cachedImage = sender as CachedImage;
            if (cachedImage is null) return;

            var _ = cachedImage.Transformations[0];
            cachedImage.Transformations.Clear();
            cachedImage.Transformations.Add(new TintTransformation("#cc0000") {EnableSolidColor = true});
            cachedImage.ReloadImage();

            methodQ.Schedule(() =>
            {
                cachedImage.Transformations.Clear();
                cachedImage.Transformations.Add(_);
                cachedImage.ReloadImage();
            }, DateTime.UtcNow.AddSeconds(0.25));
        }
    }
}
