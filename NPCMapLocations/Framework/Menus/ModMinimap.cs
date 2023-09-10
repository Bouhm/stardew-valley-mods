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

        /// <summary>The pixel position and size of the minimap, adjusted automatically to fit on the screen.</summary>
        private readonly ScreenBounds ScreenBounds;

        private Vector2 MmLoc; // minimap location relative to map location
        private readonly Dictionary<string, NpcMarker> NpcMarkers;
        private Vector2 PlayerLoc;
        private int PrevMmX;
        private int PrevMmY;
        private int DrawDelay;
        private bool DragStarted;

        private Dictionary<string, bool> ConditionalNpcs { get; }
        private Dictionary<string, BuildingMarker> FarmBuildings { get; }


        /*********
        ** Public methods
        *********/
        public ModMinimap(
          Dictionary<string, NpcMarker> npcMarkers,
          Dictionary<string, bool> conditionalNpcs,
          Dictionary<long, FarmerMarker> farmerMarkers,
          Dictionary<string, BuildingMarker> farmBuildings,
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

            this.ScreenBounds = new(
                x: ModEntry.Globals.MinimapX,
                y: ModEntry.Globals.MinimapY,
                width: ModEntry.Globals.MinimapWidth * Game1.pixelZoom,
                height: ModEntry.Globals.MinimapHeight * Game1.pixelZoom
            );
        }

        // Check if cursor is hovering the drag zone (top of minimap)
        public bool IsHoveringDragZone()
        {
            return
                !ModEntry.Globals.LockMinimapPosition
                && this.ScreenBounds.Contains(MouseUtil.GetScreenPoint());
        }

        public void HandleMouseDown()
        {
            if (Context.IsPlayerFree)
            {
                this.PrevMmX = this.ScreenBounds.X;
                this.PrevMmY = this.ScreenBounds.Y;
                this.DragStarted = true;
            }
        }

        public void HandleMouseDrag()
        {
            if (this.DragStarted)
            {
                // stop if dragging should no longer be allowed
                if (!Context.IsPlayerFree)
                    this.HandleMouseRelease();

                // else move minimap
                else
                {
                    var mousePos = MouseUtil.GetScreenPosition();
                    this.ScreenBounds.SetDesiredBounds(
                        x: this.NormalizeToMap(MathHelper.Clamp(this.PrevMmX + mousePos.X - MouseUtil.BeginMousePosition.X, this.BorderWidth, Game1.uiViewport.Width - this.ScreenBounds.Width - this.BorderWidth)),
                        y: this.NormalizeToMap(MathHelper.Clamp(this.PrevMmY + mousePos.Y - MouseUtil.BeginMousePosition.Y, this.BorderWidth, Game1.uiViewport.Height - this.ScreenBounds.Height - this.BorderWidth))
                    );
                }
            }
        }

        public void HandleMouseRelease()
        {
            ModEntry.Globals.MinimapX = this.ScreenBounds.X;
            ModEntry.Globals.MinimapY = this.ScreenBounds.Y;
            ModEntry.StaticHelper.Data.WriteJsonFile("config/globals.json", ModEntry.Globals);
            this.DragStarted = false;

            // Delay drawing on minimap after it's moved
            if (this.IsHoveringDragZone())
                this.DrawDelay = 30;
        }

        public void UpdateMapForSeason()
        {
            ModEntry.Map = Game1.content.Load<Texture2D>("LooseSprites\\map");
        }

        /// <summary>Adjust the minimap draw bounds to fit on-screen when the window size changes.</summary>
        public void OnWindowResized()
        {
            this.ScreenBounds.Recalculate();
        }

        public void Resize()
        {
            this.ScreenBounds.SetDesiredBounds(
                width: ModEntry.Globals.MinimapWidth * Game1.pixelZoom,
                height: ModEntry.Globals.MinimapHeight * Game1.pixelZoom
            );
        }

        public void Update()
        {
            int x = this.ScreenBounds.X;
            int y = this.ScreenBounds.Y;
            int width = this.ScreenBounds.Width;
            int height = this.ScreenBounds.Height;

            // Note: Absolute positions relative to viewport are scaled 4x (Game1.pixelZoom).
            // Positions relative to the map are not.

            WorldMapPosition mapPos = ModEntry.GetWorldMapPosition(Game1.player.currentLocation.uniqueName.Value ?? Game1.player.currentLocation.Name, Game1.player.TilePoint.X, Game1.player.TilePoint.Y, this.Customizations.LocationExclusions);

            this.Center = new Vector2(mapPos.X, mapPos.Y);

            // Player in unknown location, use previous location as center
            if (mapPos.IsEmpty && this.PrevCenter != null)
                this.Center = this.PrevCenter;
            else
                this.PrevCenter = this.Center;
            this.PlayerLoc = this.PrevCenter;

            this.Center.X = this.NormalizeToMap(this.Center.X);
            this.Center.Y = this.NormalizeToMap(this.Center.Y);

            // Top-left offset for markers, relative to the minimap
            this.MmLoc = new Vector2(x - this.Center.X + (float)Math.Floor(width / 2.0), y - this.Center.Y + (float)Math.Floor(height / 2.0));

            // Top-left corner of minimap cropped from the whole map
            // Centered around the player's location on the map
            this.CropX = this.Center.X - (float)Math.Floor(width / 2.0);
            this.CropY = this.Center.Y - (float)Math.Floor(height / 2.0);

            // Handle cases when reaching edge of map 
            // Change offsets accordingly when player is no longer centered
            if (this.CropX < 0)
            {
                this.Center.X = width / 2;
                this.MmLoc.X = x;
                this.CropX = 0;
            }
            else if (this.CropX + width > 1200)
            {
                this.Center.X = 1200 - width / 2;
                this.MmLoc.X = x - (1200 - width);
                this.CropX = 1200 - width;
            }

            if (this.CropY < 0)
            {
                this.Center.Y = height / 2;
                this.MmLoc.Y = y;
                this.CropY = 0;
            }
            // Actual map is 1200x720 but map.Height includes the farms
            else if (this.CropY + height > 720)
            {
                this.Center.Y = 720 - height / 2;
                this.MmLoc.Y = y - (720 - height);
                this.CropY = 720 - height;
            }
        }

        // Center or the player's position is used as reference; player is not center when reaching edge of map
        public void DrawMiniMap()
        {
            int x = this.ScreenBounds.X;
            int y = this.ScreenBounds.Y;
            int width = this.ScreenBounds.Width;
            int height = this.ScreenBounds.Height;

            var b = Game1.spriteBatch;
            bool isHoveringMinimap = this.IsHoveringDragZone();

            // Make transparent on hover
            var color = isHoveringMinimap
              ? Color.White * 0.25f
              : Color.White;

            if (isHoveringMinimap)
                Game1.mouseCursor = 2;

            if (ModEntry.Map == null)
                return;

            Rectangle drawBounds = new Rectangle(
                x: (int)Math.Floor(this.CropX / Game1.pixelZoom),
                y: (int)Math.Floor(this.CropY / Game1.pixelZoom),
                width: width / Game1.pixelZoom + 2,
                height: height / Game1.pixelZoom + 2
            );
            b.Draw(ModEntry.Map, new Vector2(x, y), drawBounds, color, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.86f);

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
            this.DrawLine(b, new Vector2(x, y - this.BorderWidth), new Vector2(x + width - 2, y - this.BorderWidth), this.BorderWidth,
              Game1.menuTexture, new Rectangle(8, 256, 3, this.BorderWidth), color * 1.5f);
            this.DrawLine(b, new Vector2(x + width + this.BorderWidth, y),
              new Vector2(x + width + this.BorderWidth, y + height - 2), this.BorderWidth, Game1.menuTexture,
              new Rectangle(8, 256, 3, this.BorderWidth), color * 1.5f);
            this.DrawLine(b, new Vector2(x + width, y + height + this.BorderWidth),
              new Vector2(x + 2, y + height + this.BorderWidth), this.BorderWidth, Game1.menuTexture,
              new Rectangle(8, 256, 3, this.BorderWidth), color * 1.5f);
            this.DrawLine(b, new Vector2(x - this.BorderWidth, height + y), new Vector2(x - this.BorderWidth, y + 2), this.BorderWidth,
              Game1.menuTexture, new Rectangle(8, 256, 3, this.BorderWidth), color * 1.5f);

            // Draw the border corners
            b.Draw(Game1.menuTexture, new Rectangle(x - this.BorderWidth, y - this.BorderWidth, this.BorderWidth, this.BorderWidth),
              new Rectangle(0, 256, this.BorderWidth, this.BorderWidth), color * 1.5f);
            b.Draw(Game1.menuTexture, new Rectangle(x + width, y - this.BorderWidth, this.BorderWidth, this.BorderWidth),
              new Rectangle(48, 256, this.BorderWidth, this.BorderWidth), color * 1.5f);
            b.Draw(Game1.menuTexture, new Rectangle(x + width, y + height, this.BorderWidth, this.BorderWidth),
              new Rectangle(48, 304, this.BorderWidth, this.BorderWidth), color * 1.5f);
            b.Draw(Game1.menuTexture, new Rectangle(x - this.BorderWidth, y + height, this.BorderWidth, this.BorderWidth),
              new Rectangle(0, 304, this.BorderWidth, this.BorderWidth), color * 1.5f);
        }


        /*********
        ** Private methods
        *********/
        private void DrawMarkers()
        {
            string regionId = ModEntry.GetWorldMapRegion();

            int x = this.ScreenBounds.X;
            int y = this.ScreenBounds.Y;
            int width = this.ScreenBounds.Width;
            int height = this.ScreenBounds.Height;

            var b = Game1.spriteBatch;
            var color = Color.White;
            var offsetMmLoc = new Vector2(this.MmLoc.X + 2, this.MmLoc.Y + 2);

            if (regionId == "Valley")
            {
                //
                // ===== Farm types =====
                //
                // The farms are always overlayed at (0, 43) on the map
                // The crop position, dimensions, and overlay position must all be adjusted accordingly
                // When any part of the cropped farm is outside of the minimap as player moves
                int farmWidth = 131;
                int farmHeight = 61;
                int farmX = this.NormalizeToMap(MathHelper.Clamp(offsetMmLoc.X, x, x + width));
                int farmY = this.NormalizeToMap(MathHelper.Clamp(offsetMmLoc.Y + 172, y, y + height) + 2);
                int farmCropX = (int)MathHelper.Clamp((x - offsetMmLoc.X) / Game1.pixelZoom, 0, farmWidth);
                int farmCropY = (int)MathHelper.Clamp((y - offsetMmLoc.Y - 172) / Game1.pixelZoom, 0, farmHeight);

                // Check if farm crop extends outside of minimap
                int farmCropWidth = (farmX / Game1.pixelZoom + farmWidth > (x + width) / Game1.pixelZoom) ? (x + width - farmX) / Game1.pixelZoom : farmWidth - farmCropX;
                int farmCropHeight = (farmY / Game1.pixelZoom + farmHeight > (y + height) / Game1.pixelZoom) ? (y + height - farmY) / Game1.pixelZoom : farmHeight - farmCropY;

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
                    var houseLoc = ModEntry.GetWorldMapPosition("Trailer_Big");
                    int screenX = this.NormalizeToMap(offsetMmLoc.X + houseLoc.X - 16);
                    int screenY = this.NormalizeToMap(offsetMmLoc.Y + houseLoc.Y - 11);

                    if (this.ScreenBounds.Contains(screenX, screenY))
                        b.Draw(ModEntry.Map, new Vector2(screenX, screenY), new Rectangle(263, 181, 8, 8), color, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
                }

                if (this.DrawMovieTheater || this.DrawMovieTheaterJoja)
                {
                    var theaterLoc = ModEntry.GetWorldMapPosition("JojaMart");
                    int screenX = this.NormalizeToMap(offsetMmLoc.X + theaterLoc.X - 20);
                    int screenY = this.NormalizeToMap(offsetMmLoc.Y + theaterLoc.Y - 11);

                    if (this.ScreenBounds.Contains(screenX, screenY))
                    {
                        b.Draw(ModEntry.Map, new Vector2(screenX, screenY), new Rectangle(275, 181, 15, 11), color, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
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

                    int islandX = this.NormalizeToMap(MathHelper.Clamp(offsetMmLoc.X + mapX, x, x + width));
                    int islandY = this.NormalizeToMap(MathHelper.Clamp(offsetMmLoc.Y + mapY, y, y + height) + 2);
                    int islandCropX = (int)MathHelper.Clamp((x - offsetMmLoc.X - mapX) / Game1.pixelZoom, 0, islandWidth);
                    int islandCropY = (int)MathHelper.Clamp((y - offsetMmLoc.Y - mapY) / Game1.pixelZoom, 0, islandHeight);

                    // Check if island crop extends outside of minimap
                    int islandCropWidth = (islandX / Game1.pixelZoom + islandWidth > (x + width) / Game1.pixelZoom) ? (x + width - islandX) / Game1.pixelZoom : islandWidth - islandCropX;
                    int islandCropHeight = (islandY / Game1.pixelZoom + islandHeight > (y + height) / Game1.pixelZoom) ? (y + height - islandY) / Game1.pixelZoom : islandHeight - islandCropY;

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
                    foreach (BuildingMarker building in this.FarmBuildings.Values.OrderBy(p => p.WorldMapPosition.Y))
                    {
                        if (ModConstants.FarmBuildingRects.TryGetValue(building.CommonName, out var buildingRect))
                        {
                            int screenX = this.NormalizeToMap(offsetMmLoc.X + building.WorldMapPosition.X - (float)Math.Floor(buildingRect.Width / 2.0));
                            int screenY = this.NormalizeToMap(offsetMmLoc.Y + building.WorldMapPosition.Y - (float)Math.Floor(buildingRect.Height / 2.0));

                            if (this.ScreenBounds.Contains(screenX, screenY))
                                b.Draw(this.BuildingMarkers, new Vector2(screenX, screenY), buildingRect, color, 0f, Vector2.Zero, 3f, SpriteEffects.None, 1f);
                        }
                    }
                }
            }

            //
            // ===== NPCs =====
            //
            // Sort by drawing order
            if (this.NpcMarkers != null)
            {
                var sortedMarkers = this.NpcMarkers
                    .Where(p => p.Value.WorldMapPosition.RegionId == regionId)
                    .OrderBy(p => p.Value.Layer);

                foreach (var npcMarker in sortedMarkers)
                {
                    string name = npcMarker.Key;
                    var marker = npcMarker.Value;

                    Vector2 rawPos = new(offsetMmLoc.X + marker.WorldMapPosition.X, offsetMmLoc.Y + marker.WorldMapPosition.Y);
                    Point screenPos = new(this.NormalizeToMap(rawPos.X), this.NormalizeToMap(rawPos.Y));

                    // Skip if no specified location
                    if (
                        marker.Sprite == null
                        || !this.ScreenBounds.Contains(screenPos)
                        || ModEntry.ShouldExcludeNpc(name)
                        || (!ModEntry.Globals.ShowHiddenVillagers && marker.IsHidden)
                        || (this.ConditionalNpcs.ContainsKey(name) && !this.ConditionalNpcs[name])
                    )
                        continue;

                    var markerColor = marker.IsHidden ? Color.DarkGray * 0.7f : Color.White;

                    // Draw NPC marker
                    Rectangle spriteRect = marker.GetSpriteSourceRect();
                    b.Draw(marker.Sprite, new Rectangle(screenPos.X, screenPos.Y, 30, 32), spriteRect, markerColor);

                    // Icons for birthday/quest
                    if (ModEntry.Globals.ShowQuests)
                    {
                        if (marker.IsBirthday && (Game1.player.friendshipData.ContainsKey(name) && Game1.player.friendshipData[name].GiftsToday == 0))
                            b.Draw(Game1.mouseCursors, new Vector2(this.NormalizeToMap(rawPos.X + 20), screenPos.Y), new Rectangle(147, 412, 10, 11), markerColor, 0f, Vector2.Zero, 1.8f, SpriteEffects.None, 0f);

                        if (marker.HasQuest)
                            b.Draw(Game1.mouseCursors, new Vector2(this.NormalizeToMap(rawPos.X + 22), this.NormalizeToMap(rawPos.Y - 3)), new Rectangle(403, 496, 5, 14), markerColor, 0f, Vector2.Zero, 1.8f, SpriteEffects.None, 0f);
                    }
                }
            }

            if (Context.IsMultiplayer)
            {
                foreach (var farmer in Game1.getOnlineFarmers())
                {
                    // Temporary solution to handle desync of farmhand location/tile position when changing location
                    if (this.FarmerMarkers.TryGetValue(farmer.UniqueMultiplayerID, out var farMarker) && farMarker.WorldMapPosition.RegionId == regionId)
                    {
                        int screenX = this.NormalizeToMap(offsetMmLoc.X + farMarker.WorldMapPosition.X - 16);
                        int screenY = this.NormalizeToMap(offsetMmLoc.Y + farMarker.WorldMapPosition.Y - 15);

                        if (this.ScreenBounds.Contains(screenX, screenY) && farMarker.DrawDelay == 0)
                            farmer.FarmerRenderer.drawMiniPortrat(b, new Vector2(screenX, screenY), 0.00011f, 2f, 1, farmer);
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
