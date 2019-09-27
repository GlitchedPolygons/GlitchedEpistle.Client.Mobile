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

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization
{
    /// <summary>
	/// Helper class for splitting locales like for example:<para> </para>
	///   iOS: it_CH <para> </para>
	///   Android: it-CH <para> </para>
	/// into parts that are more suitable for the creation of a .NET culture (or fallback culture).
	/// </summary>
    public class PlatformCulture
    {
        /// <summary>
        /// The compliant culture identifier (e.g. "it-CH").
        /// </summary>
        public string PlatformString { get; private set; }

        /// <summary>
        /// In the case of "it-CH" this would be the "it".
        /// </summary>
        public string LanguageCode { get; private set; }

        /// <summary>
        /// In the case of "it-CH" this would be the "CH".
        /// </summary>
        public string LocaleCode { get; private set; }

        /// <summary>
        /// Creates a new .NET compliant <see cref="PlatformCulture"/> instance 
        /// from a platform-specific culture string. <para> </para>
        /// iOS uses underscores as language/locale separator; Android uses dashes. Lovely.
        /// </summary>
        /// <param name="platformCultureString"></param>
        public PlatformCulture(string platformCultureString)
        {
            if (string.IsNullOrEmpty(platformCultureString))
            {
                throw new ArgumentException("Expected a valid culture identifier!", nameof(platformCultureString));
            }

            PlatformString = platformCultureString.Replace("_", "-"); // .NET expects a dash, not an underscore.

            var dashIndex = PlatformString.IndexOf("-", StringComparison.Ordinal);
            if (dashIndex > 0)
            {
                string[] parts = PlatformString.Split('-');

                if (parts.Length != 2)
                {
                    throw new ArgumentException("Invalid culture identifier (multiple dashes/underscores detected; only one allowed).", nameof(platformCultureString));
                }

                LanguageCode = parts[0];
                LocaleCode = parts[1];
            }
            else
            {
                LocaleCode = string.Empty;
                LanguageCode = PlatformString;
            }
        }

        /// <summary>
        /// Returns the <see cref="PlatformCulture"/>'s <see cref="PlatformString"/>.
        /// </summary>
        /// <returns>The <see cref="PlatformCulture"/>'s <see cref="PlatformString"/>.</returns>
        public override string ToString()
        {
            return this;
        }

        /// <summary>
        /// Stringifies the <see cref="PlatformCulture"/> instance by returning its <see cref="PlatformString"/>.
        /// </summary>
        /// <param name="c">The <see cref="PlatformCulture"/> instance to convert to <c>string</c>.</param>
        public static implicit operator string(PlatformCulture c)
        {
            return c.PlatformString;
        }
    }
}
