/*
NPC Map Locations Mod by Bouhm.
Shows NPC locations real-time on the map.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Menus;
using StardewValley.Quests;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley.Locations;

namespace NPCMapLocations
{
    public class MapModMain : Mod
    {
        public static string current;
        public static IModHelper modHelper;
        public static IMonitor monitor;
        public static MapModConfig config;
        public static string saveName;
        public static int customNpcId = 0;
        public static int menuOpen = 0;
        public static Dictionary<string, int> markerCrop; // NPC head crops, top left corner (0, y), width = 16, height = 15 
        public static Texture2D map;
        public static Texture2D buildings;
        private static int mapX;
        private static int mapY;
        private static Dictionary<string, Dictionary<string, int>> customNPCs;
        private static Dictionary<string, NPCMarker> npcMarkers = new Dictionary<string, NPCMarker>();
        private static Dictionary<string, MapVectors[]> mapVectors;
        private static Dictionary<string, string> indoorLocations;
        private static MapModMapPage modMapPage;
        private static string hoveredNPCNames;
        private static HashSet<string> birthdayNPCs;
        private static HashSet<string> questNPCs;
        private static HashSet<string> hiddenNPCs;
        private static Dictionary<string, string> npcNames = new Dictionary<string, string>();
        private bool[] showExtras = new Boolean[4];
        private bool loadComplete = false;
        private bool initialized = false;

        // For debug info
        private const bool DEBUG_MODE = false;
        private static Vector2 _tileLower; 
        private static Vector2 _tileUpper; 
        private static string alertFlag; 

        public override void Entry(IModHelper helper)
        {
            modHelper = helper;
            monitor = this.Monitor;
            SaveEvents.AfterLoad += SaveEvents_AfterLoad;
            GameEvents.UpdateTick += GameEvents_UpdateTick;
            GraphicsEvents.OnPostRenderEvent += GraphicsEvents_OnPostRenderEvent;
            GraphicsEvents.OnPostRenderGuiEvent += GraphicsEvents_OnPostRenderGuiEvent;
            InputEvents.ButtonPressed += InputEvents_ButtonPressed;
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
            current = ModManifest.Version.ToString();
            saveName = Constants.SaveFolderName;
            config = modHelper.ReadJsonFile<MapModConfig>($"config/{saveName}.json") ?? new MapModConfig();
            markerCrop = MapModConstants.markerCrop;
            mapVectors = MapModConstants.mapVectors;
            indoorLocations = MapModConstants.indoorLocations;
            customNPCs = config.customNPCs;
            MapModMain.map = MapModMain.modHelper.Content.Load<Texture2D>(@"content/map", ContentSource.ModFolder); // Load modified map page
            MapModMain.buildings = MapModMain.modHelper.Content.Load<Texture2D>(@"content/buildings", ContentSource.ModFolder);
            loadComplete = true;
        }

        private void HandleInput(GameMenu menu, SButton input)
        {
            if (menu.currentTab != GameMenu.mapTab) { return; }
            if (input.ToString().Equals(config.menuKey) || input is SButton.ControllerB)
            {
                openModMenu();
            }
            else if (input.ToString().Equals(config.tooltipKey) || input is SButton.ControllerY)
            {
                changeTooltipConfig();
            }
        }

        private void openModMenu()
        {
            Game1.activeClickableMenu = new MapModMenu(Game1.viewport.Width / 2 - (950 + IClickableMenu.borderWidth * 2) / 2, Game1.viewport.Height / 2 - (750 + IClickableMenu.borderWidth * 2) / 2, 900 + IClickableMenu.borderWidth * 2, 650 + IClickableMenu.borderWidth * 2, showExtras, customNPCs, npcNames);
            menuOpen = 1;
        }

        private void changeTooltipConfig()
        {
            if (++config.nameTooltipMode > 3)
            {
                config.nameTooltipMode = 1;
            }
            modHelper.WriteJsonFile($"config/{saveName}.json", config);
        }

        private void LoadCustomMods()
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
            config.customNPCs = customNPCs;
            modHelper.WriteJsonFile($"config/{saveName}.json", config);
            initialized = true;
        }

        private int LoadCustomNPCs(NPC npc, int initialize, int id)
        {
            if (initialize == 0)
            {
                int idx = 1;
                foreach (KeyValuePair<string, Dictionary<string, int>> customNPC in customNPCs)
                {
                    // isInGame = 0;
                    if (npc.name.Equals(customNPC.Key))
                    {
                        // isInGame = 1;
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
            if (config.villagerCrop != null && config.villagerCrop.Count > 0)
            {
                foreach (KeyValuePair<string, int> villager in config.villagerCrop)
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
            string[] buildings = { "Coop", "Big Coop", "Deluxe Coop", "Barn", "Big Barn", "Deluxe Barn", "Slime Hutch", "Shed" };
            if (buildings.Contains(location))
            {
                foreach (Building building in Game1.getFarm().buildings)
                {
                    if (building.indoors != null && building.indoors.name.Equals(location))
                    {
                        // Set origin to center
                        tileX = (int)(building.tileX - building.tilesWide/2);
                        tileY = (int)(building.tileY - building.tilesHigh/2);
                        location = "Farm";
                    }
                }
            }

            var locVectors = mapVectors[location];
            Vector2 mapPagePos = Utility.getTopLeftPositionForCenteringOnScreen(300 * Game1.pixelZoom, 180 * Game1.pixelZoom, 0, 0);
            mapX = (int)mapPagePos.X;
            mapY = (int)mapPagePos.Y;
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
                    // Handle exact points
                    if (tileX == vector.tileX && tileY == vector.tileY)
                    {
                        return new Vector2(mapX + vector.x - 16, mapY + vector.y - 15);
                    }
                    else
                    {
                        if (lower != null && upper != null)
                        {
                            // Don't want to exclude points where tile = vector x/y (hence the <= and >=) but avoid cases where both upper/lower are equal
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

                /*
                // For use with vanilla map. Requires a LOT of points in mapVectors for some accuracy (from past experience, not worth).
                // With modified accurate map, this is no longer necessary since lower x,y < upper x,y is always true for two closest points.
                int tileXMin = Math.Min(lower.tileX, upper.tileX);
                int tileXMax = Math.Max(lower.tileX, upper.tileX);
                int tileYMin = Math.Min(lower.tileY, upper.tileY);
                int tileYMax = Math.Max(lower.tileY, upper.tileY);
                int xMin = Math.Min(lower.x, upper.x);
                int xMax = Math.Max(lower.x, upper.x);
                int yMin = Math.Min(lower.y, upper.y);
                int yMax = Math.Max(lower.y, upper.y);

                x = (int)(xMin + (double)(tileX - tileXMin) / (double)(tileXMax - tileXMin) * (xMax - xMin));
                y = (int)(yMin + (double)(tileY - tileYMin) / (double)(tileYMax - tileYMin) * (yMax - yMin));
                */

                x = (int)(lower.x + (double)(tileX - lower.tileX) / (double)(upper.tileX - lower.tileX) * (upper.x - lower.x));
                y = (int)(lower.y + (double)(tileY - lower.tileY) / (double)(upper.tileY - lower.tileY) * (upper.y - lower.y));

                if (DEBUG_MODE && isFarmer)
                {
                    MapModMain._tileUpper = new Vector2(upper.tileX, upper.tileY);
                    MapModMain._tileLower = new Vector2(lower.tileX, lower.tileY);
                }
            }
            return new Vector2(mapX + x, mapY + y);
        }

        private void GameEvents_UpdateTick(object sender, EventArgs e)
        {
            if (!Game1.hasLoadedGame) { return; }
            if (!(Game1.activeClickableMenu is GameMenu)) { return; }

            updateNPCMarkers((GameMenu)Game1.activeClickableMenu);
        }

        private void updateNPCMarkers(GameMenu menu)
        {
            if (menu.currentTab != GameMenu.mapTab) { return; }
            if (loadComplete && !initialized)
            {
                LoadCustomMods();
            }

            List<string> hoveredList = new List<String>();
            birthdayNPCs = new HashSet<string>();
            questNPCs = new HashSet<string>();
            hiddenNPCs = new HashSet<string>();
            hoveredNPCNames = "";

            foreach (NPC npc in Utility.getAllCharacters())
            {
                if (!npc.isVillager()) { continue; }

                MapVectors[] npcLocation;
                string currentLocation;

                // Handle null locations at beginning of new game
                if (npc.currentLocation == null)
                {
                    MapModConstants.startingLocations.TryGetValue(npc.name, out currentLocation);
                }
                else
                {
                    currentLocation = npc.currentLocation.name;
                }

                mapVectors.TryGetValue(currentLocation, out npcLocation);
                if (npcLocation == null)
                {
                    continue;
                }

                if (npc.Schedule != null || npc.isMarried() || npc.name.Equals("Sandy") || npc.name.Equals("Marlon") || npc.name.Equals("Wizard"))
                {
                    bool sameLocation = false;
                    showExtras[0] = Game1.player.mailReceived.Contains("ccVault");
                    showExtras[1] = Game1.stats.DaysPlayed >= 5u;
                    showExtras[2] = Game1.stats.DaysPlayed >= 5u;
                    showExtras[3] = Game1.year >= 2;

                    if (config.onlySameLocation)
                    {
                        indoorLocations.TryGetValue(npc.currentLocation.name, out string indoorLocationNPC);
                        indoorLocations.TryGetValue(Game1.player.currentLocation.name, out string indoorLocationPlayer);
                        if (indoorLocationPlayer == null || indoorLocationNPC == null)
                        {
                            sameLocation = false;
                        }
                        else
                        {
                            sameLocation = indoorLocationNPC.Equals(indoorLocationPlayer);
                        }
                    }

                    if ((config.immersionOption == 2 && !Game1.player.hasTalkedToFriendToday(npc.name)) ||
                        (config.immersionOption == 3 && Game1.player.hasTalkedToFriendToday(npc.name)) ||
                        (config.onlySameLocation && !sameLocation) ||
                        (config.byHeartLevel && !(Game1.player.getFriendshipHeartLevelForNPC(npc.name) >= config.heartLevelMin && Game1.player.getFriendshipHeartLevelForNPC(npc.name) <= config.heartLevelMax)))
                    {
                        hiddenNPCs.Add(npc.name);
                    }


                    if (config.showHiddenVillagers ? ShowNPC(npc.name, showExtras) : (!hiddenNPCs.Contains(npc.name) && ShowNPC(npc.name, showExtras)))
                    {
                        int x = (int)LocationToMap(currentLocation, npc.getTileX(), npc.getTileY()).X - 16;
                        int y = (int)LocationToMap(currentLocation, npc.getTileX(), npc.getTileY()).Y - 15;
                        int width = 32;
                        int height = 30;
                        
                        if (npcMarkers.ContainsKey(npc.name))
                        {
                            npcMarkers[npc.name].location = new Rectangle(x, y, width, height);
                        }
                        else
                        {
                            npcMarkers.Add(npc.name, new NPCMarker(npc.sprite.Texture, new Rectangle(x, y, width, height)));
                        }
                        
                        if (Game1.getMouseX() >= x + 2 && Game1.getMouseX() <= x - 2 + width && Game1.getMouseY() >= y + 2 && Game1.getMouseY() <= y - 2 + height)
                        {
                            if (npcNames.ContainsKey(npc.name))
                            {
                                hoveredList.Add(npcNames[npc.name]);
                            }
                        }

                        if (config.markQuests)
                        {
                            if (npc.isBirthday(Game1.currentSeason, Game1.dayOfMonth))
                            {
                                if (Game1.player.friendships.ContainsKey(npc.name) && Game1.player.friendships[npc.name][3] != 1)
                                {
                                    birthdayNPCs.Add(npc.name);
                                }
                                else
                                {
                                    birthdayNPCs.Add(npc.name);
                                }
                            }
                            foreach (Quest quest in Game1.player.questLog)
                            {
                                if (quest.accepted && quest.dailyQuest && !quest.completed)
                                {
                                    if (quest.questType == 3)
                                    {
                                        var current = (ItemDeliveryQuest)quest;
                                        if (current.target == npc.name)
                                        {
                                            questNPCs.Add(npc.name);
                                        }
                                    }
                                    else if (quest.questType == 4)
                                    {
                                        var current = (SlayMonsterQuest)quest;
                                        if (current.target == npc.name)
                                        {
                                            questNPCs.Add(npc.name);
                                        }
                                    }
                                    else if (quest.questType == 7)
                                    {
                                        var current = (FishingQuest)quest;
                                        if (current.target == npc.name)
                                        {
                                            questNPCs.Add(npc.name);
                                        }
                                    }
                                    else if (quest.questType == 10)
                                    {
                                        var current = (ResourceCollectionQuest)quest;
                                        if (current.target == npc.name)
                                        {
                                            questNPCs.Add(npc.name);
                                        }
                                    }
                                }
                            }
                        }
                        // Draw order
                        if (hiddenNPCs.Contains(npc.name))
                        {
                            npcMarkers[npc.name].layer = 4;
                            if (questNPCs.Contains(npc.name) || (birthdayNPCs.Contains(npc.name)))
                            {
                                npcMarkers[npc.name].layer = 3;
                            }
                        }
                        else
                        {
                            npcMarkers[npc.name].layer = 2;
                            if (questNPCs.Contains(npc.name) || (birthdayNPCs.Contains(npc.name)))
                            {
                                npcMarkers[npc.name].layer = 1;
                            }
                        }
                    }
                    else
                    {
                        npcMarkers.Remove(npc.name);
                    }
                }
            }

            if (hoveredList != null && hoveredList.Count > 0)
            {
                hoveredNPCNames = hoveredList[0];
                for (int i = 1; i < hoveredList.Count; i++)
                {
                    var lines = hoveredNPCNames.Split('\n');
                    if ((int)Game1.smallFont.MeasureString(lines[lines.Length - 1] + ", " + hoveredList[i]).X > (int)Game1.smallFont.MeasureString("Home of Robin, Demetrius, Sebastian & Maru").X) // Longest string
                    {
                        hoveredNPCNames += ", " + Environment.NewLine;
                        hoveredNPCNames += hoveredList[i];
                    }
                    else
                    {
                        hoveredNPCNames += ", " + hoveredList[i];
                    }
                };
            }
            modMapPage = new MapModMapPage(hoveredNPCNames, npcNames, config.nameTooltipMode, menu);
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
            if (!(Game1.activeClickableMenu is GameMenu)) { return; }

            DrawMapPage((GameMenu)Game1.activeClickableMenu);
        }

        // Draw event
        // Subtractions within location vectors are to set the origin to the center of the sprite
        static void DrawMapPage(GameMenu menu)
        {
            if (menu.currentTab == GameMenu.mapTab)
            {
                SpriteBatch b = Game1.spriteBatch;
                // Draw map overlay
                modMapPage.DrawMap(b);

                if (config.showFarmBuildings)
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
                            b.Draw(buildings, new Vector2(locVector.X, locVector.Y + 1), new Rectangle?(new Rectangle(10, 0, 6, 7)), Color.White, 0f, Vector2.Zero, 3f, SpriteEffects.None, 1f);
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

                // Player
                
                Vector2 playerLoc = MapModMain.LocationToMap(Game1.player.currentLocation.name, Game1.player.getTileX(), Game1.player.getTileY(), true);
                Game1.player.FarmerRenderer.drawMiniPortrat(b, new Vector2(playerLoc.X - 16, playerLoc.Y - 15), 0.00011f, 2f, 1, Game1.player);
                

                // NPC markers and icons
                if (config.showTravelingMerchant && (Game1.dayOfMonth == 5 || Game1.dayOfMonth == 7 || Game1.dayOfMonth == 12 || Game1.dayOfMonth == 14 || Game1.dayOfMonth == 19 || Game1.dayOfMonth == 21 || Game1.dayOfMonth == 26 || Game1.dayOfMonth == 28))
                {
                    Vector2 merchantLoc = LocationToMap("Forest", 27, 11);
                    b.Draw(Game1.mouseCursors, new Vector2(merchantLoc.X - 16, merchantLoc.Y - 15), new Rectangle?(new Rectangle(191, 1410, 22, 21)), Color.White, 0f, Vector2.Zero, 1.3f, SpriteEffects.None, 1f);
                }
                var sortedMarkers = npcMarkers.ToList();
                sortedMarkers.Sort((y, x) => x.Value.layer.CompareTo(y.Value.layer));

                foreach (KeyValuePair<string, NPCMarker> npc in sortedMarkers)
                {
                    if (hiddenNPCs.Contains(npc.Key))
                    {
                        b.Draw(npc.Value.marker, npc.Value.location, new Rectangle?(new Rectangle(0, markerCrop[npc.Key], 16, 15)), Color.DimGray * 0.8f);
                        if (birthdayNPCs.Contains(npc.Key))
                        {
                            b.Draw(Game1.mouseCursors, new Vector2(npc.Value.location.X + 20, npc.Value.location.Y), new Rectangle?(new Rectangle(147, 412, 10, 11)), Color.DimGray * 0.8f, 0f, Vector2.Zero, 1.8f, SpriteEffects.None, 0f);
                        }
                        if (questNPCs.Contains(npc.Key))
                        {
                            b.Draw(Game1.mouseCursors, new Vector2(npc.Value.location.X + 22, npc.Value.location.Y - 3), new Rectangle?(new Rectangle(403, 496, 5, 14)), Color.DimGray * 0.8f, 0f, Vector2.Zero, 1.8f, SpriteEffects.None, 0f);
                        }
                    }
                    else
                    {
                        b.Draw(npc.Value.marker, npc.Value.location, new Rectangle?(new Rectangle(0, markerCrop[npc.Key], 16, 15)), Color.White);
                        if (birthdayNPCs.Contains(npc.Key))
                        {
                            b.Draw(Game1.mouseCursors, new Vector2(npc.Value.location.X + 20, npc.Value.location.Y), new Rectangle?(new Rectangle(147, 412, 10, 11)), Color.White, 0f, Vector2.Zero, 1.8f, SpriteEffects.None, 0f);
                        }
                        if (questNPCs.Contains(npc.Key))
                        {
                            b.Draw(Game1.mouseCursors, new Vector2(npc.Value.location.X + 22, npc.Value.location.Y - 3), new Rectangle?(new Rectangle(403, 496, 5, 14)), Color.White, 0f, Vector2.Zero, 1.8f, SpriteEffects.None, 0f);
                        }
                    }
                }

                // Location and name tooltips
                modMapPage.draw(b);

                // Cursor
                if (!Game1.options.hardwareCursor)
                {
                    b.Draw(Game1.mouseCursors, new Vector2((float)Game1.getOldMouseX(), (float)Game1.getOldMouseY()), new Rectangle?(Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, (Game1.options.gamepadControls ? 44 : 0), 16, 16)), Color.White, 0f, Vector2.Zero, ((float)Game1.pixelZoom + Game1.dialogueButtonScale / 150f), SpriteEffects.None, 1f);
                }
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
        private static bool ShowNPC(string npc, bool[] showExtras)
        {
            if (npc.Equals("Abigail")) { return config.showAbigail; }
            if (npc.Equals("Alex")) { return config.showAlex; }
            if (npc.Equals("Caroline")) { return config.showCaroline; }
            if (npc.Equals("Clint")) { return config.showClint; }
            if (npc.Equals("Demetrius")) { return config.showDemetrius; }
            if (npc.Equals("Elliott")) { return config.showElliott; }
            if (npc.Equals("Emily")) { return config.showEmily; }
            if (npc.Equals("Evelyn")) { return config.showEvelyn; }
            if (npc.Equals("George")) { return config.showGeorge; }
            if (npc.Equals("Gus")) { return config.showGus; }
            if (npc.Equals("Haley")) { return config.showHaley; }
            if (npc.Equals("Harvey")) { return config.showHarvey; }
            if (npc.Equals("Jas")) { return config.showJas; }
            if (npc.Equals("Jodi")) { return config.showJodi; }
            if (npc.Equals("Kent")) { return config.showKent; }
            if (npc.Equals("Leah")) { return config.showLeah; }
            if (npc.Equals("Lewis")) { return config.showLewis; }
            if (npc.Equals("Linus")) { return config.showLinus; }
            if (npc.Equals("Marnie")) { return config.showMarnie; }
            if (npc.Equals("Maru")) { return config.showMaru; }
            if (npc.Equals("Pam")) { return config.showPam; }
            if (npc.Equals("Penny")) { return config.showPenny; }
            if (npc.Equals("Pierre")) { return config.showPierre; }
            if (npc.Equals("Robin")) { return config.showRobin; }
            if (npc.Equals("Sam")) { return config.showSam; }
            if (npc.Equals("Sebastian")) { return config.showSebastian; }
            if (npc.Equals("Shane")) { return config.showShane; }
            if (npc.Equals("Vincent")) { return config.showVincent; }
            if (npc.Equals("Willy")) { return config.showWilly; }
            if (npc.Equals("Sandy")) { return config.showSandy && showExtras[0]; }
            if (npc.Equals("Marlon")) { return config.showMarlon && showExtras[1]; }
            if (npc.Equals("Wizard")) { return config.showWizard && showExtras[2]; }
            foreach (KeyValuePair<string, Dictionary<string, int>> customNPC in customNPCs)
            {
                if (customNPC.Key.Equals(npc))
                {
                    switch (customNPC.Value["id"])
                    {
                        case 1:
                            return config.showCustomNPC1;
                        case 2:
                            return config.showCustomNPC2;
                        case 3:
                            return config.showCustomNPC3;
                        case 4:
                            return config.showCustomNPC4;
                        case 5:
                            return config.showCustomNPC5;
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
    public class NPCMarker
    {
        public Texture2D marker;
        public Rectangle location;
        public int layer;

        public NPCMarker(Texture2D marker, Rectangle location)
        {
            this.marker = marker;
            this.location = location;
            this.layer = 4;
        }
    }
}