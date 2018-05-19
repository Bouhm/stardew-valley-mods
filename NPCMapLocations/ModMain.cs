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
using Netcode;
using Harmony;

namespace NPCMapLocations
{
    public class ModMain : Mod, IAssetLoader
    {
        private ModConfig Config;
        private Texture2D BuildingMarkers;
        private ModCustomHandler CustomHandler;
        private ModMapPage ModMapPage;
        private Dictionary<string, int> MarkerCrop; // NPC head crops, top left corner (0, y), width = 16, height = 15 
        private Dictionary<string, bool> SecondaryNpcs;
        private Dictionary<string, string> NpcNames;
        private HashSet<NPCMarker> NpcMarkers;
        private static Dictionary<string, KeyValuePair<string, Vector2>> FarmBuildings;
        private const int BUILDING_SCALE = 3;
        private const int DRAW_DELAY = 1;
        private bool IsUpdating = false;

        // Multiplayer
        private Dictionary<long, FarmerMarker> FarmerMarkers; 

        // For debug info
        private const bool DEBUG_MODE = false;
        private static Vector2 _tileLower;
        private static Vector2 _tileUpper;
        private static string alertFlag;

        public override void Entry(IModHelper helper)
        {
            Config = this.Helper.ReadConfig<ModConfig>();
            MarkerCrop = ModConstants.MarkerCrop;
            CustomHandler = new ModCustomHandler(helper, Config, this.Monitor);
            BuildingMarkers = this.Helper.Content.Load<Texture2D>(@"assets/buildings.png", ContentSource.ModFolder); // Load farm buildings

            SaveEvents.AfterLoad += SaveEvents_AfterLoad;
            TimeEvents.AfterDayStarted += TimeEvents_AfterDayStarted;
            LocationEvents.BuildingsChanged += LocationEvents_BuildingsChanged;
            InputEvents.ButtonPressed += InputEvents_ButtonPressed;
            MenuEvents.MenuChanged += MenuEvents_MenuChanged;
            GameEvents.QuarterSecondTick += GameEvents_UpdateTick;
            GraphicsEvents.OnPostRenderEvent += GraphicsEvents_OnPostRenderEvent;
            GraphicsEvents.OnPostRenderGuiEvent += GraphicsEvents_OnPostRenderGuiEvent;
            GraphicsEvents.Resize += GraphicsEvents_Resize;
        }

        // Replace game map with modified map
        public bool CanLoad<T>(IAssetInfo asset)
        {
            return asset.AssetNameEquals(@"LooseSprites\Map");
        }

        public T Load<T>(IAssetInfo asset)
        {
            T map;
            string mapName = CustomHandler.LoadMap();
            try
            {
                if (!mapName.Equals("default_map"))
                    this.Monitor.Log($"Detected recolored map {CustomHandler.LoadMap()}.", LogLevel.Info);

                map = (T)(object)this.Helper.Content.Load<T>($@"assets\{mapName}.png"); // Replace map page
            }
            catch
            {
                this.Monitor.Log($"Unable to find {mapName}; loaded default map instead.", LogLevel.Info);
                map = (T)(object)this.Helper.Content.Load<T>($@"assets\default_map.png");
            }
            return map;
        }

        void MenuEvents_MenuChanged(object sender, EventArgsClickableMenuChanged e)
        {
            if (IsMapOpen()) {
                UpdateMarkers();
                ModMapPage = new ModMapPage(NpcMarkers, NpcNames, FarmerMarkers, Helper, Config);   
            }
        }

            
        // For drawing farm buildings on the map 
        // and getting positions relative to the farm 
        private static void UpdateFarmBuildingLocs()
        {
            FarmBuildings = new Dictionary<string, KeyValuePair<string, Vector2>>();

            foreach (Building building in Game1.getFarm().buildings)
            {
                if (building == null) { continue; }
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
                FarmBuildings[building.nameOfIndoors] = new KeyValuePair<string, Vector2>(building.buildingType.Value, locVector);
            }

            // Greenhouse unlocked after pantry bundles completed
            if (((CommunityCenter)Game1.getLocationFromName("CommunityCenter")).areasComplete[CommunityCenter.AREA_Pantry])
            {
                Vector2 locVector = ModMain.LocationToMap("Greenhouse");
                locVector.X -= 5 / 2 * BUILDING_SCALE;
                locVector.Y -= 7 / 2 * BUILDING_SCALE;
                FarmBuildings["Greenhouse"] = new KeyValuePair<string, Vector2>("Greenhouse", locVector);
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
            SecondaryNpcs = new Dictionary<string, bool>
            {
                { "Kent", false },
                { "Marlon", false },
                { "Merchant", false },
                { "Sandy", false },
                { "Wizard", false }
            };
            CustomHandler.UpdateCustomNpcs();
            NpcNames = CustomHandler.GetNpcNames();
            UpdateFarmBuildingLocs();
        }

        private List<NPC> GetVillagers()
        {
            var allNpcs = new List<NPC>();
 
            foreach (GameLocation location in Game1.locations)
            {
                foreach (NPC npc in location.characters)
                {
                    if (npc == null) { continue; }
                    if (!allNpcs.Contains(npc) 
                      && !ModConstants.ExcludedVillagers.Contains(npc.Name) 
                      && npc.isVillager())
                        allNpcs.Add(npc);
                }
            }
            return allNpcs;
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
                if (input.ToString().Equals(Config.MenuKey) || input is SButton.ControllerY)
                {
                    Game1.activeClickableMenu = new ModMenu(
                        Game1.viewport.Width / 2 - (1100 + IClickableMenu.borderWidth * 2) / 2,
                        Game1.viewport.Height / 2 - (725 + IClickableMenu.borderWidth * 2) / 2,
                        1100 + IClickableMenu.borderWidth * 2,
                        650 + IClickableMenu.borderWidth * 2,
                        SecondaryNpcs,
                        CustomHandler.GetCustomNpcs(),
                        NpcNames,
                        MarkerCrop,
                        this.Helper,
                        this.Config
                    );
                }
            }
            if (input.ToString().Equals(Config.TooltipKey) || input is SButton.DPadUp || input is SButton.DPadRight)
            {
                ChangeTooltipConfig();
            }
            else if (input.ToString().Equals(Config.TooltipKey) || input is SButton.DPadDown || input is SButton.DPadLeft)
            {
                ChangeTooltipConfig(false);
            }
        }

        private void ChangeTooltipConfig(bool incre = true)
        {
            if (incre)
            {
                if (++Config.NameTooltipMode > 3)
                {
                    Config.NameTooltipMode = 1;
                }
                this.Helper.WriteConfig(Config);
            }
            else
            {
                if (--Config.NameTooltipMode < 1)
                {
                    Config.NameTooltipMode = 3;
                }
                this.Helper.WriteConfig(Config);
            }
        }

        // Handle any checks that need to be made per day
        private void TimeEvents_AfterDayStarted(object sender, EventArgs e)
        {
            var npcEntries = new Dictionary<string, bool>(SecondaryNpcs);
            foreach (KeyValuePair<string, bool> npcEntry in npcEntries)
            {
                string name = npcEntry.Key;

                if (!npcEntry.Value)
                {
                    switch (name)
                    {
                        case "Kent":
                            SecondaryNpcs[name] = Game1.year >= 2;
                            break;
                        case "Marlon":
                            SecondaryNpcs[name] = Game1.player.eventsSeen.Contains(100162);
                            break;
                        case "Merchant":
                            SecondaryNpcs[name] =
                                      (Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth).Equals("Friday")
                                    || Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth).Equals("Sunday"));
                            break;
                        case "Sandy":
                            SecondaryNpcs[name] = Game1.player.mailReceived.Contains("ccVault");
                            break;
                        case "Wizard":
                            SecondaryNpcs[name] = Game1.player.eventsSeen.Contains(112);
                            break;
                        default: break;

                    }
                }
            }

            // Reset NPC marker data daily
            NpcMarkers = new HashSet<NPCMarker>();
            foreach (NPC npc in GetVillagers())
            {
                // Handle case where Kent appears even though he shouldn't
                if ((npc.Name.Equals("Kent") && !SecondaryNpcs["Kent"])) { continue; }

                NPCMarker npcMarker = new NPCMarker() {
                    Npc = npc,
                    IsBirthday = npc.isBirthday(Game1.currentSeason, Game1.dayOfMonth)
                };
                NpcMarkers.Add(npcMarker);
            }

            if (Context.IsMultiplayer) 
                FarmerMarkers = new Dictionary<long, FarmerMarker>();
        }

        // Map page updates
        void GameEvents_UpdateTick(object sender, EventArgs e) 
        {
            UpdateMarkers();
        }

        private void UpdateMarkers()
        {
            // Prevent double update when
            // Map open + quarter tick update
            if (IsMapOpen() && !IsUpdating) {
                IsUpdating = true;

                if (Context.IsMainPlayer)
                    UpdateNPCMarkers();
                if (Context.IsMultiplayer)
                    UpdateFarmerMarkers();
                if (ModMapPage != null)
                    ModMapPage.RecieveMarkerUpdates(NpcMarkers, FarmerMarkers);
                
                IsUpdating = false;
            }
        }

        // Update NPC marker data and names on hover
        private void UpdateNPCMarkers()
        {
            foreach (NPCMarker npcMarker in NpcMarkers)
            {
                NPC npc = npcMarker.Npc;
                string locationName;
                GameLocation npcLocation = npc.currentLocation;

                // Handle null locations at beginning of new day
                if (npcLocation == null) { 
                    locationName = npc.DefaultMap;
                    npcLocation = Game1.getLocationFromName(locationName);
                }
                else
                    locationName = npc.currentLocation.Name;

                if (npcMarker.Location != Rectangle.Empty 
                    && (!npcLocation.IsOutdoors // Indoors
                    || !npcMarker.Npc.isMoving() // Not moving
                    || locationName == null // Couldn't resolve location name
                    || !ModConstants.MapVectors.TryGetValue(locationName, out MapVector[] npcPos))) // Location not mapped
                    continue;
                             
                // For layering indoor/outdoor NPCs and indoor indicator
                npcMarker.IsOutdoors = npcLocation.IsOutdoors;

                // For show Npcs in player's location option
                bool isSameLocation = false;

                if (Config.OnlySameLocation)
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

                // NPCs that won't be shown on the map unless Show Hidden Npcs is checked
                npcMarker.IsHidden = (
                   (Config.ImmersionOption == 2 && !Game1.player.hasTalkedToFriendToday(npc.Name))
                    || (Config.ImmersionOption == 3 && Game1.player.hasTalkedToFriendToday(npc.Name))
                    || (Config.OnlySameLocation && !isSameLocation)
                    || (Config.ByHeartLevel
                        && !(Game1.player.getFriendshipHeartLevelForNPC(npc.Name)
                        >= Config.HeartLevelMin && Game1.player.getFriendshipHeartLevelForNPC(npc.Name)
                        <= Config.HeartLevelMax)
                        )
                );

                // NPCs that will be drawn onto the map
                if (IsNPCShown(npc.Name) && (Config.ShowHiddenVillagers || !npcMarker.IsHidden))
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
                        if (quest.accepted.Value && quest.dailyQuest.Value && !quest.completed.Value)
                        {
                            switch (quest.questType.Value)
                            {
                                case 3:
                                    npcMarker.HasQuest = (((ItemDeliveryQuest)quest).target.Value == npc.Name);
                                    break;
                                case 4:
                                    npcMarker.HasQuest = (((SlayMonsterQuest)quest).target.Value == npc.Name);
                                    break;
                                case 7:
                                    npcMarker.HasQuest = (((FishingQuest)quest).target.Value == npc.Name);
                                    break;
                                case 10:
                                    npcMarker.HasQuest = (((ResourceCollectionQuest)quest).target.Value == npc.Name);
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
                    npcMarker.Location = Rectangle.Empty;
                }
            }
        }

        private void UpdateFarmerMarkers() {
            foreach (Farmer farmer in Game1.getOnlineFarmers())
            {
                if (farmer == null || (farmer != null && farmer.currentLocation == null)) { continue; }
                     
                long farmerId = farmer.UniqueMultiplayerID;
                Vector2 farmerLoc = GetMapPosition(farmer.currentLocation, farmer.getTileX(), farmer.getTileY());

                if (FarmerMarkers.TryGetValue(farmer.UniqueMultiplayerID, out FarmerMarker farMarker)) {
                    // Location changes before tile position, causing farmhands to blink
                    // to the wrong position upon entering new location. Handle this in draw.
                    if (farmer.currentLocation.Name != farMarker.PrevLocationName)
                        FarmerMarkers[farmerId].DrawDelay = DRAW_DELAY;
                    else if (farMarker.DrawDelay > 0)
                        FarmerMarkers[farmerId].DrawDelay--;
                }
                else 
                {
                    FarmerMarker newMarker = new FarmerMarker
                    {
                        Name = farmer.Name,
                        DrawDelay = 0
                    };

                    FarmerMarkers.Add(farmerId, newMarker);
                }

                FarmerMarkers[farmerId].Location = farmerLoc;
                FarmerMarkers[farmerId].PrevLocationName = farmer.currentLocation.Name;
                FarmerMarkers[farmerId].IsOutdoors = farmer.currentLocation.IsOutdoors;
            }
        }

        // Helper method for LocationToMap
        private static Vector2 GetMapPosition(GameLocation location, int tileX, int tileY)
        {
            if (location == null || tileX < 0 || tileY < 0)
            {
                return Vector2.Zero;
            }

            // Handle farm buildings
            // Match currentLocation.Name with buildingType 
            // and use uniqueName to get location of buildings with the same currentLocation.Name
            if (location.IsFarm && !location.Name.Equals("FarmHouse"))
            {
                if (location.uniqueName.Value != null 
                  && (ModMain.FarmBuildings[location.uniqueName.Value].Key.Equals(location.Name)
                  || ModMain.FarmBuildings[location.uniqueName.Value].Key.Contains("Cabin")))
                {
                    return ModMain.FarmBuildings[location.uniqueName.Value].Value;
                }         
            }

            return LocationToMap(location.Name, tileX, tileY);
        }

        // MAIN METHOD FOR PINPOINTING CHARACTERS ON THE MAP
        // Calculated from mapping of game tile positions to pixel coordinates of the map in MapModConstants. 
        // Requires MapModConstants and modified map page in ./assets
        public static Vector2 LocationToMap(string location, int tileX=-1, int tileY=-1, IMonitor monitor=null)
        {
            if (!ModConstants.MapVectors.TryGetValue(location, out MapVector[] locVectors))
            {
                if (monitor != null && alertFlag != "UnknownLocation:" + location)
                {
                    monitor.Log("Unknown location: " + location + ".", LogLevel.Trace);
                    alertFlag = "UnknownLocation:" + location;
                }
                return Vector2.Zero;
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
                    if (monitor != null && alertFlag != "NullBound:" + tilePos)
                    {
                        monitor.Log("Null lower bound: No vector less than " + tilePos + " in " + location, LogLevel.Trace);
                        alertFlag = "NullBound:" + tilePos;
                    }

                    lower = upper == vectors.First() ? vectors.Skip(1).First() : vectors.First();
                }
                if (upper == null)
                {
                    if (monitor != null && alertFlag != "NullBound:" + tilePos)
                    {
                        monitor.Log("Null upper bound: No vector greater than " + tilePos + " in " + location, LogLevel.Trace);
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
        private bool IsMapOpen()
        {
            if (!Context.IsWorldReady) return false; 
            if (Game1.activeClickableMenu == null || !(Game1.activeClickableMenu is GameMenu gameMenu)) return false;

            IList<IClickableMenu> pages = this.Helper.Reflection.GetField<List<IClickableMenu>>(gameMenu, "pages").GetValue();
            IClickableMenu page = pages[gameMenu.currentTab];
            return (page is MapPage);
        }

        // Draw event (when Map Page is opened)
        private void GraphicsEvents_OnPostRenderGuiEvent(object sender, EventArgs e)
        {
            if (IsMapOpen() && ModMapPage != null)
                DrawMapPage();
        }

        // Draw event
        // Subtractions within location vectors are to set the origin to the center of the sprite
        private void DrawMapPage()
        {
            SpriteBatch b = Game1.spriteBatch;

            if (ModMapPage == null) { return; }
            ModMapPage.DrawMap(b);
         
            if (Config.ShowFarmBuildings)
            {
                var sortedBuildings = FarmBuildings.ToList();
                sortedBuildings.Sort((x, y) => x.Value.Value.Y.CompareTo(y.Value.Value.Y));

                foreach (KeyValuePair<string, KeyValuePair<string, Vector2>> building in sortedBuildings)
                {
                    if (ModConstants.FarmBuildingRects.TryGetValue(building.Value.Key, out Rectangle buildingRect))
                        b.Draw(BuildingMarkers, new Vector2(building.Value.Value.X - buildingRect.Width/2, building.Value.Value.Y - buildingRect.Height/2), new Rectangle?(buildingRect), Color.White, 0f, Vector2.Zero, BUILDING_SCALE, SpriteEffects.None, 1f);
                }
            }

            // Traveling Merchant
            if (Config.ShowTravelingMerchant && SecondaryNpcs["Merchant"])
            {
                Vector2 merchantLoc = LocationToMap("Forest", 27, 11);
                b.Draw(Game1.mouseCursors, new Vector2(merchantLoc.X - 16, merchantLoc.Y - 15), new Rectangle?(new Rectangle(191, 1410, 22, 21)), Color.White, 0f, Vector2.Zero, 1.3f, SpriteEffects.None, 1f);
            }

            // Farmers
            if (Context.IsMultiplayer)
            {
                foreach (Farmer farmer in Game1.getOnlineFarmers())
                {
                    // Temporary solution to handle desync of farmhand location/tile position when changing location
                    if (FarmerMarkers.TryGetValue(farmer.UniqueMultiplayerID, out FarmerMarker farMarker))
                    {
                        if (farMarker.DrawDelay == 0)
                            farmer.FarmerRenderer.drawMiniPortrat(b, new Vector2(farMarker.Location.X - 16, farMarker.Location.Y - 15), 0.00011f, 2f, 1, farmer);
                    }
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

            // Location and name tooltips
            ModMapPage.draw(b);

            // Cursor
            if (!Game1.options.hardwareCursor)
            {
                b.Draw(Game1.mouseCursors, new Vector2(Game1.getOldMouseX(), Game1.getOldMouseY()), new Rectangle?(Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, (Game1.options.gamepadControls ? 44 : 0), 16, 16)), Color.White, 0f, Vector2.Zero, Game1.pixelZoom + Game1.dialogueButtonScale / 150f, SpriteEffects.None, 1f);
            }
        }

        private void GraphicsEvents_Resize(object sender, EventArgs e)
        {
            if (!Context.IsWorldReady) { return; }
            UpdateFarmBuildingLocs();
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
            bool showNPC = !Config.NpcBlacklist.Contains(npc);   
            if (!CustomHandler.GetCustomNpcs().ContainsKey(npc)) {
                if (npc.Equals("Sandy")) { return showNPC && SecondaryNpcs["Sandy"]; }
                else if (npc.Equals("Marlon")) { return showNPC && SecondaryNpcs["Marlon"]; }
                else if (npc.Equals("Wizard")) { return showNPC && SecondaryNpcs["Wizard"]; }
                else return showNPC;
            }
            else 
            {
                var customNpcs = CustomHandler.GetCustomNpcs();
                for (int i = 0; i < customNpcs.Count; i++) 
                {
                    if (customNpcs.Keys.ElementAt(i).Equals(npc))
                    {
                        return Config.CustomNpcBlacklist.Contains(npc);
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
        public Rectangle Location { get; set; } = Rectangle.Empty;
        public bool IsBirthday { get; set; } = false;
        public bool HasQuest { get; set; } = false;
        public bool IsOutdoors { get; set; } = true;
        public bool IsHidden { get; set; } = false;
        public int Layer { get; set; } = 0;
    }

    // Class for Active Farmers
    public class FarmerMarker
    {
        public string Name { get; set; } = null;
        public Vector2 Location { get; set; } = Vector2.Zero;
        public string PrevLocationName { get; set; } = "";
        public bool IsOutdoors { get; set; } = true;
        public int DrawDelay { get; set; } = 0;
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