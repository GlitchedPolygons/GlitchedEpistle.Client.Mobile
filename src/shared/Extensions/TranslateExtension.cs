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
using System.Resources;
using System.Reflection;
using System.Globalization;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Extensions
{
    [ContentProperty("Text")]
    public class TranslateExtension : IMarkupExtension
    {
        private readonly CultureInfo ci = null;
        private const string RESOURCE_ID = "GlitchedPolygons.GlitchedEpistle.Client.Mobile.Resources.LocalizedStrings";

        private static readonly Lazy<ResourceManager> RESOURCES = new Lazy<ResourceManager>(() => new ResourceManager(RESOURCE_ID, IntrospectionExtensions.GetTypeInfo(typeof(TranslateExtension)).Assembly));

        public string Text { get; set; }

        public TranslateExtension()
        {
            if (Device.RuntimePlatform == Device.iOS || Device.RuntimePlatform == Device.Android)
            {
                ci = DependencyService.Get<ILocalization>().GetCurrentCultureInfo();
            }
        }

        public object ProvideValue(IServiceProvider serviceProvider)
        {
            if (Text == null)
            {
                return string.Empty;
            }

            var translation = RESOURCES.Value.GetString(Text, ci);
            if (translation == null)
            {
                translation = Text;
#if DEBUG
                Console.WriteLine(string.Format("Key '{0}' was not found in resources '{1}' for culture '{2}'.", Text, RESOURCE_ID, ci.Name));
#endif
            }

            return translation;
        }
    }
}
