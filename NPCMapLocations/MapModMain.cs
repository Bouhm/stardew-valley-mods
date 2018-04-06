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
using System.Reflection;

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
        private static int customNpcId;
        private static bool snappyMenuOption;
        private static bool[] showSecondaryNPCs = new Boolean[5];
        private static MapModMapPage modMapPage;
        private static Dictionary<string, int> markerCrop; // NPC head crops, top left corner (0, y), width = 16, height = 15 
        private static Dictionary<string, Dictionary<string, int>> customNPCs;
        private static Dictionary<string, string> npcNames = new Dictionary<string, string>(); // For custom names
        private static HashSet<NPCMarker> npcMarkers = new HashSet<NPCMarker>();
        private static Dictionary<string, Vector2> farmBuildings = new Dictionary<string, Vector2>();

        // For debug info
        private const bool DEBUG_MODE = true;
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
            MenuEvents.MenuClosed += MenuEvents_MenuClosed;
        }

        // Load config and other one-off data
        private void SaveEvents_AfterLoad(object sender, EventArgs e)
        {
            saveName = Constants.SaveFolderName;
            config = modHelper.ReadJsonFile<MapModConfig>($"config/{saveName}.json") ?? new MapModConfig();
            markerCrop = MapModConstants.MarkerCrop;
            customNPCs = config.CustomNPCs;
            snappyMenuOption = Game1.options.snappyMenus;
            HandleCustomMods();
        }

        // Handles customizations for NPCs
        // Custom NPCs and custom names or sprites for existing NPCs
        private void HandleCustomMods()
        {
            bool areCustomNPCsInstalled = (customNPCs != null && customNPCs.Count > 0);
            int id = 1;
            foreach (NPC npc in Utility.getAllCharacters())
            {
                id = LoadCustomNPCs(npc, id, areCustomNPCsInstalled);
                LoadNPCCrop(npc);
                LoadCustomNames(npc);
            }
            config.CustomNPCs = customNPCs;
            modHelper.WriteJsonFile($"config/{saveName}.json", config);
        }

        // Handle modified or custom NPCs
        private int LoadCustomNPCs(NPC npc, int id, bool areCustomNPCsInstalled)
        {
            if (areCustomNPCsInstalled)
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


        // Handle modified NPC names
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

        // Load user-specified NPC crops for custom sprites
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

        // Handle opening mod menu and changing tooltip options
        private void InputEvents_ButtonPressed(object sender, EventArgsInput e)
        {
            if (Game1.hasLoadedGame && Game1.activeClickableMenu is GameMenu)
            {
                HandleInput((GameMenu)Game1.activeClickableMenu, e.Button);
            }
        }

        // Handle keyboard/controller inputs
        private void HandleInput(GameMenu menu, SButton input)
        {
            if (menu.currentTab != GameMenu.mapTab) { return; }
            if (input.ToString().Equals(config.MenuKey) || input is SButton.ControllerY)
            {
                Game1.activeClickableMenu = new MapModMenu(
                    Game1.viewport.Width / 2 - (1100 + IClickableMenu.borderWidth * 2) / 2,
                    Game1.viewport.Height / 2 - (725 + IClickableMenu.borderWidth * 2) / 2,
                    1100 + IClickableMenu.borderWidth * 2,
                    650 + IClickableMenu.borderWidth * 2,
                    showSecondaryNPCs,
                    customNPCs,
                    customNpcId,
                    markerCrop,
                    npcNames
                );
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

        // Handle any checks that need to be made per day
        private void TimeEvents_AfterDayStarted(object sender, EventArgs e)
        {
            // Check unlocked NPCs (hidden to avoid spoilers)
            showSecondaryNPCs[0] = Game1.player.mailReceived.Contains("ccVault"); // Sandy
            showSecondaryNPCs[1] = Game1.player.eventsSeen.Contains(100162); // Marlon
            showSecondaryNPCs[2] = Game1.player.eventsSeen.Contains(112); // Wizard
            showSecondaryNPCs[3] = Game1.year >= 2; // Kent
            showSecondaryNPCs[4] = // Traveling Merchant
                (Game1.dayOfMonth == 5
                || Game1.dayOfMonth == 7
                || Game1.dayOfMonth == 12
                || Game1.dayOfMonth == 14
                || Game1.dayOfMonth == 19
                || Game1.dayOfMonth == 21
                || Game1.dayOfMonth == 26
                || Game1.dayOfMonth == 28);

            // Reset NPC marker data daily
            npcMarkers = new HashSet<NPCMarker>();
            foreach (NPC npc in Utility.getAllCharacters())
            {
                // Handle case where Kent appears even though he shouldn't
                if (!npc.isVillager() || (npc.name.Equals("Kent") && !showSecondaryNPCs[3])) { continue; }
               
                NPCMarker npcMarker = new NPCMarker(){
                    Name = npc.name, 
                    IsBirthday = npc.isBirthday(Game1.currentSeason, Game1.dayOfMonth)
                };
                npcMarkers.Add(npcMarker);
            }
        }

        // Handle updating NPC marker data when map is open
        private void GameEvents_UpdateTick(object sender, EventArgs e)
        {
            if (!Game1.hasLoadedGame) { return; }
            if (!(Game1.activeClickableMenu is GameMenu)) { return; }
            if (!IsMapOpen((GameMenu)Game1.activeClickableMenu)) { return; }
            if (Game1.options.snappyMenus)
                modHelper.Reflection.GetField<Boolean>(Game1.options, "snappyMenus").SetValue(false);

            GetFarmBuildingLocs();
            UpdateMarkers();
        }

        // Update NPC marker data and names on hover
        private void UpdateMarkers()
        {
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

                MapModConstants.MapVectors.TryGetValue(currentLocation, out MapVector[] npcLocation);
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
                    npcMarker.IsHidden = (
                        (config.ImmersionOption == 2 && !Game1.player.hasTalkedToFriendToday(npc.name))
                        || (config.ImmersionOption == 3 && Game1.player.hasTalkedToFriendToday(npc.name))
                        || (config.OnlySameLocation && !sameLocation)
                        || (config.ByHeartLevel
                            && !(Game1.player.getFriendshipHeartLevelForNPC(npc.name)
                            >= config.HeartLevelMin && Game1.player.getFriendshipHeartLevelForNPC(npc.name)
                            <= config.HeartLevelMax)
                           )

                    );

                    // NPCs that will be drawn onto the map
                    if (IsNPCShown(npc.name) && (config.ShowHiddenVillagers || !npcMarker.IsHidden))
                    {
                        int width = 32;
                        int height = 30;
                        // Get center of NPC marker 
                        int x = (int)LocationToMap(currentLocation, npc.getTileX(), npc.getTileY()).X - width/2;
                        int y = (int)LocationToMap(currentLocation, npc.getTileX(), npc.getTileY()).Y - height/2;

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

                        // Establish draw order, higher number infront
                        // Layers 4 - 7: Outdoor NPCs in order of hidden, hidden w/ quest/birthday, standard, standard w/ quest/birthday
                        // Layers 0 - 3: Indoor NPCs in order of hidden, hidden w/ quest/birthday, standard, standard w/ quest/birthday
                        npcMarker.Layer = npcMarker.IsOutdoors ? 6 : 2;
                        if (npcMarker.IsHidden) { npcMarker.Layer -= 2; }
                        if (npcMarker.HasQuest || npcMarker.IsBirthday) { npcMarker.Layer++; }
                    }
                    else
                    {
                        // Set no location so they don't get drawn
                        npcMarker.Location = new Rectangle();
                    }
                }
            }
            modMapPage = new MapModMapPage(npcMarkers, npcNames);
        }

        // MAIN METHOD FOR PINPOINTING NPCS ON THE MAP
        // Calculated from mapping of game tile positions to pixel coordinates of the map in MapModConstants. 
        // Requires MapModConstants and modified map page in ./content 
        public static Vector2 LocationToMap(string location, int tileX = -1, int tileY = -1, bool isFarmer = false)
        {
            if (location == null || !MapModConstants.MapVectors.ContainsKey(location))
            {
                if (DEBUG_MODE && alertFlag != "UnknownLocation:" + location)
                {
                    MapModMain.monitor.Log("Unknown Location: " + location + ".", LogLevel.Alert);
                    alertFlag = "UnknownLocation:" + location;
                }
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
                        tileX = (int)(farmBuilding.tileX - farmBuilding.tilesWide / 2);
                        tileY = (int)(farmBuilding.tileY - farmBuilding.tilesHigh / 2);
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

                MapVector lower = null;
                MapVector upper = null;
                var hasEqualTile = false;

                // Create rectangle bound from two pre-defined points (lower & upper bound) and calculate map scale for that area
                foreach (MapVector vector in vectors)
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

        // Helper to check if map is opened
        private static bool IsMapOpen(GameMenu menu)
        {
            if (menu == null) { return false; }
            return (menu.currentTab == GameMenu.mapTab);
        }

        // Get locations of farm buildings
        private void GetFarmBuildingLocs()
        {
            foreach (Building building in Game1.getFarm().buildings)
            {
                if (building.baseNameOfIndoors == null)
                {
                    continue;
                }

                Vector2 locVector = MapModMain.LocationToMap("Farm", building.tileX, building.tileY);
                if (building.baseNameOfIndoors.Equals("Shed"))
                {
                    farmBuildings["Shed"] = locVector;
                }
                else if (building.baseNameOfIndoors.Equals("Coop"))
                {
                    farmBuildings["Coop"] = locVector;
                }
                else if (building.baseNameOfIndoors.Equals("Barn"))
                {
                    locVector = new Vector2(locVector.X, locVector.Y + 2);
                    farmBuildings["Barn"] = locVector;
                }
                else if (building.baseNameOfIndoors.Equals("SlimeHutch"))
                {
                    farmBuildings["SlimeHutch"] = locVector;
                }
            }

            // Greenhouse unlocked after pantry bundles completed
            if (((CommunityCenter)Game1.getLocationFromName("CommunityCenter")).areasComplete[CommunityCenter.AREA_Pantry])
            {
                Vector2 locVector = MapModMain.LocationToMap("Greenhouse");
                locVector = new Vector2((int)(locVector.X - 5 / 2 * 3), (int)(locVector.Y - 7 / 2 * 3));
                farmBuildings["Greenhouse"] = locVector;
            }
        }

        // Draw event (when Map Page is opened)
        private void GraphicsEvents_OnPostRenderGuiEvent(object sender, EventArgs e)
        {
            if (!Game1.hasLoadedGame) { return; }
            if (Game1.activeClickableMenu == null || !(Game1.activeClickableMenu is GameMenu)) { return; }
            if (!IsMapOpen((GameMenu)Game1.activeClickableMenu)) { return; }

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
                foreach (var building in farmBuildings)
                {
                    b.Draw(buildings, building.Value, new Rectangle?(MapModConstants.FarmBuildingRects[building.Key]), Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
                }
            }

            // Traveling Merchant
            if (config.ShowTravelingMerchant && showSecondaryNPCs[4])
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

        // Hack to disable snappy menu with Map Page since ModMapPage doesn't replace the menu
        // And hence can't override the snappy control like I did in MapModMenu
        private void MenuEvents_MenuClosed(object sender, EventArgsClickableMenuClosed e)
        {
            if (!Game1.hasLoadedGame || Game1.options == null) { return; }
            if (e.PriorMenu is GameMenu menu)
            {
                // Reset option after map sets option to false
                if (IsMapOpen(menu))
                {
                    modHelper.Reflection.GetField<Boolean>(Game1.options, "snappyMenus").SetValue(snappyMenuOption);
                }
                else
                // Handle any option changes by the player
                // Caveat: If player is turning snappy menu on, they MUST close the menu to update the stored menu option
                {
                    snappyMenuOption = Game1.options.snappyMenus;
                }
            }
        }

        // For debugging
        private void GraphicsEvents_OnPostRenderEvent(object sender, EventArgs e)
        {
            if (!Game1.hasLoadedGame || Game1.player == null) { return; }
            if (DEBUG_MODE)
                #pragma warning disable CS0162 // Unreachable code detected
                ShowDebugInfo();
                #pragma warning restore CS0162 // Unreachable code detected
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
        private static bool IsNPCShown(string npc)
        {
            bool showNPC = !config.NPCBlacklist.Contains(npc);   
            if (!IsCustomNPC(npc)) {
                if (npc.Equals("Sandy")) { return showNPC && showSecondaryNPCs[0]; }
                else if (npc.Equals("Marlon")) { return showNPC && showSecondaryNPCs[1]; }
                else if (npc.Equals("Wizard")) { return showNPC && showSecondaryNPCs[2]; }
                else return showNPC;
            }
            else 
            {
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
            }
            return true;
        }

        // Only checks against existing villager names
        public static bool IsCustomNPC(string npc)
        {
            return !MapModConstants.Villagers.Contains(npc);
        }
    }

    // Class for NPC markers
    public class NPCMarker
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

    // Class for Location Vectors
    public class MapVector
    {
        public int tileX;
        public int tileY;
        public int x;
        public int y;

        public MapVector()
        {
            this.tileX = 0;
            this.tileY = 0;
            this.x = 0;
            this.y = 0;
        }

        public MapVector(int x, int y)
        {
            this.tileX = 0;
            this.tileY = 0;
            this.x = x;
            this.y = y;
        }

        public MapVector(int tileX, int tileY, int x, int y)
        {
            this.tileX = tileX;
            this.tileY = tileY;
            this.x = x;
            this.y = y;
        }

        public int[] GetValues()
        {
            return new int[] { this.tileX, this.tileY, this.x, this.y };
        }
    }
}
