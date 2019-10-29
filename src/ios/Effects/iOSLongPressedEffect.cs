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

using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using GlitchedEpistle.Client.Mobile.iOS.Effects;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Effects;

[assembly: ResolutionGroupName("GlitchedPolygons.GlitchedEpistle.Client.Mobile.Effects")]
[assembly: ExportEffect(typeof(iOSLongPressedEffect), "LongPressedEffect")]

namespace GlitchedEpistle.Client.Mobile.iOS.Effects
{
    /// <summary>
    /// Long pressed effect on iOS.
    /// </summary>
    public class iOSLongPressedEffect : PlatformEffect
    {
        private bool attached;
        private readonly UILongPressGestureRecognizer longPressRecognizer;

        public iOSLongPressedEffect()
        {
            longPressRecognizer = new UILongPressGestureRecognizer(HandleLongClick);
        }

        protected override void OnAttached()
        {
            if (!attached)
            {
                Container.AddGestureRecognizer(longPressRecognizer);
                attached = true;
            }
        }

        /// <summary>
        /// Invoke the command if there is one.
        /// </summary>
        private void HandleLongClick()
        {
            var command = LongPressedEffect.GetCommand(Element);
            command?.Execute(LongPressedEffect.GetCommandParameter(Element));
        }

        /// <summary>
        /// Clean the event handler on detach.
        /// </summary>
        protected override void OnDetached()
        {
            if (attached)
            {
                Container.RemoveGestureRecognizer(longPressRecognizer);
                attached = false;
            }
        }
    }
}
