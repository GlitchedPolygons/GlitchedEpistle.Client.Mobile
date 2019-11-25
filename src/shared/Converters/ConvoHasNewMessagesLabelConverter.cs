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

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using System;
using System.Globalization;

using GlitchedPolygons.ExtensionMethods;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Converters
{
    public class ConvoHasNewMessagesLabelConverter : IValueConverter, IMarkupExtension
    {
        /// <summary>
        /// Don't use this directly! Use <see cref="Localization"/> for the included <c>null</c> check!
        /// </summary>
        private ILocalization _localization;

        /// <summary>
        /// Localizer service.
        /// </summary>
        public ILocalization Localization => _localization ?? (_localization = DependencyService.Get<ILocalization>());

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string convoId;
            if ((convoId = value as string).NullOrEmpty()) return false;
            return (Application.Current as App)?.HasNewMessages(convoId) ?? false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}