using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace NPCMapLocations
{
    public class ModPlusMinus : OptionsElement
    {
        public static bool snapZoomPlus;
        public static bool snapZoomMinus;
        public static Rectangle minusButtonSource = new Rectangle(177, 345, 7, 8);
        public static Rectangle plusButtonSource = new Rectangle(184, 345, 7, 8);
        public List<string> displayOptions;
        private Rectangle minusButton;
        public List<int> options;
        private Rectangle plusButton;
        private int txtSize;
        public int selected;

        public ModPlusMinus(string label, int whichOption, List<int> options, int x = -1, int y = -1)
            : base(label, x, y, 28, 28, whichOption)
        {
            this.options = options;
            this.displayOptions = new List<string>();

            if (x == -1) x = 32;
            if (y == -1) y = 16;

            this.txtSize = (int)Game1.dialogueFont.MeasureString($"options[0]").X + 28;
            foreach (int displayOption in options)
            {
                this.txtSize = Math.Max((int)Game1.dialogueFont.MeasureString($"{displayOption}px").X + 28, this.txtSize);
                this.displayOptions.Add($"{displayOption}px");
            }

            this.bounds = new Rectangle(x, y, (int)(1.5 * this.txtSize), 32);
            this.label = ModMain.Helper.Translation.Get(label);
            this.whichOption = whichOption;
            this.minusButton = new Rectangle(x, 16, 28, 32);
            this.plusButton = new Rectangle(this.bounds.Right - 96, 16, 28, 32);

            switch (whichOption)
            {
                case 1:
                    this.selected = (int)MathHelper.Clamp(((int)Math.Floor((ModMain.Globals.MinimapWidth - 75) / 15.0)), 0,
                        options.Count - 1);
                    options[this.selected] = ModMain.Globals.MinimapWidth;
                    break;
                case 2:
                    this.selected = (int)MathHelper.Clamp(((int)Math.Floor((ModMain.Globals.MinimapHeight - 45) / 15.0)), 0,
                        options.Count - 1);
                    options[this.selected] = ModMain.Globals.MinimapHeight;
                    break;
                default:
                    break;
            }
        }

        public override void receiveLeftClick(int x, int y)
        {
            if (!this.greyedOut && this.options.Count > 0)
            {
                int num = this.selected;
                if (this.minusButton.Contains(x, y) && this.selected != 0)
                {
                    this.selected--;
                    snapZoomMinus = true;
                    Game1.playSound("drumkit6");
                }
                else if (this.plusButton.Contains(x, y) && this.selected != this.options.Count - 1)
                {
                    this.selected++;
                    snapZoomPlus = true;
                    Game1.playSound("drumkit6");
                }

                if (this.selected < 0)
                    this.selected = 0;
                else if (this.selected >= this.options.Count) this.selected = this.options.Count - 1;
            }

            switch (this.whichOption)
            {
                case 1:
                    ModMain.Globals.MinimapWidth = this.options[this.selected];
                    break;
                case 2:
                    ModMain.Globals.MinimapHeight = this.options[this.selected];
                    break;
                default:
                    break;
            }

            ModMain.Helper.Data.WriteJsonFile($"config/{Constants.SaveFolderName}.json", ModMain.Config);
        }

        public override void receiveKeyPress(Keys key)
        {
            base.receiveKeyPress(key);
            if (Game1.options.snappyMenus && Game1.options.gamepadControls)
            {
                if (Game1.options.doesInputListContain(Game1.options.moveRightButton, key))
                    this.receiveLeftClick(this.plusButton.Center.X, this.plusButton.Center.Y);
                else if (Game1.options.doesInputListContain(Game1.options.moveLeftButton, key))
                    this.receiveLeftClick(this.minusButton.Center.X, this.minusButton.Center.Y);
            }
        }

        public override void draw(SpriteBatch b, int slotX, int slotY, IClickableMenu context = null)
        {
            this.greyedOut = !ModMain.Globals.ShowMinimap;
            b.Draw(Game1.mouseCursors, new Vector2(slotX + this.minusButton.X, slotY + this.minusButton.Y), minusButtonSource,
                Color.White * (this.greyedOut ? 0.33f : 1f) * (this.selected == 0 ? 0.5f : 1f), 0f, Vector2.Zero, 4f, SpriteEffects.None,
                0.4f);
            b.DrawString(Game1.dialogueFont,
                this.selected < this.displayOptions.Count && this.selected != -1 ? this.displayOptions[this.selected] : "",
                new Vector2((int)(this.txtSize / 2) + slotX, slotY + this.minusButton.Y), Game1.textColor * (this.greyedOut ? 0.33f : 1f));
            b.Draw(Game1.mouseCursors, new Vector2(slotX + this.plusButton.X, slotY + this.plusButton.Y), plusButtonSource,
                Color.White * (this.greyedOut ? 0.33f : 1f) * (this.selected == this.displayOptions.Count - 1 ? 0.5f : 1f), 0f, Vector2.Zero,
                4f, SpriteEffects.None, 0.4f);
            if (!Game1.options.snappyMenus && Game1.options.gamepadControls)
            {
                if (snapZoomMinus)
                {
                    Game1.setMousePosition(slotX + this.minusButton.Center.X, slotY + this.minusButton.Center.Y);
                    snapZoomMinus = false;
                }
                else if (snapZoomPlus)
                {
                    Game1.setMousePosition(slotX + this.plusButton.Center.X, slotY + this.plusButton.Center.Y);
                    snapZoomPlus = false;
                }
            }

            base.draw(b, slotX, slotY, context);
        }
    }
}
