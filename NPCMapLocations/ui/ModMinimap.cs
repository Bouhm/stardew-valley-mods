using System;
using System.Collections.Generic;
using System.Linq;
using Bouhm.Shared.Mouse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NPCMapLocations.Framework.Models;
using StardewModdingAPI;
using StardewValley;

namespace NPCMapLocations.Framework.Menus
{
    internal class ModMinimap
    {
        private readonly Texture2D BuildingMarkers;
        private readonly int borderWidth = 12;
        private readonly bool drawPamHouseUpgrade;
        private readonly bool drawMovieTheaterJoja;
        private readonly bool drawMovieTheater;
        private readonly bool drawIsland;
        private readonly Dictionary<long, FarmerMarker> FarmerMarkers;
        private readonly ModCustomizations Customizations;

        private Vector2 prevCenter;
        private Vector2 center; // Center position of minimap
        private float cropX; // Top-left position of crop on map
        private float cropY; // Top-left position of crop on map
        private int mmWidth; // minimap width
        private int mmHeight; // minimap height
        private Vector2 mmLoc; // minimap location relative to map location
        private int mmX; // top-left position of minimap relative to viewport
        private int mmY; // top-left position of minimap relative to viewport
        private int offset = 0; // offset for minimap if viewport changed
        private readonly Dictionary<string, NpcMarker> NpcMarkers;
        private Vector2 playerLoc;
        private int prevMmX;
        private int prevMmY;
        private int drawDelay = 0;
        private bool dragStarted;

        public ModMinimap(
          Dictionary<string, NpcMarker> npcMarkers,
          Dictionary<string, bool> conditionalNpcs,
          Dictionary<long, FarmerMarker> farmerMarkers,
          Dictionary<string, KeyValuePair<string, Vector2>> farmBuildings,
          Texture2D buildingMarkers,
          ModCustomizations customizations
        )
        {
            // renderTarget = new RenderTarget2D(Game1.graphics.GraphicsDevice,Game1.viewport.Width, Game1.viewport.Height);
            this.NpcMarkers = npcMarkers;
            this.ConditionalNpcs = conditionalNpcs;
            this.FarmerMarkers = farmerMarkers;
            this.FarmBuildings = farmBuildings;
            this.BuildingMarkers = buildingMarkers;
            this.Customizations = customizations;

            this.drawPamHouseUpgrade = Game1.MasterPlayer.mailReceived.Contains("pamHouseUpgrade");
            this.drawMovieTheaterJoja = Utility.doesMasterPlayerHaveMailReceivedButNotMailForTomorrow("ccMovieTheaterJoja");
            this.drawMovieTheater = Utility.doesMasterPlayerHaveMailReceivedButNotMailForTomorrow("ccMovieTheater");
            this.drawIsland = Game1.MasterPlayer.hasOrWillReceiveMail("Visited_Island");

            this.mmX = ModMain.Globals.MinimapX;
            this.mmY = ModMain.Globals.MinimapY;
            this.mmWidth = ModMain.Globals.MinimapWidth * Game1.pixelZoom;
            this.mmHeight = ModMain.Globals.MinimapHeight * Game1.pixelZoom;
        }

        private Dictionary<string, bool> ConditionalNpcs { get; }
        private Dictionary<string, KeyValuePair<string, Vector2>> FarmBuildings { get; }

        // Check if cursor is hovering the drag zone (top of minimap)
        public bool isHoveringDragZone()
        {
            return (Game1.getMouseX() >= this.mmX - this.borderWidth && Game1.getMouseX() <= this.mmX + this.mmWidth + this.borderWidth &&
                Game1.getMouseY() >= this.mmY - this.borderWidth && Game1.getMouseY() < this.mmY + this.mmHeight + this.borderWidth);
        }
        public void HandleMouseDown()
        {
            this.prevMmX = this.mmX;
            this.prevMmY = this.mmY;
            this.dragStarted = true;
        }

        public void HandleMouseDrag()
        {
            if (this.dragStarted)
            {
                // Move minimap with mouse on drag
                this.mmX = this.NormalizeToMap(MathHelper.Clamp(this.prevMmX + Game1.getMouseX() - MouseUtil.BeginMousePosition.X, this.borderWidth,
                  Game1.viewport.Width - this.mmWidth - this.borderWidth));
                this.mmY = this.NormalizeToMap(MathHelper.Clamp(this.prevMmY + Game1.getMouseY() - MouseUtil.BeginMousePosition.Y, this.borderWidth,
                  Game1.viewport.Height - this.mmHeight - this.borderWidth));
            }
        }

        public void HandleMouseRelease()
        {
            ModMain.Globals.MinimapX = this.mmX;
            ModMain.Globals.MinimapY = this.mmY;
            ModMain.Helper.Data.WriteJsonFile($"config/{Constants.SaveFolderName}.json", ModMain.Config);
            this.dragStarted = false;

            // Delay drawing on minimap after it's moved
            if (this.isHoveringDragZone())
                this.drawDelay = 30;
        }

        public void UpdateMapForSeason()
        {
            ModMain.Map = Game1.content.Load<Texture2D>("LooseSprites\\map");
        }

        public void CheckOffsetForMap()
        {
            // When ModMain.Map is smaller than viewport (ex. Bus Stop)
            if (Game1.isOutdoorMapSmallerThanViewport())
            {
                this.offset = (int)(Game1.viewport.Width - Game1.currentLocation.map.Layers[0].LayerWidth * Game1.tileSize) / 2;

                if (this.mmX > Math.Max(12, this.offset))
                    this.offset = 0;
            }
            else
                this.offset = 0;
        }
        public void Resize()
        {
            this.mmWidth = ModMain.Globals.MinimapWidth * Game1.pixelZoom;
            this.mmHeight = ModMain.Globals.MinimapHeight * Game1.pixelZoom;
        }

        public void Update()
        {
            // Note: Absolute positions relative to viewport are scaled 4x (Game1.pixelZoom).
            // Positions relative to the map are not.

            this.center = ModMain.LocationToMap(Game1.player.currentLocation.uniqueName.Value ?? Game1.player.currentLocation.Name, Game1.player.getTileX(),
              Game1.player.getTileY(), this.Customizations.MapVectors, this.Customizations.LocationExclusions, true);

            // Player in unknown location, use previous location as center
            if (this.center.Equals(ModMain.UNKNOWN) && this.prevCenter != null)
                this.center = this.prevCenter;
            else
                this.prevCenter = this.center;
            this.playerLoc = this.prevCenter;

            this.center.X = this.NormalizeToMap(this.center.X);
            this.center.Y = this.NormalizeToMap(this.center.Y);

            // Top-left offset for markers, relative to the minimap
            this.mmLoc = new Vector2(this.mmX - this.center.X + (float)Math.Floor(this.mmWidth / 2.0), this.mmY - this.center.Y + (float)Math.Floor(this.mmHeight / 2.0));

            // Top-left corner of minimap cropped from the whole map
            // Centered around the player's location on the map
            this.cropX = this.center.X - (float)Math.Floor(this.mmWidth / 2.0);
            this.cropY = this.center.Y - (float)Math.Floor(this.mmHeight / 2.0);

            // Handle cases when reaching edge of map 
            // Change offsets accordingly when player is no longer centered
            if (this.cropX < 0)
            {
                this.center.X = this.mmWidth / 2;
                this.mmLoc.X = this.mmX;
                this.cropX = 0;
            }
            else if (this.cropX + this.mmWidth > 1200)
            {
                this.center.X = 1200 - this.mmWidth / 2;
                this.mmLoc.X = this.mmX - (1200 - this.mmWidth);
                this.cropX = 1200 - this.mmWidth;
            }

            if (this.cropY < 0)
            {
                this.center.Y = this.mmHeight / 2;
                this.mmLoc.Y = this.mmY;
                this.cropY = 0;
            }
            // Actual map is 1200x720 but map.Height includes the farms
            else if (this.cropY + this.mmHeight > 720)
            {
                this.center.Y = 720 - this.mmHeight / 2;
                this.mmLoc.Y = this.mmY - (720 - this.mmHeight);
                this.cropY = 720 - this.mmHeight;
            }
        }

        // Center or the player's position is used as reference; player is not center when reaching edge of map
        public void DrawMiniMap()
        {
            var b = Game1.spriteBatch;
            bool IsHoveringMinimap = this.isHoveringDragZone();
            int offsetMmX = this.mmX + this.offset;

            // Make transparent on hover
            var color = IsHoveringMinimap
              ? Color.White * 0.25f
              : Color.White;

            if (IsHoveringMinimap)
            {
                Game1.mouseCursor = 2;
            }

            if (ModMain.Map == null) return;

            b.Draw(ModMain.Map, new Vector2(offsetMmX, this.mmY),
              new Rectangle((int)Math.Floor(this.cropX / Game1.pixelZoom),
                (int)Math.Floor(this.cropY / Game1.pixelZoom), this.mmWidth / Game1.pixelZoom + 2,
                this.mmHeight / Game1.pixelZoom + 2), color, 0f, Vector2.Zero,
              4f, SpriteEffects.None, 0.86f);

            // Don't draw markers while being dragged
            if (!(this.isHoveringDragZone() && ModMain.Helper.Input.GetState(SButton.MouseRight) == SButtonState.Held))
            {
                // When minimap is moved, redraw markers after recalculating & repositioning
                if (this.drawDelay == 0)
                {
                    if (!IsHoveringMinimap)
                        this.DrawMarkers();
                }
                else
                    this.drawDelay--;
            }

            // Border around minimap that will also help mask markers outside of the minimap
            // Which gives more padding for when they are considered within the minimap area
            // Draw border
            this.DrawLine(b, new Vector2(offsetMmX, this.mmY - this.borderWidth), new Vector2(offsetMmX + this.mmWidth - 2, this.mmY - this.borderWidth), this.borderWidth,
              Game1.menuTexture, new Rectangle(8, 256, 3, this.borderWidth), color * 1.5f);
            this.DrawLine(b, new Vector2(offsetMmX + this.mmWidth + this.borderWidth, this.mmY),
              new Vector2(offsetMmX + this.mmWidth + this.borderWidth, this.mmY + this.mmHeight - 2), this.borderWidth, Game1.menuTexture,
              new Rectangle(8, 256, 3, this.borderWidth), color * 1.5f);
            this.DrawLine(b, new Vector2(offsetMmX + this.mmWidth, this.mmY + this.mmHeight + this.borderWidth),
              new Vector2(offsetMmX + 2, this.mmY + this.mmHeight + this.borderWidth), this.borderWidth, Game1.menuTexture,
              new Rectangle(8, 256, 3, this.borderWidth), color * 1.5f);
            this.DrawLine(b, new Vector2(offsetMmX - this.borderWidth, this.mmHeight + this.mmY), new Vector2(offsetMmX - this.borderWidth, this.mmY + 2), this.borderWidth,
              Game1.menuTexture, new Rectangle(8, 256, 3, this.borderWidth), color * 1.5f);

            // Draw the border corners
            b.Draw(Game1.menuTexture, new Rectangle(offsetMmX - this.borderWidth, this.mmY - this.borderWidth, this.borderWidth, this.borderWidth),
              new Rectangle(0, 256, this.borderWidth, this.borderWidth), color * 1.5f);
            b.Draw(Game1.menuTexture, new Rectangle(offsetMmX + this.mmWidth, this.mmY - this.borderWidth, this.borderWidth, this.borderWidth),
              new Rectangle(48, 256, this.borderWidth, this.borderWidth), color * 1.5f);
            b.Draw(Game1.menuTexture, new Rectangle(offsetMmX + this.mmWidth, this.mmY + this.mmHeight, this.borderWidth, this.borderWidth),
              new Rectangle(48, 304, this.borderWidth, this.borderWidth), color * 1.5f);
            b.Draw(Game1.menuTexture, new Rectangle(offsetMmX - this.borderWidth, this.mmY + this.mmHeight, this.borderWidth, this.borderWidth),
              new Rectangle(0, 304, this.borderWidth, this.borderWidth), color * 1.5f);
        }

        private void DrawMarkers()
        {
            var b = Game1.spriteBatch;
            var color = Color.White;
            int offsetMmX = this.mmX + this.offset;
            var offsetMmLoc = new Vector2(this.mmLoc.X + this.offset + 2, this.mmLoc.Y + 2);

            //
            // ===== Farm types =====
            //
            // The farms are always overlayed at (0, 43) on the map
            // The crop position, dimensions, and overlay position must all be adjusted accordingly
            // When any part of the cropped farm is outside of the minimap as player moves
            int farmWidth = 131;
            int farmHeight = 61;
            int farmX = this.NormalizeToMap(MathHelper.Clamp(offsetMmLoc.X, offsetMmX, offsetMmX + this.mmWidth));
            int farmY = this.NormalizeToMap(MathHelper.Clamp(offsetMmLoc.Y + 172, this.mmY, this.mmY + this.mmHeight) + 2);
            int farmCropX = (int)MathHelper.Clamp((offsetMmX - offsetMmLoc.X) / Game1.pixelZoom, 0, farmWidth);
            int farmCropY = (int)MathHelper.Clamp((this.mmY - offsetMmLoc.Y - 172) / Game1.pixelZoom, 0, farmHeight);

            // Check if farm crop extends outside of minimap
            int farmCropWidth = (farmX / Game1.pixelZoom + farmWidth > (offsetMmX + this.mmWidth) / Game1.pixelZoom) ? (int)((offsetMmX + this.mmWidth - farmX) / Game1.pixelZoom) : farmWidth - farmCropX;
            int farmCropHeight = (farmY / Game1.pixelZoom + farmHeight > (this.mmY + this.mmHeight) / Game1.pixelZoom) ? (int)((this.mmY + this.mmHeight - farmY) / Game1.pixelZoom) : farmHeight - farmCropY;

            // Check if farm crop extends beyond farm size
            if (farmCropX + farmCropWidth > farmWidth)
                farmCropWidth = farmWidth - farmCropX;

            if (farmCropY + farmCropHeight > farmHeight)
                farmCropHeight = farmHeight - farmCropY;

            switch (Game1.whichFarm)
            {
                case 1:
                    b.Draw(ModMain.Map, new Vector2(farmX, farmY),
                      new Rectangle(0 + farmCropX, 180 + farmCropY, farmCropWidth, farmCropHeight), color,
                      0f,
                      Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
                    break;
                case 2:
                    b.Draw(ModMain.Map, new Vector2(farmX, farmY),
                      new Rectangle(131 + farmCropX, 180 + farmCropY, farmCropWidth, farmCropHeight), color,
                      0f,
                      Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
                    break;
                case 3:
                    b.Draw(ModMain.Map, new Vector2(farmX, farmY),
                      new Rectangle(0 + farmCropX, 241 + farmCropY, farmCropWidth, farmCropHeight), color,
                      0f,
                      Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
                    break;
                case 4:
                    b.Draw(ModMain.Map, new Vector2(farmX, farmY),
                      new Rectangle(131 + farmCropX, 241 + farmCropY, farmCropWidth, farmCropHeight), color,
                      0f,
                      Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
                    break;
                case 5:
                    b.Draw(ModMain.Map, new Vector2(farmX, farmY),
                       new Rectangle(0 + farmCropX, 302 + farmCropY, farmCropWidth, farmCropHeight), color,
                       0f,
                      Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
                    break;
                case 6:
                    b.Draw(ModMain.Map, new Vector2(farmX, farmY + 172),
                      new Rectangle(131 + farmCropX, 302 + farmCropY, farmCropWidth, farmCropHeight), color,
                      0f,
                      Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
                    break;
            }

            if (this.drawPamHouseUpgrade)
            {
                var houseLoc = ModMain.LocationToMap("Trailer_Big");
                if (this.IsWithinMapArea(houseLoc.X, houseLoc.Y))
                    b.Draw(ModMain.Map, new Vector2(this.NormalizeToMap(offsetMmLoc.X + houseLoc.X - 16), this.NormalizeToMap(offsetMmLoc.Y + houseLoc.Y - 11)),
                      new Rectangle(263, 181, 8, 8), color,
                      0f, Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
            }

            if (this.drawMovieTheater || this.drawMovieTheaterJoja)
            {
                var theaterLoc = ModMain.LocationToMap("JojaMart");
                if (this.IsWithinMapArea(theaterLoc.X, theaterLoc.Y))
                {
                    b.Draw(ModMain.Map, new Vector2(this.NormalizeToMap(offsetMmLoc.X + theaterLoc.X - 20), this.NormalizeToMap(offsetMmLoc.Y + theaterLoc.Y - 11)),
                      new Rectangle(275, 181, 15, 11), color,
                      0f, Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
                }
            }

            if (this.drawIsland)
            {
                int islandWidth = 40;
                int islandHeight = 30;
                int islandImageX = 208;
                int islandImageY = 363;
                int mapX = 1040;
                int mapY = 600;

                if (ModMain.Globals.UseDetailedIsland)
                {
                    islandWidth = 45;
                    islandHeight = 40;
                    islandImageX = 248;
                    islandImageY = 363;
                    mapX = 1020;
                    mapY = 560;
                }

                int islandX = this.NormalizeToMap(MathHelper.Clamp(offsetMmLoc.X + mapX, offsetMmX, offsetMmX + this.mmWidth));
                int islandY = this.NormalizeToMap(MathHelper.Clamp(offsetMmLoc.Y + mapY, this.mmY, this.mmY + this.mmHeight) + 2);
                int islandCropX = (int)MathHelper.Clamp((offsetMmX - offsetMmLoc.X - mapX) / Game1.pixelZoom, 0, islandWidth);
                int islandCropY = (int)MathHelper.Clamp((this.mmY - offsetMmLoc.Y - mapY) / Game1.pixelZoom, 0, islandHeight);

                // Check if island crop extends outside of minimap
                int islandCropWidth = (islandX / Game1.pixelZoom + islandWidth > (offsetMmX + this.mmWidth) / Game1.pixelZoom) ? (int)((offsetMmX + this.mmWidth - islandX) / Game1.pixelZoom) : islandWidth - islandCropX;
                int islandCropHeight = (islandY / Game1.pixelZoom + islandHeight > (this.mmY + this.mmHeight) / Game1.pixelZoom) ? (int)((this.mmY + this.mmHeight - islandY) / Game1.pixelZoom) : islandHeight - islandCropY;

                // Check if island crop extends beyond island size
                if (islandCropX + islandCropWidth > islandWidth)
                    islandCropWidth = islandWidth - islandCropX;

                if (islandCropY + islandCropHeight > islandHeight)
                    islandCropHeight = islandHeight - islandCropY;

                b.Draw(ModMain.Map, new Vector2(islandX, islandY), new Rectangle(islandImageX + islandCropX, islandImageY + islandCropY, islandCropWidth, islandCropHeight), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
            }

            //
            // ===== Farm buildings =====
            //
            if (ModMain.Globals.ShowFarmBuildings && this.FarmBuildings != null && this.BuildingMarkers != null)
            {
                var sortedBuildings = this.FarmBuildings.ToList();
                sortedBuildings.Sort((x, y) => x.Value.Value.Y.CompareTo(y.Value.Value.Y));

                foreach (var building in sortedBuildings)
                {
                    if (ModConstants.FarmBuildingRects.TryGetValue(building.Value.Key, out var buildingRect))
                    {
                        if (this.IsWithinMapArea(building.Value.Value.X - buildingRect.Width / 2,
                          building.Value.Value.Y - buildingRect.Height / 2))
                        {
                            //  if (Customizations.MapName == "starblue_map")
                            //   buildingRect.Y = 7;
                            //  else if (Customizations.MapName == "eemie_recolour_map")
                            //    buildingRect.Y = 14;

                            b.Draw(
                                this.BuildingMarkers,
                                new Vector2(
                                    this.NormalizeToMap(offsetMmLoc.X + building.Value.Value.X - (float)Math.Floor(buildingRect.Width / 2.0)),
                                    this.NormalizeToMap(offsetMmLoc.Y + building.Value.Value.Y - (float)Math.Floor(buildingRect.Height / 2.0))
                              ),
                              buildingRect, color, 0f, Vector2.Zero, 3f, SpriteEffects.None, 1f
                            );
                        }
                    }
                }
            }

            //
            // ===== Traveling Merchant =====
            //
            if (ModMain.Globals.ShowTravelingMerchant && this.ConditionalNpcs["Merchant"])
            {
                Vector2 merchantLoc = new Vector2(ModConstants.MapVectors["Merchant"][0].MapX, ModConstants.MapVectors["Merchant"][0].MapY);
                if (this.IsWithinMapArea(merchantLoc.X - 16, merchantLoc.Y - 16))
                    b.Draw(Game1.mouseCursors, new Vector2(this.NormalizeToMap(offsetMmLoc.X + merchantLoc.X - 16), this.NormalizeToMap(offsetMmLoc.Y + merchantLoc.Y - 15)), new Rectangle(191, 1410, 22, 21), Color.White, 0f, Vector2.Zero, 1.3f, SpriteEffects.None,
                      1f);
            }

            //
            // ===== NPCs =====
            //
            // Sort by drawing order
            if (this.NpcMarkers != null)
            {
                var sortedMarkers = this.NpcMarkers.ToList();
                sortedMarkers.Sort((x, y) => x.Value.Layer.CompareTo(y.Value.Layer));

                foreach (var npcMarker in sortedMarkers)
                {
                    string name = npcMarker.Key;
                    var marker = npcMarker.Value;

                    // Skip if no specified location
                    if (marker.Sprite == null
                        || !this.IsWithinMapArea(marker.MapX, marker.MapY)
                        || ModMain.Globals.NpcExclusions.Contains(name)
                        || (!ModMain.Globals.ShowHiddenVillagers && marker.IsHidden)
                        || (this.ConditionalNpcs.ContainsKey(name) && !this.ConditionalNpcs[name])
                    )
                        continue;

                    var markerColor = marker.IsHidden ? Color.DarkGray * 0.7f : Color.White;

                    // Draw NPC marker
                    var spriteRect = marker.Type == Character.Horse ? new Rectangle(17, 104, 16, 14) : new Rectangle(0, marker.CropOffset, 16, 15);

                    if (marker.Type == Character.Horse)
                    {
                        b.Draw(marker.Sprite,
                          new Rectangle(this.NormalizeToMap(offsetMmLoc.X + marker.MapX),
                            this.NormalizeToMap(offsetMmLoc.Y + marker.MapY),
                            30, 32),
                            spriteRect, markerColor);
                    }
                    else
                    {
                        b.Draw(marker.Sprite,
                          new Rectangle(this.NormalizeToMap(offsetMmLoc.X + marker.MapX),
                            this.NormalizeToMap(offsetMmLoc.Y + marker.MapY),
                            30, 32),
                            spriteRect, markerColor);
                    }

                    // Icons for birthday/quest
                    if (ModMain.Globals.ShowQuests)
                    {
                        if (marker.IsBirthday && (Game1.player.friendshipData.ContainsKey(name) && Game1.player.friendshipData[name].GiftsToday == 0))
                            b.Draw(Game1.mouseCursors,
                              new Vector2(this.NormalizeToMap(offsetMmLoc.X + marker.MapX + 20),
                              this.NormalizeToMap(offsetMmLoc.Y + marker.MapY)),
                              new Rectangle(147, 412, 10, 11), markerColor, 0f, Vector2.Zero, 1.8f,
                              SpriteEffects.None,
                              0f);

                        if (marker.HasQuest)
                            b.Draw(Game1.mouseCursors,
                              new Vector2(this.NormalizeToMap(offsetMmLoc.X + marker.MapX + 22),
                              this.NormalizeToMap(offsetMmLoc.Y + marker.MapY - 3)),
                              new Rectangle(403, 496, 5, 14), markerColor, 0f, Vector2.Zero, 1.8f, SpriteEffects.None,
                              0f);
                    }
                }
            }

            if (Context.IsMultiplayer)
            {
                foreach (var farmer in Game1.getOnlineFarmers())
                {
                    // Temporary solution to handle desync of farmhand location/tile position when changing location
                    if (this.FarmerMarkers.TryGetValue(farmer.UniqueMultiplayerID, out var farMarker))
                    {
                        if (this.IsWithinMapArea(farMarker.MapX - 16, farMarker.MapY - 15) && farMarker.DrawDelay == 0)
                        {
                            farmer.FarmerRenderer.drawMiniPortrat(b,
                              new Vector2(this.NormalizeToMap(offsetMmLoc.X + farMarker.MapX - 16),
                              this.NormalizeToMap(offsetMmLoc.Y + farMarker.MapY - 15)),
                              0.00011f, 2f, 1, farmer);
                        }
                    }
                }
            }
            else
            {
                Game1.player.FarmerRenderer.drawMiniPortrat(b,
                  new Vector2(this.NormalizeToMap(offsetMmLoc.X + this.playerLoc.X - 16), this.NormalizeToMap(offsetMmLoc.Y + this.playerLoc.Y - 15)), 0.00011f,
                  2f, 1,
                  Game1.player);
            }
        }

        // Normalize offset differences caused by map being 4x less precise than ModMain.Map markers 
        // Makes the map and markers move together instead of markers moving more precisely (moving when minimap does not shift)
        private int NormalizeToMap(float n)
        {
            return (int)Math.Floor(n / Game1.pixelZoom) * Game1.pixelZoom;
        }

        // Check if within map
        private bool IsWithinMapArea(float x, float y)
        {
            return x > this.center.X - this.mmWidth / 2 - (Game1.tileSize / 4)
                   && x < this.center.X + this.mmWidth / 2 - (Game1.tileSize / 4)
                   && y > this.center.Y - this.mmHeight / 2 - (Game1.tileSize / 4)
                   && y < this.center.Y + this.mmHeight / 2 - (Game1.tileSize / 4);
        }

        // For borders
        private void DrawLine(SpriteBatch b, Vector2 begin, Vector2 end, int width, Texture2D tex, Rectangle srcRect, Color color)
        {
            var r = new Rectangle((int)begin.X, (int)begin.Y, (int)(end - begin).Length() + 2, width);
            var v = Vector2.Normalize(begin - end);
            float angle = (float)Math.Acos(Vector2.Dot(v, -Vector2.UnitX));
            if (begin.Y > end.Y) angle = MathHelper.TwoPi - angle;
            b.Draw(tex, r, srcRect, color, angle, Vector2.Zero, SpriteEffects.None, 0);
        }
    }
}
