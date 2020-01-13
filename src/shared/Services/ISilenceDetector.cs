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

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services
{
    /// <summary>
    /// Dependency service for finding out if audio playback is eligible
    /// (if audio has been muted on the current device altogether).
    /// </summary>
    public interface ISilenceDetector
    {
        /// <summary>
        /// Checks if audio has been silenced on the current device.
        /// </summary>
        /// <returns>Whether audio has been silenced on the current device or not.</returns>
        bool IsAudioMuted();
    }
}
