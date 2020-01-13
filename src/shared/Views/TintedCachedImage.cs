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
using FFImageLoading.Work;
using FFImageLoading.Forms;
using FFImageLoading.Transformations;
using System.Collections.Generic;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Views
{
    public class TintedCachedImage : CachedImage
    {
        public static BindableProperty TintColorProperty = BindableProperty.Create(nameof(TintColor), typeof(Color), typeof(TintedCachedImage), Color.Transparent, propertyChanged: UpdateColor);

        public Color TintColor
        {
            get => (Color) GetValue(TintColorProperty);
            set => SetValue(TintColorProperty, value);
        }

        private static void UpdateColor(BindableObject bindable, object oldColor, object newColor)
        {
            Color oldcolor = (Color)oldColor;
            Color newcolor = (Color)newColor;

            if (!oldcolor.Equals(newcolor))
            {
                var view = (TintedCachedImage) bindable;
                var transformations = new List<ITransformation>()
                {
                    new TintTransformation((int)(newcolor.R * 255), (int)(newcolor.G * 255), (int)(newcolor.B * 255), (int)(newcolor.A * 255))
                    {
                        EnableSolidColor = true
                    }
                };
                view.Transformations = transformations;
            }
        }
    }
}
