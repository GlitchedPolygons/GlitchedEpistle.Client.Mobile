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

using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Effects;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Android.Effects;

using View = Android.Views.View;

[assembly: ResolutionGroupName("GlitchedPolygons.GlitchedEpistle.Client.Mobile.Effects")]
[assembly: ExportEffect(typeof(AndroidLongPressedEffect), "LongPressedEffect")]

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile.Android.Effects
{
    /// <summary>
    /// Android long pressed effect.
    /// </summary>
    public class AndroidLongPressedEffect : PlatformEffect
    {
        private bool attached;

        /// <summary>
        /// Initializer to avoid linking out.
        /// </summary>
        public static void Initialize()
        {
        }

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="AndroidLongPressedEffect"/> class.
        /// Empty constructor required for the Xamarin.Forms reflection constructor search.
        /// </summary>
        public AndroidLongPressedEffect()
        {
        }

        /// <summary>
        /// Apply the handler.
        /// </summary>
        protected override void OnAttached()
        {
            // Because an effect can be detached immediately after being attached
            // (happens in list views), only attach the handler one time.
            if (attached)
                return;

            if (Control != null)
            {
                Control.LongClickable = true;
                Control.LongClick += Control_LongClick;
            }
            else
            {
                Container.LongClickable = true;
                Container.LongClick += Control_LongClick;
            }

            attached = true;
        }

        /// <summary>
        /// Invoke the command if there is one.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event arguments.</param>
        private void Control_LongClick(object sender, View.LongClickEventArgs e)
        {
            var command = LongPressedEffect.GetCommand(Element);
            command?.Execute(LongPressedEffect.GetCommandParameter(Element));
        }

        /// <summary>
        /// Clean the event handler on detach
        /// </summary>
        protected override void OnDetached()
        {
            if (!attached)
                return;

            if (Control != null)
            {
                Control.LongClickable = true;
                Control.LongClick -= Control_LongClick;
            }
            else
            {
                Container.LongClickable = true;
                Container.LongClick -= Control_LongClick;
            }

            attached = false;
        }
    }
}
