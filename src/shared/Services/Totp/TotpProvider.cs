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

using OtpNet;
using System;
using System.Threading.Tasks;
using GlitchedPolygons.ExtensionMethods;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Totp
{
    /// <summary>
    /// Time-based One-Time Password provider.
    /// </summary>
    public class TotpProvider : ITotpProvider
    {
        /// <summary>
        /// Pass in a valid Base-32 encoded TOTP-Secret; receive a valid 2FA token.<para> </para>
        /// If the token would be closer than 2 seconds to expiry, the method waits for a fresh one to avoid authentication failures due to latency.
        /// </summary>
        /// <param name="totpSecret">The Base-32 encoded secret to use for TOTP generation. If this is <c>null</c> or empty, <c>null</c> is returned.</param>
        /// <returns>The 6-cipher string containing the TOTP; <c>null</c> if generation failed for some reason (e.g. invalid <paramref name="totpSecret"/> parameter).</returns>
        public async Task<string> GetTotp(string totpSecret)
        {
            if (totpSecret.NullOrEmpty())
            {
                return null;
            }

            try
            {
                var totp = new OtpNet.Totp(Base32Encoding.ToBytes(totpSecret));

                if (totp.RemainingSeconds() < 2)
                {
                    await Task.Delay(1250);
                }

                return totp.ComputeTotp();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
