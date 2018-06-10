using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NPCMapLocations
{

    public class ModMapPage : MapPage
    {
        private readonly IModHelper Helper;
        private readonly ModConfig Config;
        private readonly Dictionary<string, string> NpcNames;
        private Dictionary<string, bool> SecondaryNpcs { get; }
        private readonly Dictionary<string, Rect> locationRects = ModConstants.LocationRects;
        private HashSet<NPCMarker> NpcMarkers;
        private Dictionary<long, FarmerMarker> FarmerMarkers;
        private Dictionary<string, int> MarkerCrop { get; }
        private Dictionary<string, KeyValuePair<string, Vector2>> FarmBuildings { get; }
        private readonly Texture2D BuildingMarkers;
        private string hoveredNames = "";
        private string hoveredLocationText = "";
        private Texture2D map;
        private int mapX;
        private int mapY;
        private bool hasIndoorCharacter;
        private Vector2 indoorIconVector;
        private bool drawPamHouseUpgrade;

        // Map menu that uses modified map page and modified component locations for hover
        public ModMapPage(
            HashSet<NPCMarker> npcMarkers,
            Dictionary<string, string> npcNames,
            Dictionary<string, bool> secondaryNpcs,
            Dictionary<long, FarmerMarker> farmerMarkers,
            Dictionary<string, int> markerCrop,
            Dictionary<string, KeyValuePair<string, Vector2>> farmBuildings,
            Texture2D buildingMarkers,
            IModHelper helper,
            ModConfig config
        ) : base(Game1.viewport.Width / 2 - (800 + IClickableMenu.borderWidth * 2) / 2, Game1.viewport.Height / 2 - (600 + IClickableMenu.borderWidth * 2) / 2, 800 + IClickableMenu.borderWidth * 2, 600 + IClickableMenu.borderWidth * 2)
        {
            this.NpcMarkers = npcMarkers;
            this.NpcNames = npcNames;
            this.SecondaryNpcs = secondaryNpcs;
            this.FarmerMarkers = farmerMarkers;
            this.MarkerCrop = markerCrop;
            this.FarmBuildings = farmBuildings;
            this.BuildingMarkers = buildingMarkers;
            this.Helper = helper;
            this.Config = config;

            map = Game1.content.Load<Texture2D>("LooseSprites\\map");
            drawPamHouseUpgrade = Game1.MasterPlayer.mailReceived.Contains("pamHouseUpgrade");
            Vector2 center = Utility.getTopLeftPositionForCenteringOnScreen(map.Bounds.Width * 4, 720, 0, 0);
            mapX = (int)center.X;
            mapY = (int)center.Y;

            this.points.Clear();
            foreach (ClickableComponent point in this.GetMapPoints())
            {
                point.label = "";
                point.scale = 0.1f;
                this.points.Add(point);
            }
        }

        public override void performHoverAction(int x, int y)
        {
            hoveredLocationText = "";
            hoveredNames = "";
            hasIndoorCharacter = false;
            foreach (ClickableComponent current in points)
            {
                if (current.containsPoint(x, y))
                {
                    hoveredLocationText = current.name;
                    break;
                }
            }

            List<string> hoveredList = new List<string>();

            const int markerWidth = 32;
            const int markerHeight = 30;
            // Have to use special character to separate strings for Chinese
            string separator = LocalizedContentManager.CurrentLanguageCode.Equals(LocalizedContentManager.LanguageCode.zh) ? "，" : ", ";

            if (Context.IsMainPlayer)
            {
                foreach (NPCMarker npcMarker in this.NpcMarkers)
                {
                    Rectangle npcLocation = npcMarker.Location;
                    if (Game1.getMouseX() >= npcLocation.X && Game1.getMouseX() <= npcLocation.X + markerWidth && Game1.getMouseY() >= npcLocation.Y && Game1.getMouseY() <= npcLocation.Y + markerHeight)
                    {
                        if (this.NpcNames.ContainsKey(npcMarker.Npc.Name) && !npcMarker.IsHidden)
                            hoveredList.Add(this.NpcNames[npcMarker.Npc.Name]);

                        if (!npcMarker.IsOutdoors && !hasIndoorCharacter)
                            hasIndoorCharacter = true;
                    }
                }
            }
            if (Context.IsMultiplayer)
            {
                foreach (FarmerMarker farMarker in FarmerMarkers.Values)
                {
                    if (Game1.getMouseX() >= farMarker.Location.X - markerWidth / 2
                        && Game1.getMouseX() <= farMarker.Location.X + markerWidth / 2
                        && Game1.getMouseY() >= farMarker.Location.Y - markerHeight / 2
                        && Game1.getMouseY() <= farMarker.Location.Y + markerHeight / 2)
                    {
                        hoveredList.Add(farMarker.Name);

                        if (!farMarker.IsOutdoors && !hasIndoorCharacter)
                            hasIndoorCharacter = true;
                    }
                }
            }

            foreach (string name in hoveredList)
            {
                hoveredNames = hoveredList[0];
                for (int i = 1; i < hoveredList.Count; i++)
                {
                    var lines = hoveredNames.Split('\n');
                    if ((int)Game1.smallFont.MeasureString(lines[lines.Length - 1] + separator + hoveredList[i]).X > (int)Game1.smallFont.MeasureString("Home of Robin, Demetrius, Sebastian & Maru").X) // Longest string
                    {
                        hoveredNames += separator + Environment.NewLine;
                        hoveredNames += hoveredList[i];
                    }
                    else
                    {
                        hoveredNames += separator + hoveredList[i];
                    }
                }
            }
        }

        // Draw location and name tooltips
        public override void draw(SpriteBatch b)
        {
            DrawMap(b);
            DrawMarkers(b);

            int x = Game1.getMouseX() + Game1.tileSize / 2;
            int y = Game1.getMouseY() + Game1.tileSize / 2;
            int width;
            int height;
            int offsetY = 0;

            this.performHoverAction(x - Game1.tileSize / 2, y - Game1.tileSize / 2);

            if (!hoveredLocationText.Equals(""))
            {
                IClickableMenu.drawHoverText(b, hoveredLocationText, Game1.smallFont, 0, 0, -1, null, -1, null, null, 0, -1, -1, -1, -1, 1f, null);
                int textLength = (int)Game1.smallFont.MeasureString(hoveredLocationText).X + Game1.tileSize / 2;
                width = Math.Max((int)Game1.smallFont.MeasureString(hoveredLocationText).X + Game1.tileSize / 2, textLength);
                height = (int)Math.Max(60, Game1.smallFont.MeasureString(hoveredLocationText).Y + Game1.tileSize / 2);
                if (x + width > Game1.viewport.Width)
                {
                    x = Game1.viewport.Width - width;
                    y += Game1.tileSize / 4;
                }
                if (this.Config.NameTooltipMode == 1)
                {
                    if (y + height > Game1.viewport.Height)
                    {
                        x += Game1.tileSize / 4;
                        y = Game1.viewport.Height - height;
                    }
                    offsetY = 2 - Game1.tileSize;
                }
                else if (this.Config.NameTooltipMode == 2)
                {
                    if (y + height > Game1.viewport.Height)
                    {
                        x += Game1.tileSize / 4;
                        y = Game1.viewport.Height - height;
                    }
                    offsetY = height - 4;
                }
                else
                {
                    if (y + height > Game1.viewport.Height)
                    {
                        x += Game1.tileSize / 4;
                        y = Game1.viewport.Height - height;
                    }
                }

                // Draw name tooltip positioned around location tooltip
                DrawNames(b, hoveredNames, x, y, offsetY, height, this.Config.NameTooltipMode);

                // Draw location tooltip
                IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, y, width, height, Color.White, 1f, false);
                b.DrawString(Game1.smallFont, hoveredLocationText, new Vector2((float)(x + Game1.tileSize / 4), (float)(y + Game1.tileSize / 4 + 4)) + new Vector2(2f, 2f), Game1.textShadowColor);
                b.DrawString(Game1.smallFont, hoveredLocationText, new Vector2((float)(x + Game1.tileSize / 4), (float)(y + Game1.tileSize / 4 + 4)) + new Vector2(0f, 2f), Game1.textShadowColor);
                b.DrawString(Game1.smallFont, hoveredLocationText, new Vector2((float)(x + Game1.tileSize / 4), (float)(y + Game1.tileSize / 4 + 4)) + new Vector2(2f, 0f), Game1.textShadowColor);
                b.DrawString(Game1.smallFont, hoveredLocationText, new Vector2((float)(x + Game1.tileSize / 4), (float)(y + Game1.tileSize / 4 + 4)), Game1.textColor * 0.9f);
            }
            else
            {
                // Draw name tooltip only
                DrawNames(Game1.spriteBatch, hoveredNames, x, y, offsetY, this.height, this.Config.NameTooltipMode);
            }

            // Draw indoor icon
            if (hasIndoorCharacter && !String.IsNullOrEmpty(hoveredNames))
                b.Draw(Game1.mouseCursors, indoorIconVector, new Rectangle?(new Rectangle(448, 64, 32, 32)), Color.White, 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);

            // Cursor
            if (!Game1.options.hardwareCursor)
                b.Draw(Game1.mouseCursors, new Vector2(Game1.getOldMouseX(), Game1.getOldMouseY()), new Rectangle?(Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, (Game1.options.gamepadControls ? 44 : 0), 16, 16)), Color.White, 0f, Vector2.Zero, Game1.pixelZoom + Game1.dialogueButtonScale / 150f, SpriteEffects.None, 1f);
        }

        // Draw map to cover base rendering 
        public void DrawMap(SpriteBatch b)
        {
            int boxY = mapY - 96;
            int mY = mapY;
            Game1.drawDialogueBox(mapX - 32, boxY, (map.Bounds.Width + 16) * 4, 848, false, true, null, false);
            b.Draw(map, new Vector2((float)mapX, (float)mY), new Rectangle(0, 0, 300, 180), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.86f);
            switch (Game1.whichFarm)
            {
                case 1:
                    b.Draw(map, new Vector2((float)mapX, (float)(mY + 172)), new Rectangle(0, 180, 131, 61), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
                    break;
                case 2:
                    b.Draw(map, new Vector2((float)mapX, (float)(mY + 172)), new Rectangle(131, 180, 131, 61), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
                    break;
                case 3:
                    b.Draw(map, new Vector2((float)mapX, (float)(mY + 172)), new Rectangle(0, 241, 131, 61), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
                    break;
                case 4:
                    b.Draw(map, new Vector2((float)mapX, (float)(mY + 172)), new Rectangle(131, 241, 131, 61), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
                    break;
            }
            if (drawPamHouseUpgrade)
            {
                b.Draw(map, new Vector2((float)(mapX + 780), (float)(mapY + 348)), new Rectangle(263, 181, 8, 8), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
            }

            var player = Game1.player;
            int x = player.getTileX();
            int y = player.getTileY();
            string playerLocationName = null;
            switch (player.currentLocation.Name)
            {
                case "Saloon":
                    playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11172");
                    break;
                case "Beach":
                    playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11174");
                    break;
                case "Mountain":
                    if (x < 38)
                        playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11176");
                    else if (x < 96)
                        playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11177");
                    else
                        playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11178");
                    break;
                case "Tunnel":
                case "Backwoods":
                    playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11180");
                    break;
                case "FarmHouse":
                case "Barn":
                case "Big Barn":
                case "Deluxe Barn":
                case "Coop":
                case "Big Coop":
                case "Deluxe Coop":
                case "Cabin":
                case "Slime Hutch":
                case "Greenhouse":
                case "FarmCave":
                case "Shed":
                case "Farm":
                    playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11064", player.farmName.Value);
                    break;
                case "Forest":
                    if (y > 51)
                        playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11186");
                    else if (x < 58)
                        playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11186");
                    else
                        playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11188");
                    break;
                case "Town":
                    if (x > 84 && y < 68)
                        playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11190");
                    else if (x > 80 && y >= 68)
                        playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11190");
                    else if (y <= 42)
                        playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11190");
                    else if (y > 42 && y < 76)
                        playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11190");
                    else
                        playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11190");
                    break;
                case "Temp":
                    if (player.currentLocation.Map.Id.Contains("Town"))
                    {
                        if (x > 84 && y < 68)
                            playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11190");
                        else if (x > 80 && y >= 68)
                            playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11190");
                        else if (y <= 42)
                            playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11190");
                        else if (y > 42 && y < 76)
                            playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11190");
                        else
                            playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11190");
                    }
                    break;
            }
            if (playerLocationName != null)
            {
                SpriteText.drawStringWithScrollCenteredAt(b, playerLocationName, base.xPositionOnScreen + base.width / 2, base.yPositionOnScreen + base.height + 32 + 16, "", 1f, -1, 0, 0.88f, false);
            }
        }

        // Draw event
        // Subtractions within location vectors are to set the origin to the center of the sprite
        public void DrawMarkers(SpriteBatch b)
        {
            if (Config.ShowFarmBuildings)
            {
                var sortedBuildings = ModMain.FarmBuildings.ToList();
                sortedBuildings.Sort((x, y) => x.Value.Value.Y.CompareTo(y.Value.Value.Y));

                foreach (KeyValuePair<string, KeyValuePair<string, Vector2>> building in sortedBuildings)
                {
                    if (ModConstants.FarmBuildingRects.TryGetValue(building.Value.Key, out Rectangle buildingRect))
                        b.Draw(BuildingMarkers, new Vector2(building.Value.Value.X - buildingRect.Width / 2, building.Value.Value.Y - buildingRect.Height / 2), new Rectangle?(buildingRect), Color.White, 0f, Vector2.Zero, 3f, SpriteEffects.None, 1f);
                }
            }

            // Traveling Merchant
            if (Config.ShowTravelingMerchant && SecondaryNpcs["Merchant"])
            {
                Vector2 merchantLoc = ModMain.LocationToMap("Forest", 28, 11);
                b.Draw(Game1.mouseCursors, new Vector2(merchantLoc.X - 16, merchantLoc.Y - 15), new Rectangle?(new Rectangle(191, 1410, 22, 21)), Color.White, 0f, Vector2.Zero, 1.3f, SpriteEffects.None, 1f);
            }

            // Farmers
            if (Context.IsMultiplayer)
            {
                foreach (Farmer farmer in Game1.getOnlineFarmers())
                {
                    // Temporary solution to handle desync of farmhand location/tile position when changing location
                    if (FarmerMarkers.TryGetValue(farmer.UniqueMultiplayerID, out FarmerMarker farMarker))
                        if (farMarker.DrawDelay == 0)
                            farmer.FarmerRenderer.drawMiniPortrat(b, new Vector2(farMarker.Location.X - 16, farMarker.Location.Y - 15), 0.00011f, 2f, 1, farmer);
                }
            }
            else
            {
                Vector2 playerLoc = ModMain.GetMapPosition(Game1.player.currentLocation, Game1.player.getTileX(), Game1.player.getTileY());
                Game1.player.FarmerRenderer.drawMiniPortrat(b, new Vector2(playerLoc.X - 16, playerLoc.Y - 15), 0.00011f, 2f, 1, Game1.player);
            }

            // NPCs
            // Sort by drawing order
            var sortedMarkers = NpcMarkers.ToList();
            sortedMarkers.Sort((x, y) => x.Layer.CompareTo(y.Layer));

            foreach (NPCMarker npcMarker in sortedMarkers)
            {
                if (npcMarker.Location == Rectangle.Empty || npcMarker.Marker == null || !MarkerCrop.ContainsKey(npcMarker.Npc.Name)) { continue; }

                // Tint/dim hidden markers
                if (npcMarker.IsHidden)
                {
                    b.Draw(npcMarker.Marker, npcMarker.Location, new Rectangle?(new Rectangle(0, MarkerCrop[npcMarker.Npc.Name], 16, 15)), Color.DimGray * 0.7f);
                    {
                        b.Draw(Game1.mouseCursors, new Vector2(npcMarker.Location.X + 20, npcMarker.Location.Y), new Rectangle?(new Rectangle(147, 412, 10, 11)), Color.DimGray * 0.7f, 0f, Vector2.Zero, 1.8f, SpriteEffects.None, 0f);
                    }
                    if (npcMarker.HasQuest)
                    {
                        b.Draw(Game1.mouseCursors, new Vector2(npcMarker.Location.X + 22, npcMarker.Location.Y - 3), new Rectangle?(new Rectangle(403, 496, 5, 14)), Color.DimGray * 0.7f, 0f, Vector2.Zero, 1.8f, SpriteEffects.None, 0f);
                    }
                }
                else
                {
                    b.Draw(npcMarker.Marker, npcMarker.Location, new Rectangle?(new Rectangle(0, MarkerCrop[npcMarker.Npc.Name], 16, 15)), Color.White);
                    if (npcMarker.IsBirthday)
                    {
                        b.Draw(Game1.mouseCursors, new Vector2(npcMarker.Location.X + 20, npcMarker.Location.Y), new Rectangle?(new Rectangle(147, 412, 10, 11)), Color.White, 0f, Vector2.Zero, 1.8f, SpriteEffects.None, 0f);
                    }
                    if (npcMarker.HasQuest)
                    {
                        b.Draw(Game1.mouseCursors, new Vector2(npcMarker.Location.X + 22, npcMarker.Location.Y - 3), new Rectangle?(new Rectangle(403, 496, 5, 14)), Color.White, 0f, Vector2.Zero, 1.8f, SpriteEffects.None, 0f);
                    }
                }
            }
        }

        // Draw NPC name tooltips map page
        public void DrawNames(SpriteBatch b, string names, int x, int y, int offsetY, int relocate, int nameTooltipMode)
        {
            if (hoveredNames.Equals("")) return;

            indoorIconVector = Vector2.Zero;
            var lines = names.Split('\n');
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
                if (x + width > Game1.viewport.Width)
                {
                    x = Game1.viewport.Width - width;
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
                if (x + width > Game1.viewport.Width)
                {
                    x = Game1.viewport.Width - width;
                }
                // If going off screen on the bottom, move tooltip to above location tooltip so it stays visible
                if (y + height > Game1.viewport.Height)
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

            if (hasIndoorCharacter) { indoorIconVector = new Vector2(x - Game1.tileSize / 8 + 2, y - Game1.tileSize / 8 + 2); }
            Vector2 vector = new Vector2(x + (float)(Game1.tileSize / 4), y + (float)(Game1.tileSize / 4 + 4));

            drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, y, width, height, Color.White, 1f, true);
            b.DrawString(Game1.smallFont, names, vector + new Vector2(2f, 2f), Game1.textShadowColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            b.DrawString(Game1.smallFont, names, vector + new Vector2(0f, 2f), Game1.textShadowColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            b.DrawString(Game1.smallFont, names, vector + new Vector2(2f, 0f), Game1.textShadowColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            b.DrawString(Game1.smallFont, names, vector, Game1.textColor * 0.9f, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
        }

        // Override snappy controls on controller
        public override bool overrideSnappyMenuCursorMovementBan()
        {
            return true;
        }

        // Get location and area of location component
        private Rectangle GetLocationRect(string location)
        {
            // Set origin to center
            return new Rectangle(
                (int)ModMain.LocationToMap(location).X - locationRects[location].width / 2,
                (int)ModMain.LocationToMap(location).Y - locationRects[location].height / 2,
                locationRects[location].width,
                locationRects[location].height
            );
        }

        /// <summary>Get the map points to display on a map.</summary>
        private IEnumerable<ClickableComponent> GetMapPoints()
        {
            yield return new ClickableComponent(
                GetLocationRect("Desert_Region"),
                Game1.player.mailReceived.Contains("ccVault") ? Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11062", new object[0]) : "???"
            );
            yield return new ClickableComponent(
                GetLocationRect("Farm_Region"),
                Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11064", new object[] { Game1.player.farmName.Value })
            );
            yield return new ClickableComponent(
                GetLocationRect("Backwoods_Region"),
                Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11065", new object[0])
            );
            yield return new ClickableComponent(
                GetLocationRect("BusStop_Region"),
                Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11066", new object[0])
            );
            yield return new ClickableComponent(
                GetLocationRect("WizardHouse"),
                Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11067", new object[0])
            );
            yield return new ClickableComponent(
                GetLocationRect("AnimalShop"),
                Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11068", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11069", new object[0])
            );
            yield return new ClickableComponent(
                GetLocationRect("LeahHouse"),
                Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11070", new object[0])
            );
            yield return new ClickableComponent(
                GetLocationRect("SamHouse"),
                Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11071", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11072", new object[0])
            );
            yield return new ClickableComponent(
                GetLocationRect("HaleyHouse"),
                Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11073", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11074", new object[0])
            );
            yield return new ClickableComponent(
                GetLocationRect("TownSquare"),
                Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11075", new object[0])
            );
            yield return new ClickableComponent(
                GetLocationRect("Hospital"),
                Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11076", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11077", new object[0])
            );
            yield return new ClickableComponent(
                GetLocationRect("SeedShop"),
                string.Concat(new string[]
                {
                Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11078", new object[0]),
                Environment.NewLine,
                Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11079", new object[0]),
                Environment.NewLine,
                Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11080", new object[0])
                })
            );
            yield return new ClickableComponent(
                GetLocationRect("Blacksmith"),
                Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11081", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11082", new object[0])
            );
            yield return new ClickableComponent(
                GetLocationRect("Saloon"),
                Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11083", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11084", new object[0])
            );
            yield return new ClickableComponent(
                GetLocationRect("ManorHouse"),
                Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11085", new object[0])
            );
            yield return new ClickableComponent(
                GetLocationRect("ArchaeologyHouse"),
                Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11086", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11087", new object[0])
            );
            yield return new ClickableComponent(
                GetLocationRect("ElliottHouse"),
                Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11088", new object[0])
            );
            yield return new ClickableComponent(
                GetLocationRect("Sewer"),
                Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11089", new object[0])
            );
            yield return new ClickableComponent(
                GetLocationRect("Graveyard"),
                Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11090", new object[0])
            );
            yield return new ClickableComponent(
                GetLocationRect("Trailer"),
                Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11091", new object[0])
            );
            yield return new ClickableComponent(
                GetLocationRect("JoshHouse"),
                Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11092", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11093", new object[0])
            );
            yield return new ClickableComponent(
                GetLocationRect("ScienceHouse"),
                string.Concat(
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11094", new object[0]),
                    Environment.NewLine,
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11095", new object[0]),
                    Environment.NewLine,
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11096", new object[0])
                )
            );
            yield return new ClickableComponent(
                GetLocationRect("Tent"),
                Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11097", new object[0])
            );
            yield return new ClickableComponent(
                GetLocationRect("Mine"),
                Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11098", new object[0])
            );
            yield return new ClickableComponent(
                GetLocationRect("AdventureGuild"),
                (Game1.stats.DaysPlayed >= 5u) ? (Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11099", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11100", new object[0])) : "???"
            );
            yield return new ClickableComponent(
                GetLocationRect("Quarry"),
                Game1.player.mailReceived.Contains("ccCraftsRoom") ? Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11103", new object[0]) : "???"
            );
            yield return new ClickableComponent(
                GetLocationRect("JojaMart"),
                Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11105", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11106", new object[0])
            );
            yield return new ClickableComponent(
                GetLocationRect("FishShop"),
                Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11107", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11108", new object[0])
            );
            yield return new ClickableComponent(
                GetLocationRect("Spa"),
                Game1.isLocationAccessible("Railroad") ? (Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11110", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11111", new object[0])) : "???"
            );
            yield return new ClickableComponent(
                GetLocationRect("Woods"),
                Game1.player.mailReceived.Contains("beenToWoods") ? Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11114", new object[0]) : "???"
            );
            yield return new ClickableComponent(
                GetLocationRect("RuinedHouse"),
                Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11116", new object[0])
            );
            yield return new ClickableComponent(
                GetLocationRect("CommunityCenter"),
                Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11117", new object[0])
            );
            yield return new ClickableComponent(
                GetLocationRect("SewerPipe"),
                Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11118", new object[0])
            );
            yield return new ClickableComponent(
                GetLocationRect("Railroad_Region"),
                Game1.isLocationAccessible("Railroad") ? Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11119", new object[0]) : "???"
            );
            yield return new ClickableComponent(
                GetLocationRect("LonelyStone"),
                Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11122", new object[0])
            );
        }
    }

    public class Rect
    {
        public int width;
        public int height;

        public Rect(int width, int height)
        {
            this.width = width;
            this.height = height;
        }
    }
}
