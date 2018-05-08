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
using System.Reflection;
using System.Threading.Tasks;

namespace NPCMapLocations
{
    public class ModMain : Mod, IAssetLoader
    {
        public static IModHelper modHelper;
        public static IMonitor monitor;
        public static ModConfig config;
        private static Texture2D buildingMarkers;
        private const int BUILDING_SCALE = 3;
        private static ModCustomHandler CustomHandler;
        private static bool snappyMenuOption;
        private static ModMapPage modMapPage;
        private static Dictionary<string, int> markerCrop; // NPC head crops, top left corner (0, y), width = 16, height = 15 
        private static Dictionary<string, bool> secondaryNPCs;
        private static Dictionary<string, string> npcNames;
        private static Dictionary<string, KeyValuePair<string, Vector2>> farmBuildings;
        private static HashSet<NPCMarker> npcMarkers;

        // Multiplayer
        private static Dictionary<Farmer, Vector2> activeFarmers;
        private static Dictionary<long, KeyValuePair<string, Vector2>> farmerLocationChanges;

        // For debug info
        private const bool DEBUG_MODE = false;
        private static Vector2 _tileLower; 
        private static Vector2 _tileUpper; 
        private static string alertFlag; 

        public override void Entry(IModHelper helper)
        {
            modHelper = helper;
            monitor = this.Monitor;
            config = modHelper.ReadConfig<ModConfig>();
            markerCrop = ModConstants.MarkerCrop;
            CustomHandler = new ModCustomHandler(markerCrop);
            ModMain.buildingMarkers = ModMain.modHelper.Content.Load<Texture2D>(@"assets/buildings.png", ContentSource.ModFolder); // Load farm buildings

            SaveEvents.AfterLoad += SaveEvents_AfterLoad;
            TimeEvents.AfterDayStarted += TimeEvents_AfterDayStarted;
            GameEvents.UpdateTick += GameEvents_UpdateTick;
            LocationEvents.BuildingsChanged += LocationEvents_BuildingsChanged;
            InputEvents.ButtonPressed += InputEvents_ButtonPressed;
            MenuEvents.MenuClosed += MenuEvents_MenuClosed;
            GraphicsEvents.OnPostRenderEvent += GraphicsEvents_OnPostRenderEvent;
            GraphicsEvents.OnPostRenderGuiEvent += GraphicsEvents_OnPostRenderGuiEvent;
        }

        // Replace game map with modified map
        public bool CanLoad<T>(IAssetInfo asset)
        {
            return asset.AssetNameEquals(@"LooseSprites\Map");
        }

        public T Load<T>(IAssetInfo asset)
        {
            return (T)(object)ModMain.modHelper.Content.Load<Texture2D>($@"assets\{CustomHandler.LoadMap()}.png"); // Replace map page
        }

        // For drawing farm buildings on the map 
        // and getting positions relative to the farm 
        private void UpdateFarmBuildingLocs()
        {
            farmBuildings = new Dictionary<string, KeyValuePair<string, Vector2>>();

            foreach (Building building in Game1.getFarm().buildings)
            {
                if (building.nameOfIndoorsWithoutUnique == null 
                  || building.nameOfIndoors == null
                  || building.nameOfIndoors.Equals("null")) // Some actually have value of "null"
                {
                    continue;
                }

                Vector2 locVector = LocationToMap(
                    "Farm", // Get building position in farm
                    building.tileX.Value, 
                    building.tileY.Value
                ); 
                // Using buildingType instead of nameOfIndoorsWithoutUnique because it is a better subset of currentLocation.Name 
                // since nameOfIndoorsWithoutUnique for Barn/Coop does not use Big/Deluxe but rather the upgrade level
                string commonName = building.buildingType.Value ?? building.nameOfIndoorsWithoutUnique;

                if (commonName.Contains("Barn"))
                {
                    locVector.Y += 3;
                }

                // Format: { uniqueName: { commonName: positionOnFarm } }
                // buildingType will match currentLocation.Name for commonName
                farmBuildings[building.nameOfIndoors] = new KeyValuePair<string, Vector2>(building.buildingType.Value, locVector);
            }

            // Greenhouse unlocked after pantry bundles completed
            if (((CommunityCenter)Game1.getLocationFromName("CommunityCenter")).areasComplete[CommunityCenter.AREA_Pantry])
            {
                Vector2 locVector = ModMain.LocationToMap("Greenhouse");
                locVector.X -= 5 / 2 * BUILDING_SCALE;
                locVector.Y -= 7 / 2 * BUILDING_SCALE;
                farmBuildings["Greenhouse"] = new KeyValuePair<string, Vector2>("Greenhouse", locVector);
            }
        }

        private void LocationEvents_BuildingsChanged(object sender, EventArgsLocationBuildingsChanged e)
        {
            if (e.Location.Name.Equals("Farm"))
                UpdateFarmBuildingLocs();
        }

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
            CustomHandler.UpdateCustomNPCs();
            npcNames = CustomHandler.GetNPCNames();
            UpdateFarmBuildingLocs();
        }

        private NetCollection<NPC> GetAllVillagers()
        {
            var allNPCs = new NetCollection<NPC>();
 
            foreach (GameLocation location in Game1.locations)
            {
                foreach (NPC npc in location.characters)
                {
                    if (npc == null) { continue; }
                    if (!allNPCs.Contains(npc) 
                      && !ModConstants.ExcludedVillagers.Contains(npc.Name) 
                      && npc.isVillager())
                        allNPCs.Add(npc);
                }
            }
            return allNPCs;
        }

        // Handle opening mod menu and changing tooltip options
        private void InputEvents_ButtonPressed(object sender, EventArgsInput e)
        {
            if (Context.IsWorldReady && Game1.activeClickableMenu is GameMenu)
            {
                HandleInput((GameMenu)Game1.activeClickableMenu, e.Button);
            }
        }

        // Handle keyboard/controller inputs
        private void HandleInput(GameMenu menu, SButton input)
        {
            if (menu.currentTab != GameMenu.mapTab) { return; }
            if (Context.IsMainPlayer)
            {
                if (input.ToString().Equals(config.MenuKey) || input is SButton.ControllerY)
                {
                    Game1.activeClickableMenu = new ModMenu(
                        Game1.viewport.Width / 2 - (1100 + IClickableMenu.borderWidth * 2) / 2,
                        Game1.viewport.Height / 2 - (725 + IClickableMenu.borderWidth * 2) / 2,
                        1100 + IClickableMenu.borderWidth * 2,
                        650 + IClickableMenu.borderWidth * 2,
                        secondaryNPCs,
                        CustomHandler.GetCustomNPCs(),
                        npcNames,
                        markerCrop
                    );
                }
            }
            if (input.ToString().Equals(config.TooltipKey) || input is SButton.DPadUp || input is SButton.DPadRight)
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
                modHelper.WriteConfig(config);
            }
            else
            {
                if (--config.NameTooltipMode < 1)
                {
                    config.NameTooltipMode = 3;
                }
                modHelper.WriteConfig(config);
            }
        }

        // Handle any checks that need to be made per day
        private void TimeEvents_AfterDayStarted(object sender, EventArgs e)
        {
            var npcEntries = new Dictionary<string, bool>(secondaryNPCs);
            foreach (KeyValuePair<string, bool> npcEntry in npcEntries)
            {
                string name = npcEntry.Key;

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
            foreach (NPC npc in GetAllVillagers())
            {
                // Handle case where Kent appears even though he shouldn't
                if ((npc.Name.Equals("Kent") && !secondaryNPCs["Kent"])) { continue; }

                NPCMarker npcMarker = new NPCMarker() {
                    Npc = npc,
                    IsBirthday = npc.isBirthday(Game1.currentSeason, Game1.dayOfMonth)
                };
                npcMarkers.Add(npcMarker);
            }
        }

        // Map page updates
        private void GameEvents_UpdateTick(object sender, EventArgs e)
        {
            if (!Context.IsWorldReady) { return; }
            if (!(Game1.activeClickableMenu is GameMenu)) { return; }
            if (!IsMapOpen((GameMenu)Game1.activeClickableMenu)) { return; }

            if (Game1.options.SnappyMenus)
                modHelper.Reflection.GetField<Boolean>(Game1.options, "SnappyMenus").SetValue(false);

            if (Context.IsMainPlayer)
            {
                UpdateNPCMarkers();
            }

            UpdateActiveFarmersAsync();
            modMapPage = new ModMapPage(npcNames, npcMarkers, activeFarmers);
        }

        // Update NPC marker data and names on hover
        private void UpdateNPCMarkers()
        {
            foreach (NPCMarker npcMarker in npcMarkers)
            {
                NPC npc = npcMarker.Npc;
                string locationName;
                GameLocation npcLocation = npc.currentLocation;

                // Handle null locations at beginning of new day
                // !currentLocation is null for FarmHands in MP
                if (npcLocation == null) { 
                    locationName = npc.DefaultMap;
                    npcLocation = Game1.getLocationFromName(locationName);
                }
                else
                    locationName = npc.currentLocation.Name;

                if (locationName == null) { continue; }

                // Isn't mapped by the mod; skip
                if (!ModConstants.MapVectors.TryGetValue(locationName, out MapVector[] npcPos))
                    continue;


                // For layering indoor/outdoor NPCs and indoor indicator
                npcMarker.IsOutdoors = npcLocation.IsOutdoors;

                // For show NPCs in player's location option
                bool isSameLocation = false;

                if (config.OnlySameLocation)
                {
                    isSameLocation = locationName.Equals(Game1.player.currentLocation.Name);
                    // Check inside buildings and rooms
                    foreach (KeyValuePair<Point, string> door in Game1.player.currentLocation.doors.Pairs)
                    {
                        // Check buildings
                        if (door.Value.Equals(locationName))
                        {
                            isSameLocation = true;
                            break;
                        }
                        // Check rooms
                        else
                        {
                            foreach (KeyValuePair<Point, string> roomDoor in npcLocation.doors.Pairs)
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
                    int x = (int)GetMapPosition(npcLocation, npc.getTileX(), npc.getTileY()).X - width/2;
                    int y = (int)GetMapPosition(npcLocation, npc.getTileX(), npc.getTileY()).Y - height/2;

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
            modMapPage = new ModMapPage(npcNames, npcMarkers, activeFarmers);
        }

        // Async to handle mismatch of farmer position/location when changing locations
        // Locaction updates before position, which causes the farmer to jump 
        // to the old position in the the new map before 
        private static async void UpdateActiveFarmersAsync()
        {
            if (!Context.IsMultiplayer) { return; }

            activeFarmers = new Dictionary<Farmer, Vector2>();
            farmerLocationChanges = new Dictionary<long, KeyValuePair<string, Vector2>>();

            foreach (Farmer farmer in Game1.getOnlineFarmers())
            {
                if (farmer == null || (farmer != null && farmer.currentLocation == null)) { continue; }

                long farmerId = farmer.UniqueMultiplayerID;
                Vector2 farmerLoc;
                if (farmerLocationChanges.ContainsKey(farmerId))
                {
                    var deltaX = farmer.getTileX() - farmerLocationChanges[farmerId].Value.X;
                    var deltaY = farmer.getTileY() - farmerLocationChanges[farmerId].Value.Y;
                    if (farmerLocationChanges[farmerId].Key.Equals(farmer.currentLocation.Name) && MathHelper.Distance(deltaX, deltaY) < 10)
                    {
                        farmerLoc = ModMain.GetMapPosition(farmer.currentLocation, farmer.getTileX(), farmer.getTileY());
                        activeFarmers[farmer] = farmerLoc;
                    }
                    else
                    {
                        // This prevents the farmer "blinking" across the map before location change
                        await Task.Run(() =>
                        {
                            Task.Delay(1000).ContinueWith(t =>
                            {
                                farmerLocationChanges[farmerId] = new KeyValuePair<string, Vector2>(farmer.currentLocation.Name, new Vector2(farmer.getTileX(), farmer.getTileY()));
                                Vector2 farmerLoc2 = ModMain.GetMapPosition(farmer.currentLocation, farmer.getTileX(), farmer.getTileY());
                                activeFarmers[farmer] = farmerLoc2;
                            });
                        });
                        continue;
                    }
                }
                else
                {
                    var newLoc = new KeyValuePair<string, Vector2>(farmer.currentLocation.Name, new Vector2(farmer.getTileX(), farmer.getTileY()));
                    farmerLocationChanges.Add(farmer.UniqueMultiplayerID, newLoc);
                    farmerLoc = ModMain.GetMapPosition(farmer.currentLocation, farmer.getTileX(), farmer.getTileY());
                    activeFarmers[farmer] = farmerLoc;
                }
            }
        }

        // Helper method for LocationToMap
        private static Vector2 GetMapPosition(GameLocation location, int tileX, int tileY)
        {
            if (location == null || tileX < 0 || tileY < 0)
            {
                if (!Context.IsMultiplayer)
                {
                    if (alertFlag != "UnknownLocation:" + location.Name)
                    {
                        ModMain.monitor.Log("Unknown location: " + location.Name + ".", LogLevel.Trace);
                        alertFlag = "UnknownLocation:" + location.Name;
                    }
                }
                return new Vector2(-5000, -5000);
            }

            // Handle farm buildings
            // Match currentLocation.Name with buildingType 
            // and use uniqueName to get location of buildings with the same currentLocation.Name
            if (location.IsFarm && !location.Name.Equals("FarmHouse"))
            {
                if (location.uniqueName.Value != null 
                  && (farmBuildings[location.uniqueName.Value].Key.Equals(location.Name)
                  || farmBuildings[location.uniqueName.Value].Key.Contains("Cabin")))
                {
                    return farmBuildings[location.uniqueName.Value].Value;
                }         
            }

            return LocationToMap(location.Name, tileX, tileY);
        }

        // MAIN METHOD FOR PINPOINTING CHARACTERS ON THE MAP
        // Calculated from mapping of game tile positions to pixel coordinates of the map in MapModConstants. 
        // Requires MapModConstants and modified map page in ./assets
        public static Vector2 LocationToMap(string location, int tileX=-1, int tileY=-1)
        {
            if (!ModConstants.MapVectors.TryGetValue(location, out MapVector[] locVectors))
            {
                if (alertFlag != "UnknownLocation:" + location)
                {
                    ModMain.monitor.Log("Unknown location: " + location + ".", LogLevel.Trace);
                    alertFlag = "UnknownLocation:" + location;
                }
                return new Vector2(-5000, -5000);
            }

            Vector2 mapPagePos = Utility.getTopLeftPositionForCenteringOnScreen(300 * Game1.pixelZoom, 180 * Game1.pixelZoom, 0, 0);
            int x = 0;
            int y = 0;

            // Precise (static) regions and indoor locations
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
                    if (alertFlag != "NullBound:" + tilePos)
                    {
                        ModMain.monitor.Log("Null lower bound: No vector less than " + tilePos + " in " + location, LogLevel.Trace);
                        alertFlag = "NullBound:" + tilePos;
                    }

                    lower = upper == vectors.First() ? vectors.Skip(1).First() : vectors.First();
                }
                if (upper == null)
                {
                    if (alertFlag != "NullBound:" + tilePos)
                    {
                        ModMain.monitor.Log("Null upper bound: No vector greater than " + tilePos + " in " + location, LogLevel.Trace);
                        alertFlag = "NullBound:" + tilePos;
                    }

                    upper = lower == vectors.First() ? vectors.Skip(1).First() : vectors.First();
                }

                x = (int)(lower.x + (tileX - lower.tileX) / (double)(upper.tileX - lower.tileX) * (upper.x - lower.x));
                y = (int)(lower.y + (tileY - lower.tileY) / (double)(upper.tileY - lower.tileY) * (upper.y - lower.y));

                if (DEBUG_MODE)
                {
                    #pragma warning disable CS0162 // Unreachable code detected
                    ModMain._tileUpper = new Vector2(upper.tileX, upper.tileY);
                    #pragma warning restore CS0162 // Unreachable code detected
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

        // Draw event (when Map Page is opened)
        private void GraphicsEvents_OnPostRenderGuiEvent(object sender, EventArgs e)
        {
            if (!Context.IsWorldReady) { return; }
            if (Game1.activeClickableMenu == null || !(Game1.activeClickableMenu is GameMenu)) { return; }
            if (!IsMapOpen((GameMenu)Game1.activeClickableMenu)) { return; }

            DrawMapPage();
        }

        // Draw event
        // Subtractions within location vectors are to set the origin to the center of the sprite
        static void DrawMapPage()
        {
            SpriteBatch b = Game1.spriteBatch;

            if (modMapPage == null) { return; }
            modMapPage.DrawMap(b);
         
            if (config.ShowFarmBuildings)
            {
                var sortedBuildings = farmBuildings.ToList();
                sortedBuildings.Sort((x, y) => x.Value.Value.Y.CompareTo(y.Value.Value.Y));

                foreach (KeyValuePair<string, KeyValuePair<string, Vector2>> building in sortedBuildings)
                {
                    if (ModConstants.FarmBuildingRects.TryGetValue(building.Value.Key, out Rectangle buildingRect))
                        b.Draw(buildingMarkers, new Vector2(building.Value.Value.X - buildingRect.Width/2, building.Value.Value.Y - buildingRect.Height/2), new Rectangle?(buildingRect), Color.White, 0f, Vector2.Zero, BUILDING_SCALE, SpriteEffects.None, 1f);
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
                if (npcMarker.Location == Rectangle.Empty || npcMarker.Marker == null || !markerCrop.ContainsKey(npcMarker.Npc.Name)) { continue; }

                // Tint/dim hidden markers
                if (npcMarker.IsHidden)
                {
                    b.Draw(npcMarker.Marker, npcMarker.Location, new Rectangle?(new Rectangle(0, markerCrop[npcMarker.Npc.Name], 16, 15)), Color.DimGray * 0.7f);
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
                    b.Draw(npcMarker.Marker, npcMarker.Location, new Rectangle?(new Rectangle(0, markerCrop[npcMarker.Npc.Name], 16, 15)), Color.White);
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

            // Farmers
            if (Context.IsMultiplayer)
            {
                foreach (KeyValuePair<Farmer, Vector2> farmer in activeFarmers)
                    farmer.Key.FarmerRenderer.drawMiniPortrat(b, new Vector2(farmer.Value.X - 16, farmer.Value.Y - 15), 0.00011f, 2f, 1, farmer.Key);
            }
            else
            {
                Vector2 playerLoc = ModMain.GetMapPosition(Game1.player.currentLocation, Game1.player.getTileX(), Game1.player.getTileY());
                Game1.player.FarmerRenderer.drawMiniPortrat(b, new Vector2(playerLoc.X - 16, playerLoc.Y - 15), 0.00011f, 2f, 1, Game1.player);
            }

            // Location and name tooltips
            modMapPage.draw(b);

            // Cursor
            if (!Game1.options.hardwareCursor)
            {
                b.Draw(Game1.mouseCursors, new Vector2(Game1.getOldMouseX(), Game1.getOldMouseY()), new Rectangle?(Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, (Game1.options.gamepadControls ? 44 : 0), 16, 16)), Color.White, 0f, Vector2.Zero, Game1.pixelZoom + Game1.dialogueButtonScale / 150f, SpriteEffects.None, 1f);
            }

        }

        // Hack to disable snappy menu with MapPage since ModMapPage doesn't replace the menu
        // and thus cannot override snappyMovement
        private void MenuEvents_MenuClosed(object sender, EventArgsClickableMenuClosed e)
        {
            if (!Context.IsWorldReady || Game1.options == null) { return; }

            if (e.PriorMenu is GameMenu menu)
            {
                // Reset option after map sets option to false
                if (IsMapOpen(menu))
                {
                    modHelper.Reflection.GetField<Boolean>(Game1.options, "snappyMenus").SetValue(snappyMenuOption);
                }
                else
                // Handle any option changes by the player
                // Caveat: If player is turning snappy menu on, they MUST close the menu first to update the stored menu option
                {
                    snappyMenuOption = Game1.options.SnappyMenus;
                }
            }
        }

        // For debugging
        private void GraphicsEvents_OnPostRenderEvent(object sender, EventArgs e)
        {
            if (!Context.IsWorldReady || Game1.player == null) { return; }
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
        private bool IsNPCShown(string npc)
        {
            bool showNPC = !config.NPCBlacklist.Contains(npc);   
            if (!CustomHandler.GetCustomNPCs().ContainsKey(npc)) {
                if (npc.Equals("Sandy")) { return showNPC && secondaryNPCs["Sandy"]; }
                else if (npc.Equals("Marlon")) { return showNPC && secondaryNPCs["Marlon"]; }
                else if (npc.Equals("Wizard")) { return showNPC && secondaryNPCs["Wizard"]; }
                else return showNPC;
            }
            else 
            {
                var customNPCs = CustomHandler.GetCustomNPCs();
                for (int i = 0; i < customNPCs.Count; i++) 
                {
                    if (customNPCs.Keys.ElementAt(i).Equals(npc))
                    {
                        return config.CustomNPCBlacklist.Contains(npc);
                    }
                }
            }
            return true;
        }
    }

    // Class for NPC markers
    public class NPCMarker
    {
        public NPC Npc { get; set; } = null;
        public Texture2D Marker { get; set; } = null;
        public Rectangle Location { get; set; } = new Rectangle();
        public bool IsBirthday { get; set; } = false;
        public bool HasQuest { get; set; } = false;
        public bool IsOutdoors { get; set; } = true;
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