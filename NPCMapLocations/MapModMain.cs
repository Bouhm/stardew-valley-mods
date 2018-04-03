/*
NPC Map Locations Mod by Bouhm.
Shows NPC locations on a modified map.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Quests;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NPCMapLocations
{
    public class MapModMain : Mod
    {
        public static IModHelper modHelper;
        public static IMonitor monitor;
        public static MapModConfig config;
        public static Texture2D map;
        public static Texture2D buildings;
        public static string saveName;
        public static int customNpcId;
        public static bool isMenuOpen;
        public static Dictionary<string, int> markerCrop; // NPC head crops, top left corner (0, y), width = 16, height = 15 
        private static Dictionary<string, Dictionary<string, int>> customNPCs;
        private static Dictionary<string, string> npcNames = new Dictionary<string, string>(); // For custom names
        private static HashSet<NPCMarker> npcMarkers = new HashSet<NPCMarker>();
        private static bool[] showSecondaryNPCs = new Boolean[4];
        private static MapModMapPage modMapPage;
        private bool isInitialized;

        // For debug info
        private const bool DEBUG_MODE = false;
        private static Vector2 _tileLower; 
        private static Vector2 _tileUpper; 
        private static string alertFlag; 

        public override void Entry(IModHelper helper)
        {
            modHelper = helper;
            monitor = this.Monitor;
            MapModMain.map = MapModMain.modHelper.Content.Load<Texture2D>(@"content/map", ContentSource.ModFolder); // Load modified map page
            MapModMain.buildings = MapModMain.modHelper.Content.Load<Texture2D>(@"content/buildings", ContentSource.ModFolder); // Load cfarm buildings
            SaveEvents.AfterLoad += SaveEvents_AfterLoad;
            GameEvents.UpdateTick += GameEvents_UpdateTick;
            GraphicsEvents.OnPostRenderEvent += GraphicsEvents_OnPostRenderEvent;
            GraphicsEvents.OnPostRenderGuiEvent += GraphicsEvents_OnPostRenderGuiEvent;
            InputEvents.ButtonPressed += InputEvents_ButtonPressed;
            TimeEvents.AfterDayStarted += TimeEvents_AfterDayStarted;
        }

        private void InputEvents_ButtonPressed(object sender, EventArgsInput e)
        {
            if (Game1.hasLoadedGame && Game1.activeClickableMenu is GameMenu)
            {
                HandleInput((GameMenu)Game1.activeClickableMenu, e.Button);
            }
        }

        private void SaveEvents_AfterLoad(object sender, EventArgs e)
        {
            saveName = Constants.SaveFolderName;
            config = modHelper.ReadJsonFile<MapModConfig>($"config/{saveName}.json") ?? new MapModConfig();
            markerCrop = MapModConstants.MarkerCrop;
            customNPCs = config.CustomNPCs;
            HandleCustomMods();
        }

        private void TimeEvents_AfterDayStarted(object sender, EventArgs e)
        {
            showSecondaryNPCs[0] = Game1.player.mailReceived.Contains("ccVault");
            showSecondaryNPCs[1] = Game1.stats.DaysPlayed >= 5u;
            showSecondaryNPCs[2] = Game1.stats.DaysPlayed >= 5u;
            showSecondaryNPCs[3] = Game1.year >= 2;
            foreach (NPC npc in Utility.getAllCharacters())
            {
                // Handle case where Kent appears even though he shouldn't
                if (!npc.isVillager() || (npc.name.Equals("Kent") && Game1.year < 2)) { continue; }

                NPCMarker npcMarker = new NPCMarker(){
                    Name = npc.name, 
                    IsBirthday = npc.isBirthday(Game1.currentSeason, Game1.dayOfMonth)
                };
                npcMarkers.Add(npcMarker);
            }
        }

        private void HandleInput(GameMenu menu, SButton input)
        {
            if (menu.currentTab != GameMenu.mapTab) { return; }
            if (input.ToString().Equals(config.MenuKey) || input is SButton.ControllerB)
            {
                Game1.activeClickableMenu = new MapModMenu(
                    Game1.viewport.Width / 2 - (1100 + IClickableMenu.borderWidth * 2) / 2, 
                    Game1.viewport.Height / 2 - (725 + IClickableMenu.borderWidth * 2) / 2, 
                    1100 + IClickableMenu.borderWidth * 2, 
                    650 + IClickableMenu.borderWidth * 2, 
                    showSecondaryNPCs, 
                    customNPCs, 
                    npcNames
                );
                isMenuOpen = true;
            }
            else if (input.ToString().Equals(config.TooltipKey) || input is SButton.DPadUp || input is SButton.DPadRight)
            {
                ChangeTooltipConfig();
            }
            else if (input.ToString().Equals(config.TooltipKey) || input is SButton.DPadDown || input is SButton.DPadLeft)
            {
                ChangeTooltipConfig(false);
            }
        }

        private void ChangeTooltipConfig(bool incre = true)
        {
            if (incre)
            {
                if (++config.NameTooltipMode > 3)
                {
                    config.NameTooltipMode = 1;
                }
                modHelper.WriteJsonFile($"config/{saveName}.json", config);
            }
            else
            {
                if (--config.NameTooltipMode < 1)
                {
                    config.NameTooltipMode = 3;
                }
                modHelper.WriteJsonFile($"config/{saveName}.json", config);
            }
        }

        private void HandleCustomMods()
        {
            var initializeCustomNPCs = 1;
            if (customNPCs != null && customNPCs.Count != 0)
            {
                initializeCustomNPCs = 0;
            }
            int id = 1;
            foreach (NPC npc in Utility.getAllCharacters())
            {
                id = LoadCustomNPCs(npc, initializeCustomNPCs, id);
                LoadNPCCrop(npc);
                LoadCustomNames(npc);
            }
            config.CustomNPCs = customNPCs;
            modHelper.WriteJsonFile($"config/{saveName}.json", config);
            isInitialized = true;
        }

        private int LoadCustomNPCs(NPC npc, int initialize, int id)
        {
            if (initialize == 0)
            {
                int idx = 1;
                foreach (KeyValuePair<string, Dictionary<string, int>> customNPC in customNPCs)
                {
                    if (npc.name.Equals(customNPC.Key))
                    {
                        customNpcId = idx;
                    }

                    if (!customNPC.Value.ContainsKey("crop"))
                    {
                        customNPC.Value.Add("crop", 0);
                    }
                    if (!markerCrop.ContainsKey(customNPC.Key))
                    {
                        markerCrop.Add(customNPC.Key, customNPC.Value["crop"]);
                    }
                    idx++;
                }
            }
            else
            {
                if (npc.Schedule != null && IsCustomNPC(npc.name))
                {
                    if (!customNPCs.ContainsKey(npc.name))
                    {
                        var npcEntry = new Dictionary<string, int>
                        {
                            { "id", id },
                            { "crop", 0 }
                        };
                        customNPCs.Add(npc.name, npcEntry);
                        markerCrop.Add(npc.name, 0);
                        id++;
                    }
                }
            }
            return id;
        }

        private void LoadCustomNames(NPC npc)
        {
            if (!npcNames.ContainsKey(npc.name))
            {
                var customName = npc.getName();
                if (string.IsNullOrEmpty(customName))
                {
                    customName = npc.name;
                }
                npcNames.Add(npc.name, customName);
            }
        }

        private void LoadNPCCrop(NPC npc)
        {
            if (config.VillagerCrop != null && config.VillagerCrop.Count > 0)
            {
                foreach (KeyValuePair<string, int> villager in config.VillagerCrop)
                {
                    if (npc.name.Equals(villager.Key))
                    {
                        markerCrop[npc.name] = villager.Value;
                    }
                }
            }
        }

        // Calculated from mapping of game tile positions to pixel coordinates of the map in MapModConstants. 
        public static Vector2 LocationToMap(string location, int tileX = -1, int tileY = -1, bool isFarmer = false)
        {
            if (location == null)
            {
                return new Vector2(-5000, -5000);
            }

            // Get tile location of farm buildings in farm
            string[] farmBuildings = { "Coop", "Big Coop", "Deluxe Coop", "Barn", "Big Barn", "Deluxe Barn", "Slime Hutch", "Shed" };
            if (farmBuildings.Contains(location))
            {
                foreach (Building farmBuilding in Game1.getFarm().buildings)
                {
                    if (farmBuilding.indoors != null && farmBuilding.indoors.name.Equals(location))
                    {
                        // Set origin to center
                        tileX = (int)(farmBuilding.tileX - farmBuilding.tilesWide/2);
                        tileY = (int)(farmBuilding.tileY - farmBuilding.tilesHigh/2);
                        location = "Farm";
                    }
                }
            }

            var locVectors = MapModConstants.MapVectors[location];
            Vector2 mapPagePos = Utility.getTopLeftPositionForCenteringOnScreen(300 * Game1.pixelZoom, 180 * Game1.pixelZoom, 0, 0);
            int x = 0;
            int y = 0;

            // Handle regions and indoor locations
            if (locVectors.Count() == 1 || (tileX == -1 || tileY == -1))
            {
                x = locVectors.FirstOrDefault().x;
                y = locVectors.FirstOrDefault().y;
            }
            else
            {
                // Sort map vectors by distance to point
                var vectors = locVectors.OrderBy(vector => Math.Sqrt(Math.Pow(vector.tileX - tileX, 2) + Math.Pow(vector.tileY - tileY, 2)));

                MapVectors lower = null;
                MapVectors upper = null;
                var hasEqualTile = false;

                // Create rectangle bound from two pre-defined points (lower & upper bound) and calculate map scale for that area
                foreach (MapVectors vector in vectors)
                {
                    if (lower != null && upper != null)
                    {
                        if (lower.tileX == upper.tileX || lower.tileY == upper.tileY)
                        {
                            hasEqualTile = true;
                        }
                        else
                        {
                            break;
                        }
                    }
                    if ((lower == null || hasEqualTile) && (tileX >= vector.tileX && tileY >= vector.tileY))
                    {
                        lower = vector;
                        continue;
                    }
                    if ((upper == null || hasEqualTile) && (tileX <= vector.tileX && tileY <= vector.tileY))
                    {
                        upper = vector;
                    }
                }

                // Handle null cases - not enough vectors to calculate using lower/upper bound strategy
                // Uses fallback strategy - get closest points such that lower != upper
                string tilePos = "(" + tileX + ", " + tileY + ")";
                if (lower == null)
                {
                    if (isFarmer && DEBUG_MODE && alertFlag != "NullBound:" + tilePos)
                    {
                        MapModMain.monitor.Log("Null lower bound - No vector less than " + tilePos + " to calculate location.", LogLevel.Alert);
                        alertFlag = "NullBound:" + tilePos;
                    }

                    lower = upper == vectors.First() ? vectors.Skip(1).First() : vectors.First();
                }
                if (upper == null)
                {
                    if (DEBUG_MODE && isFarmer && alertFlag != "NullBound:" + tilePos)
                    {
                        MapModMain.monitor.Log("Null upper bound - No vector greater than " + tilePos + " to calculate location.", LogLevel.Alert);
                        alertFlag = "NullBound:" + tilePos;
                    }

                    upper = lower == vectors.First() ? vectors.Skip(1).First() : vectors.First();
                }

                x = (int)(lower.x + (double)(tileX - lower.tileX) / (double)(upper.tileX - lower.tileX) * (upper.x - lower.x));
                y = (int)(lower.y + (double)(tileY - lower.tileY) / (double)(upper.tileY - lower.tileY) * (upper.y - lower.y));

                if (DEBUG_MODE && isFarmer)
                {
                    MapModMain._tileUpper = new Vector2(upper.tileX, upper.tileY);
                    MapModMain._tileLower = new Vector2(lower.tileX, lower.tileY);
                }
            }
            return new Vector2((int)mapPagePos.X + x, (int)mapPagePos.Y + y);
        }

        private void GameEvents_UpdateTick(object sender, EventArgs e)
        {
            if (!Game1.hasLoadedGame) { return; }
            if (!IsMapOpen()) { return; }

            UpdateNPCMarkers();
        }

        private static bool IsMapOpen()
        {
            if (Game1.activeClickableMenu == null || isMenuOpen) { return false; }
            GameMenu menu = (GameMenu)Game1.activeClickableMenu;
            return menu.currentTab == GameMenu.mapTab;
        }

        private void UpdateNPCMarkers()
        {
            if (!isInitialized)
            {
                HandleCustomMods();
            }

            List<string> hoveredNames = new List<string>();

            foreach (NPCMarker npcMarker in npcMarkers)
            {
                NPC npc = Game1.getCharacterFromName(npcMarker.Name, true);
                string currentLocation;

                // Handle null locations at beginning of new game
                if (npc.currentLocation == null)
                {
                    MapModConstants.StartingLocations.TryGetValue(npc.name, out currentLocation);
                }
                else
                {
                    currentLocation = npc.currentLocation.name;
                }

                MapModConstants.MapVectors.TryGetValue(currentLocation, out MapVectors[] npcLocation);
                if (npcLocation == null)
                    continue;         

                // For layering indoor/outdoor NPCs and indoor indicator
                npcMarker.IsOutdoors = Game1.getLocationFromName(currentLocation).isOutdoors;

                if (npc.Schedule != null || npc.isMarried() || npc.name.Equals("Sandy") || npc.name.Equals("Marlon") || npc.name.Equals("Wizard"))
                {
                    // For show NPCs in player's location option
                    bool sameLocation = false;
                    if (config.OnlySameLocation)
                    {
                        MapModConstants.IndoorLocations.TryGetValue(npc.currentLocation.name, out string indoorLocationNPC);
                        MapModConstants.IndoorLocations.TryGetValue(Game1.player.currentLocation.name, out string indoorLocationPlayer);
                        if (indoorLocationPlayer != null && indoorLocationNPC != null)
                        {
                            sameLocation = indoorLocationNPC.Equals(indoorLocationPlayer);
                        }
                    }

                    // NPCs that won't be shown on the map unless Show Hidden NPCs is checked
                    npcMarker.IsHidden =
                        ((config.ImmersionOption == 2 && !Game1.player.hasTalkedToFriendToday(npc.name))
                        || (config.ImmersionOption == 3 && Game1.player.hasTalkedToFriendToday(npc.name))
                        || (config.OnlySameLocation && !sameLocation)
                        || (config.ByHeartLevel && !(Game1.player.getFriendshipHeartLevelForNPC(npc.name) >= config.HeartLevelMin && Game1.player.getFriendshipHeartLevelForNPC(npc.name) <= config.HeartLevelMax)));

                    // NPCs that will be drawn onto the map
                    if (config.ShowHiddenVillagers ? ShowNPC(npc.name) : !npcMarker.IsHidden && ShowNPC(npc.name))
                    {
                        int x = (int)LocationToMap(currentLocation, npc.getTileX(), npc.getTileY()).X - 16;
                        int y = (int)LocationToMap(currentLocation, npc.getTileX(), npc.getTileY()).Y - 15;
                        int width = 32;
                        int height = 30;

                        npcMarker.Location = new Rectangle(x, y, width, height);
                        npcMarker.Marker = npc.sprite.Texture;

                        // Check for daily quests
                        foreach (Quest quest in Game1.player.questLog)
                        {
                            if (quest.accepted && quest.dailyQuest && !quest.completed)
                            {
                                switch (quest.questType)
                                {
                                    case 3:
                                        npcMarker.HasQuest = (((ItemDeliveryQuest)quest).target == npc.name);
                                        break;
                                    case 4:
                                        npcMarker.HasQuest = (((SlayMonsterQuest)quest).target == npc.name);
                                        break;
                                    case 7:
                                        npcMarker.HasQuest = (((FishingQuest)quest).target == npc.name);
                                        break;
                                    case 10:
                                        npcMarker.HasQuest = (((ResourceCollectionQuest)quest).target == npc.name);
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }

                        // Hovered NPCs
                        if (Game1.getMouseX() >= x + 2 && Game1.getMouseX() <= x - 2 + width && Game1.getMouseY() >= y + 2 && Game1.getMouseY() <= y - 2 + height)
                        {
                            if (npcNames.ContainsKey(npc.name) && (!config.ShowHiddenVillagers || npcMarker.IsHidden))
                            {
                                string name = npcNames[npc.name];
                                if (!npcMarker.IsOutdoors)
                                {
                                    name = "^" + name;
                                }
                                hoveredNames.Add(npcNames[npc.name]);
                            }
                        }

                        // Establish draw order, higher number infront
                        // Layers 4 - 7: Outdoor NPCs in order of hidden, hidden w/ quest/birthday, standard, standard w/ quest/birthday
                        // Layers 0 - 3: Indoor NPCs in order of hidden, hidden w/ quest/birthday, standard, standard w/ quest/birthday
                        npcMarker.Layer = npcMarker.IsOutdoors ? 6 : 2;
                        if (npcMarker.IsHidden) { npcMarker.Layer -= 2; }
                        if (npcMarker.HasQuest || npcMarker.IsBirthday) { npcMarker.Layer++; }
                    }
                }
            }

            modMapPage = new MapModMapPage(hoveredNames, npcNames, config.NameTooltipMode);
        }

        // Draw misc
        private void GraphicsEvents_OnPostRenderEvent(object sender, EventArgs e)
        {
            if (!Game1.hasLoadedGame || Game1.player == null) { return; }
            if (DEBUG_MODE)
                ShowDebugInfo();
        }

        // Draw event (when Map Page is opened)
        private void GraphicsEvents_OnPostRenderGuiEvent(object sender, EventArgs e)
        {
            if (!Game1.hasLoadedGame) { return; }
            if (!IsMapOpen()) { return; }

            DrawMapPage();
        }

        // Draw event
        // Subtractions within location vectors are to set the origin to the center of the sprite
        static void DrawMapPage()
        {
            SpriteBatch b = Game1.spriteBatch;
            // Draw map overlay
            modMapPage.DrawMap(b);

            if (config.ShowFarmBuildings)
            {
                float scale = 3;

                // Draw farm buildings
                foreach (Building building in Game1.getFarm().buildings)
                {
                    if (building.baseNameOfIndoors == null)
                    {
                        continue;
                    }

                    Vector2 locVector = MapModMain.LocationToMap("Farm", building.tileX, building.tileY);
                    if (building.baseNameOfIndoors.Equals("Shed"))
                    {
                        b.Draw(buildings, locVector, new Rectangle?(new Rectangle(0, 0, 5, 7)), Color.White, 0f, Vector2.Zero, 3f, SpriteEffects.None, 1f);
                    }
                    else if (building.baseNameOfIndoors.Equals("Coop"))
                    {
                        b.Draw(buildings, locVector, new Rectangle?(new Rectangle(5, 0, 5, 7)), Color.White, 0f, Vector2.Zero, 3f, SpriteEffects.None, 1f);
                    }
                    else if (building.baseNameOfIndoors.Equals("Barn"))
                    {
                        b.Draw(buildings, new Vector2(locVector.X, locVector.Y + scale), new Rectangle?(new Rectangle(10, 0, 6, 7)), Color.White, 0f, Vector2.Zero, 3f, SpriteEffects.None, 1f);
                    }
                    else if (building.baseNameOfIndoors.Equals("SlimeHutch"))
                    {
                        b.Draw(buildings, locVector, new Rectangle?(new Rectangle(16, 0, 7, 7)), Color.White, 0f, Vector2.Zero, 3f, SpriteEffects.None, 1f);
                    }
                }

                // Greenhouse unlocked after pantry bundles completed
                if (((CommunityCenter)Game1.getLocationFromName("CommunityCenter")).areasComplete[CommunityCenter.AREA_Pantry])
                {
                    Vector2 locVector = MapModMain.LocationToMap("Greenhouse");
                    b.Draw(buildings, new Vector2((int)(locVector.X - 5/2 * scale), (int)(locVector.Y - 7/2 * scale)), new Rectangle?(new Rectangle(23, 0, 5, 7)), Color.White, 0f, Vector2.Zero, 3f, SpriteEffects.None, 1f);
                }
            }

            // Traveling Merchant
            if (config.ShowTravelingMerchant && (Game1.dayOfMonth == 5 || Game1.dayOfMonth == 7 || Game1.dayOfMonth == 12 || Game1.dayOfMonth == 14 || Game1.dayOfMonth == 19 || Game1.dayOfMonth == 21 || Game1.dayOfMonth == 26 || Game1.dayOfMonth == 28))
            {
                Vector2 merchantLoc = LocationToMap("Forest", 27, 11);
                b.Draw(Game1.mouseCursors, new Vector2(merchantLoc.X - 16, merchantLoc.Y - 15), new Rectangle?(new Rectangle(191, 1410, 22, 21)), Color.White, 0f, Vector2.Zero, 1.3f, SpriteEffects.None, 1f);
            }

            // NPCs
            // Sort by drawing order
            var sortedMarkers = npcMarkers.ToList();
            sortedMarkers.Sort((x, y) => x.Layer.CompareTo(y.Layer));

            foreach (NPCMarker npcMarker in sortedMarkers)
            {
                if (npcMarker.Location == Rectangle.Empty) { continue; }

                // Tint/dim hidden markers
                if (npcMarker.IsHidden)
                {
                    b.Draw(npcMarker.Marker, npcMarker.Location, new Rectangle?(new Rectangle(0, markerCrop[npcMarker.Name], 16, 15)), Color.DimGray * 0.7f);
                    if (npcMarker.IsBirthday)
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
                    b.Draw(npcMarker.Marker, npcMarker.Location, new Rectangle?(new Rectangle(0, markerCrop[npcMarker.Name], 16, 15)), Color.White);
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

            Vector2 playerLoc = MapModMain.LocationToMap(Game1.player.currentLocation.name, Game1.player.getTileX(), Game1.player.getTileY(), true);
            Game1.player.FarmerRenderer.drawMiniPortrat(b, new Vector2(playerLoc.X - 16, playerLoc.Y - 15), 0.00011f, 2f, 1, Game1.player); 

            // Location and name tooltips
            modMapPage.draw(b);

            // Cursor
            if (!Game1.options.hardwareCursor)
            {
                b.Draw(Game1.mouseCursors, new Vector2((float)Game1.getOldMouseX(), (float)Game1.getOldMouseY()), new Rectangle?(Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, (Game1.options.gamepadControls ? 44 : 0), 16, 16)), Color.White, 0f, Vector2.Zero, ((float)Game1.pixelZoom + Game1.dialogueButtonScale / 150f), SpriteEffects.None, 1f);
            }
            
        }

        // Show debug info in top left corner
        private static void ShowDebugInfo()
        {
            if (Game1.player.currentLocation == null) { return; }

            // Black backgronud for legible text
            Game1.spriteBatch.Draw(Game1.shadowTexture, new Rectangle(0, 0, 425, 160), new Rectangle(6, 3, 1, 1), Color.Black);

            // Show map location and tile positions
            DrawText(Game1.player.currentLocation.name + " (" + Game1.player.getTileX() + ", " + Game1.player.getTileY() + ")", new Vector2(Game1.tileSize / 4, Game1.tileSize / 4));

            var currMenu = Game1.activeClickableMenu is GameMenu ? (GameMenu)Game1.activeClickableMenu : null;

            // Show lower & upper bound tiles used for calculations 
            if (currMenu != null && currMenu.currentTab == GameMenu.mapTab)
            {
                DrawText("Lower bound: (" + MapModMain._tileLower.X + ", " + MapModMain._tileLower.Y + ")", new Vector2(Game1.tileSize / 4, Game1.tileSize * 3 / 4 + 8));
                DrawText("Upper bound: (" + MapModMain._tileUpper.X + ", " + MapModMain._tileUpper.Y + ")", new Vector2(Game1.tileSize / 4, Game1.tileSize * 5 / 4 + 8 * 2));
            }
            else
            {
                DrawText("Lower bound: (" + MapModMain._tileLower.X + ", " + MapModMain._tileLower.Y + ")", new Vector2(Game1.tileSize / 4, Game1.tileSize * 3 / 4 + 8), Color.DimGray);
                DrawText("Upper bound: (" + MapModMain._tileUpper.X + ", " + MapModMain._tileUpper.Y + ")", new Vector2(Game1.tileSize / 4, Game1.tileSize * 5 / 4 + 8 * 2), Color.DimGray);
            }
        }

        // Draw outlined text
        private static void DrawText(string text, Vector2 pos, Color? color = null)
        {
            Game1.spriteBatch.DrawString(Game1.dialogueFont, text, pos + new Vector2(1, 1), Color.Black);
            Game1.spriteBatch.DrawString(Game1.dialogueFont, text, pos + new Vector2(-1, 1), Color.Black);
            Game1.spriteBatch.DrawString(Game1.dialogueFont, text, pos + new Vector2(1, -1), Color.Black);
            Game1.spriteBatch.DrawString(Game1.dialogueFont, text, pos + new Vector2(-1, -1), Color.Black);
            Game1.spriteBatch.DrawString(Game1.dialogueFont, text, pos, color ?? Color.White);
        }

        // Config show/hide 
        private static bool ShowNPC(string npc)
        {
            if (npc.Equals("Abigail")) { return config.ShowAbigail; }
            if (npc.Equals("Alex")) { return config.ShowAlex; }
            if (npc.Equals("Caroline")) { return config.ShowCaroline; }
            if (npc.Equals("Clint")) { return config.ShowClint; }
            if (npc.Equals("Demetrius")) { return config.ShowDemetrius; }
            if (npc.Equals("Elliott")) { return config.ShowElliott; }
            if (npc.Equals("Emily")) { return config.ShowEmily; }
            if (npc.Equals("Evelyn")) { return config.ShowEvelyn; }
            if (npc.Equals("George")) { return config.ShowGeorge; }
            if (npc.Equals("Gus")) { return config.ShowGus; }
            if (npc.Equals("Haley")) { return config.ShowHaley; }
            if (npc.Equals("Harvey")) { return config.ShowHarvey; }
            if (npc.Equals("Jas")) { return config.ShowJas; }
            if (npc.Equals("Jodi")) { return config.ShowJodi; }
            if (npc.Equals("Kent")) { return config.ShowKent; }
            if (npc.Equals("Leah")) { return config.ShowLeah; }
            if (npc.Equals("Lewis")) { return config.ShowLewis; }
            if (npc.Equals("Linus")) { return config.ShowLinus; }
            if (npc.Equals("Marnie")) { return config.ShowMarnie; }
            if (npc.Equals("Maru")) { return config.ShowMaru; }
            if (npc.Equals("Pam")) { return config.ShowPam; }
            if (npc.Equals("Penny")) { return config.ShowPenny; }
            if (npc.Equals("Pierre")) { return config.ShowPierre; }
            if (npc.Equals("Robin")) { return config.ShowRobin; }
            if (npc.Equals("Sam")) { return config.ShowSam; }
            if (npc.Equals("Sebastian")) { return config.ShowSebastian; }
            if (npc.Equals("Shane")) { return config.ShowShane; }
            if (npc.Equals("Vincent")) { return config.ShowVincent; }
            if (npc.Equals("Willy")) { return config.ShowWilly; }
            if (npc.Equals("Sandy")) { return config.ShowSandy && showSecondaryNPCs[0]; }
            if (npc.Equals("Marlon")) { return config.ShowMarlon && showSecondaryNPCs[1]; }
            if (npc.Equals("Wizard")) { return config.ShowWizard && showSecondaryNPCs[2]; }
            foreach (KeyValuePair<string, Dictionary<string, int>> customNPC in customNPCs)
            {
                if (customNPC.Key.Equals(npc))
                {
                    switch (customNPC.Value["id"])
                    {
                        case 1:
                            return config.ShowCustomNPC1;
                        case 2:
                            return config.ShowCustomNPC2;
                        case 3:
                            return config.ShowCustomNPC3;
                        case 4:
                            return config.ShowCustomNPC4;
                        case 5:
                            return config.ShowCustomNPC5;
                    }
                }
            }
            return true;
        }

        // Only checks against existing villager names
        public static bool IsCustomNPC(string npc)
        {
            return (!(
                npc.Equals("Abigail") ||
                npc.Equals("Alex") ||
                npc.Equals("Caroline") ||
                npc.Equals("Clint") ||
                npc.Equals("Demetrius") ||
                npc.Equals("Elliott") ||
                npc.Equals("Emily") ||
                npc.Equals("Evelyn") ||
                npc.Equals("George") ||
                npc.Equals("Gus") ||
                npc.Equals("Haley") ||
                npc.Equals("Harvey") ||
                npc.Equals("Jas") ||
                npc.Equals("Jodi") ||
                npc.Equals("Kent") ||
                npc.Equals("Leah") ||
                npc.Equals("Lewis") ||
                npc.Equals("Linus") ||
                npc.Equals("Marnie") ||
                npc.Equals("Maru") ||
                npc.Equals("Pam") ||
                npc.Equals("Penny") ||
                npc.Equals("Pierre") ||
                npc.Equals("Robin") ||
                npc.Equals("Sam") ||
                npc.Equals("Sebastian") ||
                npc.Equals("Shane") ||
                npc.Equals("Vincent") ||
                npc.Equals("Willy") ||
                npc.Equals("Sandy") ||
                npc.Equals("Marlon") ||
                npc.Equals("Wizard"))
            );
        }
    }


    // Class for NPC markers
    internal class NPCMarker
    {
        public string Name { get; set; } = "";
        public Texture2D Marker { get; set; } = null;
        public Rectangle Location { get; set; } = new Rectangle();
        public bool IsBirthday { get; set; } = false;
        public bool HasQuest { get; set; } = false;
        public bool IsOutdoors { get; set; } = false;
        public bool IsHidden { get; set; } = false;
        public int Layer { get; set; } = 0;
    }
}
