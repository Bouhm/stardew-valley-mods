/*
Menu settings for the mod. 
Menu code regurgitated from the game code
Settings loaded from this.Config file and changes saved onto this.Config file.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;

namespace NPCMapLocations
{
    public class ModMenu : IClickableMenu
    {
        private readonly IModHelper Helper;
        private readonly ModConfig Config;
        private readonly Texture2D map;
        private readonly List<ClickableComponent> optionSlots = new List<ClickableComponent>();
        private readonly ClickableTextureComponent upArrow;
        private readonly ClickableTextureComponent downArrow;
        private readonly ClickableTextureComponent scrollBar;
        private readonly MapModButton immersionButton1;
        private readonly MapModButton immersionButton2;
        private readonly MapModButton immersionButton3;
        private readonly List<OptionsElement> options = new List<OptionsElement>();
        private readonly Rectangle scrollBarRunner;
        private readonly ClickableTextureComponent okButton;
        private readonly int mapX;
        private readonly int mapY;
        private const int itemsPerPage = 7;
        private const int indexOfGraphicsPage = 6;
        private int currentItemIndex;
        private bool scrolling;
        private bool canClose;
        private int optionsSlotHeld = -1;
            
        public ModMenu(
            int x, 
            int y, 
            int width, 
            int height, 
            Dictionary<string, bool> secondaryNpcs, 
            Dictionary<string, object> customNpcs, 
            Dictionary<string, string> npcNames, 
            Dictionary<string, int> markerCrop, 
            IModHelper helper, 
            ModConfig config
        ) : base(x, y, width, height, false)
        {
            this.Helper = helper;
            this.Config = config;
            this.map = Game1.content.Load<Texture2D>("LooseSprites\\map");
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
            string immersionLabel = helper.Translation.Get("immersion.label");
            string extraLabel = helper.Translation.Get("extra.label");
            string villagersLabel = helper.Translation.Get("villagers.label");
            immersionButton1 = new MapModButton("immersion.option1", 4, -1, -1, -1, -1, helper, config);
            immersionButton2 = new MapModButton("immersion.option2", 5, -1, -1, -1, -1, helper, config);
            immersionButton3 = new MapModButton("immersion.option3", 6, -1, -1, -1, -1, helper, config);

            //this.options.Add(new OptionsElement("Menu Key:"));
            //this.options.Add(new MapModInputListener("Change menu key", 37, this.optionSlots[0].bounds.Width, -1, -1));
            this.options.Add(new OptionsElement("NPC Map Locations"));
            this.options.Add(new OptionsElement(immersionLabel));
            this.options.Add(immersionButton1);
            this.options.Add(immersionButton2);
            this.options.Add(immersionButton3);
            this.options.Add(new ModCheckbox("immersion.option4", 44, -1, -1, customNpcs, npcNames, markerCrop, helper, config));
            this.options.Add(new ModCheckbox("immersion.option5", 45, -1, -1, customNpcs, npcNames, markerCrop, helper, config));
            this.options.Add(new MapModSlider("immersion.slider1", 0, -1, -1, helper, config));
            this.options.Add(new MapModSlider("immersion.slider2", 1, -1, -1, helper, config));
            this.options.Add(new OptionsElement(extraLabel));
            this.options.Add(new ModCheckbox("extra.option1", 46, -1, -1, customNpcs, npcNames, markerCrop, helper, config));
            this.options.Add(new ModCheckbox("extra.option2", 47, -1, -1, customNpcs, npcNames, markerCrop, helper, config));
            this.options.Add(new ModCheckbox("extra.option3", 48, -1, -1, customNpcs, npcNames, markerCrop, helper, config));
            this.options.Add(new OptionsElement(villagersLabel));
            // Custom Npcs
            if (customNpcs != null)
            {
                for (int i = 0; i < customNpcs.Count; i++)
                    this.options.Add(new ModCheckbox(customNpcs.Keys.ElementAt(i), 39 + i, -1, -1, customNpcs, npcNames, markerCrop, helper, config));
            }
            // Villagers
            var orderedNames = npcNames.Keys.ToList();
            orderedNames.Sort();
            int idx = 7;
            foreach (var name in orderedNames)
            {
                if (secondaryNpcs.ContainsKey(name))
                    if (secondaryNpcs[name])
                    {
                    this.options.Add(new ModCheckbox(name, idx++, -1, -1, customNpcs, npcNames, markerCrop, helper, config));
                    }   
                    else
                    {
                        idx++;
                        continue;
                    }
                else
                {
                    this.options.Add(new ModCheckbox(name, idx++, -1, -1, customNpcs, npcNames, markerCrop, helper, config));
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
            if (options.Any())
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
                this.SetScrollBarToCurrentIndex();
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
                this.exitThisMenu(true);
                return;
            }
            if (key.ToString().Equals(this.Config.MenuKey) && this.readyToClose() && this.canClose)
            {
                Game1.exitActiveMenu();
                Game1.activeClickableMenu = new GameMenu();
                (Game1.activeClickableMenu as GameMenu).changeTab(3);
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
                this.UpArrowPressed();
                Game1.playSound("shiny4");
                return;
            }
            if (direction < 0 && this.currentItemIndex < Math.Max(0, this.options.Count<OptionsElement>() - 7))
            {
                this.DownArrowPressed();
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
            if (GameMenu.forcePreventClose)
            {
                return;
            }
            if (this.downArrow.containsPoint(x, y) && this.currentItemIndex < Math.Max(0, this.options.Count<OptionsElement>() - 7))
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
            else if (!this.downArrow.containsPoint(x, y) && x > this.xPositionOnScreen + this.width && x < this.xPositionOnScreen + this.width + Game1.tileSize * 2 && y > this.yPositionOnScreen && y < this.yPositionOnScreen + this.height)
            {
                this.scrolling = true;
                this.leftClickHeld(x, y);
            }
            else if (immersionButton1.rect.Contains(x, y))
            {
                immersionButton1.receiveLeftClick(x, y);
                immersionButton2.GreyOut();
                immersionButton3.GreyOut();
            }
            else if (immersionButton2.rect.Contains(x, y))
            {
                immersionButton2.receiveLeftClick(x, y);
                immersionButton1.GreyOut();
                immersionButton3.GreyOut();
            }
            else if (immersionButton3.rect.Contains(x, y))
            {
                immersionButton3.receiveLeftClick(x, y);
                immersionButton1.GreyOut();
                immersionButton2.GreyOut();
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

        // Draw menu
        public override void draw(SpriteBatch b)
        {
            if (Game1.options.showMenuBackground)
            {
                drawBackground(b);
            }
            Game1.drawDialogueBox(this.mapX - Game1.pixelZoom * 8, this.mapY - Game1.pixelZoom * 24, (this.map.Bounds.Width + 16) * Game1.pixelZoom, 212 * Game1.pixelZoom, false, true, null, false);
            b.Draw(this.map, new Vector2((float)this.mapX, (float)this.mapY), new Rectangle?(new Rectangle(0, 0, 300, 180)), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.86f);
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.4f);
            Game1.drawDialogueBox(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, false, true, null, false);
            this.okButton.draw(b);
            int buttonWidth = (int)Game1.dialogueFont.MeasureString(this.Helper.Translation.Get("immersion.option3")).X;
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
                            Rectangle bounds = new Rectangle(x + 28, y, buttonWidth + Game1.tileSize + 8, Game1.tileSize + 8);
                            switch (options[this.currentItemIndex + i].whichOption)
                            {
                                case 4:
                                    immersionButton1.rect = bounds;
                                    break;
                                case 5:
                                    immersionButton2.rect = bounds;
                                    break;
                                case 6:
                                    immersionButton3.rect = bounds;
                                    break;
                            }

                            IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), bounds.X, bounds.Y, bounds.Width, bounds.Height, Color.White * (options[this.currentItemIndex + i].greyedOut ? 0.33f : 1f), 1f, false);
                        }
                        if (this.currentItemIndex + i == 0)
                        {
                            Utility.drawTextWithShadow(b, "NPC Map Locations", Game1.dialogueFont, new Vector2(x + Game1.tileSize / 2, y + Game1.tileSize / 4), Color.Black);
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

     // Mod button for the three main modes
    internal class MapModButton : OptionsElement
    {
        private readonly IModHelper Helper;
        private ModConfig Config;
        public const int pixelsWide = 9;
        public bool isActive;
        public Rectangle rect;

        public MapModButton(
            string label, 
            int whichOption, 
            int x, 
            int y, 
            int width, 
            int height, 
            IModHelper helper,
            ModConfig config
        ) : base(label, x, y, 9 * Game1.pixelZoom, 9 * Game1.pixelZoom, whichOption)
        {
            this.Helper = helper;
            this.Config = config;
            this.label = helper.Translation.Get(label);
            this.rect = new Rectangle(x, y, width, height);

            if (config.ImmersionOption == whichOption - 3)
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
                    this.Config.ImmersionOption = whichOption - 3;
                }
            }
        }

        public void GreyOut()
        {
            this.isActive = false;
            this.greyedOut = true;
        }

        public override void draw(SpriteBatch b, int slotX, int slotY)
        {
            base.draw(b, slotX - 32, slotY);
        }
    }

    // Mod checkbox for settings and npc blacklst
    internal class ModCheckbox : OptionsElement
    {
        private readonly IModHelper Helper;
        private readonly Dictionary<string, string> NpcNames;
        private readonly Dictionary<string, int> MarkerCrop;
        private readonly Dictionary<string, object> CustomNpcs;
        private ModConfig Config;
        private List<string> orderedNames;
        public const int pixelsWide = 9;
        public bool isChecked;
        public static Rectangle sourceRectUnchecked = new Rectangle(227, 425, 9, 9);
        public static Rectangle sourceRectChecked = new Rectangle(236, 425, 9, 9);

        public ModCheckbox(
            string label,
            int whichOption,
            int x,
            int y,
            Dictionary<string, object> customNpcs,
            Dictionary<string, string> npcNames,
            Dictionary<string, int> markerCrop,
            IModHelper helper,
            ModConfig config
        ) : base(label, x, y, 9 * Game1.pixelZoom, 9 * Game1.pixelZoom, whichOption)                                                       
        {
            this.Helper = helper;
            this.Config = config;
            this.MarkerCrop = markerCrop;
            this.NpcNames = npcNames;
            this.CustomNpcs = customNpcs;
            this.label = label;

            if (whichOption < 44)
            {
                orderedNames = npcNames.Keys.ToList();
                orderedNames.Sort();

                if (whichOption > 6 && whichOption < 39)
                {
                    this.isChecked = !this.Config.NpcBlacklist.Contains(orderedNames[whichOption - 7]);
                    return;
                }
                else if (whichOption > 38 && whichOption < 44)
                {
                    this.isChecked = !this.Config.CustomNpcBlacklist.Contains(customNpcs.Keys.ElementAt(whichOption - 39));
                    return;
                }
            }
            else if (whichOption > 43)
            {
                this.label = this.Helper.Translation.Get(label);
            }
            switch (whichOption)
            {
                case 44:
                    this.isChecked = this.Config.OnlySameLocation;
                    return;
                case 45:
                    this.isChecked = this.Config.ByHeartLevel;
                    return;
                case 46:
                    this.isChecked = this.Config.MarkQuests;
                    return;
                case 47:
                    this.isChecked = this.Config.ShowHiddenVillagers;
                    return;
                case 48:
                    this.isChecked = this.Config.ShowTravelingMerchant;
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

            if (whichOption > 6 && whichOption < 39)
            {
                if (this.isChecked)
                    this.Config.NpcBlacklist.Remove(orderedNames[whichOption - 7]);
                else
                    this.Config.NpcBlacklist.Add(orderedNames[whichOption - 7]);
            }
            else if (whichOption > 38 && whichOption < 44)
            {
                if (this.isChecked)
                    this.Config.CustomNpcBlacklist.Remove(this.CustomNpcs.Keys.ElementAt(whichOption - 39));
                else
                    this.Config.CustomNpcBlacklist.Add(this.CustomNpcs.Keys.ElementAt(whichOption - 39));
            }
            else
            {
                switch (whichOption)
                {
                    case 44:
                        this.Config.OnlySameLocation = this.isChecked;
                        break;
                    case 45:
                        this.Config.ByHeartLevel = this.isChecked;
                        break;
                    case 46:
                        this.Config.MarkQuests = this.isChecked;
                        break;
                    case 47:
                        this.Config.ShowHiddenVillagers = this.isChecked;
                        break;
                    case 48:
                        this.Config.ShowTravelingMerchant = this.isChecked;
                        break;
                    default:
                        break;
                }
            }
            this.Helper.WriteConfig(this.Config);
        }

        public override void draw(SpriteBatch b, int slotX, int slotY)
        {
            b.Draw(Game1.mouseCursors, new Vector2((float)(slotX + this.bounds.X), (float)(slotY + this.bounds.Y)), new Rectangle?(this.isChecked ? OptionsCheckbox.sourceRectChecked : OptionsCheckbox.sourceRectUnchecked), Color.White * (this.greyedOut ? 0.33f : 1f), 0f, Vector2.Zero, (float)Game1.pixelZoom, SpriteEffects.None, 0.4f);
            if (whichOption > 6 && whichOption < 39)
            {
                NPC npc = Game1.getCharacterFromName(label);
                if (npc == null) { return; }

                if (this.isChecked)
                {
                    Game1.spriteBatch.Draw(npc.Sprite.Texture, new Vector2((float)slotX + this.bounds.X + 50, slotY), new Rectangle?(new Rectangle(0, MarkerCrop[npc.Name], 16, 15)), Color.White, 0f, Vector2.Zero, (float)Game1.pixelZoom, SpriteEffects.None, 0.4f);
                }
                else
                {
                    Game1.spriteBatch.Draw(npc.Sprite.Texture, new Vector2((float)slotX + this.bounds.X + 50, slotY), new Rectangle?(new Rectangle(0, MarkerCrop[npc.Name], 16, 15)), Color.White * 0.33f, 0f, Vector2.Zero, (float)Game1.pixelZoom, SpriteEffects.None, 0.4f);
                }

                // Draw names
                slotX += 75;
                if (this.whichOption == -1)
                    SpriteText.drawString(b, this.label, slotX + this.bounds.X, slotY + this.bounds.Y + 12, 999, -1, 999, 1f, 0.1f, false, -1, "", -1);
                else
                    Utility.drawTextWithShadow(b, NpcNames[label], Game1.dialogueFont, new Vector2((float)(slotX + this.bounds.X + this.bounds.Width + 8), (float)(slotY + this.bounds.Y)), this.greyedOut ? Game1.textColor * 0.33f : Game1.textColor, 1f, 0.1f, -1, -1, 1f, 3);
            }
            else
            {
                base.draw(b, slotX, slotY);
            }
        }
    }

    // Mod slider for heart level this.Config
    internal class MapModSlider : OptionsElement
    {
        private readonly IModHelper Helper;
        private ModConfig Config;
        public static Rectangle sliderBGSource = new Rectangle(403, 383, 6, 6);
        public static Rectangle sliderButtonRect = new Rectangle(420, 441, 10, 6);
        public const int pixelsWide = 48;
        public const int pixelsHigh = 6;
        public const int sliderButtonWidth = 10;
        public int sliderMaxValue = 12;
        public int value;
        public string valueLabel;

        public MapModSlider(
            string label,
            int whichOption,
            int x,
            int y,
            IModHelper helper,
            ModConfig config
        ) : base(label, x, y, 48 * Game1.pixelZoom, 6 * Game1.pixelZoom, whichOption)
        {
            this.Helper = helper;
            this.Config = config;
            valueLabel = helper.Translation.Get(label);

            if (whichOption == 0)
            {
                this.value = this.Config.HeartLevelMin;
            }
            else if (whichOption == 1)
            {
                this.value = this.Config.HeartLevelMax;
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
                this.Config.HeartLevelMin = this.value;
            }
            else if (this.whichOption == 1)
            {
                this.Config.HeartLevelMax = this.value;
            }
            this.Helper.WriteConfig(this.Config);
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
                this.greyedOut = !this.Config.ByHeartLevel;
            }
            base.draw(b, slotX, slotY);
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, OptionsSlider.sliderBGSource, slotX + this.bounds.X, slotY + this.bounds.Y, this.bounds.Width, this.bounds.Height, Color.White, (float)Game1.pixelZoom, false);
            b.Draw(Game1.mouseCursors, new Vector2((float)(slotX + this.bounds.X) + (float)(this.bounds.Width - 10 * Game1.pixelZoom) * ((float)this.value / (float)this.sliderMaxValue), (float)(slotY + this.bounds.Y)), new Rectangle?(OptionsSlider.sliderButtonRect), Color.White, 0f, Vector2.Zero, (float)Game1.pixelZoom, SpriteEffects.None, 0.9f);
        }
    }    
}

