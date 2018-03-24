/*
Menu settings for the mod. 
Settings loaded from config file and changes saved onto config file.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NPCMapLocations
{
    public class MapModMenu : IClickableMenu
    {
        public const int itemsPerPage = 7;
        public const int indexOfGraphicsPage = 6;
        public int currentItemIndex;
        private Texture2D map;
        private int mapX;
        private int mapY;
        private List<ClickableComponent> optionSlots = new List<ClickableComponent>();
        private ClickableTextureComponent upArrow;
        private ClickableTextureComponent downArrow;
        private ClickableTextureComponent scrollBar;
        private MapModButton immersionButton1;
        private MapModButton immersionButton2;
        private MapModButton immersionButton3;
        private bool scrolling;
        private bool canClose;
        private List<OptionsElement> options = new List<OptionsElement>();
        private Rectangle scrollBarRunner;
        private int optionsSlotHeld = -1;
        private ClickableTextureComponent okButton;

        public MapModMenu(int x, int y, int width, int height, bool[] showExtras, Dictionary<string, Dictionary<string, int>> customNPCs, Dictionary<string, string> npcNames) : base(x, y, width, height, false)
        {
            this.map = MapModMain.map;
            Vector2 topLeftPositionForCenteringOnScreen = Utility.getTopLeftPositionForCenteringOnScreen(this.map.Bounds.Width * Game1.pixelZoom, 180 * Game1.pixelZoom, 0, 0);
            this.mapX = (int)topLeftPositionForCenteringOnScreen.X;
            this.mapY = (int)topLeftPositionForCenteringOnScreen.Y;
            this.okButton = new ClickableTextureComponent("OK", new Rectangle(this.xPositionOnScreen + width + Game1.tileSize, this.yPositionOnScreen + height - IClickableMenu.borderWidth - Game1.tileSize / 4, Game1.tileSize, Game1.tileSize), null, null, Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46, -1, -1), 1f, false);
            this.upArrow = new ClickableTextureComponent(new Rectangle(this.xPositionOnScreen + width + Game1.tileSize / 4, this.yPositionOnScreen + Game1.tileSize, 11 * Game1.pixelZoom, 12 * Game1.pixelZoom), Game1.mouseCursors, new Rectangle(421, 459, 11, 12), (float)Game1.pixelZoom);
            this.downArrow = new ClickableTextureComponent(new Rectangle(this.xPositionOnScreen + width + Game1.tileSize / 4, this.yPositionOnScreen + height - Game1.tileSize, 11 * Game1.pixelZoom, 12 * Game1.pixelZoom), Game1.mouseCursors, new Rectangle(421, 472, 11, 12), (float)Game1.pixelZoom);
            this.scrollBar = new ClickableTextureComponent(new Rectangle(this.upArrow.bounds.X + Game1.pixelZoom * 3, this.upArrow.bounds.Y + this.upArrow.bounds.Height + Game1.pixelZoom, 6 * Game1.pixelZoom, 10 * Game1.pixelZoom), Game1.mouseCursors, new Rectangle(435, 463, 6, 10), (float)Game1.pixelZoom);
            this.scrollBarRunner = new Rectangle(this.scrollBar.bounds.X, this.upArrow.bounds.Y + this.upArrow.bounds.Height + Game1.pixelZoom, this.scrollBar.bounds.Width, height - Game1.tileSize * 2 - this.upArrow.bounds.Height - Game1.pixelZoom * 2);
            for (int i = 0; i < 7; i++)
            {
                this.optionSlots.Add(new ClickableComponent(new Rectangle(this.xPositionOnScreen + Game1.tileSize / 4, this.yPositionOnScreen + Game1.tileSize * 5 / 4 + Game1.pixelZoom + i * ((height - Game1.tileSize * 2) / 7), width - Game1.tileSize / 2, (height - Game1.tileSize * 2) / 7 + Game1.pixelZoom), string.Concat(i)));
            }

            // Translate labels and initialize buttons to handle button press
            string immersionLabel = MapModMain.modHelper.Translation.Get("immersion.label");
            string extraLabel = MapModMain.modHelper.Translation.Get("extra.label");
            string villagersLabel = MapModMain.modHelper.Translation.Get("villagers.label");
            immersionButton1 = new MapModButton("immersion.option1", 4, -1, -1, -1, -1);
            immersionButton2 = new MapModButton("immersion.option2", 5, -1, -1, -1, -1);
            immersionButton3 = new MapModButton("immersion.option3", 6, -1, -1, -1, -1);

            //this.options.Add(new OptionsElement("Menu Key:"));
            //this.options.Add(new MapModInputListener("Change menu key", 37, this.optionSlots[0].bounds.Width, -1, -1));
            this.options.Add(new OptionsElement("NPC Map Locations Mod Version"));
            this.options.Add(new OptionsElement(immersionLabel));
            this.options.Add(immersionButton1);
            this.options.Add(immersionButton2);
            this.options.Add(immersionButton3);
            this.options.Add(new MapModCheckbox("immersion.option4", 44, -1, -1));
            this.options.Add(new MapModCheckbox("immersion.option5", 45, -1, -1));
            this.options.Add(new MapModSlider("immersion.slider1", 0, -1, -1));
            this.options.Add(new MapModSlider("immersion.slider2", 1, -1, -1));
            this.options.Add(new OptionsElement(extraLabel));
            this.options.Add(new MapModCheckbox("extra.option1", 46, -1, -1));
            this.options.Add(new MapModCheckbox("extra.option2", 47, -1, -1));
            this.options.Add(new MapModCheckbox("extra.option3", 48, -1, -1));
            this.options.Add(new OptionsElement(villagersLabel));
            // Custom NPCs
            if (customNPCs != null)
            {
                foreach (KeyValuePair<string, Dictionary<string, int>> entry in customNPCs)
                {
                    if (MapModMain.customNpcId > 0)
                    {
                        this.options.Add(new MapModCheckbox(npcNames[entry.Key], 39 + MapModMain.customNpcId, -1, -1));
                    }
                }
            }
            this.options.Add(new MapModCheckbox(npcNames["Abigail"], 7, -1, -1));
            this.options.Add(new MapModCheckbox(npcNames["Alex"], 8, -1, -1));
            this.options.Add(new MapModCheckbox(npcNames["Caroline"], 9, -1, -1));
            this.options.Add(new MapModCheckbox(npcNames["Clint"], 10, -1, -1));
            this.options.Add(new MapModCheckbox(npcNames["Demetrius"], 11, -1, -1));
            this.options.Add(new MapModCheckbox(npcNames["Elliott"], 12, -1, -1));
            this.options.Add(new MapModCheckbox(npcNames["Emily"], 13, -1, -1));
            this.options.Add(new MapModCheckbox(npcNames["Evelyn"], 14, -1, -1));
            this.options.Add(new MapModCheckbox(npcNames["George"], 15, -1, -1));
            this.options.Add(new MapModCheckbox(npcNames["Gus"], 16, -1, -1));
            this.options.Add(new MapModCheckbox(npcNames["Haley"], 17, -1, -1));
            this.options.Add(new MapModCheckbox(npcNames["Harvey"], 18, -1, -1));
            this.options.Add(new MapModCheckbox(npcNames["Jas"], 19, -1, -1));
            this.options.Add(new MapModCheckbox(npcNames["Jodi"], 20, -1, -1));
            if (showExtras[3])
            {
                this.options.Add(new MapModCheckbox(npcNames["Kent"], 21, -1, -1));
            }
            this.options.Add(new MapModCheckbox(npcNames["Leah"], 22, -1, -1));
            this.options.Add(new MapModCheckbox(npcNames["Lewis"], 23, -1, -1));
            this.options.Add(new MapModCheckbox(npcNames["Linus"], 24, -1, -1));
            if (showExtras[1])
            {
                this.options.Add(new MapModCheckbox(npcNames["Marlon"], 38, -1, -1));
            }
            this.options.Add(new MapModCheckbox(npcNames["Marnie"], 25, -1, -1));
            this.options.Add(new MapModCheckbox(npcNames["Maru"], 26, -1, -1));
            this.options.Add(new MapModCheckbox(npcNames["Pam"], 27, -1, -1));
            this.options.Add(new MapModCheckbox(npcNames["Penny"], 28, -1, -1));
            this.options.Add(new MapModCheckbox(npcNames["Pierre"], 29, -1, -1));
            this.options.Add(new MapModCheckbox(npcNames["Robin"], 30, -1, -1));
            this.options.Add(new MapModCheckbox(npcNames["Sam"], 31, -1, -1));
            if (showExtras[0])
            {
                this.options.Add(new MapModCheckbox(npcNames["Sandy"], 36, -1, -1));
            }
            this.options.Add(new MapModCheckbox(npcNames["Sebastian"], 32, -1, -1));
            this.options.Add(new MapModCheckbox(npcNames["Shane"], 33, -1, -1));
            this.options.Add(new MapModCheckbox(npcNames["Vincent"], 34, -1, -1));
            this.options.Add(new MapModCheckbox(npcNames["Willy"], 35, -1, -1));
            if (showExtras[2])
            {
                this.options.Add(new MapModCheckbox(npcNames["Wizard"], 37, -1, -1));
            }
        }

        private void setScrollBarToCurrentIndex()
        {
            if (this.options.Count<OptionsElement>() > 0)
            {
                this.scrollBar.bounds.Y = this.scrollBarRunner.Height / Math.Max(1, this.options.Count - 7 + 1) * this.currentItemIndex + this.upArrow.bounds.Bottom + Game1.pixelZoom;
                if (this.currentItemIndex == this.options.Count<OptionsElement>() - 7)
                {
                    this.scrollBar.bounds.Y = this.downArrow.bounds.Y - this.scrollBar.bounds.Height - Game1.pixelZoom;
                }
            }
        }

        public override void leftClickHeld(int x, int y)
        {
            if (GameMenu.forcePreventClose)
            {
                return;
            }
            base.leftClickHeld(x, y);
            if (this.scrolling)
            {
                int y2 = this.scrollBar.bounds.Y;
                this.scrollBar.bounds.Y = Math.Min(this.yPositionOnScreen + this.height - Game1.tileSize - Game1.pixelZoom * 3 - this.scrollBar.bounds.Height, Math.Max(y, this.yPositionOnScreen + this.upArrow.bounds.Height + Game1.pixelZoom * 5));
                float num = (float)(y - this.scrollBarRunner.Y) / (float)this.scrollBarRunner.Height;
                this.currentItemIndex = Math.Min(this.options.Count - 7, Math.Max(0, (int)((float)this.options.Count * num)));
                this.setScrollBarToCurrentIndex();
                if (y2 != this.scrollBar.bounds.Y)
                {
                    Game1.playSound("shiny4");
                    return;
                }
            }
            else
            {
                if (this.optionsSlotHeld == -1 || this.optionsSlotHeld + this.currentItemIndex >= this.options.Count)
                {
                    return;
                }
                this.options[this.currentItemIndex + this.optionsSlotHeld].leftClickHeld(x - this.optionSlots[this.optionsSlotHeld].bounds.X, y - this.optionSlots[this.optionsSlotHeld].bounds.Y);
                return;
            }
        }

        public override void receiveKeyPress(Keys key)
        {
            if ((Game1.options.menuButton.Contains(new InputButton(key)) || Game1.options.doesInputListContain(Game1.options.mapButton, key)) && this.readyToClose() && this.canClose)
            {
                Game1.exitActiveMenu();
                MapModMain.menuOpen = 0;
                //Game1.soundBank.PlayCue("bigDeSelect");
                return;
            }
            if (key.ToString().Equals(MapModMain.config.menuKey) && MapModMain.menuOpen == 1 && this.readyToClose() && this.canClose)
            {
                Game1.exitActiveMenu();
                Game1.activeClickableMenu = new GameMenu();
                (Game1.activeClickableMenu as GameMenu).changeTab(3);
                MapModMain.menuOpen = 0;
                return;
            }
            this.canClose = true;
            if (this.optionsSlotHeld == -1 || this.optionsSlotHeld + this.currentItemIndex >= this.options.Count)
            {
                return;
            }
            this.options[this.currentItemIndex + this.optionsSlotHeld].receiveKeyPress(key);
        }

        public override void receiveScrollWheelAction(int direction)
        {
            if (GameMenu.forcePreventClose)
            {
                return;
            }
            base.receiveScrollWheelAction(direction);
            if (direction > 0 && this.currentItemIndex > 0)
            {
                this.upArrowPressed();
                Game1.playSound("shiny4");
                return;
            }
            if (direction < 0 && this.currentItemIndex < Math.Max(0, this.options.Count<OptionsElement>() - 7))
            {
                this.downArrowPressed();
                Game1.playSound("shiny4");
            }
        }

        public override void releaseLeftClick(int x, int y)
        {
            if (GameMenu.forcePreventClose)
            {
                return;
            }
            base.releaseLeftClick(x, y);
            if (this.optionsSlotHeld != -1 && this.optionsSlotHeld + this.currentItemIndex < this.options.Count)
            {
                this.options[this.currentItemIndex + this.optionsSlotHeld].leftClickReleased(x - this.optionSlots[this.optionsSlotHeld].bounds.X, y - this.optionSlots[this.optionsSlotHeld].bounds.Y);
            }
            this.optionsSlotHeld = -1;
            this.scrolling = false;
        }

        private void downArrowPressed()
        {
            this.downArrow.scale = this.downArrow.baseScale;
            this.currentItemIndex++;
            this.setScrollBarToCurrentIndex();
        }

        private void upArrowPressed()
        {
            this.upArrow.scale = this.upArrow.baseScale;
            this.currentItemIndex--;
            this.setScrollBarToCurrentIndex();
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (GameMenu.forcePreventClose)
            {
                return;
            }
            if (this.downArrow.containsPoint(x, y) && this.currentItemIndex < Math.Max(0, this.options.Count<OptionsElement>() - 7))
            {
                this.downArrowPressed();
                Game1.playSound("shwip");
            }
            else if (this.upArrow.containsPoint(x, y) && this.currentItemIndex > 0)
            {
                this.upArrowPressed();
                Game1.playSound("shwip");
            }
            else if (this.scrollBar.containsPoint(x, y))
            {
                this.scrolling = true;
            }
            else if (!this.downArrow.containsPoint(x, y) && x > this.xPositionOnScreen + this.width && x < this.xPositionOnScreen + this.width + Game1.tileSize * 2 && y > this.yPositionOnScreen && y < this.yPositionOnScreen + this.height)
            {
                this.scrolling = true;
                this.leftClickHeld(x, y);
            }
            else if (immersionButton1.rect.Contains(x, y))
            {
                immersionButton1.receiveLeftClick(x, y);
                immersionButton2.greyOut();
                immersionButton3.greyOut();
            }
            else if (immersionButton2.rect.Contains(x, y))
            {
                immersionButton2.receiveLeftClick(x, y);
                immersionButton1.greyOut();
                immersionButton3.greyOut();
            }
            else if (immersionButton3.rect.Contains(x, y))
            {
                immersionButton3.receiveLeftClick(x, y);
                immersionButton1.greyOut();
                immersionButton2.greyOut();
            }
            if (this.okButton.containsPoint(x, y))
            {
                this.okButton.scale -= 0.25f;
                this.okButton.scale = Math.Max(0.75f, this.okButton.scale);
                (Game1.activeClickableMenu as MapModMenu).exitThisMenu(true);
                Game1.activeClickableMenu = new GameMenu();
                (Game1.activeClickableMenu as GameMenu).changeTab(3);
                MapModMain.menuOpen = 0;
            }
            y -= 15;
            this.currentItemIndex = Math.Max(0, Math.Min(this.options.Count<OptionsElement>() - 7, this.currentItemIndex));
            for (int i = 0; i < this.optionSlots.Count<ClickableComponent>(); i++)
            {
                if (this.optionSlots[i].bounds.Contains(x, y) && this.currentItemIndex + i < this.options.Count<OptionsElement>() && this.options[this.currentItemIndex + i].bounds.Contains(x - this.optionSlots[i].bounds.X, y - this.optionSlots[i].bounds.Y))
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
            if (GameMenu.forcePreventClose)
            {
                return;
            }
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

        public override void draw(SpriteBatch b)
        {
            if (Game1.options.showMenuBackground)
            {
                drawBackground(b);
            }
            Game1.drawDialogueBox(this.mapX - Game1.pixelZoom * 8, this.mapY - Game1.pixelZoom * 24, (this.map.Bounds.Width + 16) * Game1.pixelZoom, 212 * Game1.pixelZoom, false, true, null, false);
            b.Draw(this.map, new Vector2((float)this.mapX, (float)this.mapY), new Rectangle?(new Rectangle(0, 0, 300, 180)), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.86f);
            switch (Game1.whichFarm)
            {
                case 1:
                    b.Draw(this.map, new Vector2((float)this.mapX, (float)(this.mapY + 43 * Game1.pixelZoom)), new Rectangle?(new Rectangle(0, 180, 131, 61)), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
                    break;
                case 2:
                    b.Draw(this.map, new Vector2((float)this.mapX, (float)(this.mapY + 43 * Game1.pixelZoom)), new Rectangle?(new Rectangle(131, 180, 131, 61)), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
                    break;
                case 3:
                    b.Draw(this.map, new Vector2((float)this.mapX, (float)(this.mapY + 43 * Game1.pixelZoom)), new Rectangle?(new Rectangle(0, 241, 131, 61)), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
                    break;
                case 4:
                    b.Draw(this.map, new Vector2((float)this.mapX, (float)(this.mapY + 43 * Game1.pixelZoom)), new Rectangle?(new Rectangle(131, 241, 131, 61)), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
                    break;
            }
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.4f);
            Game1.drawDialogueBox(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, false, true, null, false);
            this.okButton.draw(b);
            if (!GameMenu.forcePreventClose)
            {
                this.upArrow.draw(b);
                this.downArrow.draw(b);

                if (this.options.Count<OptionsElement>() > 7)
                {
                    IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(403, 383, 6, 6), this.scrollBarRunner.X, this.scrollBarRunner.Y, this.scrollBarRunner.Width, this.scrollBarRunner.Height, Color.White, (float)Game1.pixelZoom, false);
                    this.scrollBar.draw(b);
                }
                for (int i = 0; i < this.optionSlots.Count<ClickableComponent>(); i++)
                {
                    int x = this.optionSlots[i].bounds.X;
                    int y = this.optionSlots[i].bounds.Y + Game1.tileSize / 4;
                    if (this.currentItemIndex >= 0 && this.currentItemIndex + i < this.options.Count<OptionsElement>())
                    {
                        if (options[this.currentItemIndex + i] is MapModButton)
                        {
                            Rectangle bounds = new Rectangle(x + 28, y, 700, Game1.tileSize + 8);
                            if (options[this.currentItemIndex + i].whichOption == 4)
                            {
                                immersionButton1.rect = bounds;
                            }
                            else if (options[this.currentItemIndex + i].whichOption == 5)
                            {
                                immersionButton2.rect = bounds;
                            }
                            else if (options[this.currentItemIndex + i].whichOption == 6)
                            {
                                immersionButton3.rect = bounds;
                            }

                            IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), bounds.X, bounds.Y, bounds.Width, bounds.Height, Color.White * (options[this.currentItemIndex + i].greyedOut ? 0.33f : 1f), 1f, false);
                        }
                        if (this.currentItemIndex + i == 0)
                        {
                            Utility.drawTextWithShadow(b, "NPC Map Locations v" + MapModMain.current, Game1.dialogueFont, new Vector2(x + Game1.tileSize / 2, y + Game1.tileSize / 4), Color.Black);
                        }
                        else
                        {
                            this.options[this.currentItemIndex + i].draw(b, x, y);
                        }
                    }
                }
            }
            if (!Game1.options.hardwareCursor)
            {
                b.Draw(Game1.mouseCursors, new Vector2((float)Game1.getOldMouseX(), (float)Game1.getOldMouseY()), new Rectangle?(Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, Game1.options.gamepadControls ? 44 : 0, 16, 16)), Color.White, 0f, Vector2.Zero, (float)Game1.pixelZoom + Game1.dialogueButtonScale / 150f, SpriteEffects.None, 1f);
            }
        }
    }

    public class MapModButton : OptionsElement
    {
        public const int pixelsWide = 9;
        public bool isActive;
        public Rectangle rect;

        public MapModButton(string label, int whichOption, int x, int y, int width, int height) : base(label, x, y, 9 * Game1.pixelZoom, 9 * Game1.pixelZoom, whichOption)
        {
            this.label = MapModMain.modHelper.Translation.Get(label);
            this.rect = new Rectangle(x, y, width, height);
            if (MapModMain.config.immersionOption == whichOption - 3)
            {
                this.greyedOut = false;
            }
            else
            {
                greyedOut = true;
            }
        }

        public override void receiveLeftClick(int x, int y)
        {
            if (!isActive)
            {
                if (whichOption > 3)
                {
                    Game1.playSound("drumkit6");
                    base.receiveLeftClick(x, y);
                    this.isActive = true;
                    this.greyedOut = false;
                    MapModMain.config.immersionOption = whichOption - 3;
                }
            }
        }

        public void greyOut()
        {
            this.isActive = false;
            this.greyedOut = true;
        }

        public override void draw(SpriteBatch b, int slotX, int slotY)
        {
            base.draw(b, slotX - 32, slotY);
        }
    }

    public class MapModCheckbox : OptionsElement
    {
        public const int pixelsWide = 9;
        public bool isChecked;
        public static Rectangle sourceRectUnchecked = new Rectangle(227, 425, 9, 9);
        public static Rectangle sourceRectChecked = new Rectangle(236, 425, 9, 9);

        public MapModCheckbox(string label, int whichOption, int x = -1, int y = -1) : base(label, x, y, 9 * Game1.pixelZoom, 9 * Game1.pixelZoom, whichOption)
        {
            if (whichOption > 43)
            {
                this.label = MapModMain.modHelper.Translation.Get(label);
            }

            switch (whichOption)
            {
                case 7:
                    this.isChecked = MapModMain.config.showAbigail;
                    return;
                case 8:
                    this.isChecked = MapModMain.config.showAlex;
                    return;
                case 9:
                    this.isChecked = MapModMain.config.showCaroline;
                    return;
                case 10:
                    this.isChecked = MapModMain.config.showClint;
                    return;
                case 11:
                    this.isChecked = MapModMain.config.showDemetrius;
                    return;
                case 12:
                    this.isChecked = MapModMain.config.showElliott;
                    return;
                case 13:
                    this.isChecked = MapModMain.config.showEmily;
                    return;
                case 14:
                    this.isChecked = MapModMain.config.showEvelyn;
                    return;
                case 15:
                    this.isChecked = MapModMain.config.showGeorge;
                    return;
                case 16:
                    this.isChecked = MapModMain.config.showGus;
                    return;
                case 17:
                    this.isChecked = MapModMain.config.showHaley;
                    return;
                case 18:
                    this.isChecked = MapModMain.config.showHarvey;
                    return;
                case 19:
                    this.isChecked = MapModMain.config.showJas;
                    return;
                case 20:
                    this.isChecked = MapModMain.config.showJodi;
                    return;
                case 21:
                    this.isChecked = MapModMain.config.showKent;
                    return;
                case 22:
                    this.isChecked = MapModMain.config.showLeah;
                    return;
                case 23:
                    this.isChecked = MapModMain.config.showLewis;
                    return;
                case 24:
                    this.isChecked = MapModMain.config.showLinus;
                    return;
                case 25:
                    this.isChecked = MapModMain.config.showMarnie;
                    return;
                case 26:
                    this.isChecked = MapModMain.config.showMaru;
                    return;
                case 27:
                    this.isChecked = MapModMain.config.showPam;
                    return;
                case 28:
                    this.isChecked = MapModMain.config.showPenny;
                    return;
                case 29:
                    this.isChecked = MapModMain.config.showPierre;
                    return;
                case 30:
                    this.isChecked = MapModMain.config.showRobin;
                    return;
                case 31:
                    this.isChecked = MapModMain.config.showSam;
                    return;
                case 32:
                    this.isChecked = MapModMain.config.showSebastian;
                    return;
                case 33:
                    this.isChecked = MapModMain.config.showShane;
                    return;
                case 34:
                    this.isChecked = MapModMain.config.showVincent;
                    return;
                case 35:
                    this.isChecked = MapModMain.config.showWilly;
                    return;
                case 36:
                    this.isChecked = MapModMain.config.showSandy;
                    return;
                case 37:
                    this.isChecked = MapModMain.config.showWizard;
                    return;
                case 38:
                    this.isChecked = MapModMain.config.showMarlon;
                    return;
                case 39:
                    this.isChecked = MapModMain.config.showCustomNPC1;
                    return;
                case 40:
                    this.isChecked = MapModMain.config.showCustomNPC2;
                    return;
                case 41:
                    this.isChecked = MapModMain.config.showCustomNPC3;
                    return;
                case 42:
                    this.isChecked = MapModMain.config.showCustomNPC4;
                    return;
                case 43:
                    this.isChecked = MapModMain.config.showCustomNPC5;
                    return;
                case 44:
                    this.isChecked = MapModMain.config.onlySameLocation;
                    return;
                case 45:
                    this.isChecked = MapModMain.config.byHeartLevel;
                    return;
                case 46:
                    this.isChecked = MapModMain.config.markQuests;
                    return;
                case 47:
                    this.isChecked = MapModMain.config.showHiddenVillagers;
                    return;
                case 48:
                    this.isChecked = MapModMain.config.showTravelingMerchant;
                    return;
                default:
                    return;
            }
        }

        public override void receiveLeftClick(int x, int y)
        {
            if (this.greyedOut)
            {
                return;
            }
            //Game1.soundBank.PlayCue("drumkit6");
            base.receiveLeftClick(x, y);
            this.isChecked = !this.isChecked;
            int whichOption = this.whichOption;
            switch (whichOption)
            {
                case 7:
                    MapModMain.config.showAbigail = this.isChecked;
                    break;
                case 8:
                    MapModMain.config.showAlex = this.isChecked;
                    break;
                case 9:
                    MapModMain.config.showCaroline = this.isChecked;
                    break;
                case 10:
                    MapModMain.config.showClint = this.isChecked;
                    break;
                case 11:
                    MapModMain.config.showDemetrius = this.isChecked;
                    break;
                case 12:
                    MapModMain.config.showElliott = this.isChecked;
                    break;
                case 13:
                    MapModMain.config.showEmily = this.isChecked;
                    break;
                case 14:
                    MapModMain.config.showEvelyn = this.isChecked;
                    break;
                case 15:
                    MapModMain.config.showGeorge = this.isChecked;
                    break;
                case 16:
                    MapModMain.config.showGus = this.isChecked;
                    break;
                case 17:
                    MapModMain.config.showHaley = this.isChecked;
                    break;
                case 18:
                    MapModMain.config.showHarvey = this.isChecked;
                    break;
                case 19:
                    MapModMain.config.showJas = this.isChecked;
                    break;
                case 20:
                    MapModMain.config.showJodi = this.isChecked;
                    break;
                case 21:
                    MapModMain.config.showKent = this.isChecked;
                    break;
                case 22:
                    MapModMain.config.showLeah = this.isChecked;
                    break;
                case 23:
                    MapModMain.config.showLewis = this.isChecked;
                    break;
                case 24:
                    MapModMain.config.showLinus = this.isChecked;
                    break;
                case 25:
                    MapModMain.config.showMarnie = this.isChecked;
                    break;
                case 26:
                    MapModMain.config.showMaru = this.isChecked;
                    break;
                case 27:
                    MapModMain.config.showPam = this.isChecked;
                    break;
                case 28:
                    MapModMain.config.showPenny = this.isChecked;
                    break;
                case 29:
                    MapModMain.config.showPierre = this.isChecked;
                    break;
                case 30:
                    MapModMain.config.showRobin = this.isChecked;
                    break;
                case 31:
                    MapModMain.config.showSam = this.isChecked;
                    break;
                case 32:
                    MapModMain.config.showSebastian = this.isChecked;
                    break;
                case 33:
                    MapModMain.config.showShane = this.isChecked;
                    break;
                case 34:
                    MapModMain.config.showVincent = this.isChecked;
                    break;
                case 35:
                    MapModMain.config.showWilly = this.isChecked;
                    break;
                case 36:
                    MapModMain.config.showSandy = this.isChecked;
                    break;
                case 37:
                    MapModMain.config.showWizard = this.isChecked;
                    break;
                case 38:
                    MapModMain.config.showMarlon = this.isChecked;
                    break;
                case 39:
                    MapModMain.config.showCustomNPC1 = this.isChecked;
                    break;
                case 40:
                    MapModMain.config.showCustomNPC2 = this.isChecked;
                    break;
                case 41:
                    MapModMain.config.showCustomNPC3 = this.isChecked;
                    break;
                case 42:
                    MapModMain.config.showCustomNPC4 = this.isChecked;
                    break;
                case 43:
                    MapModMain.config.showCustomNPC5 = this.isChecked;
                    break;
                case 44:
                    MapModMain.config.onlySameLocation = this.isChecked;
                    break;
                case 45:
                    MapModMain.config.byHeartLevel = this.isChecked;
                    break;
                case 46:
                    MapModMain.config.markQuests = this.isChecked;
                    break;
                case 47:
                    MapModMain.config.showHiddenVillagers = this.isChecked;
                    break;
                case 48:
                    MapModMain.config.showTravelingMerchant = this.isChecked;
                    break;
                default:
                    break;
            }
            MapModMain.modHelper.WriteJsonFile($"data/{MapModMain.saveName}.json", MapModMain.config);
        }

        public override void draw(SpriteBatch b, int slotX, int slotY)
        {
            b.Draw(Game1.mouseCursors, new Vector2((float)(slotX + this.bounds.X), (float)(slotY + this.bounds.Y)), new Rectangle?(this.isChecked ? OptionsCheckbox.sourceRectChecked : OptionsCheckbox.sourceRectUnchecked), Color.White * (this.greyedOut ? 0.33f : 1f), 0f, Vector2.Zero, (float)Game1.pixelZoom, SpriteEffects.None, 0.4f);
            if (whichOption > 6 && whichOption < 39)
            {
                foreach (NPC npc in Utility.getAllCharacters())
                {
                    var name = npc.getName();
                    if (string.IsNullOrEmpty(name))
                    {
                        name = npc.name;
                    }
                    if (name.Equals(this.label))
                    {
                        if (this.isChecked)
                        {
                            Game1.spriteBatch.Draw(npc.sprite.Texture, new Vector2((float)slotX + this.bounds.X + 50, slotY), new Rectangle?(new Rectangle(0, MapModMain.markerCrop[npc.name], 16, 15)), Color.White, 0f, Vector2.Zero, (float)Game1.pixelZoom, SpriteEffects.None, 0.4f);
                        }
                        else
                        {
                            Game1.spriteBatch.Draw(npc.sprite.Texture, new Vector2((float)slotX + this.bounds.X + 50, slotY), new Rectangle?(new Rectangle(0, MapModMain.markerCrop[npc.name], 16, 15)), Color.White * 0.33f, 0f, Vector2.Zero, (float)Game1.pixelZoom, SpriteEffects.None, 0.4f);
                        }
                        base.draw(b, slotX + 75, slotY);
                        break;
                    }
                }
            }
            else
            {
                base.draw(b, slotX, slotY);
            }
        }
    }

    internal class MapModSlider : OptionsElement
    {
        public static Rectangle sliderBGSource = new Rectangle(403, 383, 6, 6);
        public static Rectangle sliderButtonRect = new Rectangle(420, 441, 10, 6);
        public const int pixelsWide = 48;
        public const int pixelsHigh = 6;
        public const int sliderButtonWidth = 10;
        public int sliderMaxValue = 12;
        public int value;
        public string valueLabel;

        public MapModSlider(string label, int whichOption, int x = -1, int y = -1) : base(label, x, y, 48 * Game1.pixelZoom, 6 * Game1.pixelZoom, whichOption)
        {
            valueLabel = MapModMain.modHelper.Translation.Get(label);
            if (whichOption == 0)
            {
                this.value = MapModMain.config.heartLevelMin;
            }
            else if (whichOption == 1)
            {
                this.value = MapModMain.config.heartLevelMax;
            }
        }

        public override void leftClickHeld(int x, int y)
        {
            if (this.greyedOut)
            {
                return;
            }
            base.leftClickHeld(x, y);
            this.value = ((x >= this.bounds.X) ? ((x <= this.bounds.Right - 10 * Game1.pixelZoom) ? ((int)((double)((float)(x - this.bounds.X) / (float)(this.bounds.Width - 10 * Game1.pixelZoom)) * (double)this.sliderMaxValue)) : this.sliderMaxValue) : 0);
            if (this.whichOption == 0)
            {
                MapModMain.config.heartLevelMin = this.value;
            }
            else if (this.whichOption == 1)
            {
                MapModMain.config.heartLevelMax = this.value;
            }
            MapModMain.modHelper.WriteJsonFile($"data/{MapModMain.saveName}.json", MapModMain.config);
        }

        public override void receiveLeftClick(int x, int y)
        {
            if (this.greyedOut)
            {
                return;
            }
            base.receiveLeftClick(x, y);
            this.leftClickHeld(x, y);
        }

        public override void draw(SpriteBatch b, int slotX, int slotY)
        {
            this.label = valueLabel + ": " + this.value;
            this.greyedOut = false;
            if (this.whichOption == 0 || this.whichOption == 1)
            {
                this.greyedOut = !MapModMain.config.byHeartLevel;
            }
            base.draw(b, slotX, slotY);
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, OptionsSlider.sliderBGSource, slotX + this.bounds.X, slotY + this.bounds.Y, this.bounds.Width, this.bounds.Height, Color.White, (float)Game1.pixelZoom, false);
            b.Draw(Game1.mouseCursors, new Vector2((float)(slotX + this.bounds.X) + (float)(this.bounds.Width - 10 * Game1.pixelZoom) * ((float)this.value / (float)this.sliderMaxValue), (float)(slotY + this.bounds.Y)), new Rectangle?(OptionsSlider.sliderButtonRect), Color.White, 0f, Vector2.Zero, (float)Game1.pixelZoom, SpriteEffects.None, 0.9f);
        }
    }
}

