/*
Menu settings for the mod. 
Menu code regurgitated from the game code
Settings loaded from this.Config file and changes saved onto this.Config file.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

namespace NPCMapLocations
{
    public class ModMenu : IClickableMenu
    {
        private readonly Dictionary<string, NpcMarker> npcMarkers;
        private readonly ClickableTextureComponent downArrow;
        private readonly MapModButton immersionButton1;
        private readonly MapModButton immersionButton2;
        private readonly MapModButton immersionButton3;
        private readonly int mapX;
        private readonly int mapY;
        private readonly ClickableTextureComponent okButton;
        private readonly List<OptionsElement> options = new List<OptionsElement>();
        private readonly List<ClickableComponent> optionSlots = new List<ClickableComponent>();
        private readonly ClickableTextureComponent scrollBar;
        private readonly Rectangle scrollBarRunner;
        private readonly ClickableTextureComponent upArrow;
        private bool canClose;
        private int currentItemIndex;
        private int optionsSlotHeld = -1;
        private bool scrolling;

        public ModMenu(
      Dictionary<string, NpcMarker> npcMarkers,
            Dictionary<string, bool> conditionalNpcs
        ) : base(Game1.viewport.Width / 2 - (1000 + IClickableMenu.borderWidth * 2) / 2, Game1.viewport.Height / 2 - (600 + IClickableMenu.borderWidth * 2) / 2, 1000 + IClickableMenu.borderWidth * 2, 600 + IClickableMenu.borderWidth * 2, true)
        {
            this.npcMarkers = npcMarkers;

            var topLeftPositionForCenteringOnScreen =
                Utility.getTopLeftPositionForCenteringOnScreen(ModMain.Map.Bounds.Width * Game1.pixelZoom, 180 * Game1.pixelZoom,
                    0, 0);
            this.mapX = (int)topLeftPositionForCenteringOnScreen.X;
            this.mapY = (int)topLeftPositionForCenteringOnScreen.Y;

            // Most of this mess is straight from the game code just... just give it space
            this.okButton = new ClickableTextureComponent(
                "OK",
                new Rectangle(this.xPositionOnScreen + this.width - Game1.tileSize * 2, this.yPositionOnScreen + this.height - 7 * Game1.tileSize / 4, Game1.tileSize, Game1.tileSize),
                null,
                null,
                Game1.mouseCursors,
                Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46, -1, -1),
                1f,
                false
            );

            this.upArrow = new ClickableTextureComponent(
                new Rectangle(this.xPositionOnScreen + this.width + Game1.tileSize / 4, this.yPositionOnScreen + Game1.tileSize, 11 * Game1.pixelZoom, 12 * Game1.pixelZoom),
                Game1.mouseCursors,
                new Rectangle(421, 459, 11, 12),
                Game1.pixelZoom
            );

            this.downArrow = new ClickableTextureComponent(
                new Rectangle(this.xPositionOnScreen + this.width + Game1.tileSize / 4, this.yPositionOnScreen + this.height - Game1.tileSize, 11 * Game1.pixelZoom, 12 * Game1.pixelZoom),
                Game1.mouseCursors,
                new Rectangle(421, 472, 11, 12),
                Game1.pixelZoom
            );

            this.scrollBar = new ClickableTextureComponent(
                new Rectangle(this.upArrow.bounds.X + Game1.pixelZoom * 3, this.upArrow.bounds.Y + this.upArrow.bounds.Height + Game1.pixelZoom, 6 * Game1.pixelZoom, 10 * Game1.pixelZoom),
                Game1.mouseCursors,
                new Rectangle(435, 463, 6, 10),
                Game1.pixelZoom
            );

            this.scrollBarRunner = new Rectangle(this.scrollBar.bounds.X, this.upArrow.bounds.Y + this.upArrow.bounds.Height + Game1.pixelZoom, this.scrollBar.bounds.Width, this.height - Game1.tileSize * 2 - this.upArrow.bounds.Height - Game1.pixelZoom * 2);

            for (int i = 0; i < 7; i++)
            {
                this.optionSlots.Add(new ClickableComponent(
                    new Rectangle(this.xPositionOnScreen + Game1.tileSize / 4, this.yPositionOnScreen + Game1.tileSize * 5 / 4 + Game1.pixelZoom + i * ((this.height - Game1.tileSize * 2) / 7), this.width - Game1.tileSize / 2, (this.height - Game1.tileSize * 2) / 7 + Game1.pixelZoom), string.Concat(i)
                ));
            }

            this.options.Add(new OptionsElement("NPC Map Locations"));

            var widths = new List<int>();
            for (int i = 0; i < 16; i++)
                widths.Add(75 + i * 15);

            var heights = new List<int>();
            for (int j = 0; j < 10; j++)
                heights.Add(45 + j * 15);

            string minimapLabel = ModMain.Helper.Translation.Get("minimap.label");
            this.options.Add(new OptionsElement(minimapLabel));
            this.options.Add(new ModCheckbox("minimap.option1", 0, -1, -1));
            this.options.Add(new ModPlusMinus("minimap.plusMinus1", 1, widths));
            this.options.Add(new ModPlusMinus("minimap.plusMinus2", 2, heights));

            // Translate labels and initialize buttons to handle button press
            string immersionLabel = ModMain.Helper.Translation.Get("immersion.label");
            this.options.Add(new OptionsElement(immersionLabel));
            this.immersionButton1 = new MapModButton("immersion.option1", 3, -1, -1, -1, -1);
            this.immersionButton2 = new MapModButton("immersion.option2", 4, -1, -1, -1, -1);
            this.immersionButton3 = new MapModButton("immersion.option3", 5, -1, -1, -1, -1);
            this.options.Add(this.immersionButton1);
            this.options.Add(this.immersionButton2);
            this.options.Add(this.immersionButton3);

            this.options.Add(new ModCheckbox("immersion.option4", 6, -1, -1));
            this.options.Add(new ModCheckbox("immersion.option5", 7, -1, -1));
            this.options.Add(new MapModSlider("immersion.slider1", 8, -1, -1, 0, 12));
            this.options.Add(new MapModSlider("immersion.slider2", 9, -1, -1, 0, 12));

            this.options.Add(new ModCheckbox("extra.option1", 10, -1, -1));
            this.options.Add(new ModCheckbox("extra.option2", 11, -1, -1));
            this.options.Add(new ModCheckbox("extra.option3", 12, -1, -1));

            string villagersLabel = ModMain.Helper.Translation.Get("villagers.label");
            this.options.Add(new OptionsElement(villagersLabel));

            var orderedMarkers = npcMarkers.ToList()
              .Where(x => x.Value.Sprite != null && x.Value.Type == Character.Villager)
              .OrderBy(x => x.Value.DisplayName);

            int idx = 13;
            foreach (var npcMarker in orderedMarkers)
            {
                if (conditionalNpcs.ContainsKey(npcMarker.Key))
                {
                    if (conditionalNpcs[npcMarker.Key])
                        this.options.Add(new ModCheckbox(npcMarker.Value.DisplayName, idx++, -1, -1, orderedMarkers));
                    else
                        idx++;
                }
                else
                {
                    this.options.Add(new ModCheckbox(npcMarker.Value.DisplayName, idx++, -1, -1, orderedMarkers));
                }
            }
        }

        // Override snappy controls on controller
        public override bool overrideSnappyMenuCursorMovementBan()
        {
            return true;
        }

        private void SetScrollBarToCurrentIndex()
        {
            if (this.options.Any())
            {
                this.scrollBar.bounds.Y = this.scrollBarRunner.Height / Math.Max(1, this.options.Count - 7 + 1) * this.currentItemIndex + this.upArrow.bounds.Bottom + Game1.pixelZoom;
                if (this.currentItemIndex == this.options.Count() - 7)
                    this.scrollBar.bounds.Y = this.downArrow.bounds.Y - this.scrollBar.bounds.Height - Game1.pixelZoom;
            }
        }

        public override void leftClickHeld(int x, int y)
        {
            if (GameMenu.forcePreventClose) return;

            base.leftClickHeld(x, y);
            if (this.scrolling)
            {
                int y2 = this.scrollBar.bounds.Y;
                this.scrollBar.bounds.Y = Math.Min(
                    this.yPositionOnScreen + this.height - Game1.tileSize - Game1.pixelZoom * 3 - this.scrollBar.bounds.Height,
                    Math.Max(y, this.yPositionOnScreen + this.upArrow.bounds.Height + Game1.pixelZoom * 5)
                );
                float num = (y - this.scrollBarRunner.Y) / (float)this.scrollBarRunner.Height;
                this.currentItemIndex = Math.Min(this.options.Count - 7, Math.Max(0, (int)(this.options.Count * num)));
                this.SetScrollBarToCurrentIndex();
                if (y2 != this.scrollBar.bounds.Y)
                    Game1.playSound("shiny4");
            }
            else
            {
                if (this.optionsSlotHeld == -1 || this.optionsSlotHeld + this.currentItemIndex >= this.options.Count)
                    return;

                this.options[this.currentItemIndex + this.optionsSlotHeld].leftClickHeld(
                    x - this.optionSlots[this.optionsSlotHeld].bounds.X,
                    y - this.optionSlots[this.optionsSlotHeld].bounds.Y
                );
            }
        }

        public override void receiveKeyPress(Keys key)
        {
            if ((Game1.options.menuButton.Contains(new InputButton(key)) ||
                 Game1.options.doesInputListContain(Game1.options.mapButton, key)) && this.readyToClose() && this.canClose)
            {
                this.exitThisMenu(true);
                return;
            }

            if (key.ToString().Equals(ModMain.Globals.MenuKey) && this.readyToClose() && this.canClose)
            {
                Game1.exitActiveMenu();
                Game1.activeClickableMenu = new GameMenu();
                (Game1.activeClickableMenu as GameMenu).changeTab(ModConstants.MapTabIndex);
                return;
            }

            this.canClose = true;
            if (this.optionsSlotHeld == -1 || this.optionsSlotHeld + this.currentItemIndex >= this.options.Count)
                return;

            this.options[this.currentItemIndex + this.optionsSlotHeld].receiveKeyPress(key);
        }

        public override void receiveScrollWheelAction(int direction)
        {
            if (GameMenu.forcePreventClose) return;

            base.receiveScrollWheelAction(direction);
            if (direction > 0 && this.currentItemIndex > 0)
            {
                this.UpArrowPressed();
                Game1.playSound("shiny4");
                return;
            }

            if (direction < 0 && this.currentItemIndex < Math.Max(0, this.options.Count() - 7))
            {
                this.DownArrowPressed();
                Game1.playSound("shiny4");
            }
        }

        public override void releaseLeftClick(int x, int y)
        {
            if (GameMenu.forcePreventClose) return;

            base.releaseLeftClick(x, y);
            if (this.optionsSlotHeld != -1 && this.optionsSlotHeld + this.currentItemIndex < this.options.Count)
            {
                this.options[this.currentItemIndex + this.optionsSlotHeld].leftClickReleased(
                    x - this.optionSlots[this.optionsSlotHeld].bounds.X,
                    y - this.optionSlots[this.optionsSlotHeld].bounds.Y
                );
            }

            this.optionsSlotHeld = -1;
            this.scrolling = false;
        }

        private void DownArrowPressed()
        {
            this.downArrow.scale = this.downArrow.baseScale;
            this.currentItemIndex++;
            this.SetScrollBarToCurrentIndex();
        }

        private void UpArrowPressed()
        {
            this.upArrow.scale = this.upArrow.baseScale;
            this.currentItemIndex--;
            this.SetScrollBarToCurrentIndex();
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (GameMenu.forcePreventClose) return;

            if (this.downArrow.containsPoint(x, y) && this.currentItemIndex < Math.Max(0, this.options.Count() - 7))
            {
                this.DownArrowPressed();
                Game1.playSound("shwip");
            }
            else if (this.upArrow.containsPoint(x, y) && this.currentItemIndex > 0)
            {
                this.UpArrowPressed();
                Game1.playSound("shwip");
            }
            else if (this.scrollBar.containsPoint(x, y))
            {
                this.scrolling = true;
            }
            else if (!this.downArrow.containsPoint(x, y) && x > this.xPositionOnScreen + this.width &&
                     x < this.xPositionOnScreen + this.width + Game1.tileSize * 2 && y > this.yPositionOnScreen &&
                     y < this.yPositionOnScreen + this.height)
            {
                this.scrolling = true;
                this.leftClickHeld(x, y);
            }
            else if (this.immersionButton1.rect.Contains(x, y))
            {
                this.immersionButton1.receiveLeftClick(x, y);
                this.immersionButton2.GreyOut();
                this.immersionButton3.GreyOut();
            }
            else if (this.immersionButton2.rect.Contains(x, y))
            {
                this.immersionButton2.receiveLeftClick(x, y);
                this.immersionButton1.GreyOut();
                this.immersionButton3.GreyOut();
            }
            else if (this.immersionButton3.rect.Contains(x, y))
            {
                this.immersionButton3.receiveLeftClick(x, y);
                this.immersionButton1.GreyOut();
                this.immersionButton2.GreyOut();
            }

            if (this.okButton.containsPoint(x, y))
            {
                this.okButton.scale -= 0.25f;
                this.okButton.scale = Math.Max(0.75f, this.okButton.scale);
                (Game1.activeClickableMenu as ModMenu).exitThisMenu(false);
                Game1.activeClickableMenu = new GameMenu();
                (Game1.activeClickableMenu as GameMenu).changeTab(3);
            }

            y -= 15;
            this.currentItemIndex = Math.Max(0, Math.Min(this.options.Count() - 7, this.currentItemIndex));
            for (int i = 0; i < this.optionSlots.Count(); i++)
            {
                if (this.optionSlots[i].bounds.Contains(x, y) && this.currentItemIndex + i < this.options.Count() && this.options[this.currentItemIndex + i]
                        .bounds.Contains(x - this.optionSlots[i].bounds.X, y - this.optionSlots[i].bounds.Y))
                {
                    this.options[this.currentItemIndex + i].receiveLeftClick(x - this.optionSlots[i].bounds.X, y - this.optionSlots[i].bounds.Y);
                    this.optionsSlotHeld = i;
                    break;
                }
            }
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
        }

        public override void performHoverAction(int x, int y)
        {
            if (GameMenu.forcePreventClose) return;

            if (this.okButton.containsPoint(x, y))
            {
                this.okButton.scale = Math.Min(this.okButton.scale + 0.02f, this.okButton.baseScale + 0.1f);
                return;
            }

            this.okButton.scale = Math.Max(this.okButton.scale - 0.02f, this.okButton.baseScale);
            this.upArrow.tryHover(x, y, 0.1f);
            this.downArrow.tryHover(x, y, 0.1f);
            this.scrollBar.tryHover(x, y, 0.1f);
        }

        // Draw menu
        public override void draw(SpriteBatch b)
        {
            if (Game1.options.showMenuBackground) this.drawBackground(b);

            Game1.drawDialogueBox(this.mapX - Game1.pixelZoom * 8, this.mapY - Game1.pixelZoom * 24,
              (ModMain.Map.Bounds.Width + 16) * Game1.pixelZoom, 212 * Game1.pixelZoom, false, true, null, false);
            b.Draw(ModMain.Map, new Vector2(this.mapX, this.mapY), new Rectangle(0, 0, 300, 180),
              Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.86f);
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.4f);
            Game1.drawDialogueBox(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, false, true, null, false);
            this.okButton.draw(b);
            int buttonWidth = (int)Game1.dialogueFont.MeasureString(ModMain.Helper.Translation.Get("immersion.option3")).X;
            if (!GameMenu.forcePreventClose)
            {
                this.upArrow.draw(b);
                this.downArrow.draw(b);

                if (this.options.Count() > 7)
                {
                    drawTextureBox(b, Game1.mouseCursors, new Rectangle(403, 383, 6, 6), this.scrollBarRunner.X,
                        this.scrollBarRunner.Y, this.scrollBarRunner.Width, this.scrollBarRunner.Height, Color.White,
                        Game1.pixelZoom, false);
                    this.scrollBar.draw(b);
                }

                for (int i = 0; i < this.optionSlots.Count(); i++)
                {
                    int x = this.optionSlots[i].bounds.X;
                    int y = this.optionSlots[i].bounds.Y + Game1.tileSize / 4;
                    if (this.currentItemIndex >= 0 && this.currentItemIndex + i < this.options.Count())
                    {
                        if (this.options[this.currentItemIndex + i] is MapModButton)
                        {
                            var bounds = new Rectangle(x + 28, y, buttonWidth + Game1.tileSize + 8, Game1.tileSize + 8);
                            switch (this.options[this.currentItemIndex + i].whichOption)
                            {
                                case 3:
                                    this.immersionButton1.rect = bounds;
                                    break;
                                case 4:
                                    this.immersionButton2.rect = bounds;
                                    break;
                                case 5:
                                    this.immersionButton3.rect = bounds;
                                    break;
                            }

                            drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), bounds.X, bounds.Y,
                                bounds.Width, bounds.Height, Color.White * (this.options[this.currentItemIndex + i].greyedOut ? 0.33f : 1f),
                                1f, false);
                        }

                        if (this.currentItemIndex + i == 0)
                            Utility.drawTextWithShadow(b, "NPC Map Locations", Game1.dialogueFont, new Vector2(x + Game1.tileSize / 2, y + Game1.tileSize / 4), Color.Black);
                        else
                            this.options[this.currentItemIndex + i].draw(b, x, y);
                    }
                }
            }

            if (!Game1.options.hardwareCursor)
                b.Draw(Game1.mouseCursors, new Vector2(Game1.getOldMouseX(), Game1.getOldMouseY()),
                    Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors,
                        Game1.options.gamepadControls ? 44 : 0, 16, 16), Color.White, 0f, Vector2.Zero,
                    Game1.pixelZoom + Game1.dialogueButtonScale / 150f, SpriteEffects.None, 1f);
        }
    }
}
