/*
MapPage for the mod that handles logic for tooltips
and drawing everything.
Based on regurgitated game code.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using Bouhm.Shared.Locations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace NPCMapLocations.Framework.Menus
{
    internal class ModMapPage : MapPage
    {
        /*********
        ** Fields
        *********/
        private Dictionary<string, bool> ConditionalNpcs { get; }
        private Dictionary<string, NpcMarker> NpcMarkers { get; }
        private Dictionary<long, FarmerMarker> FarmerMarkers { get; }
        private Dictionary<string, BuildingMarker> FarmBuildings { get; }

        private readonly Texture2D BuildingMarkers;
        private readonly ModCustomizations Customizations;
        private string HoveredNames = "";
        private readonly int MapX;
        private readonly int MapY;
        private bool HasIndoorCharacter;
        private Vector2 IndoorIconVector;

        /// <summary>Scans and maps locations in the game world.</summary>
        private readonly LocationUtil LocationUtil;


        /*********
        ** Public methods
        *********/
        // Map menu that uses modified map page and modified component locations for hover
        public ModMapPage(
          Dictionary<string, NpcMarker> npcMarkers,
          Dictionary<string, bool> conditionalNpcs,
          Dictionary<long, FarmerMarker> farmerMarkers,
          Dictionary<string, BuildingMarker> farmBuildings,
          Texture2D buildingMarkers,
          ModCustomizations customizations,
          LocationUtil locationUtil
        ) : base(Game1.uiViewport.Width / 2 - (800 + IClickableMenu.borderWidth * 2) / 2,
          Game1.uiViewport.Height / 2 - (600 + IClickableMenu.borderWidth * 2) / 2, 800 + IClickableMenu.borderWidth * 2,
          600 + IClickableMenu.borderWidth * 2)
        {
            this.NpcMarkers = npcMarkers;
            this.ConditionalNpcs = conditionalNpcs;
            this.FarmerMarkers = farmerMarkers;
            this.FarmBuildings = farmBuildings;
            this.BuildingMarkers = buildingMarkers;
            this.Customizations = customizations;
            this.LocationUtil = locationUtil;

            Vector2 center = Utility.getTopLeftPositionForCenteringOnScreen(ModEntry.Map.Bounds.Width * 4, 720);
            this.MapX = (int)center.X;
            this.MapY = (int)center.Y;
        }

        public override void performHoverAction(int x, int y)
        {
            // set location tooltips
            base.performHoverAction(x, y);

            // set marker tooltips
            this.HoveredNames = "";
            this.HasIndoorCharacter = false;

            List<string> hoveredList = new List<string>();

            const int markerWidth = 32;
            const int markerHeight = 30;

            // Have to use special character to separate strings for Chinese
            string separator = LocalizedContentManager.CurrentLanguageCode.Equals(LocalizedContentManager.LanguageCode.zh)
              ? "，"
              : ", ";

            if (this.NpcMarkers != null)
            {
                foreach (var npcMarker in this.NpcMarkers)
                {
                    Vector2 npcLocation = new Vector2(this.MapX + npcMarker.Value.MapX, this.MapY + npcMarker.Value.MapY);
                    if (Game1.getMouseX() >= npcLocation.X && Game1.getMouseX() <= npcLocation.X + markerWidth &&
                        Game1.getMouseY() >= npcLocation.Y && Game1.getMouseY() <= npcLocation.Y + markerHeight)
                    {
                        if (!npcMarker.Value.IsHidden) //&& !(npcMarker.Value.Type == Character.Horse))
                        {
                            if (this.Customizations.Names.TryGetValue(npcMarker.Key, out string name))
                                hoveredList.Add(name);
                            else if (npcMarker.Value.Type == CharacterType.Horse)
                                hoveredList.Add(npcMarker.Key);
                        }

                        if (!this.LocationUtil.IsOutdoors(npcMarker.Value.LocationName) && !this.HasIndoorCharacter)
                            this.HasIndoorCharacter = true;
                    }
                }
            }

            if (Context.IsMultiplayer && this.FarmerMarkers != null)
            {
                foreach (var farMarker in this.FarmerMarkers.Values)
                {
                    Vector2 farmerLocation = new Vector2(this.MapX + farMarker.MapX, this.MapY + farMarker.MapY);
                    if (Game1.getMouseX() >= farmerLocation.X - markerWidth / 2
                     && Game1.getMouseX() <= farmerLocation.X + markerWidth / 2
                     && Game1.getMouseY() >= farmerLocation.Y - markerHeight / 2
                     && Game1.getMouseY() <= farmerLocation.Y + markerHeight / 2)
                    {
                        hoveredList.Add(farMarker.Name);

                        if (!this.LocationUtil.IsOutdoors(farMarker.LocationName) && !this.HasIndoorCharacter)
                            this.HasIndoorCharacter = true;
                    }
                }
            }

            if (hoveredList.Count > 0)
            {
                this.HoveredNames = hoveredList[0];
                for (int i = 1; i < hoveredList.Count; i++)
                {
                    string[] lines = this.HoveredNames.Split('\n');
                    if ((int)Game1.smallFont.MeasureString(lines[lines.Length - 1] + separator + hoveredList[i]).X >
                        (int)Game1.smallFont.MeasureString("Home of Robin, Demetrius, Sebastian & Maru").X) // Longest string
                    {
                        this.HoveredNames += separator + Environment.NewLine;
                        this.HoveredNames += hoveredList[i];
                    }
                    else
                    {
                        this.HoveredNames += separator + hoveredList[i];
                    }
                }
            }
        }

        public override void drawMiniPortraits(SpriteBatch b)
        {
            // base.drawMiniPortraits(b); // draw our own smaller farmer markers instead

            this.DrawMarkers(b);
        }

        public override void drawTooltip(SpriteBatch b)
        {
            int x = Game1.getMouseX() + Game1.tileSize / 2;
            int y = Game1.getMouseY() + Game1.tileSize / 2;
            int width;
            int height;
            int offsetY = 0;

            this.performHoverAction(x - Game1.tileSize / 2, y - Game1.tileSize / 2);

            if (!string.IsNullOrEmpty(this.hoverText))
            {
                int textLength = (int)Game1.smallFont.MeasureString(this.hoverText).X + Game1.tileSize / 2;
                width = Math.Max((int)Game1.smallFont.MeasureString(this.hoverText).X + Game1.tileSize / 2, textLength);
                height = (int)Math.Max(60, Game1.smallFont.MeasureString(this.hoverText).Y + 5 * Game1.tileSize / 8);
                if (x + width > Game1.uiViewport.Width)
                {
                    x = Game1.uiViewport.Width - width;
                    y += Game1.tileSize / 4;
                }

                if (ModEntry.Globals.NameTooltipMode == 1)
                {
                    if (y + height > Game1.uiViewport.Height)
                    {
                        x += Game1.tileSize / 4;
                        y = Game1.uiViewport.Height - height;
                    }

                    offsetY = 2 - Game1.tileSize;
                }
                else if (ModEntry.Globals.NameTooltipMode == 2)
                {
                    if (y + height > Game1.uiViewport.Height)
                    {
                        x += Game1.tileSize / 4;
                        y = Game1.uiViewport.Height - height;
                    }

                    offsetY = height - 4;
                }
                else
                {
                    if (y + height > Game1.uiViewport.Height)
                    {
                        x += Game1.tileSize / 4;
                        y = Game1.uiViewport.Height - height;
                    }
                }

                // Draw name tooltip positioned around location tooltip
                this.DrawNames(b, this.HoveredNames, x, y, offsetY, height, ModEntry.Globals.NameTooltipMode);

                // Draw location tooltip
                IClickableMenu.drawHoverText(b, this.hoverText, Game1.smallFont);
            }
            else
            {
                // Draw name tooltip only
                this.DrawNames(Game1.spriteBatch, this.HoveredNames, x, y, offsetY, this.height, ModEntry.Globals.NameTooltipMode);
            }

            // Draw indoor icon
            if (this.HasIndoorCharacter && !string.IsNullOrEmpty(this.HoveredNames))
                b.Draw(Game1.mouseCursors, this.IndoorIconVector, new Rectangle(448, 64, 32, 32), Color.White, 0f,
                  Vector2.Zero, 0.75f, SpriteEffects.None, 0f);
        }


        /*********
        ** Private methods
        *********/
        // Draw event
        // Subtractions within location vectors are to set the origin to the center of the sprite
        private void DrawMarkers(SpriteBatch b)
        {
            if (ModEntry.Globals.ShowFarmBuildings && this.FarmBuildings != null && this.BuildingMarkers != null)
            {
                foreach (BuildingMarker building in this.FarmBuildings.Values.OrderBy(p => p.MapPosition.Y))
                {
                    if (ModConstants.FarmBuildingRects.TryGetValue(building.CommonName, out Rectangle buildingRect))
                    {
                        b.Draw(
                            this.BuildingMarkers,
                            new Vector2(
                                this.MapX + building.MapPosition.X - buildingRect.Width / 2,
                                this.MapY + building.MapPosition.Y - buildingRect.Height / 2
                            ),
                            buildingRect, Color.White, 0f, Vector2.Zero, 3f, SpriteEffects.None, 1f
                        );
                    }
                }
            }

            // Traveling Merchant
            if (ModEntry.Globals.ShowTravelingMerchant && this.ConditionalNpcs["Merchant"])
            {
                Vector2 merchantLoc = new Vector2(ModConstants.MapVectors["Merchant"][0].MapX, ModConstants.MapVectors["Merchant"][0].MapY);
                b.Draw(Game1.mouseCursors, new Vector2(this.MapX + merchantLoc.X - 16, this.MapY + merchantLoc.Y - 15),
                  new Rectangle(191, 1410, 22, 21), Color.White, 0f, Vector2.Zero, 1.3f, SpriteEffects.None,
                  1f);
            }

            // NPCs
            // Sort by drawing order
            if (this.NpcMarkers != null)
            {
                var sortedMarkers = this.NpcMarkers.ToList();
                sortedMarkers.Sort((x, y) => x.Value.Layer.CompareTo(y.Value.Layer));

                foreach (var npcMarker in sortedMarkers)
                {
                    string name = npcMarker.Key;
                    NpcMarker marker = npcMarker.Value;

                    // Skip if no specified location or should be hidden
                    if (marker.Sprite == null
                      || ModEntry.ShouldExcludeNpc(name)
                      || (!ModEntry.Globals.ShowHiddenVillagers && marker.IsHidden)
                      || (this.ConditionalNpcs.ContainsKey(name) && !this.ConditionalNpcs[name])
                    )
                    {
                        continue;
                    }

                    // Dim marker for hidden markers
                    var markerColor = marker.IsHidden ? Color.DarkGray * 0.7f : Color.White;

                    // Draw NPC marker
                    var spriteRect = marker.Type == CharacterType.Horse ? new Rectangle(17, 104, 16, 14) : new Rectangle(0, marker.CropOffset, 16, 15);

                    b.Draw(marker.Sprite,
                      new Rectangle(this.MapX + marker.MapX, this.MapY + marker.MapY,
                        32, 30),
                      spriteRect, markerColor);

                    // Draw icons for quests/birthday
                    if (ModEntry.Globals.ShowQuests)
                    {
                        if (marker.IsBirthday && (Game1.player.friendshipData.ContainsKey(name) && Game1.player.friendshipData[name].GiftsToday == 0))
                        {
                            // Gift icon
                            b.Draw(Game1.mouseCursors,
                              new Vector2(this.MapX + marker.MapX + 20, this.MapY + marker.MapY),
                              new Rectangle(147, 412, 10, 11), markerColor, 0f, Vector2.Zero, 1.8f,
                              SpriteEffects.None, 0f);
                        }

                        if (marker.HasQuest)
                        {
                            // Quest icon
                            b.Draw(Game1.mouseCursors,
                              new Vector2(this.MapX + marker.MapX + 22, this.MapY + marker.MapY - 3),
                              new Rectangle(403, 496, 5, 14), markerColor, 0f, Vector2.Zero, 1.8f,
                              SpriteEffects.None, 0f);
                        }
                    }
                }
            }

            // Farmers
            if (Context.IsMultiplayer)
            {
                foreach (Farmer farmer in Game1.getOnlineFarmers())
                {
                    // Temporary solution to handle desync of farmhand location/tile position when changing location
                    if (this.FarmerMarkers.TryGetValue(farmer.UniqueMultiplayerID, out FarmerMarker farMarker))
                        if (farMarker == null)
                            continue;
                    if (farMarker is { DrawDelay: 0 })
                    {
                        farmer.FarmerRenderer.drawMiniPortrat(b,
                          new Vector2(this.MapX + farMarker.MapX - 16, this.MapY + farMarker.MapY - 15),
                          0.00011f, 2f, 1, farmer);
                    }
                }
            }
            else
            {
                Vector2 playerLoc = ModEntry.LocationToMap(Game1.player.currentLocation.uniqueName.Value ?? Game1.player.currentLocation.Name, Game1.player.TilePoint.X, Game1.player.TilePoint.Y, this.Customizations.MapVectors, this.Customizations.LocationExclusions);

                Game1.player.FarmerRenderer.drawMiniPortrat(b,
                  new Vector2(this.MapX + playerLoc.X - 16, this.MapY + playerLoc.Y - 15), 0.00011f, 2f, 1,
                  Game1.player);
            }
        }

        // Draw NPC name tooltips map page
        private void DrawNames(SpriteBatch b, string names, int x, int y, int offsetY, int relocate, int nameTooltipMode)
        {
            if (this.HoveredNames.Equals("")) return;

            this.IndoorIconVector = ModEntry.Unknown;
            string[] lines = names.Split('\n');
            int height = (int)Math.Max(60, Game1.smallFont.MeasureString(names).Y + Game1.tileSize / 2);
            int width = (int)Game1.smallFont.MeasureString(names).X + Game1.tileSize / 2;

            if (nameTooltipMode == 1)
            {
                x = Game1.getOldMouseX() + Game1.tileSize / 2;
                if (lines.Length > 1)
                {
                    y += offsetY - ((int)Game1.smallFont.MeasureString(names).Y) + Game1.tileSize / 2;
                }
                else
                {
                    y += offsetY;
                }

                // If going off screen on the right, move tooltip to below location tooltip so it can stay inside the screen
                // without the cursor covering the tooltip
                if (x + width > Game1.uiViewport.Width)
                {
                    x = Game1.uiViewport.Width - width;
                    if (lines.Length > 1)
                    {
                        y += relocate - 8 + ((int)Game1.smallFont.MeasureString(names).Y) + Game1.tileSize / 2;
                    }
                    else
                    {
                        y += relocate - 8 + Game1.tileSize;
                    }
                }
            }
            else if (nameTooltipMode == 2)
            {
                y += offsetY;
                if (x + width > Game1.uiViewport.Width)
                {
                    x = Game1.uiViewport.Width - width;
                }

                // If going off screen on the bottom, move tooltip to above location tooltip so it stays visible
                if (y + height > Game1.uiViewport.Height)
                {
                    x = Game1.getOldMouseX() + Game1.tileSize / 2;
                    if (lines.Length > 1)
                    {
                        y += -relocate + 8 - ((int)Game1.smallFont.MeasureString(names).Y) + Game1.tileSize / 2;
                    }
                    else
                    {
                        y += -relocate + 6 - Game1.tileSize;
                    }
                }
            }
            else
            {
                x = Game1.activeClickableMenu.xPositionOnScreen - 145;
                y = Game1.activeClickableMenu.yPositionOnScreen + 650 - height / 2;
            }

            if (this.HasIndoorCharacter)
            {
                this.IndoorIconVector = new Vector2(x - Game1.tileSize / 8 + 2, y - Game1.tileSize / 8 + 2);
            }

            Vector2 vector = new Vector2(x + (float)(Game1.tileSize / 4), y + (float)(Game1.tileSize / 4 + 4));

            drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, y, width, height, Color.White);
            b.DrawString(Game1.smallFont, names, vector + new Vector2(2f, 2f), Game1.textShadowColor, 0f, Vector2.Zero, 1f,
              SpriteEffects.None, 0f);
            b.DrawString(Game1.smallFont, names, vector + new Vector2(0f, 2f), Game1.textShadowColor, 0f, Vector2.Zero, 1f,
              SpriteEffects.None, 0f);
            b.DrawString(Game1.smallFont, names, vector + new Vector2(2f, 0f), Game1.textShadowColor, 0f, Vector2.Zero, 1f,
              SpriteEffects.None, 0f);
            b.DrawString(Game1.smallFont, names, vector, Game1.textColor * 0.9f, 0f, Vector2.Zero, 1f, SpriteEffects.None,
              0f);
        }
    }
}
