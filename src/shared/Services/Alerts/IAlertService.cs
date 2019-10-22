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

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Alerts
{
    /// <summary>
    /// Alert service for displaying quick and concise messages
    /// to the user as an overlay label (e.g. Toast on Android).
    /// </summary>
    public interface IAlertService
    {
        /// <summary>
        /// Shows a small alert message overlay to the user for a short amount of time.
        /// </summary>
        /// <param name="message"></param>
        void AlertShort(string message);
        
        /// <summary>
        /// Shows a medium-sized alert message overlay to the user for a moderate amount of time.
        /// </summary>
        /// <param name="message">The message <c>string</c> to display to the user.</param>
        void AlertLong(string message);
    }
}
