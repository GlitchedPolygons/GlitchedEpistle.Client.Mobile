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

using Java.Util;
using Xamarin.Forms;

using System;
using System.Threading;
using System.Resources;
using System.Reflection;
using System.Globalization;
using System.Collections.Generic;

using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Resources;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;

[assembly: Dependency(typeof(GlitchedPolygons.GlitchedEpistle.Client.Mobile.Droid.Services.Localization.AndroidLocalization))]

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Droid.Services.Localization
{
    /// <summary>
    /// Localization provider for Android.
    /// </summary>
    public class AndroidLocalization : ILocalization
    {
        private CultureInfo currentCulture = null;

        private const string RESOURCE_ID = "GlitchedPolygons.GlitchedEpistle.Client.Mobile.Resources.LocalizedStrings";
        private static readonly Lazy<ResourceManager> RESOURCES = new Lazy<ResourceManager>(() => new ResourceManager(RESOURCE_ID, IntrospectionExtensions.GetTypeInfo(typeof(App)).Assembly));

        private readonly IDictionary<string, CultureInfo> cachedCultures = new Dictionary<string, CultureInfo>(16)
        {
            { "en", new CultureInfo("en") }, // English.
            { "en-GB", new CultureInfo("en-GB") }, // English (traditional).
            { "en-US", new CultureInfo("en-US") }, // English (simplified).
            { "de", new CultureInfo("de") }, // German.
            { "de-DE", new CultureInfo("de-DE") }, // German from Germany.
            { "de-CH", new CultureInfo("de-CH") }, // German from Switzerland.
            { "gsw", new CultureInfo("gsw") }, // Swiss German from Switzerland.
            { "gsw-CH", new CultureInfo("gsw") },
            { "it", new CultureInfo("it") }, // Italian.
            { "it-IT", new CultureInfo("it-IT") }, // Italian from Italy.
            { "it-CH", new CultureInfo("it-CH") }, // Italian from Ticino.
        };

        /// <summary>
        /// Translates the specified <c>string</c> identifier into the target <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="key">The localization lookup key (key <c>string</c> found in the .resx file).</param>
        /// <param name="ci">The <see cref="CultureInfo"/> to localize into (if left out null, <see cref="GetCurrentCultureInfo"/> is used).</param>
        /// <returns>Hopefully, the localized <c>string</c>.</returns>
        public string this[string key, CultureInfo ci = null]
        {
            get
            {
                var culture = ci ?? GetCurrentCultureInfo();
                var translation = RESOURCES.Value.GetString(key, culture);

                if (translation == null)
                {
                    translation = key;
#if DEBUG
                    Console.WriteLine(string.Format("Key '{0}' was not found in resources '{1}' for culture '{2}'.", key, RESOURCE_ID, culture.Name));
#endif
                }

                return translation;
            }
        }

        /// <summary>
        /// Sets the <see cref="CultureInfo"/> for this app.
        /// </summary>
        /// <param name="ci">The target <see cref="CultureInfo"/> to apply.</param>
        public void SetCurrentCultureInfo(CultureInfo ci)
        {
            currentCulture = ci;
            //LocalizedStrings.Culture = ci;
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;
        }

        /// <summary>
        /// Gets the current device's preferred <see cref="CultureInfo"/>.
        /// </summary>
        public CultureInfo GetCurrentCultureInfo()
        {
            if (currentCulture != null)
            {
                return currentCulture;
            }

            string androidLocale = Locale.Default.ToString();
            string dotnetLanguage = AndroidToDotnetLanguage(androidLocale);

            if (cachedCultures.TryGetValue(dotnetLanguage, out CultureInfo ci))
            {
                return ci;
            }

            try
            {
                ci = new CultureInfo(dotnetLanguage);
                cachedCultures[dotnetLanguage] = ci;
            }
            catch (CultureNotFoundException)
            {
                // Invalid/unavailable locale (e.g. "en-ES" : English in Spain).
                // Attempt to fallback to first characters, in this case "en".
                try
                {
                    ci = new CultureInfo(ToDotnetFallbackLanguage(new PlatformCulture(dotnetLanguage)));
                    cachedCultures[dotnetLanguage] = ci;
                }
                catch (CultureNotFoundException)
                {
                    // Language not convertible to a valid .NET culture, falling back to English.
                    ci = new CultureInfo("en");
                }
            }

            return ci;
        }

        /// <summary>
        /// Converts an Android locale to a .NET compliant culture identifier <c>string</c>.
        /// </summary>
        /// <param name="androidLanguage">Android locale to convert.</param>
        /// <returns>The converted .NET locale <c>string</c>.</returns>
        private string AndroidToDotnetLanguage(string androidLanguage)
        {
            string dotnetLanguage = androidLanguage?.Replace("_", "-");

            // Certain languages need to be converted to their CultureInfo equivalent.
            switch (dotnetLanguage)
            {
                case "ms-BN":   // "Malaysian (Brunei)" is not a supported .NET culture.
                case "ms-MY":   // "Malaysian (Malaysia)" is not a supported .NET culture.
                case "ms-SG":   // "Malaysian (Singapore)" is not a supported .NET culture.
                    dotnetLanguage = "ms"; // The closest supported equivalent is just plain "ms".
                    break;
                case "in-ID":  // "Indonesian (Indonesia)" has a different code in .NET
                    dotnetLanguage = "id-ID";
                    break;
                    // Add more application-specific cases here (if required) and
                    // ONLY use cultures that have been tested and known to work!
            }

            return dotnetLanguage;
        }

        /// <summary>
        /// Define the .NET default fallback as the first part of the culture identifier, 
        /// which usually is the two characters that define the language.
        /// </summary>
        /// <param name="platformCulture">The <see cref="PlatformCulture"/> to fallback from.</param>
        /// <returns>The fallback culture <c>string</c>.</returns>
        private string ToDotnetFallbackLanguage(PlatformCulture platformCulture)
        {
            string dotnetLanguage = platformCulture.LanguageCode;

            switch (dotnetLanguage)
            {
                case "pt":
                    dotnetLanguage = "pt-PT"; // Plain portuguese falls back to Portuguese (Portugal)
                    break;

                /* Xamarin example code commented out, because Schwiizerdütsch eyfach nid s gliche isch we hoochdüttsch.
                case "gsw":
                    netLanguage = "de-CH"; // equivalent to German (Switzerland) for this app
                    break;
                */

                    // Add more application-specific cases here (if required) and
                    // ONLY use cultures that have been tested and known to work!
            }

            return dotnetLanguage;
        }
    }
}
