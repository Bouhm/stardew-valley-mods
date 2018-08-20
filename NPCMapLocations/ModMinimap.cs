using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace NPCMapLocations
{
    class ModMinimap
    {
        private Texture2D map;
        private readonly ModConfig Config;
        private bool drawPamHouseUpgrade;
        private Dictionary<string, bool> SecondaryNpcs { get; }
        private HashSet<MapMarker> NpcMarkers;
        private Dictionary<long, MapMarker> FarmerMarkers;
        private Dictionary<string, int> MarkerCropOffsets { get; }
        private Dictionary<string, KeyValuePair<string, Vector2>> FarmBuildings { get; }
        private readonly Texture2D BuildingMarkers;

        private Vector2 mmPos;
        private int mmX = 12;
        private int mmY = 12;
        private int mmWidth = 450;
        private int mmHeight = 270;
        private float cropX;
        private float cropY;
        private Vector2 center;
        private Vector2 playerLoc;

        public ModMinimap(
            HashSet<MapMarker> npcMarkers,
            Dictionary<string, bool> secondaryNpcs,
            Dictionary<long, MapMarker> farmerMarkers,
            Dictionary<string, int> MarkerCropOffsets,
            Dictionary<string, KeyValuePair<string, Vector2>> farmBuildings,
            Texture2D buildingMarkers,
            ModConfig config
        )
        {
            this.NpcMarkers = npcMarkers;
            this.SecondaryNpcs = secondaryNpcs;
            this.FarmerMarkers = farmerMarkers;
            this.MarkerCropOffsets = MarkerCropOffsets;
            this.FarmBuildings = farmBuildings;
            this.BuildingMarkers = buildingMarkers;
            this.Config = config;

            map = Game1.content.Load<Texture2D>("LooseSprites\\map");
            drawPamHouseUpgrade = Game1.MasterPlayer.mailReceived.Contains("pamHouseUpgrade");
        }

        public void Update()
        {
            center = ModMain.LocationToMap(Game1.player.currentLocation.Name, Game1.player.getTileX(),
                Game1.player.getTileY());
            playerLoc = center;

            center.X = NormalizeToMap(center.X);
            center.Y = NormalizeToMap(center.Y);

            // Top-left offset for markers, relative to the minimap
            mmPos =
                new Vector2(mmX - center.X + (float)Math.Floor(mmWidth / 2.0),
                    mmY - center.Y + (float)Math.Floor(mmHeight / 2.0));

            // Top-left corner of minimap cropped from the whole map
            // Centered around the player's location on the map
            cropX = center.X - (float)Math.Floor(mmWidth / 2.0);
            cropY = center.Y - (float)Math.Floor(mmHeight / 2.0);

            // Handle cases when reaching edge of map
            // Change offsets accordingly when player is no longer centered
            if (cropX < 0)
            {
                center.X = mmWidth / 2;
                mmPos.X = mmX;
                cropX = 0;
            }
            else if (cropX + mmWidth > map.Width * Game1.pixelZoom)
            {
                center.X = map.Width * Game1.pixelZoom - mmWidth / 2;
                mmPos.X = mmX - (map.Width * Game1.pixelZoom - mmWidth);
                cropX = map.Width - mmWidth;
            }

            if (cropY < 0)
            {
                center.Y = mmHeight / 2;
                mmPos.Y = mmY;
                cropY = 0;
            }
            // Actual map is 1200x720 but map.Height includes the farms
            else if (cropY + mmHeight > 720)
            {
                center.Y = 720 - mmHeight / 2;
                mmPos.Y = mmY - (720 - mmHeight);
                cropY = 720 - mmHeight;
            }
        }

        // Center or the player's position is used as reference; player is not center when reaching edge of map
        public void DrawMiniMap(SpriteBatch b)
        {
            // Crop and draw minimap
            b.Draw(map, new Vector2((float)mmX, (float)mmY),
                new Rectangle((int)Math.Floor(cropX / Game1.pixelZoom),
                    (int)Math.Floor(cropY / Game1.pixelZoom), mmWidth / Game1.pixelZoom + 2,
                    mmHeight / Game1.pixelZoom + 2), Color.White, 0f, Vector2.Zero,
                4f, SpriteEffects.None, 0.86f);

            // Farm overlay
            int farmCropWidth = (int)MathHelper.Min(131, (mmWidth - mmPos.X + Game1.tileSize / 4) / Game1.pixelZoom);
            int farmCropHeight = (int)MathHelper.Min(61, (mmHeight - mmPos.Y - 172 + Game1.tileSize / 4) / Game1.pixelZoom);
            switch (Game1.whichFarm)
            {
                case 1:
                    b.Draw(map, new Vector2(NormalizeToMap(mmPos.X), NormalizeToMap(mmPos.Y + 172)), new Rectangle(0, 180, farmCropWidth, farmCropHeight), Color.White,
                        0f,
                        Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
                    break;
                case 2:
                    b.Draw(map, new Vector2(NormalizeToMap(mmPos.X), NormalizeToMap(mmPos.Y + 172)), new Rectangle(131, 180, farmCropWidth, farmCropHeight), Color.White,
                        0f,
                        Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
                    break;
                case 3:
                    b.Draw(map, new Vector2(NormalizeToMap(mmPos.X), NormalizeToMap(mmPos.Y + 172)), new Rectangle(0, 241, farmCropWidth, farmCropHeight), Color.White,
                        0f,
                        Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
                    break;
                case 4:
                    b.Draw(map, new Vector2(NormalizeToMap(mmPos.X), NormalizeToMap(mmPos.Y + 172)), new Rectangle(131, 241, farmCropWidth, farmCropHeight), Color.White,
                        0f,
                        Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
                    break;
            }

            if (drawPamHouseUpgrade)
            {
                int pamHouseX = ModConstants.MapVectors["Trailer"][0].X;
                int pamHouseY = ModConstants.MapVectors["Trailer"][0].Y;
                if (IsWithinMapArea(pamHouseX, pamHouseY))
                {
                    b.Draw(map, new Vector2(NormalizeToMap(mmPos.X + pamHouseX), NormalizeToMap(mmPos.Y + pamHouseY)),
                        new Rectangle(263, 181, 8, 8), Color.White,
                        0f, Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
                }
            }

            if (Config.ShowFarmBuildings && FarmBuildings != null)
            {
                var sortedBuildings = ModMain.FarmBuildings.ToList();
                sortedBuildings.Sort((x, y) => x.Value.Value.Y.CompareTo(y.Value.Value.Y));

                foreach (KeyValuePair<string, KeyValuePair<string, Vector2>> building in sortedBuildings)
                    if (ModConstants.FarmBuildingRects.TryGetValue(building.Value.Key, out Rectangle buildingRect))
                    {
                        if (IsWithinMapArea(building.Value.Value.X - buildingRect.Width / 2,
                            building.Value.Value.Y - buildingRect.Height / 2))
                        {
                            b.Draw(
                                BuildingMarkers,
                                new Vector2(
                                    NormalizeToMap(mmPos.X + building.Value.Value.X - (float)Math.Floor(buildingRect.Width / 2.0)),
                                    NormalizeToMap(mmPos.Y + building.Value.Value.Y - (float)Math.Floor(buildingRect.Height / 2.0))
                                ),
                                new Rectangle?(buildingRect), Color.White, 0f, Vector2.Zero, 3f, SpriteEffects.None, 1f
                            );
                        }
                    }
            }

            // Traveling Merchant
            if (Config.ShowTravelingMerchant && SecondaryNpcs["Merchant"])
            {
                Vector2 merchantLoc = ModMain.LocationToMap("Forest", 28, 11);
                if (IsWithinMapArea(merchantLoc.X - 16, merchantLoc.Y - 16))
                {
                    b.Draw(Game1.mouseCursors, new Vector2(NormalizeToMap(mmPos.X + merchantLoc.X - 16), NormalizeToMap(mmPos.Y + merchantLoc.Y - 15)),
                        new Rectangle?(new Rectangle(191, 1410, 22, 21)), Color.White, 0f, Vector2.Zero, 1.3f, SpriteEffects.None,
                        1f);
                }
            }

            // Farmers
            if (Context.IsMultiplayer)
            {
                foreach (Farmer farmer in Game1.getOnlineFarmers())
                {
                    // Temporary solution to handle desync of farmhand location/tile position when changing location
                    if (FarmerMarkers.TryGetValue(farmer.UniqueMultiplayerID, out MapMarker farMarker))
                        if (farMarker.DrawDelay == 0 &&
                            IsWithinMapArea(farMarker.Location.X - 16, farMarker.Location.Y - 15))
                            farmer.FarmerRenderer.drawMiniPortrat(b,
                                new Vector2(NormalizeToMap(mmPos.X + farMarker.Location.X - 16), NormalizeToMap(mmPos.Y + farMarker.Location.Y - 15)),
                                0.00011f, 2f, 1, farmer);
                }
            }
            else
            {
                Game1.player.FarmerRenderer.drawMiniPortrat(b,
                    new Vector2(NormalizeToMap(mmPos.X + playerLoc.X - 16), NormalizeToMap(mmPos.Y + playerLoc.Y - 15)), 0.00011f, 2f, 1,
                    Game1.player);
            }

            // NPCs
            // Sort by drawing order
            var sortedMarkers = NpcMarkers.ToList();
            sortedMarkers.Sort((x, y) => x.Layer.CompareTo(y.Layer));

            foreach (MapMarker npcMarker in sortedMarkers)
            {
                // Skip if no specified location
                if (npcMarker.Location == Vector2.Zero || npcMarker.Marker == null ||
                    !MarkerCropOffsets.ContainsKey(npcMarker.Npc.Name) ||
                    !IsWithinMapArea(npcMarker.Location.X, npcMarker.Location.Y))
                {
                    continue;
                }

                // Tint/dim hidden markers
                if (npcMarker.IsHidden)
                {
                    b.Draw(npcMarker.Marker,
                        new Rectangle(NormalizeToMap(mmPos.X + npcMarker.Location.X), NormalizeToMap(mmPos.Y + npcMarker.Location.Y),
                            32, 30),
                        new Rectangle?(new Rectangle(0, MarkerCropOffsets[npcMarker.Npc.Name], 16, 15)), Color.DimGray * 0.7f);
                    if (npcMarker.IsBirthday)
                    {
                        // Gift icon
                        b.Draw(Game1.mouseCursors,
                            new Vector2(NormalizeToMap(mmPos.X + npcMarker.Location.X + 20), NormalizeToMap(mmPos.Y + npcMarker.Location.Y)),
                            new Rectangle?(new Rectangle(147, 412, 10, 11)), Color.DimGray * 0.7f, 0f, Vector2.Zero, 1.8f,
                            SpriteEffects.None, 0f);
                    }

                    if (npcMarker.HasQuest)
                    {
                        // Quest icon
                        b.Draw(Game1.mouseCursors,
                            new Vector2(NormalizeToMap(mmPos.X + npcMarker.Location.X + 22), NormalizeToMap(mmPos.Y + npcMarker.Location.Y - 3)),
                            new Rectangle?(new Rectangle(403, 496, 5, 14)), Color.DimGray * 0.7f, 0f, Vector2.Zero, 1.8f,
                            SpriteEffects.None, 0f);
                    }
                }
                else
                {
                    b.Draw(npcMarker.Marker,
                        new Rectangle(NormalizeToMap(mmPos.X + npcMarker.Location.X), NormalizeToMap(mmPos.Y + npcMarker.Location.Y),
                            30, 32),
                        new Rectangle?(new Rectangle(0, MarkerCropOffsets[npcMarker.Npc.Name], 16, 15)), Color.White);
                    if (npcMarker.IsBirthday)
                    {
                        // Gift icon
                        b.Draw(Game1.mouseCursors,
                            new Vector2(NormalizeToMap(mmPos.X + npcMarker.Location.X + 20), NormalizeToMap(mmPos.Y + npcMarker.Location.Y)),
                            new Rectangle?(new Rectangle(147, 412, 10, 11)), Color.White, 0f, Vector2.Zero, 1.8f,
                            SpriteEffects.None,
                            0f);
                    }

                    if (npcMarker.HasQuest)
                    {
                        // Quest icon
                        b.Draw(Game1.mouseCursors,
                            new Vector2(NormalizeToMap(mmPos.X + npcMarker.Location.X + 22), NormalizeToMap(mmPos.Y + npcMarker.Location.Y - 3)),
                            new Rectangle?(new Rectangle(403, 496, 5, 14)), Color.White, 0f, Vector2.Zero, 1.8f, SpriteEffects.None,
                            0f);
                    }
                }
            }

            // Border around minimap that will also help mask markers outside of the minimap
            // Which gives more padding for when they are considered within the minimap area
            int borderWidth = 12;

            // Draw border
            DrawLine(b, new Vector2(mmX, mmY - borderWidth), new Vector2(mmX + mmWidth - 2, mmY - borderWidth), borderWidth,
                Game1.menuTexture, new Rectangle(8, 256, 3, borderWidth));
            DrawLine(b, new Vector2(mmX + mmWidth + borderWidth, mmY),
                new Vector2(mmX + mmWidth + borderWidth, mmY + mmHeight - 2), borderWidth, Game1.menuTexture,
                new Rectangle(8, 256, 3, borderWidth));
            DrawLine(b, new Vector2(mmX + mmWidth, mmY + mmHeight + borderWidth),
                new Vector2(mmX + 2, mmY + mmHeight + borderWidth), borderWidth, Game1.menuTexture,
                new Rectangle(8, 256, 3, borderWidth));
            DrawLine(b, new Vector2(mmX - borderWidth, mmHeight + mmY), new Vector2(mmX - borderWidth, mmY + 2), borderWidth,
                Game1.menuTexture, new Rectangle(8, 256, 3, borderWidth));

            // Draw the border corners
            b.Draw(Game1.menuTexture, new Rectangle(mmX - borderWidth, mmY - borderWidth, borderWidth, borderWidth),
                new Rectangle?(new Rectangle(0, 256, borderWidth, borderWidth)), Color.White);
            b.Draw(Game1.menuTexture, new Rectangle(mmX + mmWidth, mmY - borderWidth, borderWidth, borderWidth),
                new Rectangle?(new Rectangle(48, 256, borderWidth, borderWidth)), Color.White);
            b.Draw(Game1.menuTexture, new Rectangle(mmX + mmWidth, mmY + mmHeight, borderWidth, borderWidth),
                new Rectangle?(new Rectangle(48, 304, borderWidth, borderWidth)), Color.White);
            b.Draw(Game1.menuTexture, new Rectangle(mmX - borderWidth, mmY + mmHeight, borderWidth, borderWidth),
                new Rectangle?(new Rectangle(0, 304, borderWidth, borderWidth)), Color.White);
        }

        // Normalize offset differences caused by map being 4x less precise than map markers 
        // Makes the map and markers move together instead of markers moving more precisely (moving when minimap does not shift)
        private int NormalizeToMap(float n)
        {
            return (int)Math.Floor(n / Game1.pixelZoom) * Game1.pixelZoom;
        }

        // Check if within map
        private bool IsWithinMapArea(float x, float y)
        {
            return (
                x > center.X - mmWidth / 2 - (Game1.tileSize / 4 + 2)
                && x < center.X + mmWidth / 2 - (Game1.tileSize / 4 + 2)
                && y > center.Y - mmHeight / 2 - (Game1.tileSize / 4 + 2)
                && y < center.Y + mmHeight / 2 - (Game1.tileSize / 4 + 2));
        }

        // For borders
        private void DrawLine(SpriteBatch b, Vector2 begin, Vector2 end, int width, Texture2D tex, Rectangle srcRect)
        {
            Rectangle r = new Rectangle((int)begin.X, (int)begin.Y, (int)(end - begin).Length() + 2, width);
            Vector2 v = Vector2.Normalize(begin - end);
            float angle = (float)Math.Acos(Vector2.Dot(v, -Vector2.UnitX));
            if (begin.Y > end.Y) angle = MathHelper.TwoPi - angle;
            b.Draw(tex, r, srcRect, Color.White, angle, Vector2.Zero, SpriteEffects.None, 0);
        }
    }
}
