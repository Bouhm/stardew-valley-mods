using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace NPCMapLocations.Framework.Menus
{
    // Mod checkbox for settings and npc blacklist
    internal class ModCheckbox : OptionsElement
    {
        private readonly KeyValuePair<string, NpcMarker>[] NpcMarkers;
        public bool IsChecked;

        public ModCheckbox(string label, int whichOption, int x, int y, KeyValuePair<string, NpcMarker>[] npcMarkers = null)
            : base(label, x, y, 9 * Game1.pixelZoom, 9 * Game1.pixelZoom, whichOption)
        {
            this.NpcMarkers = npcMarkers;
            this.label = ModEntry.StaticHelper.Translation.Get(label);

            // Villager names
            if (whichOption > 12 && npcMarkers != null)
            {
                this.IsChecked = !ModEntry.Globals.NpcExclusions.Contains(npcMarkers.ElementAt(whichOption - 13).Key);
                return;
            }

            switch (whichOption)
            {
                case 0:
                    this.IsChecked = ModEntry.Globals.ShowMinimap;
                    return;
                case 6:
                    this.IsChecked = ModEntry.Globals.OnlySameLocation;
                    return;
                case 7:
                    this.IsChecked = ModEntry.Config.ByHeartLevel;
                    return;
                case 10:
                    this.IsChecked = ModEntry.Globals.ShowQuests;
                    return;
                case 11:
                    this.IsChecked = ModEntry.Globals.ShowHiddenVillagers;
                    return;
                case 12:
                    this.IsChecked = ModEntry.Globals.ShowTravelingMerchant;
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
            this.IsChecked = !this.IsChecked;
            int whichOption = this.whichOption;

            // Show/hide villager options
            if (whichOption > 12 && this.NpcMarkers != null)
            {
                if (this.IsChecked)
                    ModEntry.Globals.NpcExclusions.Remove(this.NpcMarkers.ElementAt(whichOption - 13).Key);
                else
                    ModEntry.Globals.NpcExclusions.Add(this.NpcMarkers.ElementAt(whichOption - 13).Key);
            }
            else
            {
                switch (whichOption)
                {
                    case 0:
                        ModEntry.Globals.ShowMinimap = this.IsChecked;
                        break;
                    case 6:
                        ModEntry.Globals.OnlySameLocation = this.IsChecked;
                        break;
                    case 7:
                        ModEntry.Config.ByHeartLevel = this.IsChecked;
                        break;
                    case 10:
                        ModEntry.Globals.ShowQuests = this.IsChecked;
                        break;
                    case 11:
                        ModEntry.Globals.ShowHiddenVillagers = this.IsChecked;
                        break;
                    case 12:
                        ModEntry.Globals.ShowTravelingMerchant = this.IsChecked;
                        break;
                }
            }

            ModEntry.StaticHelper.Data.WriteJsonFile($"config/{Constants.SaveFolderName}.json", ModEntry.Config);
            ModEntry.StaticHelper.Data.WriteJsonFile("config/globals.json", ModEntry.Globals);
        }

        public override void draw(SpriteBatch b, int slotX, int slotY, IClickableMenu context = null)
        {
            b.Draw(Game1.mouseCursors, new Vector2(slotX + this.bounds.X, slotY + this.bounds.Y),
                this.IsChecked ? OptionsCheckbox.sourceRectChecked : OptionsCheckbox.sourceRectUnchecked,
                Color.White * (this.greyedOut ? 0.33f : 1f), 0f, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None,
                0.4f);
            if (this.whichOption > 12 && this.NpcMarkers != null)
            {
                var marker = this.NpcMarkers.ElementAt(this.whichOption - 13).Value;

                if (this.IsChecked)
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
                    SpriteText.drawString(b, this.label, slotX + this.bounds.X, slotY + this.bounds.Y + 12, 999, -1, 999, 1f, 0.1f);
                else
                    Utility.drawTextWithShadow(b, marker.DisplayName, Game1.dialogueFont,
                        new Vector2(slotX + this.bounds.X + this.bounds.Width + 8, slotY + this.bounds.Y),
                        this.greyedOut ? Game1.textColor * 0.33f : Game1.textColor, 1f, 0.1f);
            }
            else
            {
                base.draw(b, slotX, slotY, context);
            }
        }
    }
}
