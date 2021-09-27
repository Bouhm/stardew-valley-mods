using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bouhm.Shared.Locations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using NPCMapLocations.Framework;
using NPCMapLocations.Framework.Menus;
using NPCMapLocations.Framework.Models;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Quests;

namespace NPCMapLocations
{
    public class ModEntry : Mod, IAssetLoader
    {
        public static PlayerConfig Config;
        public static GlobalConfig Globals;
        public static IModHelper StaticHelper;
        public static Texture2D Map;
        public static Vector2 Unknown = new(-9999, -9999);
        private static Dictionary<string, KeyValuePair<string, Vector2>> FarmBuildings;

        private readonly PerScreen<Texture2D> BuildingMarkers = new();
        private readonly PerScreen<Dictionary<string, MapVector[]>> MapVectors = new();
        private readonly PerScreen<ModMinimap> Minimap = new();
        private readonly PerScreen<Dictionary<string, NpcMarker>> NpcMarkers = new();
        private readonly PerScreen<Dictionary<string, bool>> ConditionalNpcs = new();
        private readonly PerScreen<bool> HasOpenedMap = new();
        private readonly PerScreen<bool> IsModMapOpen = new();

        /// <summary>Scans and maps locations in the game world.</summary>
        private static LocationUtil LocationUtil;

        // External mod settings
        private readonly string MapFilePath = @"LooseSprites\Map";
        private readonly string NpcCustomizationsPath = "Mods/Bouhm.NPCMapLocations/NPCs";
        private readonly string LocationCustomizationsPath = "Mods/Bouhm.NPCMapLocations/Locations";

        // Multiplayer
        private readonly PerScreen<Dictionary<long, FarmerMarker>> FarmerMarkers = new();

        // Customizations/Custom mods
        private string MapSeason;
        private ModCustomizations Customizations;

        // Debugging
        private bool DebugMode;

        // Replace game map with modified map
        public bool CanLoad<T>(IAssetInfo asset)
        {
            return (
              asset.AssetNameEquals(this.MapFilePath) ||
              asset.AssetNameEquals(this.NpcCustomizationsPath) ||
              asset.AssetNameEquals(this.LocationCustomizationsPath)
            );
        }

        public T Load<T>(IAssetInfo asset)
        {
            if (asset.AssetNameEquals(this.MapFilePath))
            {
                if (this.MapSeason == null)
                {
                    this.Monitor.Log("Unable to get current season. Defaulted to spring.", LogLevel.Debug);
                    this.MapSeason = "spring";
                }

                if (!File.Exists(Path.Combine(this.Helper.DirectoryPath, this.Customizations.MapsPath, $"{this.MapSeason}_map.png")))
                {
                    this.Monitor.Log("Seasonal maps not provided. Defaulted to spring.", LogLevel.Debug);
                    this.MapSeason = null; // Set to null so that cache is not invalidated when game season changes
                }

                // Replace map page
                string defaultMapFile = File.Exists(Path.Combine(this.Helper.DirectoryPath, this.Customizations.MapsPath, "spring_map.png")) ? "spring_map.png" : "map.png";
                string filename = this.MapSeason == null ? defaultMapFile : $"{this.MapSeason}_map.png";

                bool useRecolor = this.Customizations.MapsPath != null && File.Exists(Path.Combine(this.Helper.DirectoryPath, this.Customizations.MapsPath, filename));
                T map = useRecolor
                    ? this.Helper.Content.Load<T>(Path.Combine(this.Customizations.MapsPath, filename))
                    : this.Helper.Content.Load<T>(Path.Combine(this.Customizations.MapsRootPath, "_default", filename));

                if (useRecolor)
                    this.Monitor.Log($"Using {Path.Combine(this.Customizations.MapsPath, filename)}.", LogLevel.Debug);

                return map;
            }
            else if (asset.AssetNameEquals(this.LocationCustomizationsPath) || asset.AssetNameEquals(this.NpcCustomizationsPath))
            {
                return (T)(object)new Dictionary<string, JObject>();
            }

            return (T)asset;
        }


        public override void Entry(IModHelper helper)
        {
            ModEntry.LocationUtil = new(this.Monitor);
            StaticHelper = helper;
            Globals = helper.Data.ReadJsonFile<GlobalConfig>("config/globals.json") ?? new GlobalConfig();
            this.Customizations = new ModCustomizations();

            helper.Events.GameLoop.SaveLoaded += this.GameLoop_SaveLoaded;
            helper.Events.GameLoop.DayStarted += this.GameLoop_DayStarted;
            helper.Events.World.BuildingListChanged += this.World_BuildingListChanged;
            helper.Events.Input.ButtonPressed += this.Input_ButtonPressed;
            helper.Events.Input.ButtonReleased += this.Input_ButtonReleased;
            helper.Events.GameLoop.UpdateTicked += this.GameLoop_UpdateTicked;
            helper.Events.Player.Warped += this.Player_Warped;
            helper.Events.Display.MenuChanged += this.Display_MenuChanged;
            helper.Events.Display.RenderingHud += this.Display_RenderingHud;
            helper.Events.Display.RenderedWorld += this.Display_RenderedWorld;
            helper.Events.Display.Rendered += this.Display_Rendered;
            helper.Events.Display.WindowResized += this.Display_WindowResized;
            helper.Events.Multiplayer.PeerConnected += this.Multiplayer_PeerConnected;
            helper.Events.Multiplayer.ModMessageReceived += this.Multiplayer_ModMessageReceived;
        }

        // Load config and other one-off data
        private void GameLoop_SaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            Config = this.Helper.Data.ReadJsonFile<PlayerConfig>($"config/{Constants.SaveFolderName}.json") ?? new PlayerConfig();

            // Initialize these early for multiplayer sync
            this.NpcMarkers.Value = new Dictionary<string, NpcMarker>();
            this.FarmerMarkers.Value = new Dictionary<long, FarmerMarker>();

            if (!(Context.IsSplitScreen && !Context.IsMainPlayer))
            {
                // Load customizations
                var npcSettings = this.Helper.Content.Load<Dictionary<string, JObject>>(this.NpcCustomizationsPath, ContentSource.GameContent);
                var locationSettings = this.Helper.Content.Load<Dictionary<string, JObject>>(this.LocationCustomizationsPath, ContentSource.GameContent);
                this.Customizations.LoadCustomData(npcSettings, locationSettings);
            }

            // Load farm buildings
            try
            {
                this.BuildingMarkers.Value = this.Helper.Content.Load<Texture2D>(Path.Combine(this.Customizations.MapsPath, "buildings.png"));
            }
            catch
            {
                this.BuildingMarkers.Value = File.Exists(Path.Combine("maps/_default", "buildings.png"))
                    ? this.Helper.Content.Load<Texture2D>(Path.Combine("maps/_default", "buildings.png"))
                    : null;
            }

            // Get season for map
            this.MapSeason = Globals.UseSeasonalMaps ? Game1.currentSeason : "spring";
            this.Helper.Content.InvalidateCache(this.MapFilePath);
            Map = Game1.content.Load<Texture2D>(this.MapFilePath);

            // Disable for multiplayer for anti-cheat
            this.DebugMode = Globals.DebugMode && !Context.IsMultiplayer;

            // NPCs that player should meet before being shown
            this.ConditionalNpcs.Value = new Dictionary<string, bool>();
            foreach (string npcName in ModConstants.ConditionalNpcs)
                this.ConditionalNpcs.Value[npcName] = Game1.player.friendshipData.ContainsKey(npcName);

            this.MapVectors.Value = ModConstants.MapVectors;

            // Add custom map vectors from content.json
            foreach (var locVectors in this.Customizations.MapVectors)
                this.MapVectors.Value[locVectors.Key] = locVectors.Value;

            // Get context of all locations (indoor, outdoor, relativity)
            LocationUtil.GetLocationContexts();

            // Log any custom locations not handled in content.json
            try
            {
                string alertStr = "Unknown locations:";
                foreach (var locCtx in LocationUtil.LocationContexts)
                {
                    if (
                      (locCtx.Value.Root == null && !this.MapVectors.Value.ContainsKey(locCtx.Key))
                      || (locCtx.Value.Root != null && !this.MapVectors.Value.ContainsKey(locCtx.Value.Root))
                      && (locCtx.Value.Type != LocationType.Building || locCtx.Value.Type != LocationType.Room)
                    )
                    {
                        if (this.Customizations.LocationExclusions.Contains(locCtx.Key)) return;
                        alertStr += $" {locCtx.Key},";
                    }
                }

                this.Monitor.Log(alertStr.TrimEnd(',') + ".", LogLevel.Debug);
            }
            catch
            {
                this.Monitor.Log("Too many unknown locations; NPCs in unknown locations will not be visible.", LogLevel.Debug);
            }


            this.UpdateFarmBuildingLocations();

            // Log warning if host does not have mod installed
            if (Context.IsMultiplayer)
            {
                bool hostHasMod = false;

                foreach (IMultiplayerPeer peer in this.Helper.Multiplayer.GetConnectedPlayers())
                {
                    if (peer.GetMod("Bouhm.NPCMapLocations") != null && peer.IsHost)
                    {
                        hostHasMod = true;
                        break;
                    }
                }

                if (!hostHasMod && !Context.IsMainPlayer)
                    this.Monitor.Log("Since the server host does not have NPCMapLocations installed, NPC locations cannot be synced.", LogLevel.Warn);
            }
        }

        // Get only relevant villagers for map
        private List<NPC> GetVillagers()
        {
            var villagers = new List<NPC>();

            foreach (var location in Game1.locations)
            {
                foreach (var npc in location.characters)
                {
                    bool shouldTrack =
                        npc != null
                        && !ModConstants.ExcludedNpcs.Contains(npc.Name) // note: don't check Globals.NPCExclusions here, so player can still reenable them in the map options UI
                        && (
                            npc.isVillager()
                            || npc.isMarried()
                            || (Globals.ShowHorse && npc is Horse)
                            || (Globals.ShowChildren && npc is Child)
                        );

                    if (shouldTrack && !villagers.Contains(npc))
                        villagers.Add(npc);
                }
            }

            return villagers;
        }

        // For drawing farm buildings on the map 
        // and getting positions relative to the farm 
        private void UpdateFarmBuildingLocations()
        {
            FarmBuildings = new Dictionary<string, KeyValuePair<string, Vector2>>();

            foreach (var building in Game1.getFarm().buildings)
            {
                if (building?.nameOfIndoorsWithoutUnique is null || building.nameOfIndoors is null or "null") // Some actually have value of "null"
                    continue;

                var locVector = LocationToMap(
                    "Farm", // Get building position in farm
                    building.tileX.Value,
                    building.tileY.Value,
                    this.Customizations.MapVectors,
                    this.Customizations.LocationExclusions
                );

                // Using buildingType instead of nameOfIndoorsWithoutUnique because it is a better subset of currentLocation.Name 
                // since nameOfIndoorsWithoutUnique for Barn/Coop does not use Big/Deluxe but rather the upgrade level
                string commonName = building.buildingType.Value ?? building.nameOfIndoorsWithoutUnique;

                if (commonName.Contains("Barn"))
                    locVector.Y += 3;

                // Format: { uniqueName: { commonName: positionOnFarm } }
                // buildingType will match currentLocation.Name for commonName
                FarmBuildings[building.nameOfIndoors] =
                  new KeyValuePair<string, Vector2>(building.buildingType.Value, locVector);
            }

            // Greenhouse unlocked after pantry bundles completed
            if (((CommunityCenter)Game1.getLocationFromName("CommunityCenter")).areasComplete[CommunityCenter.AREA_Pantry])
            {
                var greenhouseLoc = LocationToMap("Greenhouse", customMapVectors: this.Customizations.MapVectors);
                greenhouseLoc.X -= 5 / 2 * 3;
                greenhouseLoc.Y -= 7 / 2 * 3;
                FarmBuildings["Greenhouse"] = new KeyValuePair<string, Vector2>("Greenhouse", greenhouseLoc);
            }

            // Add FarmHouse
            var farmhouseLoc = LocationToMap("FarmHouse", customMapVectors: this.Customizations.MapVectors);
            farmhouseLoc.X -= 6;
            FarmBuildings["FarmHouse"] = new KeyValuePair<string, Vector2>("FarmHouse", farmhouseLoc);
        }

        private void World_BuildingListChanged(object sender, BuildingListChangedEventArgs e)
        {
            if (e.Location.IsFarm)
                this.UpdateFarmBuildingLocations();

            LocationUtil.GetLocationContexts();
        }

        // Handle opening mod menu and changing tooltip options
        private void Input_ButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            // Minimap dragging
            if (Globals.ShowMinimap && this.Minimap.Value != null)
            {
                if (this.Minimap.Value.IsHoveringDragZone() && e.Button == SButton.MouseRight)
                {
                    MouseUtil.HandleMouseDown(() => this.Minimap.Value.HandleMouseDown());
                }
            }

            // Debug DnD
            if (this.DebugMode && e.Button == SButton.MouseRight && this.IsModMapOpen.Value)
                MouseUtil.HandleMouseDown();

            // Minimap toggle
            if (e.Button.ToString().Equals(Globals.MinimapToggleKey) && Game1.activeClickableMenu == null)
            {
                Globals.ShowMinimap = !Globals.ShowMinimap;
                this.Helper.Data.WriteJsonFile($"config/{Constants.SaveFolderName}.json", Config);
            }

            // ModMenu
            if (Game1.activeClickableMenu is GameMenu)
                this.HandleInput((GameMenu)Game1.activeClickableMenu, e.Button);

            if (this.DebugMode && !Context.IsMultiplayer && this.Helper.Input.GetState(SButton.LeftControl) == SButtonState.Held && e.Button.Equals(SButton.MouseRight))
                Game1.player.setTileLocation(Game1.currentCursorTile);
        }

        private void Input_ButtonReleased(object sender, ButtonReleasedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            if (this.DebugMode && e.Button == SButton.MouseRight && this.IsModMapOpen.Value)
                MouseUtil.HandleMouseRelease();
            else if (this.Minimap.Value != null)
            {
                if (Game1.activeClickableMenu is ModMenu && e.Button == SButton.MouseLeft)
                    this.Minimap.Value.Resize();
                else if (Game1.activeClickableMenu == null && e.Button == SButton.MouseRight)
                    MouseUtil.HandleMouseRelease(() => this.Minimap.Value.HandleMouseRelease());
            }
        }

        // Handle keyboard/controller inputs
        private void HandleInput(GameMenu menu, SButton input)
        {
            if (menu.currentTab != ModConstants.MapTabIndex)
                return;

            if (input.ToString().Equals(Globals.MenuKey) || input is SButton.ControllerY)
                Game1.activeClickableMenu = new ModMenu(this.NpcMarkers.Value, this.ConditionalNpcs.Value);

            if (input.ToString().Equals(Globals.TooltipKey) || input is SButton.RightShoulder)
                this.ChangeTooltipConfig();
            else if (input.ToString().Equals(Globals.TooltipKey) || input is SButton.LeftShoulder)
                this.ChangeTooltipConfig(false);
        }

        private void ChangeTooltipConfig(bool increment = true)
        {
            if (increment)
            {
                if (++Globals.NameTooltipMode > 3) Globals.NameTooltipMode = 1;

                this.Helper.Data.WriteJsonFile($"config/{Constants.SaveFolderName}.json", Config);
            }
            else
            {
                if (--Globals.NameTooltipMode < 1) Globals.NameTooltipMode = 3;

                this.Helper.Data.WriteJsonFile($"config/{Constants.SaveFolderName}.json", Config);
            }
        }

        // Handle any checks that need to be made per day
        private void GameLoop_DayStarted(object sender = null, DayStartedEventArgs e = null)
        {
            // Check for traveling merchant day
            if (this.ConditionalNpcs.Value != null && this.ConditionalNpcs.Value.ContainsKey("Merchant") && this.ConditionalNpcs.Value["Merchant"])
            {
                this.ConditionalNpcs.Value["Merchant"] = ((Forest)Game1.getLocationFromName("Forest")).travelingMerchantDay;
            }

            this.ResetMarkers();
            this.UpdateMarkers(true);

            this.Minimap.Value = new ModMinimap(
                this.NpcMarkers.Value,
                this.ConditionalNpcs.Value,
                this.FarmerMarkers.Value,
                FarmBuildings,
                this.BuildingMarkers.Value,
                this.Customizations
            );
        }

        private bool IsLocationExcluded(string location)
        {
            return Globals.ShowMinimap && Globals.MinimapExclusions.Any(loc => loc != "Farm" && location.StartsWith(loc) || loc == "Farm" && location == "Farm") ||
                     ((Globals.MinimapExclusions.Contains("Mine") || Globals.MinimapExclusions.Contains("UndergroundMine")) && location.Contains("Mine"));
        }

        private void ResetMarkers()
        {
            this.NpcMarkers.Value = new Dictionary<string, NpcMarker>();
            this.FarmerMarkers.Value = new Dictionary<long, FarmerMarker>();

            if (Context.IsMainPlayer)
            {
                foreach (var npc in this.GetVillagers())
                {
                    if (!this.Customizations.Names.ContainsKey(npc.Name) && npc is not (Horse or Child))
                        continue;

                    var type = npc switch
                    {
                        Horse => CharacterType.Horse,
                        Child => CharacterType.Child,
                        _ => CharacterType.Villager
                    };

                    if (!ModEntry.Globals.NpcMarkerOffsets.TryGetValue(npc.Name, out int offset))
                        offset = 0;

                    if (!this.NpcMarkers.Value.ContainsKey(npc.Name))
                    {
                        var newMarker = new NpcMarker
                        {
                            DisplayName = npc.displayName ?? npc.Name,
                            CropOffset = offset,
                            IsBirthday = npc.isBirthday(Game1.currentSeason, Game1.dayOfMonth),
                            Type = type
                        };

                        try
                        {
                            newMarker.Sprite = new AnimatedSprite(npc.Sprite.textureName.Value, 0, 16, 32).Texture;
                        }
                        catch (Exception ex)
                        {
                            this.Monitor.Log($"Couldn't load marker for NPC '{npc.Name}'; using the default sprite instead.", LogLevel.Warn);
                            this.Monitor.Log(ex.ToString());

                            newMarker.Sprite = new AnimatedSprite($@"Characters\{(npc.Gender == NPC.male ? "maleRival" : "femaleRival")}", 0, 16, 32).Texture;
                        }

                        this.NpcMarkers.Value.Add(npc.Name, newMarker);
                    }
                }
            }
        }

        // To initialize ModMap quicker for smoother rendering when opening map
        private void GameLoop_UpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsPlayerFree) // don't try to update markers during warp transitions, etc
                return;

            // Half-second tick
            if (e.IsMultipleOf(30))
            {
                bool updateForMinimap = Globals.ShowMinimap && this.Minimap.Value != null;

                if (updateForMinimap)
                    this.Minimap.Value.Update();

                this.UpdateMarkers(updateForMinimap | Context.IsMainPlayer);

                // Sync multiplayer data
                if (Context.IsMainPlayer && Context.IsMultiplayer)
                {
                    var syncedMarkers = new Dictionary<string, SyncedNpcMarker>();

                    foreach (var npcMarker in this.NpcMarkers.Value)
                    {
                        syncedMarkers.Add(npcMarker.Key, new SyncedNpcMarker
                        {
                            DisplayName = npcMarker.Value.DisplayName,
                            LocationName = npcMarker.Value.LocationName,
                            MapX = npcMarker.Value.MapX,
                            MapY = npcMarker.Value.MapY,
                            IsBirthday = npcMarker.Value.IsBirthday,
                            Type = npcMarker.Value.Type
                        });
                    }

                    this.Helper.Multiplayer.SendMessage(syncedMarkers, ModConstants.MessageIds.SyncedNpcMarkers, modIDs: new[] { this.ModManifest.UniqueID });
                }
            }

            // One-second tick
            if (e.IsOneSecond)
            {
                // Check season change (for when it's changed via console)
                if (Globals.UseSeasonalMaps && (this.MapSeason != null && this.MapSeason != Game1.currentSeason) && Game1.currentSeason != null)
                {
                    this.MapSeason = Game1.currentSeason;

                    // Force reload of map for season changes
                    try
                    {
                        this.Helper.Content.InvalidateCache(this.MapFilePath);
                    }
                    catch
                    {
                        this.Monitor.Log("Failed to update map for current season.", LogLevel.Error);
                    }

                    this.Minimap.Value?.UpdateMapForSeason();
                }

                // Check if conditional NPCs have been talked to
                foreach (string npcName in ModConstants.ConditionalNpcs)
                {
                    if (this.ConditionalNpcs.Value[npcName]) continue;

                    this.ConditionalNpcs.Value[npcName] = Game1.player.friendshipData.ContainsKey(npcName);
                }
            }

            // Update tick
            if (Globals.ShowMinimap && this.Minimap.Value != null && this.Minimap.Value.IsHoveringDragZone() && this.Helper.Input.GetState(SButton.MouseRight) == SButtonState.Held)
            {
                this.Minimap.Value.HandleMouseDrag();
            }

            if (Game1.activeClickableMenu == null || !(Game1.activeClickableMenu is GameMenu gameMenu))
            {
                this.IsModMapOpen.Value = false;
                return;
            }

            this.HasOpenedMap.Value = gameMenu.currentTab == ModConstants.MapTabIndex; // When map accessed by switching GameMenu tab or pressing M
            this.IsModMapOpen.Value = this.HasOpenedMap.Value ? this.IsModMapOpen.Value : this.HasOpenedMap.Value; // When vanilla MapPage is replaced by ModMap

            if (this.HasOpenedMap.Value && !this.IsModMapOpen.Value) // Only run once on map open
                this.OpenModMap();
        }

        private void Multiplayer_PeerConnected(object sender, PeerConnectedEventArgs e)
        {
            if (Context.IsMainPlayer)
                this.Helper.Multiplayer.SendMessage(this.Customizations.Names, ModConstants.MessageIds.SyncedNames, modIDs: new[] { this.ModManifest.UniqueID }, playerIDs: new[] { e.Peer.PlayerID });
        }

        private void Multiplayer_ModMessageReceived(object sender, ModMessageReceivedEventArgs e)
        {
            if (!Context.IsMainPlayer && e.FromModID == this.ModManifest.UniqueID && e.FromPlayerID == Game1.MasterPlayer.UniqueMultiplayerID)
            {
                switch (e.Type)
                {
                    case ModConstants.MessageIds.SyncedNames:
                        this.Customizations.Names = e.ReadAs<Dictionary<string, string>>();
                        break;

                    case ModConstants.MessageIds.SyncedNpcMarkers:
                        if (this.NpcMarkers.Value == null)
                            return;

                        var syncedNpcMarkers = e.ReadAs<Dictionary<string, SyncedNpcMarker>>();
                        foreach (var syncedMarker in syncedNpcMarkers)
                        {
                            string internalName = syncedMarker.Key;
                            if (!ModEntry.Globals.NpcMarkerOffsets.TryGetValue(internalName, out int offset))
                                offset = 0;

                            if (!this.NpcMarkers.Value.TryGetValue(internalName, out var npcMarker))
                            {
                                npcMarker = new NpcMarker();
                                this.NpcMarkers.Value.Add(internalName, npcMarker);
                            }

                            npcMarker.LocationName = syncedMarker.Value.LocationName;
                            npcMarker.MapX = syncedMarker.Value.MapX;
                            npcMarker.MapY = syncedMarker.Value.MapY;
                            npcMarker.DisplayName = syncedMarker.Value.DisplayName;
                            npcMarker.CropOffset = offset;
                            npcMarker.IsBirthday = syncedMarker.Value.IsBirthday;
                            npcMarker.Type = syncedMarker.Value.Type;

                            try
                            {
                                if (syncedMarker.Value.Type == CharacterType.Villager)
                                {
                                    npcMarker.Sprite = internalName == "Leo"
                                        ? new AnimatedSprite("Characters\\ParrotBoy", 0, 16, 32).Texture
                                        : new AnimatedSprite($"Characters\\{internalName}", 0, 16, 32).Texture;
                                }
                                else
                                {
                                    var sprite = Game1.getCharacterFromName(internalName, false)?.Sprite.Texture;
                                    npcMarker.Sprite = sprite;
                                }
                            }
                            catch
                            {
                                npcMarker.Sprite = null;
                            }
                        }
                        break;
                }
            }
        }

        private void OpenModMap()
        {
            if (Game1.activeClickableMenu is not GameMenu gameMenu)
                return;

            this.IsModMapOpen.Value = true;

            var pages = this.Helper.Reflection
              .GetField<List<IClickableMenu>>(gameMenu, "pages").GetValue();

            // Changing the page in GameMenu instead of changing Game1.activeClickableMenu
            // allows for better compatibility with other mods that use MapPage
            pages[ModConstants.MapTabIndex] = new ModMapPage(
                this.NpcMarkers.Value,
                this.ConditionalNpcs.Value,
                this.FarmerMarkers.Value,
                FarmBuildings,
                this.BuildingMarkers.Value,
                this.Customizations,
                LocationUtil
            );
        }

        private void UpdateMarkers(bool forceUpdate = false)
        {
            if (this.IsModMapOpen.Value || forceUpdate)
            {
                if (!Context.IsMultiplayer || Context.IsMainPlayer)
                    this.UpdateNpcs();
                else
                    this.UpdateNpcsFarmhand();

                if (Context.IsMultiplayer)
                    this.UpdateFarmers();
            }
        }

        // Update NPC marker data and names on hover
        private void UpdateNpcs()
        {
            if (this.NpcMarkers.Value == null) return;

            List<NPC> npcList = this.GetVillagers();

            // If player is riding a horse, add it to list
            if (Game1.player.isRidingHorse())
            {
                npcList.Add(Game1.player.mount);
            }

            foreach (var npc in npcList)
            {
                if (!this.NpcMarkers.Value.TryGetValue(npc.Name, out var npcMarker)
                  || npc.currentLocation == null)
                {
                    continue;
                }

                /* // Hide horse if being ridden
                if (npc is Horse horse)
                {
                  /*var isRiding = false;

                  // If horse is being ridden, hide it
                  foreach (var farmer in Game1.getOnlineFarmers())
                  {
                    if (farmer.isRidingHorse())
                    {
                      isRiding = true;
                      break;
                    }
                  }

                  if (horse.rider != null)
                  {
                    npcMarker.MapX = -9999;
                    npcMarker.MapY = -9999;                                                                                                              
                    continue;
                  }
                } */

                string locationName = npc.currentLocation.uniqueName.Value ?? npc.currentLocation.Name;
                npcMarker.LocationName = locationName;

                // For show Npcs in player's location option
                bool isSameLocation = false;

                if (Globals.OnlySameLocation)
                {
                    string playerLocationName =
                      Game1.player.currentLocation.uniqueName.Value ?? Game1.player.currentLocation.Name;
                    if (locationName == playerLocationName)
                    {
                        isSameLocation = true;
                    }
                    else if (LocationUtil.LocationContexts.TryGetValue(locationName, out var npcLocCtx) &&
                             LocationUtil.LocationContexts.TryGetValue(playerLocationName, out var playerLocCtx))
                    {
                        isSameLocation = npcLocCtx.Root == playerLocCtx.Root;
                    }
                }

                // NPCs that won't be shown on the map unless 'Show Hidden NPCs' is checked
                npcMarker.IsHidden = Config.ImmersionOption == 2 && !Game1.player.hasTalkedToFriendToday(npc.Name)
                                     || Config.ImmersionOption == 3 && Game1.player.hasTalkedToFriendToday(npc.Name)
                                     || Globals.OnlySameLocation && !isSameLocation
                                     || Config.ByHeartLevel
                                     && !(Game1.player.getFriendshipHeartLevelForNPC(npc.Name)
                                          >= Config.HeartLevelMin && Game1.player.getFriendshipHeartLevelForNPC(npc.Name)
                                          <= Config.HeartLevelMax);

                // Check for daily quests
                foreach (var quest in Game1.player.questLog)
                {
                    if (quest.accepted.Value && quest.dailyQuest.Value && !quest.completed.Value)
                        npcMarker.HasQuest = quest switch
                        {
                            ItemDeliveryQuest itemDeliveryQuest => itemDeliveryQuest.target.Value == npc.Name,
                            SlayMonsterQuest slayMonsterQuest => slayMonsterQuest.target.Value == npc.Name,
                            FishingQuest fishingQuest => fishingQuest.target.Value == npc.Name,
                            ResourceCollectionQuest resourceCollectionQuest => resourceCollectionQuest.target.Value == npc.Name,
                            _ => npcMarker.HasQuest
                        };
                }

                // Establish draw order, higher number in front
                // Layers 4 - 7: Outdoor NPCs in order of hidden, hidden w/ quest/birthday, standard, standard w/ quest/birthday
                // Layers 0 - 3: Indoor NPCs in order of hidden, hidden w/ quest/birthday, standard, standard w/ quest/birthday
                if (npc is Horse || npc is Child)
                {
                    npcMarker.Layer = 0;
                }
                else
                {
                    npcMarker.Layer = LocationUtil.IsOutdoors(locationName) ? 6 : 2;
                    if (npcMarker.IsHidden) npcMarker.Layer -= 2;
                }

                if (npcMarker.HasQuest || npcMarker.IsBirthday) npcMarker.Layer++;

                if (locationName != null)
                {
                    // Get center of NPC marker
                    var npcLocation = LocationToMap(locationName, npc.getTileX(), npc.getTileY(), this.Customizations.MapVectors, this.Customizations.LocationExclusions);
                    npcMarker.MapX = (int)npcLocation.X - 16;
                    npcMarker.MapY = (int)npcLocation.Y - 15;
                }
            }
        }

        // Update npc marker properties only relevant to farmhand
        private void UpdateNpcsFarmhand()
        {
            if (this.NpcMarkers.Value == null) return;

            foreach (var npcMarker in this.NpcMarkers.Value)
            {
                string name = npcMarker.Key;
                var marker = npcMarker.Value;

                // For show Npcs in player's location option
                bool isSameLocation = false;
                if (Globals.OnlySameLocation)
                {
                    string playerLocationName =
                      Game1.player.currentLocation.uniqueName.Value ?? Game1.player.currentLocation.Name;
                    if (marker.LocationName == playerLocationName)
                    {
                        isSameLocation = true;
                    }
                    else if (LocationUtil.LocationContexts.TryGetValue(marker.LocationName, out var npcLocCtx) &&
                             LocationUtil.LocationContexts.TryGetValue(playerLocationName, out var playerLocCtx))
                    {
                        isSameLocation = npcLocCtx.Root == playerLocCtx.Root;
                    }
                }

                // NPCs that won't be shown on the map unless 'Show Hidden NPCs' is checked
                marker.IsHidden = Config.ImmersionOption == 2 && !Game1.player.hasTalkedToFriendToday(name)
                                     || Config.ImmersionOption == 3 && Game1.player.hasTalkedToFriendToday(name)
                                     || Globals.OnlySameLocation && !isSameLocation
                                     || Config.ByHeartLevel
                                     && !(Game1.player.getFriendshipHeartLevelForNPC(name)
                                          >= Config.HeartLevelMin && Game1.player.getFriendshipHeartLevelForNPC(name)
                                          <= Config.HeartLevelMax);

                // Check for daily quests
                foreach (var quest in Game1.player.questLog)
                {
                    if (quest.accepted.Value && quest.dailyQuest.Value && !quest.completed.Value)
                        marker.HasQuest = quest switch
                        {
                            ItemDeliveryQuest itemDeliveryQuest => itemDeliveryQuest.target.Value == name,
                            SlayMonsterQuest slayMonsterQuest => slayMonsterQuest.target.Value == name,
                            FishingQuest fishingQuest => fishingQuest.target.Value == name,
                            ResourceCollectionQuest resourceCollectionQuest => resourceCollectionQuest.target.Value == name,
                            _ => marker.HasQuest
                        };
                }

                // Establish draw order, higher number in front
                // Layers 4 - 7: Outdoor NPCs in order of hidden, hidden w/ quest/birthday, standard, standard w/ quest/birthday
                // Layers 0 - 3: Indoor NPCs in order of hidden, hidden w/ quest/birthday, standard, standard w/ quest/birthday
                if (marker.Type == CharacterType.Horse || marker.Type == CharacterType.Child)
                {
                    marker.Layer = 0;
                }
                else
                {
                    marker.Layer = LocationUtil.IsOutdoors(marker.LocationName) ? 6 : 2;
                    if (marker.IsHidden) marker.Layer -= 2;
                }

                if (marker.HasQuest || marker.IsBirthday) marker.Layer++;
            }
        }

        private void UpdateFarmers()
        {
            foreach (var farmer in Game1.getOnlineFarmers())
            {
                if (farmer?.currentLocation == null) continue;
                string locationName = farmer.currentLocation.uniqueName.Value ?? farmer.currentLocation.Name;

                if (locationName.Contains("UndergroundMine"))
                {
                    locationName = LocationUtil.GetMinesLocationName(locationName);
                }

                long farmerId = farmer.UniqueMultiplayerID;
                var farmerLoc = LocationToMap(
                    locationName,
                    farmer.getTileX(),
                    farmer.getTileY(),
                    this.Customizations.MapVectors,
                    this.Customizations.LocationExclusions
                );

                if (this.FarmerMarkers.Value.TryGetValue(farmer.UniqueMultiplayerID, out var farMarker))
                {
                    float deltaX = farmerLoc.X - farMarker.PrevMapX;
                    float deltaY = farmerLoc.Y - farMarker.PrevMapY;

                    // Location changes before tile position, causing farmhands to blink
                    // to the wrong position upon entering new location. Handle this in draw.
                    if (locationName == farMarker.LocationName && MathHelper.Distance(deltaX, deltaY) > 15)
                        this.FarmerMarkers.Value[farmerId].DrawDelay = 1;
                    else if (farMarker.DrawDelay > 0)
                        this.FarmerMarkers.Value[farmerId].DrawDelay--;
                }
                else
                {
                    var newMarker = new FarmerMarker
                    {
                        Name = farmer.Name,
                        DrawDelay = 0
                    };

                    this.FarmerMarkers.Value.Add(farmerId, newMarker);
                }


                this.FarmerMarkers.Value[farmerId].MapX = (int)farmerLoc.X;
                this.FarmerMarkers.Value[farmerId].MapY = (int)farmerLoc.Y;
                this.FarmerMarkers.Value[farmerId].PrevMapX = (int)farmerLoc.X;
                this.FarmerMarkers.Value[farmerId].PrevMapY = (int)farmerLoc.Y;
                this.FarmerMarkers.Value[farmerId].LocationName = locationName;
                this.FarmerMarkers.Value[farmerId].MapX = (int)farmerLoc.X;
                this.FarmerMarkers.Value[farmerId].MapY = (int)farmerLoc.Y;
            }
        }

        // MAIN METHOD FOR PINPOINTING CHARACTERS ON THE MAP
        // Calculated from mapping of game tile positions to pixel coordinates of the map in MapModConstants. 
        // Requires MapModConstants and modified map page in /maps
        public static Vector2 LocationToMap(string locationName, int tileX = -1, int tileY = -1, Dictionary<string, MapVector[]> customMapVectors = null, HashSet<string> locationExclusions = null, bool isPlayer = false)
        {
            static Vector2 ScanRecursively(string locationName, int tileX, int tileY, Dictionary<string, MapVector[]> customMapVectors, HashSet<string> locationExclusions, bool isPlayer, ISet<string> seen, int depth)
            {
                if (string.IsNullOrWhiteSpace(locationName))
                    return Unknown;

                // break infinite loops
                if (!seen.Add(locationName))
                    return Unknown;
                if (depth > LocationUtil.MaxRecursionDepth)
                    throw new InvalidOperationException($"Infinite recursion detected in location scan. Technical details:\n{nameof(locationName)}: {locationName}\n{nameof(tileX)}: {tileX}\n{nameof(tileY)}: {tileY}\n\n{Environment.StackTrace}");

                if ((locationExclusions != null && locationExclusions.Contains(locationName)) || locationName.Contains("WarpRoom"))
                    return Unknown;

                if (FarmBuildings.TryGetValue(locationName, out var mapLoc))
                    return mapLoc.Value;

                if (locationName.StartsWith("UndergroundMine"))
                {
                    string mine = locationName.Substring("UndergroundMine".Length, locationName.Length - "UndergroundMine".Length);
                    if (int.TryParse(mine, out int mineLevel))
                        locationName = mineLevel > 120 ? "SkullCave" : "Mine";
                }

                // Get location of indoor location by its warp position in the outdoor location
                if (LocationUtil.LocationContexts.TryGetValue(locationName, out var loc)
                  && loc.Type != LocationType.Outdoors
                  && loc.Root != null
                  && locationName != "MovieTheater"         // Weird edge cases where the warps are off
                )
                {
                    string building = LocationUtil.GetBuilding(locationName, depth + 1);

                    if (building != null)
                    {
                        int doorX = (int)LocationUtil.LocationContexts[building].Warp.X;
                        int doorY = (int)LocationUtil.LocationContexts[building].Warp.Y;

                        // Slightly adjust warp location to depict being inside the building 
                        var warpPos = ScanRecursively(loc.Root, doorX, doorY, customMapVectors, locationExclusions, isPlayer, seen, depth + 1);
                        return new Vector2(warpPos.X + 1, warpPos.Y - 8);
                    }
                }

                // If we fail to grab the indoor location correctly for whatever reason, fallback to old hard-coded constants

                MapVector[] locVectors;
                bool locationNotFound = false;

                if (locationName == "Farm")
                {
                    // Handle different farm types for custom vectors
                    string[] farms = { "Farm_Default", "Farm_Riverland", "Farm_Forest", "Farm_Hills", "Farm_Wilderness", "Farm_FourCorners", "Farm_Beach" };
                    if (customMapVectors != null && (customMapVectors.Keys.Any(locName => locName == farms.ElementAtOrDefault(Game1.whichFarm))))
                    {
                        if (!customMapVectors.TryGetValue(farms.ElementAtOrDefault(Game1.whichFarm), out locVectors))
                            locationNotFound = !ModConstants.MapVectors.TryGetValue(locationName, out locVectors);
                    }
                    else
                    {
                        if (!customMapVectors.TryGetValue("Farm", out locVectors))
                        {
                            locationNotFound = !ModConstants.MapVectors.TryGetValue(farms.ElementAtOrDefault(Game1.whichFarm), out locVectors);
                            if (locationNotFound)
                                locationNotFound = !ModConstants.MapVectors.TryGetValue(locationName, out locVectors);
                        }
                    }
                }
                // If not in custom vectors, use default
                else if (!(customMapVectors != null && customMapVectors.TryGetValue(locationName, out locVectors)))
                    locationNotFound = !ModConstants.MapVectors.TryGetValue(locationName, out locVectors);

                if (locVectors == null || locationNotFound)
                    return Unknown;

                int x;
                int y;

                // Precise (static) regions and indoor locations
                if (locVectors.Length == 1 || tileX == -1 || tileY == -1)
                {
                    x = locVectors.FirstOrDefault().MapX;
                    y = locVectors.FirstOrDefault().MapY;
                }
                else
                {
                    // Sort map vectors by distance to point
                    MapVector[] vectors = locVectors
                        .OrderBy(vector => Math.Sqrt(Math.Pow(vector.TileX - tileX, 2) + Math.Pow(vector.TileY - tileY, 2)))
                        .ToArray();

                    MapVector lower = null;
                    MapVector upper = null;
                    bool isSameAxis = false;

                    // Create rectangle bound from two pre-defined points (lower & upper bound) and calculate map scale for that area
                    foreach (var vector in vectors)
                    {
                        if (lower != null && upper != null)
                        {
                            if (lower.TileX == upper.TileX || lower.TileY == upper.TileY)
                                isSameAxis = true;
                            else
                                break;
                        }

                        if ((lower == null || isSameAxis) && tileX >= vector.TileX && tileY >= vector.TileY)
                        {
                            lower = vector;
                            continue;
                        }

                        if ((upper == null || isSameAxis) && tileX <= vector.TileX && tileY <= vector.TileY)
                            upper = vector;
                    }

                    // Handle null cases - not enough vectors to calculate using lower/upper bound strategy
                    // Uses fallback strategy - get closest points such that lower != upper
                    lower ??= upper == vectors.First() ? vectors.Skip(1).First() : vectors.First();
                    upper ??= lower == vectors.First() ? vectors.Skip(1).First() : vectors.First();

                    x = (int)MathHelper.Clamp((int)(lower.MapX + (tileX - lower.TileX) / (double)(upper.TileX - lower.TileX) * (upper.MapX - lower.MapX)), 0, 1200);
                    y = (int)MathHelper.Clamp((int)(lower.MapY + (tileY - lower.TileY) / (double)(upper.TileY - lower.TileY) * (upper.MapY - lower.MapY)), 0, 720);
                }

                return new Vector2(x, y);
            }

            return ScanRecursively(locationName, tileX, tileY, customMapVectors, locationExclusions, isPlayer, new HashSet<string>(), 1);
        }

        private void Display_WindowResized(object sender, WindowResizedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            this.UpdateMarkers(true);
            this.UpdateFarmBuildingLocations();
            this.Minimap.Value?.CheckOffsetForMap();

            if (this.IsModMapOpen.Value)
                this.OpenModMap();
        }

        private void Display_MenuChanged(object sender, MenuChangedEventArgs e)
        {
            if (!Context.IsWorldReady) return;
            MouseUtil.Reset();

            // Check for resize after mod menu closed
            if (e.OldMenu is ModMenu)
                this.Minimap.Value?.Resize();
        }

        private void Player_Warped(object sender, WarpedEventArgs e)
        {
            if (e.IsLocalPlayer)
            {
                // Hide minimap in blacklisted locations with special case for Mines as usual
                Globals.ShowMinimap = Globals.ShowMinimap && !this.IsLocationExcluded(e.NewLocation.Name);

                // Check if map does not fill screen and adjust for black bars (ex. BusStop)
                this.Minimap.Value?.CheckOffsetForMap();
            }
        }

        private void Display_RenderingHud(object sender, RenderingHudEventArgs e)
        {
            if (Context.IsWorldReady && Globals.ShowMinimap && Game1.displayHUD)
                this.Minimap.Value?.DrawMiniMap();
        }

        private void Display_RenderedWorld(object sender, RenderedWorldEventArgs e)
        {
            // Highlight tile for debug mode
            if (this.DebugMode)
            {
                Game1.spriteBatch.Draw(Game1.mouseCursors,
                  new Vector2(
                    Game1.tileSize * (int)Math.Floor(Game1.currentCursorTile.X) - Game1.viewport.X,
                    Game1.tileSize * (int)Math.Floor(Game1.currentCursorTile.Y) - Game1.viewport.Y),
                  new Rectangle(448, 128, 64, 64), Color.White);
            }
        }

        // DEBUG 
        private void Display_Rendered(object sender, RenderedEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.player == null) return;

            if (this.DebugMode)
                this.ShowDebugInfo();
        }

        // Show debug info in top left corner
        private void ShowDebugInfo()
        {
            if (Game1.player.currentLocation == null) return;
            string locationName = Game1.player.currentLocation.uniqueName.Value ?? Game1.player.currentLocation.Name;
            int textHeight = (int)Game1.dialogueFont
              .MeasureString("()").Y - 6;

            // If map is open, show map position at cursor
            if (this.IsModMapOpen.Value)
            {
                int borderWidth = 3;
                float borderOpacity = 0.75f;
                Vector2 mapPos = MouseUtil.GetMapPositionAtCursor();
                Rectangle bounds = MouseUtil.GetDragAndDropArea();

                var tex = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
                tex.SetData(new[] { Color.Red });

                // Draw point at cursor on map
                Game1.spriteBatch.Draw(tex,
                  new Rectangle(Game1.getMouseX() - borderWidth / 2, Game1.getMouseY() - borderWidth / 2, borderWidth, borderWidth),
                  Rectangle.Empty, Color.White);

                // Show map pixel position at cursor
                this.DrawText($"Map position: ({mapPos.X}, {mapPos.Y})", new Vector2(Game1.tileSize / 4, Game1.tileSize / 4), Color.White);

                // Draw drag and drop area
                if (this.Helper.Input.GetState(SButton.MouseRight) == SButtonState.Held)
                {
                    // Draw dragging box
                    this.DrawBorder(tex, MouseUtil.GetCurrentDraggingArea(), borderWidth, Color.White * borderOpacity);
                }
                else
                {
                    if (MouseUtil.BeginMousePosition.X < 0 && MouseUtil.EndMousePosition.X < 0) return;

                    // Draw drag and drop box
                    this.DrawBorder(tex, bounds, borderWidth, Color.White * borderOpacity);

                    var mapBounds = MouseUtil.GetRectangleOnMap(bounds);

                    if (mapBounds.Width == 0 && mapBounds.Height == 0)
                    {
                        // Show point
                        this.DrawText($"Point: ({mapBounds.X}, {mapBounds.Y})", new Vector2(Game1.tileSize / 4, Game1.tileSize / 4 + textHeight), Color.White);
                    }
                    else
                    {
                        // Show first point of DnD box
                        this.DrawText($"Top-left: ({mapBounds.X}, {mapBounds.Y})", new Vector2(Game1.tileSize / 4, Game1.tileSize / 4 + textHeight), Color.White);

                        // Show second point of DnD box
                        this.DrawText($"Bot-right: ({mapBounds.X + mapBounds.Width}, {mapBounds.Y + mapBounds.Height})", new Vector2(Game1.tileSize / 4, Game1.tileSize / 4 + textHeight * 2), Color.White);

                        // Show width of DnD box
                        this.DrawText($"Width: {mapBounds.Width}", new Vector2(Game1.tileSize / 4, Game1.tileSize / 4 + textHeight * 3), Color.White);

                        // Show height of DnD box
                        this.DrawText($"Height: {mapBounds.Height}", new Vector2(Game1.tileSize / 4, Game1.tileSize / 4 + textHeight * 4), Color.White);
                    }
                }
            }
            else
            {
                // Show tile position of tile at cursor
                var tilePos = MouseUtil.GetTilePositionAtCursor();
                this.DrawText($"{locationName} ({Game1.currentLocation.Map.DisplayWidth / Game1.tileSize} x {Game1.currentLocation.Map.DisplayHeight / Game1.tileSize})", new Vector2(Game1.tileSize / 4, Game1.tileSize / 4), Color.White);
                this.DrawText($"Tile position: ({tilePos.X}, {tilePos.Y})", new Vector2(Game1.tileSize / 4, Game1.tileSize / 4 + textHeight), Color.White);
            }
        }

        // Draw outlined text
        private void DrawText(string text, Vector2 pos, Color? color = null)
        {
            var tex = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
            tex.SetData(new[] { Color.Black * 0.75f });

            // Dark background for clearer text
            Game1.spriteBatch.Draw(
              tex,
              new Rectangle(
                (int)pos.X,
                (int)pos.Y,
                (int)Game1.dialogueFont.MeasureString(text).X,
                (int)Game1.dialogueFont.MeasureString("()").Y - 6),
              Rectangle.Empty,
              Color.Black
             );

            Game1.spriteBatch.DrawString(Game1.dialogueFont, text, pos + new Vector2(1, 1), Color.Black);
            Game1.spriteBatch.DrawString(Game1.dialogueFont, text, pos + new Vector2(-1, 1), Color.Black);
            Game1.spriteBatch.DrawString(Game1.dialogueFont, text, pos + new Vector2(1, -1), Color.Black);
            Game1.spriteBatch.DrawString(Game1.dialogueFont, text, pos + new Vector2(-1, -1), Color.Black);
            Game1.spriteBatch.DrawString(Game1.dialogueFont, text, pos, color ?? Color.White);
        }

        // Draw rectangle border
        private void DrawBorder(Texture2D tex, Rectangle rect, int borderWidth, Color color)
        {
            // Draw top line
            Game1.spriteBatch.Draw(tex, new Rectangle(rect.X - 1, rect.Y - 1, rect.Width + 3, borderWidth), color);

            // Draw left line
            Game1.spriteBatch.Draw(tex, new Rectangle(rect.X - 1, rect.Y - 1, borderWidth, rect.Height + 3), color);

            // Draw right line
            Game1.spriteBatch.Draw(tex, new Rectangle((rect.X + rect.Width - borderWidth + 2),
              rect.Y - 1,
              borderWidth,
              rect.Height + 3), color);

            // Draw bottom line
            Game1.spriteBatch.Draw(tex, new Rectangle(rect.X - 1,
              rect.Y + rect.Height - borderWidth + 2,
              rect.Width + 3,
              borderWidth), color);
        }
    }
}
