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
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
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

    // Mod button for the three main modes
    internal class MapModButton : OptionsElement
    {
        public bool isActive;
        public Rectangle rect;

        public MapModButton(
            string label,
            int whichOption,
            int x,
            int y,
            int width,
            int height
        ) : base(label, x, y, 9 * Game1.pixelZoom, 9 * Game1.pixelZoom, whichOption)
        {
            this.label = ModMain.Helper.Translation.Get(label);
            this.rect = new Rectangle(x, y, width, height);

            if (ModMain.Config.ImmersionOption == whichOption - 2)
                this.greyedOut = false;
            else
                this.greyedOut = true;
        }

        public override void receiveLeftClick(int x, int y)
        {
            if (!this.isActive)
            {
                if (this.whichOption == 3 || this.whichOption == 4 || this.whichOption == 5)
                {
                    Game1.playSound("drumkit6");
                    base.receiveLeftClick(x, y);
                    this.isActive = true;
                    this.greyedOut = false;
                    ModMain.Config.ImmersionOption = this.whichOption - 2;
                }
            }
        }

        public void GreyOut()
        {
            this.isActive = false;
            this.greyedOut = true;
        }

        public override void draw(SpriteBatch b, int slotX, int slotY, IClickableMenu context = null)
        {
            base.draw(b, slotX - 32, slotY, context);
        }
    }

    // Mod checkbox for settings and npc blacklst
    internal class ModCheckbox : OptionsElement
    {
        private readonly IOrderedEnumerable<KeyValuePair<string, NpcMarker>> npcMarkers;
        public bool isChecked;

        public ModCheckbox(
            string label,
            int whichOption,
            int x,
            int y,
      IOrderedEnumerable<KeyValuePair<string, NpcMarker>> npcMarkers = null
        ) : base(label, x, y, 9 * Game1.pixelZoom, 9 * Game1.pixelZoom, whichOption)
        {
            this.npcMarkers = npcMarkers;
            this.label = ModMain.Helper.Translation.Get(label);

            // Villager names
            if (whichOption > 12 && npcMarkers != null)
            {
                this.isChecked = !ModMain.Globals.NpcExclusions.Contains(npcMarkers.ElementAt(whichOption - 13).Key);
                return;
            }

            switch (whichOption)
            {
                case 0:
                    this.isChecked = ModMain.Globals.ShowMinimap;
                    return;
                case 6:
                    this.isChecked = ModMain.Globals.OnlySameLocation;
                    return;
                case 7:
                    this.isChecked = ModMain.Config.ByHeartLevel;
                    return;
                case 10:
                    this.isChecked = ModMain.Globals.ShowQuests;
                    return;
                case 11:
                    this.isChecked = ModMain.Globals.ShowHiddenVillagers;
                    return;
                case 12:
                    this.isChecked = ModMain.Globals.ShowTravelingMerchant;
                    return;
                default:
                    return;
            }
        }

        public override void receiveLeftClick(int x, int y)
        {
            if (this.greyedOut)
                return;

            base.receiveLeftClick(x, y);
            this.isChecked = !this.isChecked;
            int whichOption = this.whichOption;

            // Show/hide villager options
            if (whichOption > 12 && this.npcMarkers != null)
            {
                if (this.isChecked)
                    ModMain.Globals.NpcExclusions.Remove(this.npcMarkers.ElementAt(whichOption - 13).Key);
                else
                    ModMain.Globals.NpcExclusions.Add(this.npcMarkers.ElementAt(whichOption - 13).Key);
            }
            else
            {
                switch (whichOption)
                {
                    case 0:
                        ModMain.Globals.ShowMinimap = this.isChecked;
                        break;
                    case 6:
                        ModMain.Globals.OnlySameLocation = this.isChecked;
                        break;
                    case 7:
                        ModMain.Config.ByHeartLevel = this.isChecked;
                        break;
                    case 10:
                        ModMain.Globals.ShowQuests = this.isChecked;
                        break;
                    case 11:
                        ModMain.Globals.ShowHiddenVillagers = this.isChecked;
                        break;
                    case 12:
                        ModMain.Globals.ShowTravelingMerchant = this.isChecked;
                        break;
                    default:
                        break;
                }
            }

            ModMain.Helper.Data.WriteJsonFile($"config/{Constants.SaveFolderName}.json", ModMain.Config);
            ModMain.Helper.Data.WriteJsonFile("config/globals.json", ModMain.Globals);
        }

        public override void draw(SpriteBatch b, int slotX, int slotY, IClickableMenu context = null)
        {
            b.Draw(Game1.mouseCursors, new Vector2(slotX + this.bounds.X, slotY + this.bounds.Y),
                this.isChecked ? OptionsCheckbox.sourceRectChecked : OptionsCheckbox.sourceRectUnchecked,
                Color.White * (this.greyedOut ? 0.33f : 1f), 0f, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None,
                0.4f);
            if (this.whichOption > 12 && this.npcMarkers != null)
            {
                var marker = this.npcMarkers.ElementAt(this.whichOption - 13).Value;

                if (this.isChecked)
                    Game1.spriteBatch.Draw(marker.Sprite, new Vector2((float)slotX + this.bounds.X + 50, slotY),
                        new Rectangle(0, marker.CropOffset, 16, 15), Color.White, 0f, Vector2.Zero,
                        Game1.pixelZoom, SpriteEffects.None, 0.4f);
                else
                    Game1.spriteBatch.Draw(marker.Sprite, new Vector2((float)slotX + this.bounds.X + 50, slotY),
                        new Rectangle(0, marker.CropOffset, 16, 15), Color.White * 0.33f, 0f, Vector2.Zero,
                        Game1.pixelZoom, SpriteEffects.None, 0.4f);

                // Draw names
                slotX += 75;
                if (this.whichOption == -1)
                    SpriteText.drawString(b, this.label, slotX + this.bounds.X, slotY + this.bounds.Y + 12, 999, -1, 999, 1f,
                        0.1f, false, -1, "", -1);
                else
                    Utility.drawTextWithShadow(b, marker.DisplayName, Game1.dialogueFont,
                        new Vector2(slotX + this.bounds.X + this.bounds.Width + 8, slotY + this.bounds.Y),
                        this.greyedOut ? Game1.textColor * 0.33f : Game1.textColor, 1f, 0.1f, -1, -1, 1f, 3);
            }
            else
            {
                base.draw(b, slotX, slotY, context);
            }
        }
    }

    // Mod slider for heart level this.Config
    internal class MapModSlider : OptionsElement
    {
        private readonly int max;
        private int min;
        private float value;
        public string valueLabel;

        public MapModSlider(
            string label,
            int whichOption,
            int x,
            int y,
            int min,
            int max
        ) : base(label, x, y, 48 * Game1.pixelZoom, 6 * Game1.pixelZoom, whichOption)
        {
            this.min = min;
            this.max = max;
            if (whichOption != 8 && whichOption != 9) this.bounds.Width = this.bounds.Width * 2;
            this.valueLabel = ModMain.Helper.Translation.Get(label);

            switch (whichOption)
            {
                case 8:
                    this.value = ModMain.Config.HeartLevelMin;
                    break;
                case 9:
                    this.value = ModMain.Config.HeartLevelMax;
                    break;
                default:
                    break;
            }
        }

        public override void leftClickHeld(int x, int y)
        {
            if (this.greyedOut)
                return;

            base.leftClickHeld(x, y);
            this.value = x >= this.bounds.X
                ? (x <= this.bounds.Right - 10 * Game1.pixelZoom
                    ? (int)((x - this.bounds.X) / (float)(this.bounds.Width - 10 * Game1.pixelZoom) * this.max)
                    : this.max)
                : 0;

            switch (this.whichOption)
            {
                case 8:
                    ModMain.Config.HeartLevelMin = (int)this.value;
                    break;
                case 9:
                    ModMain.Config.HeartLevelMax = (int)this.value;
                    break;
                default:
                    break;
            }

            ModMain.Helper.Data.WriteJsonFile($"config/{Constants.SaveFolderName}.json", ModMain.Config);
        }

        public override void receiveLeftClick(int x, int y)
        {
            if (this.greyedOut) return;

            base.receiveLeftClick(x, y);
            this.leftClickHeld(x, y);
        }

        public override void draw(SpriteBatch b, int slotX, int slotY, IClickableMenu context = null)
        {
            this.label = this.valueLabel + ": " + this.value;
            this.greyedOut = false;
            if (this.whichOption == 8 || this.whichOption == 9) this.greyedOut = !ModMain.Config.ByHeartLevel;

            base.draw(b, slotX, slotY, context);
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, OptionsSlider.sliderBGSource, slotX + this.bounds.X,
                slotY + this.bounds.Y, this.bounds.Width, this.bounds.Height, Color.White, Game1.pixelZoom, false);
            b.Draw(Game1.mouseCursors,
                new Vector2(
                    slotX + this.bounds.X + (this.bounds.Width - 10 * Game1.pixelZoom) *
                    (this.value / this.max), slotY + this.bounds.Y),
                OptionsSlider.sliderButtonRect, Color.White, 0f, Vector2.Zero, Game1.pixelZoom,
                SpriteEffects.None, 0.9f);
        }
    }

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
