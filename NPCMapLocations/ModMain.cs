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
using StardewValley.Network;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using Netcode;

namespace NPCMapLocations
{
    public class ModMain : Mod//, IAssetLoader
    {
        public static IModHelper modHelper;
        public static IMonitor monitor;
        public static ModConfig config;
        public static Texture2D map;
        public static Texture2D buildingMarkers;
        public static string saveName;
        public static Dictionary<string, string> npcNames; // For handling custom names
        private static int customNpcId;
        private static bool snappyMenuOption;
        private static ModMapPage modMapPage;
        private static Dictionary<string, int> markerCrop; // NPC head crops, top left corner (0, y), width = 16, height = 15 
        private static Dictionary<string, Dictionary<string, int>> customNPCs;
        private static Dictionary<string, bool> secondaryNPCs;
        private static Dictionary<string, Vector2> farmBuildings;
        private static HashSet<NPCMarker> npcMarkers;
        private static HashSet<FarmerMarker> farmerMarkers;

        // For debug info
        private const bool DEBUG_MODE = false;
        private static Vector2 _tileLower; 
        private static Vector2 _tileUpper; 
        private static string alertFlag; 

        public override void Entry(IModHelper helper)
        {
            modHelper = helper;
            monitor = this.Monitor;
            // Make the mod selfishly replace the map in case of conflict with other mods 
            ModMain.map = ModMain.modHelper.Content.Load<Texture2D>(@"assets/map.png", ContentSource.ModFolder); // Load modified map page
            ModMain.buildingMarkers = ModMain.modHelper.Content.Load<Texture2D>(@"assets/buildings.png", ContentSource.ModFolder); // Load farm buildings
            saveName = Constants.SaveFolderName;
            config = modHelper.ReadJsonFile<ModConfig>($"config/{saveName}.json") ?? new ModConfig();
            markerCrop = MapModConstants.MarkerCrop;
            customNPCs = config.CustomNPCs;
            npcNames = new Dictionary<string, string>();
            farmBuildings = new Dictionary<string, Vector2>();

            SaveEvents.AfterLoad += SaveEvents_AfterLoad;
            GameEvents.UpdateTick += GameEvents_UpdateTick;
            GraphicsEvents.OnPostRenderEvent += GraphicsEvents_OnPostRenderEvent;
            GraphicsEvents.OnPostRenderGuiEvent += GraphicsEvents_OnPostRenderGuiEvent;
            InputEvents.ButtonPressed += InputEvents_ButtonPressed;
            TimeEvents.AfterDayStarted += TimeEvents_AfterDayStarted;
            MenuEvents.MenuClosed += MenuEvents_MenuClosed;
        }

        /*
        // Replace game map with modified map
        public bool CanLoad<T>(IAssetInfo asset)
        {
            return asset.AssetNameEquals(@"LooseSprites\Map");
        }

        public T Load<T>(IAssetInfo asset)
        {
            return (T)(object)MapModMain.modHelper.Content.Load<Texture2D>(@"assets\map.png"); // Replace map page
        }
        */

        // Load config and other one-off data
        private void SaveEvents_AfterLoad(object sender, EventArgs e)
        {
            snappyMenuOption = Game1.options.SnappyMenus;
            secondaryNPCs = new Dictionary<string, bool>
            {
                { "Kent", false },
                { "Marlon", false },
                { "Merchant", false },
                { "Sandy", false },
                { "Wizard", false }
            };
            HandleCustomNPCs();
        }

        private NetCollection<NPC> GetAllNPCs()
        {
            var allNPCs = new NetCollection<NPC>();
            foreach (GameLocation location in Game1.locations)
            {
                foreach (NPC npc in location.characters)
                {
                    if (!allNPCs.Contains(npc))
                        allNPCs.Add(npc);
                }
            }

            return allNPCs;
        }

        // Handles customizations for NPCs
        // Custom NPCs and custom names or sprites for existing NPCs
        private void HandleCustomNPCs()
        {
            bool areCustomNPCsInstalled = (customNPCs != null && customNPCs.Count > 0);
            int id = 1;
            foreach (NPC npc in GetAllNPCs())
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
                    if (npc.Name.Equals(customNPC.Key))
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
                if (npc.Schedule != null && IsCustomNPC(npc.Name))
                {
                    if (!customNPCs.TryGetValue(npc.Name, out Dictionary<string, int> npcEntry))
                    {
                        npcEntry = new Dictionary<string, int>
                        {
                            { "id", id },
                            { "crop", 0 }
                        };
                        customNPCs.Add(npc.Name, npcEntry);

                        if (!markerCrop.ContainsKey(npc.Name))
                            markerCrop.Add(npc.Name, 0);

                        id++;
                    }
                }
            }
            return id;
        }


        // Handle any modified NPC names 
        // Specifically mods that change names in dialogue files (displayName)
        private void LoadCustomNames(NPC npc)
        {
            if (!npcNames.TryGetValue(npc.Name, out string customName)) {
                if (npc.displayName == null)
                    npcNames.Add(npc.Name, npc.Name);
                else
                    npcNames.Add(npc.Name, npc.displayName);
            }
        }

        // Load user-specified NPC crops for custom sprites
        private void LoadNPCCrop(NPC npc)
        {
            if (config.VillagerCrop != null && config.VillagerCrop.Count > 0)
            {
                foreach (KeyValuePair<string, int> villager in config.VillagerCrop)
                {
                    if (npc.Name.Equals(villager.Key))
                    {
                        markerCrop[npc.Name] = villager.Value;
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
                Game1.activeClickableMenu = new ModMenu(
                    Game1.viewport.Width / 2 - (1100 + IClickableMenu.borderWidth * 2) / 2,
                    Game1.viewport.Height / 2 - (725 + IClickableMenu.borderWidth * 2) / 2,
                    1100 + IClickableMenu.borderWidth * 2,
                    650 + IClickableMenu.borderWidth * 2,
                    secondaryNPCs,
                    customNPCs,
                    customNpcId,
                    markerCrop
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
            var npcEntries = new Dictionary<string, bool>(secondaryNPCs);
            foreach (KeyValuePair<string, bool> npcEntry in npcEntries)
            {
                string name = npcEntry.Key;
                if (!npcNames.TryGetValue(name, out string dName))
                {
                    if (Game1.getCharacterFromName(name) != null)
                        npcNames[name] = Game1.getCharacterFromName(name).displayName;
                }

                if (!npcEntry.Value)
                {
                    switch (name)
                    {
                        case "Kent":
                            secondaryNPCs[name] = Game1.year >= 2;
                            break;
                        case "Marlon":
                            secondaryNPCs[name] = Game1.player.eventsSeen.Contains(100162);
                            break;
                        case "Merchant":
                            secondaryNPCs[name] =
                                (Game1.dayOfMonth == 5
                                || Game1.dayOfMonth == 7
                                || Game1.dayOfMonth == 12
                                || Game1.dayOfMonth == 14
                                || Game1.dayOfMonth == 19
                                || Game1.dayOfMonth == 21
                                || Game1.dayOfMonth == 26
                                || Game1.dayOfMonth == 28);
                            break;
                        case "Sandy":
                            secondaryNPCs[name] = Game1.player.mailReceived.Contains("ccVault");
                            break;
                        case "Wizard":
                            secondaryNPCs[name] = Game1.player.eventsSeen.Contains(112);
                            break;
                        default: break;

                    }
                }
            }

            // Reset NPC marker data daily
            npcMarkers = new HashSet<NPCMarker>();
            foreach (NPC npc in GetAllNPCs())
            {
                // Handle case where Kent appears even though he shouldn't
                if (!npc.isVillager() || (npc.Name.Equals("Kent") && !secondaryNPCs["Kent"])) { continue; }

                if (npcNames.TryGetValue(npc.Name, out string name))
                    name = npcNames[npc.Name];
                else
                    name = npc.Name;

                NPCMarker npcMarker = new NPCMarker() {
                    Npc = npc,
                    Name = name,
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
            if (Game1.options.SnappyMenus)
                modHelper.Reflection.GetField<Boolean>(Game1.options, "SnappyMenus").SetValue(false);

            GetFarmBuildingLocs();
            UpdateMarkers();
        }

        private static int GetTile(float p)
        {
            return (int)Math.Floor(p / Game1.tileSize);
        }

        // Update NPC marker data and names on hover
        private void UpdateMarkers()
        {
            foreach (NPCMarker npcMarker in npcMarkers)
            {
                NPC npc = npcMarker.Npc;
                string currLocation;

                // Handle null locations at beginning of new day
                if (npc.currentLocation == null)
                    currLocation = npc.DefaultMap;
                else
                    currLocation = npc.currentLocation.Name;
                
                // Isn't mapped by the mod; skip
                if (!MapModConstants.MapVectors.TryGetValue(currLocation, out MapVector[] npcLocation))
                    continue;

                // For layering indoor/outdoor NPCs and indoor indicator
                npcMarker.IsOutdoors = Game1.getLocationFromName(currLocation).IsOutdoors;

                if (npc.Schedule != null 
                  || npc.isMarried() 
                  || npc.Name.Equals(npcNames["Sandy"]) 
                  || npc.Name.Equals(npcNames["Marlon"]) 
                  || npc.Name.Equals(npcNames["Wizard"]))
                {
                    // For show NPCs in player's location option
                    bool isSameLocation = false;
                    if (config.OnlySameLocation)
                    {
                        isSameLocation = npc.currentLocation.Equals(Game1.player.currentLocation);
                        if (!npc.currentLocation.IsOutdoors)
                        {
                            if (Game1.player.currentLocation == npc.currentLocation)
                            {
                                isSameLocation = true;
                            }
                            else
                            {
                                // Check inside buildings and rooms
                                foreach (KeyValuePair<Point, string> door in Game1.player.currentLocation.doors.Pairs)
                                {
                                    // Check buildings
                                    if (door.Value.Equals(npc.currentLocation.Name))
                                    {
                                        isSameLocation = true;
                                        break;
                                    }
                                    // Check rooms
                                    else
                                    {
                                        foreach (KeyValuePair<Point, string> roomDoor in npc.currentLocation.doors.Pairs)
                                        {
                                            if (door.Value.Equals(roomDoor.Value))
                                            {
                                                isSameLocation = true;
                                                break;
                                            }
                                        }
                                    }
                                }                                
                            }
                        }
                    }

                    // NPCs that won't be shown on the map unless Show Hidden NPCs is checked
                    npcMarker.IsHidden = (
                        (config.ImmersionOption == 2 && !Game1.player.hasTalkedToFriendToday(npc.Name))
                        || (config.ImmersionOption == 3 && Game1.player.hasTalkedToFriendToday(npc.Name))
                        || (config.OnlySameLocation && !isSameLocation)
                        || (config.ByHeartLevel
                            && !(Game1.player.getFriendshipHeartLevelForNPC(npc.Name)
                            >= config.HeartLevelMin && Game1.player.getFriendshipHeartLevelForNPC(npc.Name)
                            <= config.HeartLevelMax)
                           )
                    );

                    // NPCs that will be drawn onto the map
                    if (IsNPCShown(npc.Name) && (config.ShowHiddenVillagers || !npcMarker.IsHidden))
                    {
                        int width = 32;
                        int height = 30;
                        // Get center of NPC marker 
                        int x = (int)LocationToMap(currLocation, GetTile(npc.Position.X), GetTile(npc.Position.Y)).X - width/2;
                        int y = (int)LocationToMap(currLocation, GetTile(npc.Position.X), GetTile(npc.Position.Y)).Y - height/2;

                        npcMarker.Location = new Rectangle(x, y, width, height);
                        npcMarker.Marker = npc.Sprite.Texture;

                        // Check for daily quests
                        foreach (Quest quest in Game1.player.questLog)
                        {
                            if (quest.accepted && quest.dailyQuest && !quest.completed)
                            {
                                switch (quest.questType)
                                {
                                    case 3:
                                        npcMarker.HasQuest = (((ItemDeliveryQuest)quest).target == npc.Name);
                                        break;
                                    case 4:
                                        npcMarker.HasQuest = (((SlayMonsterQuest)quest).target == npc.Name);
                                        break;
                                    case 7:
                                        npcMarker.HasQuest = (((FishingQuest)quest).target == npc.Name);
                                        break;
                                    case 10:
                                        npcMarker.HasQuest = (((ResourceCollectionQuest)quest).target == npc.Name);
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
            modMapPage = new ModMapPage(npcMarkers, npcNames);
        }

        // MAIN METHOD FOR PINPOINTING NPCS ON THE MAP
        // Calculated from mapping of game tile positions to pixel coordinates of the map in MapModConstants. 
        // Requires MapModConstants and modified map page in ./content 
        public static Vector2 LocationToMap(string location, int tileX = -1, int tileY = -1, bool isFarmer = false)
        {
            if (location == null)
            {
                if (DEBUG_MODE && alertFlag != "UnknownLocation:" + location)
                {
                    ModMain.monitor.Log("Unknown Location: " + location + ".", LogLevel.Alert);
                    alertFlag = "UnknownLocation:" + location;
                }
                return new Vector2(-5000, -5000);
            }

            // Get tile location of farm buildings in farm
            string[] buildings = { "Coop", "Big Coop", "Deluxe Coop", "Barn", "Big Barn", "Deluxe Barn", "Slime Hutch", "Shed" };
            if (buildings.Contains(location))
            {
                foreach (Building farmBuilding in Game1.getFarm().buildings)
                {
                    if (farmBuilding.indoors.Value != null && farmBuilding.indoors.Value.Name.Equals(location))
                    {
                        // Set origin to center
                        tileX = (int)(farmBuilding.tileX.Value - farmBuilding.tilesWide.Value / 2);
                        tileY = (int)(farmBuilding.tileY.Value - farmBuilding.tilesHigh.Value / 2);
                        location = "Farm";
                    }
                }
            }

            if (!MapModConstants.MapVectors.TryGetValue(location, out MapVector[] locVectors))
                return new Vector2(-5000, -5000);

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
                        ModMain.monitor.Log("Null lower bound - No vector less than " + tilePos + " to calculate location.", LogLevel.Alert);
                        alertFlag = "NullBound:" + tilePos;
                    }

                    lower = upper == vectors.First() ? vectors.Skip(1).First() : vectors.First();
                }
                if (upper == null)
                {
                    if (DEBUG_MODE && isFarmer && alertFlag != "NullBound:" + tilePos)
                    {
                        ModMain.monitor.Log("Null upper bound - No vector greater than " + tilePos + " to calculate location.", LogLevel.Alert);
                        alertFlag = "NullBound:" + tilePos;
                    }

                    upper = lower == vectors.First() ? vectors.Skip(1).First() : vectors.First();
                }

                x = (int)(lower.x + (double)(tileX - lower.tileX) / (double)(upper.tileX - lower.tileX) * (upper.x - lower.x));
                y = (int)(lower.y + (double)(tileY - lower.tileY) / (double)(upper.tileY - lower.tileY) * (upper.y - lower.y));

                if (DEBUG_MODE && isFarmer)
                {
                    ModMain._tileUpper = new Vector2(upper.tileX, upper.tileY);
                    ModMain._tileLower = new Vector2(lower.tileX, lower.tileY);
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
                if (building.nameOfIndoorsWithoutUnique == null)
                {
                    continue;
                }

                Vector2 locVector = ModMain.LocationToMap("Farm", building.tileX.Value, building.tileY.Value);
                if (building.nameOfIndoorsWithoutUnique.Equals("Shed"))
                {
                    farmBuildings["Shed"] = locVector;
                }
                else if (building.nameOfIndoorsWithoutUnique.Equals("Coop"))
                {
                    farmBuildings["Coop"] = locVector;
                }
                else if (building.nameOfIndoorsWithoutUnique.Equals("Barn"))
                {
                    locVector = new Vector2(locVector.X, locVector.Y + 2);
                    farmBuildings["Barn"] = locVector;
                }
                else if (building.nameOfIndoorsWithoutUnique.Equals("SlimeHutch"))
                {
                    farmBuildings["SlimeHutch"] = locVector;
                }
            }

            // Greenhouse unlocked after pantry bundles completed
            if (((CommunityCenter)Game1.getLocationFromName("CommunityCenter")).areasComplete[CommunityCenter.AREA_Pantry])
            {
                Vector2 locVector = ModMain.LocationToMap("Greenhouse");
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
                    b.Draw(buildingMarkers, building.Value, new Rectangle?(MapModConstants.FarmBuildingRects[building.Key]), Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
                }
            }

            // Traveling Merchant
            if (config.ShowTravelingMerchant && secondaryNPCs["Merchant"])
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

            Vector2 playerLoc = ModMain.LocationToMap(Game1.player.currentLocation.Name, GetTile(Game1.player.Position.X), GetTile(Game1.player.Position.Y), true);
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
        private void MenuEvents_MenuClosed(object sender, EventArgsClickableMenuClosed e)
        {
            if (!Game1.hasLoadedGame || Game1.options == null) { return; }
            //test();
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
                    snappyMenuOption = Game1.options.SnappyMenus;
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
            DrawText(Game1.player.currentLocation.Name + " (" + Game1.player.Position.X/Game1.tileSize + ", " + Game1.player.Position.Y/Game1.tileSize + ")", new Vector2(Game1.tileSize / 4, Game1.tileSize / 4));

            var currMenu = Game1.activeClickableMenu is GameMenu ? (GameMenu)Game1.activeClickableMenu : null;

            // Show lower & upper bound tiles used for calculations 
            if (currMenu != null && currMenu.currentTab == GameMenu.mapTab)
            {
                DrawText("Lower bound: (" + ModMain._tileLower.X + ", " + ModMain._tileLower.Y + ")", new Vector2(Game1.tileSize / 4, Game1.tileSize * 3 / 4 + 8));
                DrawText("Upper bound: (" + ModMain._tileUpper.X + ", " + ModMain._tileUpper.Y + ")", new Vector2(Game1.tileSize / 4, Game1.tileSize * 5 / 4 + 8 * 2));
            }
            else
            {
                DrawText("Lower bound: (" + ModMain._tileLower.X + ", " + ModMain._tileLower.Y + ")", new Vector2(Game1.tileSize / 4, Game1.tileSize * 3 / 4 + 8), Color.DimGray);
                DrawText("Upper bound: (" + ModMain._tileUpper.X + ", " + ModMain._tileUpper.Y + ")", new Vector2(Game1.tileSize / 4, Game1.tileSize * 5 / 4 + 8 * 2), Color.DimGray);
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
                if (npc.Equals("Sandy")) { return showNPC && secondaryNPCs["Sandy"]; }
                else if (npc.Equals("Marlon")) { return showNPC && secondaryNPCs["Marlon"]; }
                else if (npc.Equals("Wizard")) { return showNPC && secondaryNPCs["Wizard"]; }
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
            return !npcNames.ContainsKey(npc);
        }
    }

    // Class for NPC markers
    public class NPCMarker
    {
        public NPC Npc { get; set; } = null;
        public string Name { get; set; } = null; // Specifically stores any customized names
        public Texture2D Marker { get; set; } = null;
        public Rectangle Location { get; set; } = new Rectangle();
        public bool IsBirthday { get; set; } = false;
        public bool HasQuest { get; set; } = false;
        public bool IsOutdoors { get; set; } = false;
        public bool IsHidden { get; set; } = false;
        public int Layer { get; set; } = 0;
    }

    // Class for Farmer markers
    public class FarmerMarker
    {
        public Farmer Farmer { get; set; } = null;
        public Texture2D Marker { get; set; } = null;
        public Rectangle Location { get; set; } = new Rectangle();
        public bool IsOutdoors { get; set; } = false;
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