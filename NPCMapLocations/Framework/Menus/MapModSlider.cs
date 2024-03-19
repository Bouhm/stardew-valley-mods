using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace NPCMapLocations.Framework.Menus
{
    // Mod slider for heart level this.Config
    internal class MapModSlider : OptionsElement
    {
        /*********
        ** Fields
        *********/
        private readonly string ValueLabel;

        /// <summary>The minimum value that can be selected.</summary>
        private readonly int Max;

        /// <summary>The maximum value that can be selected.</summary>
        private readonly int Min;

        /// <summary>Set the new option value.</summary>
        private readonly Action<int> SetValue;

        /// <summary>Get whether the option should be grayed out and disabled.</summary>
        private readonly Func<bool> ShouldGrayOut;

        /// <summary>The currently selected value.</summary>
        private float Value;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="label">The display text.</param>
        /// <param name="value">The initial option value.</param>
        /// <param name="min">The minimum value that can be selected.</param>
        /// <param name="max">The maximum value that can be selected.</param>
        /// <param name="set">Set the new option value.</param>
        /// <param name="shouldGrayOut">Get whether the option should be grayed out and disabled.</param>
        public MapModSlider(string label, int value, int min, int max, Action<int> set, Func<bool> shouldGrayOut)
            : base(label, -1, -1, 48 * Game1.pixelZoom, 6 * Game1.pixelZoom, 1)
        {
            this.ValueLabel = label;
            this.Min = min;
            this.Max = max;
            this.SetValue = set;
            this.ShouldGrayOut = shouldGrayOut;

            this.Value = value;
        }

        public override void leftClickHeld(int x, int y)
        {
            if (this.greyedOut)
                return;

            base.leftClickHeld(x, y);
            this.Value = x >= this.bounds.X
                ? (x <= this.bounds.Right - 10 * Game1.pixelZoom
                    ? (int)((x - this.bounds.X) / (float)(this.bounds.Width - 10 * Game1.pixelZoom) * this.Max)
                    : this.Max
                )
                : 0;

            this.SetValue((int)this.Value);
        }

        public override void receiveLeftClick(int x, int y)
        {
            if (this.greyedOut) return;

            base.receiveLeftClick(x, y);
            this.leftClickHeld(x, y);
        }

        public override void draw(SpriteBatch b, int slotX, int slotY, IClickableMenu context = null)
        {
            this.label = this.ValueLabel + ": " + this.Value;
            this.greyedOut = this.ShouldGrayOut();

            base.draw(b, slotX, slotY, context);
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, OptionsSlider.sliderBGSource, slotX + this.bounds.X, slotY + this.bounds.Y, this.bounds.Width, this.bounds.Height, Color.White, Game1.pixelZoom, false);
            b.Draw(Game1.mouseCursors,
                new Vector2(
                    slotX + this.bounds.X + (this.bounds.Width - 10 * Game1.pixelZoom) * (this.Value / this.Max),
                    slotY + this.bounds.Y
                ),
                OptionsSlider.sliderButtonRect, Color.White, 0f, Vector2.Zero, Game1.pixelZoom,
                SpriteEffects.None, 0.9f
            );
        }
    }
}
