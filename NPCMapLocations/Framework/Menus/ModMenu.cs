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
using NPCMapLocations.Framework.Models;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace NPCMapLocations.Framework.Menus
{
    public class ModMenu : IClickableMenu
    {
        /*********
        ** Fields
        *********/
        private readonly ClickableTextureComponent DownArrow;
        private readonly VillagerVisibilityButton ImmersionAllButton;
        private readonly VillagerVisibilityButton ImmersionTalkedToButton;
        private readonly VillagerVisibilityButton ImmersionNotTalkedToButton;
        private readonly ClickableTextureComponent OkButton;
        private readonly List<OptionsElement> Options = new();
        private readonly List<ClickableComponent> OptionSlots = new();
        private readonly ClickableTextureComponent ScrollBar;
        private readonly Rectangle ScrollBarRunner;
        private readonly ClickableTextureComponent UpArrow;
        private bool CanClose;
        private int CurrentItemIndex;
        private int OptionsSlotHeld = -1;
        private bool Scrolling;


        /*********
        ** Public methods
        *********/
        public ModMenu(Dictionary<string, NpcMarker> npcMarkers, Dictionary<string, bool> conditionalNpcs, Action onMinimapToggled)
            : base(Game1.viewport.Width / 2 - (1000 + IClickableMenu.borderWidth * 2) / 2, Game1.viewport.Height / 2 - (600 + IClickableMenu.borderWidth * 2) / 2, 1000 + IClickableMenu.borderWidth * 2, 600 + IClickableMenu.borderWidth * 2, true)
        {
            // Most of this mess is straight from the game code just... just give it space
            this.OkButton = new ClickableTextureComponent(
                "OK",
                new Rectangle(this.xPositionOnScreen + this.width - Game1.tileSize * 2, this.yPositionOnScreen + this.height - 7 * Game1.tileSize / 4, Game1.tileSize, Game1.tileSize),
                null,
                null,
                Game1.mouseCursors,
                Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46),
                1f
            );

            this.UpArrow = new ClickableTextureComponent(
                new Rectangle(this.xPositionOnScreen + this.width + Game1.tileSize / 4, this.yPositionOnScreen + Game1.tileSize, 11 * Game1.pixelZoom, 12 * Game1.pixelZoom),
                Game1.mouseCursors,
                new Rectangle(421, 459, 11, 12),
                Game1.pixelZoom
            );

            this.DownArrow = new ClickableTextureComponent(
                new Rectangle(this.xPositionOnScreen + this.width + Game1.tileSize / 4, this.yPositionOnScreen + this.height - Game1.tileSize, 11 * Game1.pixelZoom, 12 * Game1.pixelZoom),
                Game1.mouseCursors,
                new Rectangle(421, 472, 11, 12),
                Game1.pixelZoom
            );

            this.ScrollBar = new ClickableTextureComponent(
                new Rectangle(this.UpArrow.bounds.X + Game1.pixelZoom * 3, this.UpArrow.bounds.Y + this.UpArrow.bounds.Height + Game1.pixelZoom, 6 * Game1.pixelZoom, 10 * Game1.pixelZoom),
                Game1.mouseCursors,
                new Rectangle(435, 463, 6, 10),
                Game1.pixelZoom
            );

            this.ScrollBarRunner = new Rectangle(this.ScrollBar.bounds.X, this.UpArrow.bounds.Y + this.UpArrow.bounds.Height + Game1.pixelZoom, this.ScrollBar.bounds.Width, this.height - Game1.tileSize * 2 - this.UpArrow.bounds.Height - Game1.pixelZoom * 2);

            for (int i = 0; i < 7; i++)
            {
                this.OptionSlots.Add(new ClickableComponent(
                    new Rectangle(this.xPositionOnScreen + Game1.tileSize / 4, this.yPositionOnScreen + Game1.tileSize * 5 / 4 + Game1.pixelZoom + i * ((this.height - Game1.tileSize * 2) / 7), this.width - Game1.tileSize / 2, (this.height - Game1.tileSize * 2) / 7 + Game1.pixelZoom), string.Concat(i)
                ));
            }

            this.ImmersionAllButton = new VillagerVisibilityButton(
                label: I18n.Immersion_AlwaysShowVillagers(),
                id: VillagerVisibility.All,
                get: () => ModEntry.Config.ImmersionOption,
                set: value => ModEntry.Config.ImmersionOption = value
            );
            this.ImmersionTalkedToButton = new VillagerVisibilityButton(
                label: I18n.Immersion_OnlyVillagersTalkedTo(),
                id: VillagerVisibility.TalkedTo,
                get: () => ModEntry.Config.ImmersionOption,
                set: value => ModEntry.Config.ImmersionOption = value
            );
            this.ImmersionNotTalkedToButton = new VillagerVisibilityButton(
                label: I18n.Immersion_HideVillagersTalkedTo(),
                id: VillagerVisibility.NotTalkedTo,
                get: () => ModEntry.Config.ImmersionOption,
                set: value => ModEntry.Config.ImmersionOption = value
            );

            this.Options.AddRange(new[]
            {
                new OptionsElement("NPC Map Locations"),

                // minimap enabled
                new OptionsElement(I18n.Minimap_Label()),
                new ModCheckbox(
                    label: I18n.Minimap_Enabled(),
                    value: ModEntry.Globals.ShowMinimap,
                    set: value =>
                    {
                        ModEntry.Globals.ShowMinimap = value;
                        onMinimapToggled();
                    }
                ),

                // minimap size
                new ModCheckbox(
                    label: I18n.Minimap_Locked(),
                    value: ModEntry.Globals.LockMinimapPosition,
                    set: value => ModEntry.Globals.LockMinimapPosition = value
                ),
                new ModPlusMinus(
                    label: I18n.Minimap_Width(),
                    value: ModEntry.Globals.MinimapWidth,
                    min: 75,
                    max: 300,
                    step: 15,
                    set: value => ModEntry.Globals.MinimapWidth = value,
                    format: value => $"{value}px",
                    shouldGrayOut: () => !ModEntry.Globals.ShowMinimap
                ),
                new ModPlusMinus(
                    label: I18n.Minimap_Height(),
                    value: ModEntry.Globals.MinimapHeight,
                    min: 45,
                    max: 180,
                    step: 15,
                    set: value => ModEntry.Globals.MinimapHeight = value,
                    format: value => $"{value}px",
                    shouldGrayOut: () => !ModEntry.Globals.ShowMinimap
                ),

                // NPC visibility
                new OptionsElement(I18n.Immersion_Label()),
                this.ImmersionAllButton,
                this.ImmersionTalkedToButton,
                this.ImmersionNotTalkedToButton,
                new ModCheckbox(
                    label: I18n.Immersion_OnlyVillagersInPlayerLocation(),
                    value: ModEntry.Globals.OnlySameLocation,
                    set: value => ModEntry.Globals.OnlySameLocation = value
                ),
                new ModCheckbox(
                    label: I18n.Immersion_OnlyVillagersWithinHeartLevel(),
                    value: ModEntry.Config.ByHeartLevel,
                    set: value => ModEntry.Config.ByHeartLevel = value
                ),
                new MapModSlider(
                    label: I18n.Immersion_MinHeartLevel(),
                    value: ModEntry.Config.HeartLevelMin,
                    min: 0,
                    max: PlayerConfig.MaxPossibleHeartLevel,
                    set: value => ModEntry.Config.HeartLevelMin = value,
                    shouldGrayOut: () => !ModEntry.Config.ByHeartLevel
                ),
                new MapModSlider(
                    label: I18n.Immersion_MaxHeartLevel(),
                    value: ModEntry.Config.HeartLevelMax,
                    min: 0,
                    max: PlayerConfig.MaxPossibleHeartLevel,
                    set: value => ModEntry.Config.HeartLevelMax = value,
                    shouldGrayOut: () => !ModEntry.Config.ByHeartLevel
                ),

                // extra icons
                new OptionsElement(I18n.Extra_Label()),
                new ModCheckbox(
                    label: I18n.Extra_ShowQuestsOrBirthdays(),
                    value: ModEntry.Globals.ShowQuests,
                    set: value => ModEntry.Globals.ShowQuests = value
                ),
                new ModCheckbox(
                    label: I18n.Extra_ShowHiddenVillagers(),
                    value: ModEntry.Globals.ShowHiddenVillagers,
                    set: value => ModEntry.Globals.ShowHiddenVillagers = value
                ),
                new ModCheckbox(
                    label: I18n.Extra_ShowTravelingMerchant(),
                    value: ModEntry.Globals.ShowTravelingMerchant,
                    set: value => ModEntry.Globals.ShowTravelingMerchant = value
                ),

                new OptionsElement(I18n.Villagers_Label())
            });

            this.Options.AddRange(
                from entry in npcMarkers
                let name = entry.Key
                let marker = entry.Value

                where
                    marker.Sprite != null
                    && marker.Type == CharacterType.Villager
                    && (!conditionalNpcs.TryGetValue(name, out bool enabled) || enabled)

                orderby marker.DisplayName

                select new ModCheckbox(
                    label: marker.DisplayName,
                    value: !ModEntry.ShouldExcludeNpc(name),
                    set: value =>
                    {
                        bool exclude = !value;
                        if (exclude == ModEntry.ShouldExcludeNpc(name, ignoreConfig: true))
                            ModEntry.Config.ForceNpcVisibility.Remove(name);
                        else
                            ModEntry.Config.ForceNpcVisibility[name] = exclude;
                    }
                )
            );
        }

        // Override snappy controls on controller
        public override bool overrideSnappyMenuCursorMovementBan()
        {
            return true;
        }

        public override void leftClickHeld(int x, int y)
        {
            if (GameMenu.forcePreventClose) return;

            base.leftClickHeld(x, y);
            if (this.Scrolling)
            {
                int y2 = this.ScrollBar.bounds.Y;
                this.ScrollBar.bounds.Y = Math.Min(
                    this.yPositionOnScreen + this.height - Game1.tileSize - Game1.pixelZoom * 3 - this.ScrollBar.bounds.Height,
                    Math.Max(y, this.yPositionOnScreen + this.UpArrow.bounds.Height + Game1.pixelZoom * 5)
                );
                float num = (y - this.ScrollBarRunner.Y) / (float)this.ScrollBarRunner.Height;
                this.CurrentItemIndex = Math.Min(this.Options.Count - 7, Math.Max(0, (int)(this.Options.Count * num)));
                this.SetScrollBarToCurrentIndex();
                if (y2 != this.ScrollBar.bounds.Y)
                    Game1.playSound("shiny4");
            }
            else
            {
                if (this.OptionsSlotHeld == -1 || this.OptionsSlotHeld + this.CurrentItemIndex >= this.Options.Count)
                    return;

                this.Options[this.CurrentItemIndex + this.OptionsSlotHeld].leftClickHeld(
                    x - this.OptionSlots[this.OptionsSlotHeld].bounds.X,
                    y - this.OptionSlots[this.OptionsSlotHeld].bounds.Y
                );
            }
        }

        protected override void cleanupBeforeExit()
        {
            this.UpdateConfig();
        }

        public override void receiveKeyPress(Keys key)
        {
            if ((Game1.options.menuButton.Contains(new InputButton(key)) ||
                 Game1.options.doesInputListContain(Game1.options.mapButton, key)) && this.readyToClose() && this.CanClose)
            {
                this.exitThisMenu();
                return;
            }

            if (key.ToString().Equals(ModEntry.Globals.MenuKey) && this.readyToClose() && this.CanClose)
            {
                Game1.exitActiveMenu();

                Game1.activeClickableMenu = new GameMenu(ModConstants.MapTabIndex);
                return;
            }

            this.CanClose = true;
            if (this.OptionsSlotHeld == -1 || this.OptionsSlotHeld + this.CurrentItemIndex >= this.Options.Count)
                return;

            this.Options[this.CurrentItemIndex + this.OptionsSlotHeld].receiveKeyPress(key);
        }

        public override void receiveScrollWheelAction(int direction)
        {
            if (GameMenu.forcePreventClose) return;

            base.receiveScrollWheelAction(direction);
            if (direction > 0 && this.CurrentItemIndex > 0)
            {
                this.UpArrowPressed();
                Game1.playSound("shiny4");
                return;
            }

            if (direction < 0 && this.CurrentItemIndex < Math.Max(0, this.Options.Count - 7))
            {
                this.DownArrowPressed();
                Game1.playSound("shiny4");
            }
        }

        public override void releaseLeftClick(int x, int y)
        {
            if (GameMenu.forcePreventClose) return;

            base.releaseLeftClick(x, y);
            if (this.OptionsSlotHeld != -1 && this.OptionsSlotHeld + this.CurrentItemIndex < this.Options.Count)
            {
                this.Options[this.CurrentItemIndex + this.OptionsSlotHeld].leftClickReleased(
                    x - this.OptionSlots[this.OptionsSlotHeld].bounds.X,
                    y - this.OptionSlots[this.OptionsSlotHeld].bounds.Y
                );
            }

            this.OptionsSlotHeld = -1;
            this.Scrolling = false;
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (GameMenu.forcePreventClose) return;

            if (this.DownArrow.containsPoint(x, y) && this.CurrentItemIndex < Math.Max(0, this.Options.Count - 7))
            {
                this.DownArrowPressed();
                Game1.playSound("shwip");
            }
            else if (this.UpArrow.containsPoint(x, y) && this.CurrentItemIndex > 0)
            {
                this.UpArrowPressed();
                Game1.playSound("shwip");
            }
            else if (this.ScrollBar.containsPoint(x, y))
            {
                this.Scrolling = true;
            }
            else if (!this.DownArrow.containsPoint(x, y) && x > this.xPositionOnScreen + this.width &&
                     x < this.xPositionOnScreen + this.width + Game1.tileSize * 2 && y > this.yPositionOnScreen &&
                     y < this.yPositionOnScreen + this.height)
            {
                this.Scrolling = true;
                this.leftClickHeld(x, y);
            }
            else if (this.ImmersionAllButton.Rect.Contains(x, y))
                this.ImmersionAllButton.receiveLeftClick(x, y);
            else if (this.ImmersionTalkedToButton.Rect.Contains(x, y))
                this.ImmersionTalkedToButton.receiveLeftClick(x, y);
            else if (this.ImmersionNotTalkedToButton.Rect.Contains(x, y))
                this.ImmersionNotTalkedToButton.receiveLeftClick(x, y);

            if (this.OkButton.containsPoint(x, y))
            {
                this.OkButton.scale -= 0.25f;
                this.OkButton.scale = Math.Max(0.75f, this.OkButton.scale);

                this.exitThisMenu(false);
                Game1.activeClickableMenu = new GameMenu(ModConstants.MapTabIndex);
            }

            y -= 15;
            this.CurrentItemIndex = Math.Max(0, Math.Min(this.Options.Count - 7, this.CurrentItemIndex));
            for (int i = 0; i < this.OptionSlots.Count; i++)
            {
                if (this.OptionSlots[i].bounds.Contains(x, y) && this.CurrentItemIndex + i < this.Options.Count && this.Options[this.CurrentItemIndex + i]
                        .bounds.Contains(x - this.OptionSlots[i].bounds.X, y - this.OptionSlots[i].bounds.Y))
                {
                    this.Options[this.CurrentItemIndex + i].receiveLeftClick(x - this.OptionSlots[i].bounds.X, y - this.OptionSlots[i].bounds.Y);
                    this.OptionsSlotHeld = i;
                    break;
                }
            }
        }

        public override void performHoverAction(int x, int y)
        {
            if (GameMenu.forcePreventClose) return;

            if (this.OkButton.containsPoint(x, y))
            {
                this.OkButton.scale = Math.Min(this.OkButton.scale + 0.02f, this.OkButton.baseScale + 0.1f);
                return;
            }

            this.OkButton.scale = Math.Max(this.OkButton.scale - 0.02f, this.OkButton.baseScale);
            this.UpArrow.tryHover(x, y);
            this.DownArrow.tryHover(x, y);
            this.ScrollBar.tryHover(x, y);
        }

        // Draw menu
        public override void draw(SpriteBatch b)
        {
            if (Game1.options.showMenuBackground) this.drawBackground(b);

            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.4f);
            Game1.drawDialogueBox(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, false, true);
            this.OkButton.draw(b);
            int buttonWidth = (int)Game1.dialogueFont.MeasureString(I18n.Immersion_HideVillagersTalkedTo()).X;
            if (!GameMenu.forcePreventClose)
            {
                this.UpArrow.draw(b);
                this.DownArrow.draw(b);

                if (this.Options.Count > 7)
                {
                    drawTextureBox(b, Game1.mouseCursors, new Rectangle(403, 383, 6, 6), this.ScrollBarRunner.X,
                        this.ScrollBarRunner.Y, this.ScrollBarRunner.Width, this.ScrollBarRunner.Height, Color.White,
                        Game1.pixelZoom, false);
                    this.ScrollBar.draw(b);
                }

                for (int i = 0; i < this.OptionSlots.Count; i++)
                {
                    int x = this.OptionSlots[i].bounds.X;
                    int y = this.OptionSlots[i].bounds.Y + Game1.tileSize / 4;
                    if (this.CurrentItemIndex >= 0 && this.CurrentItemIndex + i < this.Options.Count)
                    {
                        if (this.Options[this.CurrentItemIndex + i] is VillagerVisibilityButton visibilityButton)
                        {
                            var bounds = new Rectangle(x + 28, y, buttonWidth + Game1.tileSize + 8, Game1.tileSize + 8);
                            visibilityButton.Rect = bounds;

                            drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), bounds.X, bounds.Y, bounds.Width, bounds.Height, Color.White * (visibilityButton.greyedOut ? 0.33f : 1f), 1f, false);
                        }

                        if (this.CurrentItemIndex + i == 0)
                            Utility.drawTextWithShadow(b, "NPC Map Locations", Game1.dialogueFont, new Vector2(x + Game1.tileSize / 2, y + Game1.tileSize / 4), Color.Black);
                        else
                            this.Options[this.CurrentItemIndex + i].draw(b, x, y);
                    }
                }
            }

            if (!Game1.options.hardwareCursor)
                b.Draw(Game1.mouseCursors, new Vector2(Game1.getOldMouseX(), Game1.getOldMouseY()),
                    Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors,
                        Game1.options.gamepadControls ? 44 : 0, 16, 16), Color.White, 0f, Vector2.Zero,
                    Game1.pixelZoom + Game1.dialogueButtonScale / 150f, SpriteEffects.None, 1f);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Update the mod config files.</summary>
        private void UpdateConfig()
        {
            ModEntry.StaticHelper.Data.WriteJsonFile($"config/{Constants.SaveFolderName}.json", ModEntry.Config);
            ModEntry.StaticHelper.Data.WriteJsonFile("config/globals.json", ModEntry.Globals);
        }

        private void SetScrollBarToCurrentIndex()
        {
            if (this.Options.Any())
            {
                this.ScrollBar.bounds.Y = this.ScrollBarRunner.Height / Math.Max(1, this.Options.Count - 7 + 1) * this.CurrentItemIndex + this.UpArrow.bounds.Bottom + Game1.pixelZoom;
                if (this.CurrentItemIndex == this.Options.Count - 7)
                    this.ScrollBar.bounds.Y = this.DownArrow.bounds.Y - this.ScrollBar.bounds.Height - Game1.pixelZoom;
            }
        }

        private void DownArrowPressed()
        {
            this.DownArrow.scale = this.DownArrow.baseScale;
            this.CurrentItemIndex++;
            this.SetScrollBarToCurrentIndex();
        }

        private void UpArrowPressed()
        {
            this.UpArrow.scale = this.UpArrow.baseScale;
            this.CurrentItemIndex--;
            this.SetScrollBarToCurrentIndex();
        }
    }
}
