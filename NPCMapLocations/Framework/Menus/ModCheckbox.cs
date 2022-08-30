using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace NPCMapLocations.Framework.Menus
{
    /// <summary>A checkbox nod option.</summary>
    internal class ModCheckbox : OptionsElement
    {
        /*********
        ** Fields
        *********/
        /// <summary>Get the current option value.</summary>
        private readonly Func<bool> GetValue;

        /// <summary>Set the new option value.</summary>
        private readonly Action<bool> SetValue;

        /// <summary>Update the configuration after the checkbox value changes.</summary>
        private readonly Action UpdateConfig;

        /// <summary>The NPC marker and name to draw, if applicable.</summary>
        private readonly NpcMarker NpcMarker;

        /// <summary>Whether the checkbox is toggled true.</summary>
        private bool IsChecked;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="label">The display text.</param>
        /// <param name="getValue">Get the current option value.</param>
        /// <param name="setValue">Set the new option value.</param>
        /// <param name="updateConfig">Update the configuration after the checkbox value changes.</param>
        /// <param name="npcMarker">The NPC marker and name to draw, if applicable.</param>
        public ModCheckbox(string label, Func<bool> getValue, Action<bool> setValue, Action updateConfig, NpcMarker npcMarker = null)
            : base(label, -1, -1, 9 * Game1.pixelZoom, 9 * Game1.pixelZoom, 0)
        {
            this.GetValue = getValue;
            this.SetValue = setValue;
            this.UpdateConfig = updateConfig;
            this.NpcMarker = npcMarker;

            this.IsChecked = getValue();
        }

        /// <inheritdoc />
        public override void receiveLeftClick(int x, int y)
        {
            if (this.greyedOut)
                return;

            base.receiveLeftClick(x, y);

            this.SetValue(!this.IsChecked);
            this.IsChecked = this.GetValue();

            this.UpdateConfig();
        }

        /// <inheritdoc />
        public override void draw(SpriteBatch b, int slotX, int slotY, IClickableMenu context = null)
        {
            b.Draw(Game1.mouseCursors, new Vector2(slotX + this.bounds.X, slotY + this.bounds.Y), this.IsChecked ? OptionsCheckbox.sourceRectChecked : OptionsCheckbox.sourceRectUnchecked, Color.White * (this.greyedOut ? 0.33f : 1f), 0f, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, 0.4f);

            if (this.NpcMarker != null)
            {
                var marker = this.NpcMarker;

                // draw icon
                Color color = this.IsChecked
                    ? Color.White
                    : Color.White * 0.33f;
                Game1.spriteBatch.Draw(marker.Sprite, new Vector2((float)slotX + this.bounds.X + 50, slotY), new Rectangle(0, marker.CropOffset, 16, 15), color, 0f, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, 0.4f);

                // Draw name
                slotX += 75;
                Utility.drawTextWithShadow(b, marker.DisplayName, Game1.dialogueFont, new Vector2(slotX + this.bounds.X + this.bounds.Width + 8, slotY + this.bounds.Y), this.greyedOut ? Game1.textColor * 0.33f : Game1.textColor, 1f, 0.1f);
            }
            else
            {
                base.draw(b, slotX, slotY, context);
            }
        }
    }
}
