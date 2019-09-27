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

using Foundation;
using Xamarin.Forms;
using System.Threading;
using System.Globalization;
using System.Collections.Generic;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;

[assembly: Dependency(typeof(GlitchedEpistle.Client.Mobile.iOS.Services.Localization.iOSLocalization))]

namespace GlitchedEpistle.Client.Mobile.iOS.Services.Localization
{
    /// <summary>
    /// Localization provider for iOS.
    /// </summary>
    public class iOSLocalization : ILocalization
    {
        private readonly IDictionary<string, CultureInfo> cachedCultures = new Dictionary<string, CultureInfo>(16)
        {
            { "en", new CultureInfo("en") }, // English.
            { "en-GB", new CultureInfo("en-GB") }, // English (traditional).
            { "en-US", new CultureInfo("en-US") }, // English (simplified).
            { "de", new CultureInfo("de") }, // German.
            { "de-DE", new CultureInfo("de-DE") }, // German from Germany.
            { "de-CH", new CultureInfo("de-CH") }, // German from Switzerland.
            { "gsw", new CultureInfo("gsw") }, // Swiss German from Switzerland.
            { "gsw-CH", new CultureInfo("gsw-CH") },
            { "it", new CultureInfo("it") }, // Italian.
            { "it-IT", new CultureInfo("it-IT") }, // Italian from Italy.
            { "it-CH", new CultureInfo("it-CH") }, // Italian from Ticino.
        };

        /// <summary>
        /// Sets the <see cref="CultureInfo"/> for this app.
        /// </summary>
        /// <param name="ci">The target <see cref="CultureInfo"/> to apply.</param>
        public void SetCurrentCultureInfo(CultureInfo ci)
        {
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;
        }

        /// <summary>
        /// Gets the current device's preferred <see cref="CultureInfo"/>.
        /// </summary>
        public CultureInfo GetCurrentCultureInfo()
        {
            string dotnetLanguage = "en";

            if (NSLocale.PreferredLanguages.Length > 0)
            {
                dotnetLanguage = iOSToDotnetLanguage(NSLocale.PreferredLanguages[0]);
            }

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
        /// Converts an iOS locale to a .NET compliant culture identifier <c>string</c>.
        /// </summary>
        /// <param name="iOSLanguage">iOS locale to convert.</param>
        /// <returns>The converted .NET locale <c>string</c>.</returns>
        string iOSToDotnetLanguage(string iOSLanguage)
        {
            var dotnetLanguage = iOSLanguage?.Replace("_", "-");

            //certain languages need to be converted to CultureInfo equivalent
            switch (iOSLanguage)
            {
                case "ms-MY":   // "Malaysian (Malaysia)" not supported .NET culture
                case "ms-SG":   // "Malaysian (Singapore)" not supported .NET culture
                    dotnetLanguage = "ms"; // closest supported
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
        string ToDotnetFallbackLanguage(PlatformCulture platformCulture)
        {
            var dotnetLanguage = platformCulture.LanguageCode; // Use the first part of the identifier (two chars, usually).

            switch (platformCulture.LanguageCode)
            {
                case "pt":
                    dotnetLanguage = "pt-PT"; // Plain portuguese falls back to Portuguese (Portugal)
                    break;
                    // Add more application-specific cases here (if required) and
                    // ONLY use cultures that have been tested and known to work!
            }

            return dotnetLanguage;
        }
    }
}
