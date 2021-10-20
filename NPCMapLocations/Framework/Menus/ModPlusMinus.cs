using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
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
        private readonly List<string> DisplayOptions;
        private readonly List<int> Options;
        private readonly int TxtSize;
        private Rectangle MinusButton;
        private Rectangle PlusButton;
        private static bool SnapZoomPlus;
        private static bool SnapZoomMinus;
        private int Selected;


        /*********
        ** Public methods
        *********/
        public ModPlusMinus(string label, int whichOption, List<int> options, int x = -1, int y = -1)
            : base(label, x, y, 28, 28, whichOption)
        {
            this.Options = options;
            this.DisplayOptions = new List<string>();

            if (x == -1) x = 32;
            if (y == -1) y = 16;

            this.TxtSize = (int)Game1.dialogueFont.MeasureString("options[0]").X + 28;
            foreach (int displayOption in options)
            {
                this.TxtSize = Math.Max((int)Game1.dialogueFont.MeasureString($"{displayOption}px").X + 28, this.TxtSize);
                this.DisplayOptions.Add($"{displayOption}px");
            }

            this.bounds = new Rectangle(x, y, (int)(1.5 * this.TxtSize), 32);
            this.label = ModEntry.StaticHelper.Translation.Get(label);
            this.whichOption = whichOption;
            this.MinusButton = new Rectangle(x, 16, 28, 32);
            this.PlusButton = new Rectangle(this.bounds.Right - 96, 16, 28, 32);

            switch (whichOption)
            {
                case 1:
                    this.Selected = (int)MathHelper.Clamp(((int)Math.Floor((ModEntry.Globals.MinimapWidth - 75) / 15.0)), 0,
                        options.Count - 1);
                    options[this.Selected] = ModEntry.Globals.MinimapWidth;
                    break;
                case 2:
                    this.Selected = (int)MathHelper.Clamp(((int)Math.Floor((ModEntry.Globals.MinimapHeight - 45) / 15.0)), 0,
                        options.Count - 1);
                    options[this.Selected] = ModEntry.Globals.MinimapHeight;
                    break;
            }
        }

        public override void receiveLeftClick(int x, int y)
        {
            if (!this.greyedOut && this.Options.Count > 0)
            {
                if (this.MinusButton.Contains(x, y) && this.Selected != 0)
                {
                    this.Selected--;
                    SnapZoomMinus = true;
                    Game1.playSound("drumkit6");
                }
                else if (this.PlusButton.Contains(x, y) && this.Selected != this.Options.Count - 1)
                {
                    this.Selected++;
                    SnapZoomPlus = true;
                    Game1.playSound("drumkit6");
                }

                if (this.Selected < 0)
                    this.Selected = 0;
                else if (this.Selected >= this.Options.Count) this.Selected = this.Options.Count - 1;
            }

            switch (this.whichOption)
            {
                case 1:
                    ModEntry.Globals.MinimapWidth = this.Options[this.Selected];
                    break;
                case 2:
                    ModEntry.Globals.MinimapHeight = this.Options[this.Selected];
                    break;
            }

            ModEntry.StaticHelper.Data.WriteJsonFile($"config/{Constants.SaveFolderName}.json", ModEntry.Config);
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
            this.greyedOut = !ModEntry.Globals.ShowMinimap;
            b.Draw(Game1.mouseCursors, new Vector2(slotX + this.MinusButton.X, slotY + this.MinusButton.Y), MinusButtonSource,
                Color.White * (this.greyedOut ? 0.33f : 1f) * (this.Selected == 0 ? 0.5f : 1f), 0f, Vector2.Zero, 4f, SpriteEffects.None,
                0.4f);
            b.DrawString(Game1.dialogueFont,
                this.Selected < this.DisplayOptions.Count && this.Selected != -1 ? this.DisplayOptions[this.Selected] : "",
                new Vector2((this.TxtSize / 2) + slotX, slotY + this.MinusButton.Y), Game1.textColor * (this.greyedOut ? 0.33f : 1f));
            b.Draw(Game1.mouseCursors, new Vector2(slotX + this.PlusButton.X, slotY + this.PlusButton.Y), PlusButtonSource,
                Color.White * (this.greyedOut ? 0.33f : 1f) * (this.Selected == this.DisplayOptions.Count - 1 ? 0.5f : 1f), 0f, Vector2.Zero,
                4f, SpriteEffects.None, 0.4f);
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
    }
}
