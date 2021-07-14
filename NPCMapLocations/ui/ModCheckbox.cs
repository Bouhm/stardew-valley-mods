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
}
