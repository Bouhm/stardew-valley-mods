using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace NPCMapLocations.Framework.Menus
{
    internal class ModMinimap
    {
        /*********
        ** Fields
        *********/
        private readonly Texture2D BuildingMarkers;
        private readonly int BorderWidth = 12;
        private readonly bool DrawPamHouseUpgrade;
        private readonly bool DrawMovieTheaterJoja;
        private readonly bool DrawMovieTheater;
        private readonly bool DrawIsland;
        private readonly Dictionary<long, FarmerMarker> FarmerMarkers;
        private readonly ModCustomizations Customizations;

        private Vector2 PrevCenter;
        private Vector2 Center; // Center position of minimap
        private float CropX; // Top-left position of crop on map
        private float CropY; // Top-left position of crop on map
        private int MmWidth; // minimap width
        private int MmHeight; // minimap height
        private Vector2 MmLoc; // minimap location relative to map location
        private int MmX; // top-left position of minimap relative to viewport
        private int MmY; // top-left position of minimap relative to viewport
        private int Offset; // offset for minimap if viewport changed
        private readonly Dictionary<string, NpcMarker> NpcMarkers;
        private Vector2 PlayerLoc;
        private int PrevMmX;
        private int PrevMmY;
        private int DrawDelay;
        private bool DragStarted;

        private Dictionary<string, bool> ConditionalNpcs { get; }
        private Dictionary<string, KeyValuePair<string, Vector2>> FarmBuildings { get; }


        /*********
        ** Public methods
        *********/
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

            this.DrawPamHouseUpgrade = Game1.MasterPlayer.mailReceived.Contains("pamHouseUpgrade");
            this.DrawMovieTheaterJoja = Utility.doesMasterPlayerHaveMailReceivedButNotMailForTomorrow("ccMovieTheaterJoja");
            this.DrawMovieTheater = Utility.doesMasterPlayerHaveMailReceivedButNotMailForTomorrow("ccMovieTheater");
            this.DrawIsland = Game1.MasterPlayer.hasOrWillReceiveMail("Visited_Island");

            this.MmX = ModEntry.Globals.MinimapX;
            this.MmY = ModEntry.Globals.MinimapY;
            this.MmWidth = ModEntry.Globals.MinimapWidth * Game1.pixelZoom;
            this.MmHeight = ModEntry.Globals.MinimapHeight * Game1.pixelZoom;
        }

        // Check if cursor is hovering the drag zone (top of minimap)
        public bool IsHoveringDragZone()
        {
            return (Game1.getMouseX() >= this.MmX - this.BorderWidth && Game1.getMouseX() <= this.MmX + this.MmWidth + this.BorderWidth &&
                Game1.getMouseY() >= this.MmY - this.BorderWidth && Game1.getMouseY() < this.MmY + this.MmHeight + this.BorderWidth);
        }

        public void HandleMouseDown()
        {
            this.PrevMmX = this.MmX;
            this.PrevMmY = this.MmY;
            this.DragStarted = true;
        }

        public void HandleMouseDrag()
        {
            if (this.DragStarted)
            {
                // Move minimap with mouse on drag
                this.MmX = this.NormalizeToMap(MathHelper.Clamp(this.PrevMmX + Game1.getMouseX() - MouseUtil.BeginMousePosition.X, this.BorderWidth,
                  Game1.viewport.Width - this.MmWidth - this.BorderWidth));
                this.MmY = this.NormalizeToMap(MathHelper.Clamp(this.PrevMmY + Game1.getMouseY() - MouseUtil.BeginMousePosition.Y, this.BorderWidth,
                  Game1.viewport.Height - this.MmHeight - this.BorderWidth));
            }
        }

        public void HandleMouseRelease()
        {
            ModEntry.Globals.MinimapX = this.MmX;
            ModEntry.Globals.MinimapY = this.MmY;
            ModEntry.StaticHelper.Data.WriteJsonFile($"config/{Constants.SaveFolderName}.json", ModEntry.Config);
            this.DragStarted = false;

            // Delay drawing on minimap after it's moved
            if (this.IsHoveringDragZone())
                this.DrawDelay = 30;
        }

        public void UpdateMapForSeason()
        {
            ModEntry.Map = Game1.content.Load<Texture2D>("LooseSprites\\map");
        }

        public void CheckOffsetForMap()
        {
            // When ModMain.Map is smaller than viewport (ex. Bus Stop)
            if (Game1.isOutdoorMapSmallerThanViewport())
            {
                this.Offset = (Game1.viewport.Width - Game1.currentLocation.map.Layers[0].LayerWidth * Game1.tileSize) / 2;

                if (this.MmX > Math.Max(12, this.Offset))
                    this.Offset = 0;
            }
            else
                this.Offset = 0;
        }

        public void Resize()
        {
            this.MmWidth = ModEntry.Globals.MinimapWidth * Game1.pixelZoom;
            this.MmHeight = ModEntry.Globals.MinimapHeight * Game1.pixelZoom;
        }

        public void Update()
        {
            // Note: Absolute positions relative to viewport are scaled 4x (Game1.pixelZoom).
            // Positions relative to the map are not.

            this.Center = ModEntry.LocationToMap(Game1.player.currentLocation.uniqueName.Value ?? Game1.player.currentLocation.Name, Game1.player.getTileX(), Game1.player.getTileY(), this.Customizations.MapVectors, this.Customizations.LocationExclusions);

            // Player in unknown location, use previous location as center
            if (this.Center.Equals(ModEntry.Unknown) && this.PrevCenter != null)
                this.Center = this.PrevCenter;
            else
                this.PrevCenter = this.Center;
            this.PlayerLoc = this.PrevCenter;

            this.Center.X = this.NormalizeToMap(this.Center.X);
            this.Center.Y = this.NormalizeToMap(this.Center.Y);

            // Top-left offset for markers, relative to the minimap
            this.MmLoc = new Vector2(this.MmX - this.Center.X + (float)Math.Floor(this.MmWidth / 2.0), this.MmY - this.Center.Y + (float)Math.Floor(this.MmHeight / 2.0));

            // Top-left corner of minimap cropped from the whole map
            // Centered around the player's location on the map
            this.CropX = this.Center.X - (float)Math.Floor(this.MmWidth / 2.0);
            this.CropY = this.Center.Y - (float)Math.Floor(this.MmHeight / 2.0);

            // Handle cases when reaching edge of map 
            // Change offsets accordingly when player is no longer centered
            if (this.CropX < 0)
            {
                this.Center.X = this.MmWidth / 2;
                this.MmLoc.X = this.MmX;
                this.CropX = 0;
            }
            else if (this.CropX + this.MmWidth > 1200)
            {
                this.Center.X = 1200 - this.MmWidth / 2;
                this.MmLoc.X = this.MmX - (1200 - this.MmWidth);
                this.CropX = 1200 - this.MmWidth;
            }

            if (this.CropY < 0)
            {
                this.Center.Y = this.MmHeight / 2;
                this.MmLoc.Y = this.MmY;
                this.CropY = 0;
            }
            // Actual map is 1200x720 but map.Height includes the farms
            else if (this.CropY + this.MmHeight > 720)
            {
                this.Center.Y = 720 - this.MmHeight / 2;
                this.MmLoc.Y = this.MmY - (720 - this.MmHeight);
                this.CropY = 720 - this.MmHeight;
            }
        }

        // Center or the player's position is used as reference; player is not center when reaching edge of map
        public void DrawMiniMap()
        {
            var b = Game1.spriteBatch;
            bool isHoveringMinimap = this.IsHoveringDragZone();
            int offsetMmX = this.MmX + this.Offset;

            // Make transparent on hover
            var color = isHoveringMinimap
              ? Color.White * 0.25f
              : Color.White;

            if (isHoveringMinimap)
            {
                Game1.mouseCursor = 2;
            }

            if (ModEntry.Map == null) return;

            b.Draw(ModEntry.Map, new Vector2(offsetMmX, this.MmY),
              new Rectangle((int)Math.Floor(this.CropX / Game1.pixelZoom),
                (int)Math.Floor(this.CropY / Game1.pixelZoom), this.MmWidth / Game1.pixelZoom + 2,
                this.MmHeight / Game1.pixelZoom + 2), color, 0f, Vector2.Zero,
              4f, SpriteEffects.None, 0.86f);

            // Don't draw markers while being dragged
            if (!(this.IsHoveringDragZone() && ModEntry.StaticHelper.Input.GetState(SButton.MouseRight) == SButtonState.Held))
            {
                // When minimap is moved, redraw markers after recalculating & repositioning
                if (this.DrawDelay == 0)
                {
                    if (!isHoveringMinimap)
                        this.DrawMarkers();
                }
                else
                    this.DrawDelay--;
            }

            // Border around minimap that will also help mask markers outside of the minimap
            // Which gives more padding for when they are considered within the minimap area
            // Draw border
            this.DrawLine(b, new Vector2(offsetMmX, this.MmY - this.BorderWidth), new Vector2(offsetMmX + this.MmWidth - 2, this.MmY - this.BorderWidth), this.BorderWidth,
              Game1.menuTexture, new Rectangle(8, 256, 3, this.BorderWidth), color * 1.5f);
            this.DrawLine(b, new Vector2(offsetMmX + this.MmWidth + this.BorderWidth, this.MmY),
              new Vector2(offsetMmX + this.MmWidth + this.BorderWidth, this.MmY + this.MmHeight - 2), this.BorderWidth, Game1.menuTexture,
              new Rectangle(8, 256, 3, this.BorderWidth), color * 1.5f);
            this.DrawLine(b, new Vector2(offsetMmX + this.MmWidth, this.MmY + this.MmHeight + this.BorderWidth),
              new Vector2(offsetMmX + 2, this.MmY + this.MmHeight + this.BorderWidth), this.BorderWidth, Game1.menuTexture,
              new Rectangle(8, 256, 3, this.BorderWidth), color * 1.5f);
            this.DrawLine(b, new Vector2(offsetMmX - this.BorderWidth, this.MmHeight + this.MmY), new Vector2(offsetMmX - this.BorderWidth, this.MmY + 2), this.BorderWidth,
              Game1.menuTexture, new Rectangle(8, 256, 3, this.BorderWidth), color * 1.5f);

            // Draw the border corners
            b.Draw(Game1.menuTexture, new Rectangle(offsetMmX - this.BorderWidth, this.MmY - this.BorderWidth, this.BorderWidth, this.BorderWidth),
              new Rectangle(0, 256, this.BorderWidth, this.BorderWidth), color * 1.5f);
            b.Draw(Game1.menuTexture, new Rectangle(offsetMmX + this.MmWidth, this.MmY - this.BorderWidth, this.BorderWidth, this.BorderWidth),
              new Rectangle(48, 256, this.BorderWidth, this.BorderWidth), color * 1.5f);
            b.Draw(Game1.menuTexture, new Rectangle(offsetMmX + this.MmWidth, this.MmY + this.MmHeight, this.BorderWidth, this.BorderWidth),
              new Rectangle(48, 304, this.BorderWidth, this.BorderWidth), color * 1.5f);
            b.Draw(Game1.menuTexture, new Rectangle(offsetMmX - this.BorderWidth, this.MmY + this.MmHeight, this.BorderWidth, this.BorderWidth),
              new Rectangle(0, 304, this.BorderWidth, this.BorderWidth), color * 1.5f);
        }


        /*********
        ** Private methods
        *********/
        private void DrawMarkers()
        {
            var b = Game1.spriteBatch;
            var color = Color.White;
            int offsetMmX = this.MmX + this.Offset;
            var offsetMmLoc = new Vector2(this.MmLoc.X + this.Offset + 2, this.MmLoc.Y + 2);

            //
            // ===== Farm types =====
            //
            // The farms are always overlayed at (0, 43) on the map
            // The crop position, dimensions, and overlay position must all be adjusted accordingly
            // When any part of the cropped farm is outside of the minimap as player moves
            int farmWidth = 131;
            int farmHeight = 61;
            int farmX = this.NormalizeToMap(MathHelper.Clamp(offsetMmLoc.X, offsetMmX, offsetMmX + this.MmWidth));
            int farmY = this.NormalizeToMap(MathHelper.Clamp(offsetMmLoc.Y + 172, this.MmY, this.MmY + this.MmHeight) + 2);
            int farmCropX = (int)MathHelper.Clamp((offsetMmX - offsetMmLoc.X) / Game1.pixelZoom, 0, farmWidth);
            int farmCropY = (int)MathHelper.Clamp((this.MmY - offsetMmLoc.Y - 172) / Game1.pixelZoom, 0, farmHeight);

            // Check if farm crop extends outside of minimap
            int farmCropWidth = (farmX / Game1.pixelZoom + farmWidth > (offsetMmX + this.MmWidth) / Game1.pixelZoom) ? (offsetMmX + this.MmWidth - farmX) / Game1.pixelZoom : farmWidth - farmCropX;
            int farmCropHeight = (farmY / Game1.pixelZoom + farmHeight > (this.MmY + this.MmHeight) / Game1.pixelZoom) ? (this.MmY + this.MmHeight - farmY) / Game1.pixelZoom : farmHeight - farmCropY;

            // Check if farm crop extends beyond farm size
            if (farmCropX + farmCropWidth > farmWidth)
                farmCropWidth = farmWidth - farmCropX;

            if (farmCropY + farmCropHeight > farmHeight)
                farmCropHeight = farmHeight - farmCropY;

            switch (Game1.whichFarm)
            {
                case 1:
                    b.Draw(ModEntry.Map, new Vector2(farmX, farmY), new Rectangle(0 + farmCropX, 180 + farmCropY, farmCropWidth, farmCropHeight), color, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
                    break;
                case 2:
                    b.Draw(ModEntry.Map, new Vector2(farmX, farmY), new Rectangle(131 + farmCropX, 180 + farmCropY, farmCropWidth, farmCropHeight), color, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
                    break;
                case 3:
                    b.Draw(ModEntry.Map, new Vector2(farmX, farmY), new Rectangle(0 + farmCropX, 241 + farmCropY, farmCropWidth, farmCropHeight), color, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
                    break;
                case 4:
                    b.Draw(ModEntry.Map, new Vector2(farmX, farmY), new Rectangle(131 + farmCropX, 241 + farmCropY, farmCropWidth, farmCropHeight), color, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
                    break;
                case 5:
                    b.Draw(ModEntry.Map, new Vector2(farmX, farmY), new Rectangle(0 + farmCropX, 302 + farmCropY, farmCropWidth, farmCropHeight), color, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
                    break;
                case 6:
                    b.Draw(ModEntry.Map, new Vector2(farmX, farmY), new Rectangle(131 + farmCropX, 302 + farmCropY, farmCropWidth, farmCropHeight), color, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
                    break;
            }

            if (this.DrawPamHouseUpgrade)
            {
                var houseLoc = ModEntry.LocationToMap("Trailer_Big");
                if (this.IsWithinMapArea(houseLoc.X, houseLoc.Y))
                    b.Draw(ModEntry.Map, new Vector2(this.NormalizeToMap(offsetMmLoc.X + houseLoc.X - 16), this.NormalizeToMap(offsetMmLoc.Y + houseLoc.Y - 11)), new Rectangle(263, 181, 8, 8), color, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
            }

            if (this.DrawMovieTheater || this.DrawMovieTheaterJoja)
            {
                var theaterLoc = ModEntry.LocationToMap("JojaMart");
                if (this.IsWithinMapArea(theaterLoc.X, theaterLoc.Y))
                {
                    b.Draw(ModEntry.Map, new Vector2(this.NormalizeToMap(offsetMmLoc.X + theaterLoc.X - 20), this.NormalizeToMap(offsetMmLoc.Y + theaterLoc.Y - 11)), new Rectangle(275, 181, 15, 11), color, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
                }
            }

            if (this.DrawIsland)
            {
                int islandWidth = 40;
                int islandHeight = 30;
                int islandImageX = 208;
                int islandImageY = 363;
                int mapX = 1040;
                int mapY = 600;

                if (ModEntry.Globals.UseDetailedIsland)
                {
                    islandWidth = 45;
                    islandHeight = 40;
                    islandImageX = 248;
                    islandImageY = 363;
                    mapX = 1020;
                    mapY = 560;
                }

                int islandX = this.NormalizeToMap(MathHelper.Clamp(offsetMmLoc.X + mapX, offsetMmX, offsetMmX + this.MmWidth));
                int islandY = this.NormalizeToMap(MathHelper.Clamp(offsetMmLoc.Y + mapY, this.MmY, this.MmY + this.MmHeight) + 2);
                int islandCropX = (int)MathHelper.Clamp((offsetMmX - offsetMmLoc.X - mapX) / Game1.pixelZoom, 0, islandWidth);
                int islandCropY = (int)MathHelper.Clamp((this.MmY - offsetMmLoc.Y - mapY) / Game1.pixelZoom, 0, islandHeight);

                // Check if island crop extends outside of minimap
                int islandCropWidth = (islandX / Game1.pixelZoom + islandWidth > (offsetMmX + this.MmWidth) / Game1.pixelZoom) ? (offsetMmX + this.MmWidth - islandX) / Game1.pixelZoom : islandWidth - islandCropX;
                int islandCropHeight = (islandY / Game1.pixelZoom + islandHeight > (this.MmY + this.MmHeight) / Game1.pixelZoom) ? (this.MmY + this.MmHeight - islandY) / Game1.pixelZoom : islandHeight - islandCropY;

                // Check if island crop extends beyond island size
                if (islandCropX + islandCropWidth > islandWidth)
                    islandCropWidth = islandWidth - islandCropX;

                if (islandCropY + islandCropHeight > islandHeight)
                    islandCropHeight = islandHeight - islandCropY;

                b.Draw(ModEntry.Map, new Vector2(islandX, islandY), new Rectangle(islandImageX + islandCropX, islandImageY + islandCropY, islandCropWidth, islandCropHeight), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
            }

            //
            // ===== Farm buildings =====
            //
            if (ModEntry.Globals.ShowFarmBuildings && this.FarmBuildings != null && this.BuildingMarkers != null)
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
            if (ModEntry.Globals.ShowTravelingMerchant && this.ConditionalNpcs["Merchant"])
            {
                Vector2 merchantLoc = new Vector2(ModConstants.MapVectors["Merchant"][0].MapX, ModConstants.MapVectors["Merchant"][0].MapY);
                if (this.IsWithinMapArea(merchantLoc.X - 16, merchantLoc.Y - 16))
                    b.Draw(Game1.mouseCursors, new Vector2(this.NormalizeToMap(offsetMmLoc.X + merchantLoc.X - 16), this.NormalizeToMap(offsetMmLoc.Y + merchantLoc.Y - 15)), new Rectangle(191, 1410, 22, 21), Color.White, 0f, Vector2.Zero, 1.3f, SpriteEffects.None, 1f);
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
                        || ModEntry.ShouldExcludeNpc(name)
                        || (!ModEntry.Globals.ShowHiddenVillagers && marker.IsHidden)
                        || (this.ConditionalNpcs.ContainsKey(name) && !this.ConditionalNpcs[name])
                    )
                        continue;

                    var markerColor = marker.IsHidden ? Color.DarkGray * 0.7f : Color.White;

                    // Draw NPC marker
                    var spriteRect = marker.Type == CharacterType.Horse ? new Rectangle(17, 104, 16, 14) : new Rectangle(0, marker.CropOffset, 16, 15);

                    if (marker.Type == CharacterType.Horse)
                    {
                        b.Draw(marker.Sprite, new Rectangle(this.NormalizeToMap(offsetMmLoc.X + marker.MapX), this.NormalizeToMap(offsetMmLoc.Y + marker.MapY), 30, 32), spriteRect, markerColor);
                    }
                    else
                    {
                        b.Draw(marker.Sprite, new Rectangle(this.NormalizeToMap(offsetMmLoc.X + marker.MapX), this.NormalizeToMap(offsetMmLoc.Y + marker.MapY), 30, 32), spriteRect, markerColor);
                    }

                    // Icons for birthday/quest
                    if (ModEntry.Globals.ShowQuests)
                    {
                        if (marker.IsBirthday && (Game1.player.friendshipData.ContainsKey(name) && Game1.player.friendshipData[name].GiftsToday == 0))
                            b.Draw(Game1.mouseCursors, new Vector2(this.NormalizeToMap(offsetMmLoc.X + marker.MapX + 20), this.NormalizeToMap(offsetMmLoc.Y + marker.MapY)), new Rectangle(147, 412, 10, 11), markerColor, 0f, Vector2.Zero, 1.8f, SpriteEffects.None, 0f);

                        if (marker.HasQuest)
                            b.Draw(Game1.mouseCursors, new Vector2(this.NormalizeToMap(offsetMmLoc.X + marker.MapX + 22), this.NormalizeToMap(offsetMmLoc.Y + marker.MapY - 3)), new Rectangle(403, 496, 5, 14), markerColor, 0f, Vector2.Zero, 1.8f, SpriteEffects.None, 0f);
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
                            farmer.FarmerRenderer.drawMiniPortrat(b, new Vector2(this.NormalizeToMap(offsetMmLoc.X + farMarker.MapX - 16), this.NormalizeToMap(offsetMmLoc.Y + farMarker.MapY - 15)), 0.00011f, 2f, 1, farmer);
                        }
                    }
                }
            }
            else
            {
                Game1.player.FarmerRenderer.drawMiniPortrat(b, new Vector2(this.NormalizeToMap(offsetMmLoc.X + this.PlayerLoc.X - 16), this.NormalizeToMap(offsetMmLoc.Y + this.PlayerLoc.Y - 15)), 0.00011f, 2f, 1, Game1.player);
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
            return x > this.Center.X - this.MmWidth / 2 - (Game1.tileSize / 4)
                   && x < this.Center.X + this.MmWidth / 2 - (Game1.tileSize / 4)
                   && y > this.Center.Y - this.MmHeight / 2 - (Game1.tileSize / 4)
                   && y < this.Center.Y + this.MmHeight / 2 - (Game1.tileSize / 4);
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
