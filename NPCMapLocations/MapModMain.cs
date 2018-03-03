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

namespace NPCMapLocations
{
    public class MapModMain : Mod
    {
        public static string current;
        public static IModHelper modHelper;
        private static IMonitor monitor;
        public static MapModConfig config;
        public static int customNpcId = 0;
        public static int menuOpen = 0;
        private static Dictionary<string, Dictionary<string, int>> customNPCs;
        private static Dictionary<string, NPCMarker> npcMarkers = new Dictionary<string, NPCMarker>();
        public static Dictionary<string, int> spriteCrop; // NPC head crops, top left corner (0, y), width = 16, height = 15 
        private static Dictionary<string, string> startingLocations;
        private static Dictionary<string, MapVectors[]> mapVectors;
        private static Dictionary<string, string> indoorLocations;
        private static MapPageTooltips toolTips;
        private static string hoveredNPCNames;
        private static HashSet<string> birthdayNPCs;
        private static HashSet<string> questNPCs;
        private static HashSet<string> hiddenNPCs;
        private static Dictionary<string, string> npcNames = new Dictionary<string, string>();
        private bool[] showExtras = new Boolean[4];
        private bool loadComplete = false;
        private bool initialized = false;
        private static Vector2 _tileLower; // For debug info
        private static Vector2 _tileUpper; // For debug info
        private static string alertFlag;

        private const bool DEBUG_MODE = true; // For showing debug info. Set false for release.

        public override void Entry(IModHelper helper)
        {
            modHelper = helper;
            monitor = this.Monitor;
            config = helper.ReadConfig<MapModConfig>();
            SaveEvents.AfterLoad += SaveEvents_AfterLoad;
            GameEvents.UpdateTick += GameEvents_UpdateTick;
            GraphicsEvents.OnPostRenderEvent += GraphicsEvents_OnPostRenderEvent;
            GraphicsEvents.OnPostRenderGuiEvent += GraphicsEvents_OnPostRenderGuiEvent;
            ControlEvents.KeyPressed += KeyboardInput_KeyDown;
        }

        private void SaveEvents_AfterLoad(object sender, EventArgs e)
        {
            current = ModManifest.Version.ToString();
            spriteCrop = MapModConstants.spriteCrop;
            startingLocations = MapModConstants.startingLocations;
            mapVectors = MapModConstants.mapVectors;
            indoorLocations = MapModConstants.indoorLocations;
            customNPCs = config.customNPCs;
            loadComplete = true;
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
            modHelper.WriteConfig(config);
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
                    if (!spriteCrop.ContainsKey(customNPC.Key))
                    {
                        spriteCrop.Add(customNPC.Key, customNPC.Value["crop"]);
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
                        spriteCrop.Add(npc.name, 0);
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
                        spriteCrop[npc.name] = villager.Value;
                    }
                }
            }
        }

        // Open menu key
        private void KeyboardInput_KeyDown(object sender, EventArgsKeyPressed e)
        {
            if (Game1.hasLoadedGame && Game1.activeClickableMenu is GameMenu)
            {
                ChangeKey(e.KeyPressed.ToString(), (GameMenu)Game1.activeClickableMenu);
            }
        }

        private void ChangeKey(string key, GameMenu menu)
        {
            if (menu.currentTab != 3) { return; }
            if (key.Equals(config.menuKey))
            {
                Game1.activeClickableMenu = new MapModMenu(Game1.viewport.Width / 2 - (950 + IClickableMenu.borderWidth * 2) / 2, Game1.viewport.Height / 2 - (750 + IClickableMenu.borderWidth * 2) / 2, 900 + IClickableMenu.borderWidth * 2, 650 + IClickableMenu.borderWidth * 2, showExtras, customNPCs, npcNames);
                menuOpen = 1;
            }
        }

        // Calculated from mapping of game tile positions to pixel coordinates of the map in MapModConstants. 
        public static Vector2 LocationToMap(string location, int tileX, int tileY)
        {
            if (location == null)
            {
                return new Vector2(-5000, -5000);
            }

            var locVectors = mapVectors[location];
            var playerLoc = new Vector2(Game1.player.getTileX(), Game1.player.getTileY());
            Vector2 mapPagePos = Utility.getTopLeftPositionForCenteringOnScreen(300 * Game1.pixelZoom, 180 * Game1.pixelZoom, 0, 0);
            int mapX = (int)mapPagePos.X;
            int mapY = (int)mapPagePos.Y;
            int x = 0;
            int y = 0;

            // Get tile location of farm buildings in farm
            string[] farmBuildings = { "Coop", "Big Coop", "Deluxe Coop", "Barn", "Big Barn", "Deluxe Barn", "Slime Hutch", "Shed" };
            if (farmBuildings.Contains(location))
            {
                foreach (Building building in Game1.getFarm().buildings)
                {
                    if (building.indoors != null && building.indoors.name.Equals(location))
                    {
                        tileX = building.tileX;
                        tileY = building.tileY;
                        location = "Farm";
                    }
                }
            }

            // Handle indoor locations
            if (locVectors.Count() == 1)
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

                // Create bounding rectangle from two pre-defined points (lower & upper bound) and calculate map scale for that area
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
                if (lower == null)
                {
                    if (alertFlag != "NullBound:" + playerLoc)
                    { 
                        MapModMain.monitor.Log("Null lower bound - No vector less than (" + playerLoc.X + ", " + playerLoc.Y + ") to calculate location.", LogLevel.Alert);
                        alertFlag = "NullBound:" + playerLoc;
                    }

                    lower = upper == vectors.First() ? vectors.Skip(1).First() : vectors.First();
                }
                if (upper == null)
                {
                    if (alertFlag != "NullBound:" + playerLoc)
                    {
                        MapModMain.monitor.Log("Null upper bound - No vector greater than (" + playerLoc.X + ", " + playerLoc.Y + ") to calculate location.", LogLevel.Alert);
                        alertFlag = "NullBound:" + playerLoc;
                    }

                    upper = lower == vectors.First() ? vectors.Skip(1).First() : vectors.First();
                }

                // Quick maffs
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

                // For debug info
                MapModMain._tileUpper = new Vector2(upper.tileX, upper.tileY);
                MapModMain._tileLower = new Vector2(lower.tileX, lower.tileY);
            }
            return new Vector2(mapX + x - 16, mapY + y - 15);
        }

        private void GameEvents_UpdateTick(object sender, EventArgs e)
        {
            if (!Game1.hasLoadedGame) { return; }
            if (loadComplete && !initialized)
            {
                LoadCustomMods();
            }

            if (!(Game1.activeClickableMenu is GameMenu)) { return; }

            List<string> hoveredList = new List<String>();
            birthdayNPCs = new HashSet<string>();
            questNPCs = new HashSet<string>();
            hiddenNPCs = new HashSet<string>();
            hoveredNPCNames = "";

            foreach (NPC npc in Utility.getAllCharacters())
            {
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
                        int x = (int)LocationToMap(npc.currentLocation.name, npc.getTileX(), npc.getTileY()).X;
                        int y = (int)LocationToMap(npc.currentLocation.name, npc.getTileX(), npc.getTileY()).Y;
                        int width = 32;
                        int height = 30;

                        if (npcMarkers.ContainsKey(npc.name))
                        {
                            npcMarkers[npc.name].position = new Rectangle(x, y, width, height);
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
            toolTips = new MapPageTooltips(hoveredNPCNames, npcNames, config.nameTooltipMode);
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
            if (Game1.activeClickableMenu is GameMenu)
            {
                DrawMarkers((GameMenu)Game1.activeClickableMenu);
            }
        }

        // Actual draw event
        static void DrawMarkers(GameMenu menu)
        {
            if (menu.currentTab == GameMenu.mapTab)
            {
                SpriteBatch b = Game1.spriteBatch;
                // Draw map overlay
                toolTips.DrawMap(b);

                // NPC markers and icons
                if (config.showTravelingMerchant && (Game1.dayOfMonth == 5 || Game1.dayOfMonth == 7 || Game1.dayOfMonth == 12 || Game1.dayOfMonth == 14 || Game1.dayOfMonth == 19 || Game1.dayOfMonth == 21 || Game1.dayOfMonth == 26 || Game1.dayOfMonth == 28))
                {
                    b.Draw(Game1.mouseCursors, new Vector2(Game1.activeClickableMenu.xPositionOnScreen + 130, Game1.activeClickableMenu.yPositionOnScreen + 355), new Rectangle?(new Rectangle(191, 1410, 22, 21)), Color.White, 0f, Vector2.Zero, 1.3f, SpriteEffects.None, 1f);
                }
                var sortedMarkers = npcMarkers.ToList();
                sortedMarkers.Sort((y, x) => x.Value.layer.CompareTo(y.Value.layer));
              
                foreach (KeyValuePair<string, NPCMarker> npc in sortedMarkers)
                {
                    if (hiddenNPCs.Contains(npc.Key)) {
                        b.Draw(npc.Value.marker, npc.Value.position, new Rectangle?(new Rectangle(0, spriteCrop[npc.Key], 16, 15)), Color.DimGray * 0.9f);
                        if (birthdayNPCs.Contains(npc.Key))
                        {
                            b.Draw(Game1.mouseCursors, new Vector2(npc.Value.position.X + 20, npc.Value.position.Y), new Rectangle?(new Rectangle(147, 412, 10, 11)), Color.DimGray * 0.9f, 0f, Vector2.Zero, 1.8f, SpriteEffects.None, 0f);
                        }
                        if (questNPCs.Contains(npc.Key))
                        {
                            b.Draw(Game1.mouseCursors, new Vector2(npc.Value.position.X + 22, npc.Value.position.Y - 3), new Rectangle?(new Rectangle(403, 496, 5, 14)), Color.DimGray * 0.9f, 0f, Vector2.Zero, 1.8f, SpriteEffects.None, 0f);
                        }
                    }
                    else
                    {
                        b.Draw(npc.Value.marker, npc.Value.position, new Rectangle?(new Rectangle(0, spriteCrop[npc.Key], 16, 15)), Color.White * 0.9f);
                        if (birthdayNPCs.Contains(npc.Key))
                        {
                            b.Draw(Game1.mouseCursors, new Vector2(npc.Value.position.X + 20, npc.Value.position.Y), new Rectangle?(new Rectangle(147, 412, 10, 11)), Color.White, 0f, Vector2.Zero, 1.8f, SpriteEffects.None, 0f);
                        }
                        if (questNPCs.Contains(npc.Key))
                        {
                            b.Draw(Game1.mouseCursors, new Vector2(npc.Value.position.X + 22, npc.Value.position.Y - 3), new Rectangle?(new Rectangle(403, 496, 5, 14)), Color.White, 0f, Vector2.Zero, 1.8f, SpriteEffects.None, 0f);
                        }
                    }
                }

                // Player
                Vector2 playerLoc = MapModMain.LocationToMap(Game1.player.currentLocation.name, Game1.player.getTileX(), Game1.player.getTileY());
                Game1.player.FarmerRenderer.drawMiniPortrat(b, new Vector2(playerLoc.X, playerLoc.Y), 0.00011f, 2f, 1, Game1.player);

                // Location and name tooltips
                toolTips.draw(b);

                // Cursor
                if (!Game1.options.hardwareCursor)
                {
                    b.Draw(Game1.mouseCursors, new Vector2((float)Game1.getOldMouseX(), (float)Game1.getOldMouseY()), new Rectangle?(Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, (Game1.options.gamepadControls ? 44 : 0), 16, 16)), Color.White, 0f, Vector2.Zero, ((float)Game1.pixelZoom + Game1.dialogueButtonScale / 150f), SpriteEffects.None, 1f);
                }
            }
        }

        // Show debug info if debug mode
        private static void ShowDebugInfo()
        {
            if (Game1.player.currentLocation == null) { return; }

            // Black backgronud for legible text
            Game1.spriteBatch.Draw(Game1.shadowTexture, new Rectangle(0, 0, 410, 160), new Rectangle(6, 3, 1, 1), Color.Black);

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
        public Rectangle position;
        public int layer;

        public NPCMarker(Texture2D marker, Rectangle position)
        {
            this.marker = marker;
            this.position = position;
            this.layer = 4;
        }
    }

    // For drawing tooltips
    public class MapPageTooltips : IClickableMenu
    {
        public const int region_desert = 1001;
        public const int region_farm = 1002;
        public const int region_backwoods = 1003;
        public const int region_busstop = 1004;  
        public const int region_wizardtower = 1005;
        public const int region_marnieranch = 1006;
        public const int region_leahcottage = 1007;
        public const int region_samhouse = 1008;
        public const int region_haleyhouse = 1009;
        public const int region_townsquare = 1010;
        public const int region_harveyclinic = 1011;
        public const int region_generalstore = 1012;
        public const int region_blacksmith = 1013;
        public const int region_saloon = 1014;
        public const int region_manor = 1015;
        public const int region_museum = 1016;
        public const int region_elliottcabin = 1017;
        public const int region_sewer = 1018;
        public const int region_graveyard = 1019;
        public const int region_trailer = 1020;
        public const int region_alexhouse = 1021;
        public const int region_sciencehouse = 1022;
        public const int region_tent = 1023;
        public const int region_mines = 1024;
        public const int region_adventureguild = 1025;
        public const int region_quarry = 1026;
        public const int region_jojamart = 1027;
        public const int region_fishshop = 1028;
        public const int region_spa = 1029;
        public const int region_secretwoods = 1030;
        public const int region_ruinedhouse = 1031;
        public const int region_communitycenter = 1032;
        public const int region_sewerpipe = 1033;
        public const int region_railroad = 1034;
        private string descriptionText = "";
        private string hoverText = "";
        private string playerLocationName;
        private Texture2D map;
        private int mapX;
        private int mapY;
        private Vector2 playerMapPosition;
        public List<ClickableComponent> points = new List<ClickableComponent>();
        public ClickableTextureComponent okButton;
        private string names;
        private Dictionary<string, string> npcNames;
        private int nameTooltipMode;

        public MapPageTooltips(string names, Dictionary<string, string> npcNames, int nameTooltipMode)
        {
            this.nameTooltipMode = nameTooltipMode;
            this.names = names;
            this.npcNames = npcNames;
            this.okButton = new ClickableTextureComponent(Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11059", new object[0]), new Rectangle(this.xPositionOnScreen + width + Game1.tileSize, this.yPositionOnScreen + height - IClickableMenu.borderWidth - Game1.tileSize / 4, Game1.tileSize, Game1.tileSize), null, null, Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46, -1, -1), 1f, false);
            this.map = Game1.content.Load<Texture2D>("LooseSprites\\map");
            Vector2 topLeftPositionForCenteringOnScreen = Utility.getTopLeftPositionForCenteringOnScreen(this.map.Bounds.Width * Game1.pixelZoom, 180 * Game1.pixelZoom, 0, 0);
            this.mapX = (int)topLeftPositionForCenteringOnScreen.X;
            this.mapY = (int)topLeftPositionForCenteringOnScreen.Y;
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX, this.mapY, 292, 152), Game1.player.mailReceived.Contains("ccVault") ? Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11062", new object[0]) : "???")
            {
                myID = 1001,
                rightNeighborID = 1003,
                downNeighborID = 1030
            });
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 324, this.mapY + 252, 188, 132), Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11064", new object[]
            {
                Game1.player.farmName
            }))
            {
                myID = 1002,
                leftNeighborID = 1005,
                upNeighborID = 1003,
                rightNeighborID = 1004,
                downNeighborID = 1006
            });
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 360, this.mapY + 96, 188, 132), Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11065", new object[0]))
            {
                myID = 1003,
                downNeighborID = 1002,
                leftNeighborID = 1001,
                rightNeighborID = 1022,
                upNeighborID = 1029
            });
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 516, this.mapY + 224, 76, 100), Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11066", new object[0]))
            {
                myID = 1004,
                leftNeighborID = 1002,
                upNeighborID = 1003,
                downNeighborID = 1006,
                rightNeighborID = 1011
            });
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 196, this.mapY + 352, 36, 76), Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11067", new object[0]))
            {
                myID = 1005,
                upNeighborID = 1001,
                downNeighborID = 1031,
                rightNeighborID = 1006,
                leftNeighborID = 1030
            });
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 420, this.mapY + 392, 76, 40), Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11068", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11069", new object[0]))
            {
                myID = 1006,
                leftNeighborID = 1005,
                downNeighborID = 1007,
                upNeighborID = 1002,
                rightNeighborID = 1008
            });
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 452, this.mapY + 436, 32, 24), Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11070", new object[0]))
            {
                myID = 1007,
                upNeighborID = 1006,
                downNeighborID = 1033,
                leftNeighborID = 1005,
                rightNeighborID = 1008
            });
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 612, this.mapY + 396, 36, 52), Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11071", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11072", new object[0]))
            {
                myID = 1008,
                leftNeighborID = 1006,
                upNeighborID = 1010,
                rightNeighborID = 1009
            });
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 652, this.mapY + 408, 40, 36), Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11073", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11074", new object[0]))
            {
                myID = 1009,
                leftNeighborID = 1008,
                upNeighborID = 1010,
                rightNeighborID = 1018
            });
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 672, this.mapY + 340, 44, 60), Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11075", new object[0]))
            {
                myID = 1010,
                leftNeighborID = 1008,
                downNeighborID = 1009,
                rightNeighborID = 1014,
                upNeighborID = 1011
            });
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 680, this.mapY + 304, 16, 32), Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11076", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11077", new object[0]))
            {
                myID = 1011,
                leftNeighborID = 1004,
                rightNeighborID = 1012,
                downNeighborID = 1010,
                upNeighborID = 1032
            });
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 696, this.mapY + 296, 28, 40), string.Concat(new string[]
            {
                Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11078", new object[0]),
                Environment.NewLine,
                Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11079", new object[0]),
                Environment.NewLine,
                Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11080", new object[0])
            }))
            {
                myID = 1012,
                leftNeighborID = 1011,
                downNeighborID = 1014,
                rightNeighborID = 1021,
                upNeighborID = 1032
            });
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 852, this.mapY + 388, 80, 36), Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11081", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11082", new object[0]))
            {
                myID = 1013,
                upNeighborID = 1027,
                rightNeighborID = 1016,
                downNeighborID = 1017,
                leftNeighborID = 1015
            });
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 716, this.mapY + 352, 28, 40), Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11083", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11084", new object[0]))
            {
                myID = 1014,
                leftNeighborID = 1010,
                rightNeighborID = 1020,
                downNeighborID = 1019,
                upNeighborID = 1012
            });
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 768, this.mapY + 388, 44, 56), Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11085", new object[0]))
            {
                myID = 1015,
                leftNeighborID = 1019,
                upNeighborID = 1020,
                rightNeighborID = 1013,
                downNeighborID = 1017
            });
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 892, this.mapY + 416, 32, 28), Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11086", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11087", new object[0]))
            {
                myID = 1016,
                downNeighborID = 1017,
                leftNeighborID = 1013,
                upNeighborID = 1027,
                rightNeighborID = 99989
            });
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 824, this.mapY + 564, 28, 20), Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11088", new object[0]))
            {
                myID = 1017,
                downNeighborID = 1028,
                upNeighborID = 1015,
                rightNeighborID = 99989
            });
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 696, this.mapY + 448, 24, 20), Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11089", new object[0]))
            {
                myID = 1018,
                downNeighborID = 1017,
                rightNeighborID = 1019,
                upNeighborID = 1014,
                leftNeighborID = 1009
            });
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 724, this.mapY + 424, 40, 32), Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11090", new object[0]))
            {
                myID = 1019,
                leftNeighborID = 1018,
                upNeighborID = 1014,
                rightNeighborID = 1015,
                downNeighborID = 1017
            });
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 780, this.mapY + 360, 24, 20), Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11091", new object[0]))
            {
                myID = 1020,
                upNeighborID = 1021,
                leftNeighborID = 1014,
                downNeighborID = 1015,
                rightNeighborID = 1027
            });
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 748, this.mapY + 316, 36, 36), Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11092", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11093", new object[0]))
            {
                myID = 1021,
                rightNeighborID = 1027,
                downNeighborID = 1020,
                leftNeighborID = 1012,
                upNeighborID = 1032
            });
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 732, this.mapY + 148, 48, 32), string.Concat(new string[]
            {
                Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11094", new object[0]),
                Environment.NewLine,
                Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11095", new object[0]),
                Environment.NewLine,
                Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11096", new object[0])
            }))
            {
                myID = 1022,
                downNeighborID = 1032,
                leftNeighborID = 1003,
                upNeighborID = 1034,
                rightNeighborID = 1023
            });
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 784, this.mapY + 128, 12, 16), Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11097", new object[0]))
            {
                myID = 1023,
                leftNeighborID = 1034,
                downNeighborID = 1022,
                rightNeighborID = 1024
            });
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 880, this.mapY + 96, 16, 24), Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11098", new object[0]))
            {
                myID = 1024,
                leftNeighborID = 1023,
                rightNeighborID = 1025,
                downNeighborID = 1027
            });
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 900, this.mapY + 108, 32, 36), (Game1.stats.DaysPlayed >= 5u) ? (Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11099", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11100", new object[0])) : "???")
            {
                myID = 1025,
                leftNeighborID = 1024,
                rightNeighborID = 1026,
                downNeighborID = 1027
            });
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 968, this.mapY + 116, 88, 76), Game1.player.mailReceived.Contains("ccCraftsRoom") ? Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11103", new object[0]) : "???")
            {
                myID = 1026,
                leftNeighborID = 1025,
                downNeighborID = 1027
            });
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 872, this.mapY + 280, 52, 52), Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11105", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11106", new object[0]))
            {
                myID = 1027,
                upNeighborID = 1025,
                leftNeighborID = 1021,
                downNeighborID = 1013
            });
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 844, this.mapY + 608, 36, 40), Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11107", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11108", new object[0]))
            {
                myID = 1028,
                upNeighborID = 1017,
                rightNeighborID = 99989
            });
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 576, this.mapY + 60, 48, 36), Game1.isLocationAccessible("Railroad") ? (Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11110", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11111", new object[0])) : "???")
            {
                myID = 1029,
                rightNeighborID = 1034,
                downNeighborID = 1003,
                leftNeighborID = 1001
            });
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX, this.mapY + 272, 196, 176), Game1.player.mailReceived.Contains("beenToWoods") ? Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11114", new object[0]) : "???")
            {
                myID = 1030,
                upNeighborID = 1001,
                rightNeighborID = 1005
            });
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 260, this.mapY + 572, 20, 20), Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11116", new object[0]))
            {
                myID = 1031,
                rightNeighborID = 1033,
                upNeighborID = 1005
            });
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 692, this.mapY + 204, 44, 36), Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11117", new object[0]))
            {
                myID = 1032,
                downNeighborID = 1012,
                upNeighborID = 1022,
                leftNeighborID = 1004
            });
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 380, this.mapY + 596, 24, 32), Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11118", new object[0]))
            {
                myID = 1033,
                leftNeighborID = 1031,
                rightNeighborID = 1017,
                upNeighborID = 1007
            });
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 644, this.mapY + 64, 16, 8), Game1.isLocationAccessible("Railroad") ? Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11119", new object[0]) : "???")
            {
                myID = 1034,
                leftNeighborID = 1029,
                rightNeighborID = 1023,
                downNeighborID = 1022
            });
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 728, this.mapY + 652, 28, 28), Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11122", new object[0])));
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            foreach (ClickableComponent current in this.points)
            {
                string name = current.name;
                if (name == "Lonely Stone")
                {
                    Game1.playSound("stoneCrack");
                }
            }
            if (Game1.activeClickableMenu != null)
            {
                (Game1.activeClickableMenu as GameMenu).changeTab(0);
            }
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
        }

        public override void performHoverAction(int x, int y)
        {
            this.hoverText = "";
            foreach (ClickableComponent current in this.points)
            {
                if (current.containsPoint(x, y))
                {
                    this.hoverText = current.name;
                    return;
                }
            }
        }

        // Draw location tooltips
        public override void draw(SpriteBatch b)
        {
            int x = Game1.getMouseX() + Game1.tileSize / 2;
            int y = Game1.getMouseY() + Game1.tileSize / 2;
            int offsetY = 0;
            this.performHoverAction(x - Game1.tileSize / 2, y - Game1.tileSize / 2);

            if (this.playerLocationName != null)
            {
                StardewValley.BellsAndWhistles.SpriteText.drawStringWithScrollCenteredAt(b, this.playerLocationName, this.xPositionOnScreen + this.width / 2, this.yPositionOnScreen + this.height + Game1.tileSize / 2 + Game1.pixelZoom * 4, "", 1f, -1, 0, 0.88f, false);
            }

            if (!this.hoverText.Equals(""))
            {
                IClickableMenu.drawHoverText(b, this.hoverText, Game1.smallFont, 0, 0, -1, null, -1, null, null, 0, -1, -1, -1, -1, 1f, null);
                int textLength = (int)Game1.smallFont.MeasureString(hoverText).X + Game1.tileSize / 2;
                foreach (KeyValuePair<string, string> customName in npcNames)
                {
                    this.hoverText = this.hoverText.Replace(customName.Key, customName.Value);
                }
                int width = Math.Max((int)Game1.smallFont.MeasureString(hoverText).X + Game1.tileSize / 2, textLength);
                int height = (int)Math.Max(60, Game1.smallFont.MeasureString(hoverText).Y + Game1.tileSize / 2);
                if (x + width > Game1.viewport.Width)
                {
                    x = Game1.viewport.Width - width;
                    y += Game1.tileSize / 4;
                }
                if (this.nameTooltipMode == 1)
                {
                    if (y + height > Game1.viewport.Height)
                    {
                        x += Game1.tileSize / 4;
                        y = Game1.viewport.Height - height;
                    }
                    offsetY = 4 - Game1.tileSize;
                }
                else if (this.nameTooltipMode == 2)
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

                DrawNPCNames(Game1.spriteBatch, this.names, x, y, offsetY, height, this.nameTooltipMode);
                IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, y, width, height, Color.White, 1f, false);
                b.DrawString(Game1.smallFont, hoverText, new Vector2((float)(x + Game1.tileSize / 4), (float)(y + Game1.tileSize / 4 + 4)) + new Vector2(2f, 2f), Game1.textShadowColor);
                b.DrawString(Game1.smallFont, hoverText, new Vector2((float)(x + Game1.tileSize / 4), (float)(y + Game1.tileSize / 4 + 4)) + new Vector2(0f, 2f), Game1.textShadowColor);
                b.DrawString(Game1.smallFont, hoverText, new Vector2((float)(x + Game1.tileSize / 4), (float)(y + Game1.tileSize / 4 + 4)) + new Vector2(2f, 0f), Game1.textShadowColor);
                b.DrawString(Game1.smallFont, hoverText, new Vector2((float)(x + Game1.tileSize / 4), (float)(y + Game1.tileSize / 4 + 4)), Game1.textColor * 0.9f);
            }
            else
            {
                DrawNPCNames(Game1.spriteBatch, this.names, x, y, offsetY, height, this.nameTooltipMode);
            }
        }

        // Draw map to cover base rendering 
        public void DrawMap(SpriteBatch b)
        {
            Game1.drawDialogueBox(this.mapX - Game1.pixelZoom * 8, this.mapY - Game1.pixelZoom * 24, (this.map.Bounds.Width + 16) * Game1.pixelZoom, 212 * Game1.pixelZoom, false, true, null, false);
            b.Draw(this.map, new Vector2((float)this.mapX, (float)this.mapY), new Rectangle?(new Rectangle(0, 0, 300, 180)), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.86f);
            switch (Game1.whichFarm)
            {
                case 1:
                    b.Draw(this.map, new Vector2((float)this.mapX, (float)(this.mapY + 43 * Game1.pixelZoom)), new Rectangle?(new Rectangle(0, 180, 131, 61)), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
                    break;
                case 2:
                    b.Draw(this.map, new Vector2((float)this.mapX, (float)(this.mapY + 43 * Game1.pixelZoom)), new Rectangle?(new Rectangle(131, 180, 131, 61)), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
                    break;
                case 3:
                    b.Draw(this.map, new Vector2((float)this.mapX, (float)(this.mapY + 43 * Game1.pixelZoom)), new Rectangle?(new Rectangle(0, 241, 131, 61)), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
                    break;
                case 4:
                    b.Draw(this.map, new Vector2((float)this.mapX, (float)(this.mapY + 43 * Game1.pixelZoom)), new Rectangle?(new Rectangle(131, 241, 131, 61)), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
                    break;
            }
        }

        // Draw NPC names in bottom left corner of map page
        public void DrawNPCNames(SpriteBatch b, string names, int x, int y, int offsetY, int relocate, int nameTooltipMode)
        {
            if (!(names.Equals("")))
            {
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
                            y += -relocate + 8 - Game1.tileSize;
                        }
                    }
                }
                else
                {
                    x = Game1.activeClickableMenu.xPositionOnScreen - 145;
                    y = Game1.activeClickableMenu.yPositionOnScreen + 650 - height / 2;
                }

                drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, y, width, height, Color.White, 1f, true);
                Vector2 vector = new Vector2(x + (float)(Game1.tileSize / 4), y + (float)(Game1.tileSize / 4 + 4));
                b.DrawString(Game1.smallFont, names, vector + new Vector2(2f, 2f), Game1.textShadowColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                b.DrawString(Game1.smallFont, names, vector + new Vector2(0f, 2f), Game1.textShadowColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                b.DrawString(Game1.smallFont, names, vector + new Vector2(2f, 0f), Game1.textShadowColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                b.DrawString(Game1.smallFont, names, vector, Game1.textColor * 0.9f, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            }
        }
    }
}
