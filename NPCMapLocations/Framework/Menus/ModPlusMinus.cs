using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

namespace NPCMapLocations.Framework.Menus
{
    public class ModPlusMinus : OptionsElement
    {
        /*********
        ** Fields
        *********/
        private static readonly Rectangle MinusButtonSource = new(177, 345, 7, 8);
        private static readonly Rectangle PlusButtonSource = new(184, 345, 7, 8);
        private readonly int TxtSize;
        private Rectangle MinusButton;
        private Rectangle PlusButton;
        private static bool SnapZoomPlus;
        private static bool SnapZoomMinus;

        /// <summary>The minimum value that can be selected.</summary>
        private readonly int MinValue;

        /// <summary>The maximum value that can be selected.</summary>
        private readonly int MaxValue;

        /// <summary>The delta to apply when clicking the plus/minus buttons.</summary>
        private readonly int Step;

        /// <summary>Set the new option value.</summary>
        private readonly Action<int> SetValue;

        /// <summary>Format the value shown between the plus/minus buttons.</summary>
        private readonly Func<int, string> FormatValue;

        /// <summary>Get whether the option should be grayed out and disabled.</summary>
        private readonly Func<bool> ShouldGrayOut;

        /// <summary>The currently selected value.</summary>
        private int Value;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="label">The display text.</param>
        /// <param name="value">The initial option value.</param>
        /// <param name="min">The minimum value that can be selected.</param>
        /// <param name="max">The maximum value that can be selected.</param>
        /// <param name="step">The delta to apply when clicking the plus/minus buttons.</param>
        /// <param name="set">Set the new option value.</param>
        /// <param name="format">Format the value shown between the plus/minus buttons.</param>
        /// <param name="shouldGrayOut">Get whether the option should be grayed out and disabled.</param>
        public ModPlusMinus(string label, int value, int min, int max, int step, Action<int> set, Func<int, string> format, Func<bool> shouldGrayOut)
            : base(label, -1, 1, 28, 28, 1)
        {
            this.MinValue = min;
            this.MaxValue = max;
            this.Step = step;
            this.SetValue = set;
            this.FormatValue = format;
            this.ShouldGrayOut = shouldGrayOut;

            this.TxtSize = Math.Max(
                (int)Game1.dialogueFont.MeasureString("options[0]").X + 28,
                (int)Game1.dialogueFont.MeasureString(format(max)).X + 28
            );

            this.bounds = new Rectangle(this.bounds.X, this.bounds.Y, (int)(1.5 * this.TxtSize), 32);
            this.MinusButton = new Rectangle(this.bounds.X, 16, 28, 32);
            this.PlusButton = new Rectangle(this.bounds.Right - 96, 16, 28, 32);

            this.Value = value;
        }

        public override void receiveLeftClick(int x, int y)
        {
            if (!this.greyedOut)
            {
                // minus button
                if (this.MinusButton.Contains(x, y) && this.Value > this.MinValue)
                {
                    this.ApplyStep(-this.Step);
                    this.SetValue(this.Value);

                    SnapZoomMinus = true;
                    Game1.playSound("drumkit6");
                }

                // plus button
                else if (this.PlusButton.Contains(x, y) && this.Value < this.MaxValue)
                {
                    this.ApplyStep(this.Step);
                    this.SetValue(this.Value);

                    SnapZoomPlus = true;
                    Game1.playSound("drumkit6");
                }
            }
        }

        public override void receiveKeyPress(Keys key)
        {
            base.receiveKeyPress(key);
            if (Game1.options.snappyMenus && Game1.options.gamepadControls)
            {
                if (Game1.options.doesInputListContain(Game1.options.moveRightButton, key))
                    this.receiveLeftClick(this.PlusButton.Center.X, this.PlusButton.Center.Y);
                else if (Game1.options.doesInputListContain(Game1.options.moveLeftButton, key))
                    this.receiveLeftClick(this.MinusButton.Center.X, this.MinusButton.Center.Y);
            }
        }

        public override void draw(SpriteBatch b, int slotX, int slotY, IClickableMenu context = null)
        {
            this.greyedOut = this.ShouldGrayOut();
            b.Draw(Game1.mouseCursors, new Vector2(slotX + this.MinusButton.X, slotY + this.MinusButton.Y), MinusButtonSource, Color.White * (this.greyedOut ? 0.33f : 1f) * (this.Value == this.MinValue ? 0.5f : 1f), 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.4f);
            b.DrawString(Game1.dialogueFont, this.FormatValue(this.Value), new Vector2((this.TxtSize / 2) + slotX, slotY + this.MinusButton.Y), Game1.textColor * (this.greyedOut ? 0.33f : 1f));
            b.Draw(Game1.mouseCursors, new Vector2(slotX + this.PlusButton.X, slotY + this.PlusButton.Y), PlusButtonSource, Color.White * (this.greyedOut ? 0.33f : 1f) * (this.Value == this.MaxValue ? 0.5f : 1f), 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.4f);
            if (!Game1.options.snappyMenus && Game1.options.gamepadControls)
            {
                if (SnapZoomMinus)
                {
                    Game1.setMousePosition(slotX + this.MinusButton.Center.X, slotY + this.MinusButton.Center.Y);
                    SnapZoomMinus = false;
                }
                else if (SnapZoomPlus)
                {
                    Game1.setMousePosition(slotX + this.PlusButton.Center.X, slotY + this.PlusButton.Center.Y);
                    SnapZoomPlus = false;
                }
            }

            base.draw(b, slotX, slotY, context);
        }

        /// <summary>Add a given delta to the current value, within the <see cref="MinValue"/> and <see cref="MaxValue"/> bounds.</summary>
        /// <param name="delta">The amount to add to the value.</param>
        private void ApplyStep(int delta)
        {
            this.Value = Math.Clamp(this.Value + delta, this.MinValue, this.MaxValue);
        }
    }
}
