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

using System.IO;
using System.Globalization;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Logging;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Settings;
using Xamarin.Essentials;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Settings
{
    using Paths = GlitchedPolygons.GlitchedEpistle.Client.Mobile.Constants.Paths;
    
    /// <summary>
    /// Application-level settings that persist between user accounts.
    /// </summary>
    /// <seealso cref="ISettings"/>
    public class AppSettingsJson : SettingsJson, IAppSettings
    {
        public AppSettingsJson(ILogger logger) : base(logger, Path.Combine(Paths.ROOT_DIRECTORY, "Settings.json"))
        {
        }

        /// <summary>
        /// The Epistle client version.
        /// </summary>
        public string ClientVersion => this["Version", App.Version];

        /// <summary>
        /// The Epistle server URL to connect to.
        /// </summary>
        public string ServerUrl
        {
            get => this["ServerUrl"];
            set => this["ServerUrl"] = value;
        }

        /// <summary>
        /// The last used user id.
        /// </summary>
        public string LastUserId
        {
            get => this["LastUserId"];
            set => this["LastUserId"] = value;
        }

        /// <summary>
        /// If available, should the fingerprint reader be used for logging in the user easily?
        /// </summary>
        public bool UseFingerprint
        {
            get => this["UseFingerprint", false];
            set => this["UseFingerprint"] = value.ToString();
        }

        /// <summary>
        /// Should convo passwords be saved for easy access? No worries: they're saved in <see cref="SecureStorage"/>.
        /// </summary>
        public bool SaveConvoPasswords
        {
            get => this["SaveConvoPasswords", true];
            set => this["SaveConvoPasswords"] = value.ToString();
        }
        
        /// <summary>
        /// Should user login passwords be saved for easy login? No worries: they're saved in <see cref="SecureStorage"/>.
        /// </summary>
        public bool SaveUserPassword
        {
            get => this["SaveUserPassword", true];
            set => this["SaveUserPassword"] = value.ToString();
        }

        /// <summary>
        /// Should the TOTP-Secret that is needed to generate 2FA tokens
        /// for server request authentication be securely saved for automation purposes?
        /// </summary>
        public bool SaveTotpSecret
        {
            get => this["SaveTotpSecret", false];
            set => this["SaveTotpSecret"] = value.ToString();
        }

        /// <summary>
        /// The application's language setting.
        /// Needs to be a <see cref="CultureInfo"/>-valid culture identifier (such as "en" for English).
        /// </summary>
        public string Language
        {
            get => this["Language", "en"];
            set => this["Language"] = value;
        }
    }
}
