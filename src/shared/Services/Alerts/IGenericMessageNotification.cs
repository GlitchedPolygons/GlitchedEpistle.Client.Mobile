﻿/*
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

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Alerts
{
    /// <summary>
    /// Responsible for showing the user some sort of "You have new messages - check them out now inside the Glitched Epistle app" message on successful message fetch.
    /// </summary>
    public interface IGenericMessageNotification
    {
        /// <summary>
        /// Displays the notification to the user (if possible).
        /// </summary>
        void Push();

        /// <summary>
        /// Cancels any pending user message notification.
        /// </summary>
        void Pop();
    }
}
