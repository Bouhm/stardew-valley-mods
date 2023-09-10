using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bouhm.Shared;
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
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Quests;

namespace NPCMapLocations
{
    /// <summary>The mod entry class.</summary>
    public class ModEntry : Mod
    {
        /*********
        ** Fields
        *********/
        /// <summary>The map markers for farm buildings, indexed by the interior unique name.</summary>
        private static readonly Dictionary<string, BuildingMarker> FarmBuildings = new();

        private readonly PerScreen<Texture2D> BuildingMarkers = new();
        private readonly PerScreen<Dictionary<string, MapVector[]>> MapVectors = new();
        private readonly PerScreen<ModMinimap> Minimap = new();
        private readonly PerScreen<Dictionary<string, NpcMarker>> NpcMarkers = new();
        private readonly PerScreen<Dictionary<string, bool>> ConditionalNpcs = new();
        private readonly PerScreen<bool> HasOpenedMap = new();
        private readonly PerScreen<bool> IsModMapOpen = new();

        /// <summary>Whether to show the minimap.</summary>
        private readonly PerScreen<bool> ShowMinimap = new();

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


        /*********
        ** Accessors
        *********/
        public static PlayerConfig Config;
        public static GlobalConfig Globals;
        public static IModHelper StaticHelper;
        public static Texture2D Map;
        public static Vector2 Unknown = new(-9999, -9999);


        /*********
        ** Public methods
        *********/
        /// <inheritdoc />
        public override void Entry(IModHelper helper)
        {
            I18n.Init(helper.Translation);
            CommonHelper.RemoveObsoleteFiles(this, "NPCMapLocations.pdb");

            ModEntry.LocationUtil = new(this.Monitor);
            StaticHelper = helper;
            Globals = helper.Data.ReadJsonFile<GlobalConfig>("config/globals.json") ?? new GlobalConfig();
            this.Customizations = new ModCustomizations();

            helper.Events.Content.AssetRequested += this.OnAssetRequested;
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.World.BuildingListChanged += this.OnBuildingListChanged;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.Input.ButtonReleased += this.OnButtonReleased;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            helper.Events.Player.Warped += this.OnWarped;
            helper.Events.Display.MenuChanged += this.OnMenuChanged;
            helper.Events.Display.RenderingHud += this.OnRenderingHud;
            helper.Events.Display.RenderedWorld += this.OnRenderedWorld;
            helper.Events.Display.Rendered += this.OnRendered;
            helper.Events.Display.WindowResized += this.OnWindowResized;
            helper.Events.Multiplayer.PeerConnected += this.OnPeerConnected;
            helper.Events.Multiplayer.ModMessageReceived += this.OnModMessageReceived;

            helper.ConsoleCommands.Add(SummaryCommand.Name, SummaryCommand.GetDescription(), (_, _) => SummaryCommand.Handle(
                monitor: this.Monitor,
                locationUtil: ModEntry.LocationUtil,
                customizations: this.Customizations,
                mapVectors: this.MapVectors.Value,
                npcMarkers: this.NpcMarkers.Value,
                locationsWithoutMapVectors: this.GetLocationsWithoutMapVectors()
            ));
        }

        /// <inheritdoc cref="IGameLoopEvents.SaveLoaded"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            // Replace game map with modified map
            if (e.NameWithoutLocale.IsEquivalentTo(this.MapFilePath))
            {
                e.LoadFrom(
                    () =>
                    {
                        if (this.MapSeason == null)
                        {
                            this.Monitor.Log("Unable to get current season. Defaulted to spring.", LogLevel.Debug);
                            this.MapSeason = "spring";
                        }

                        if (!File.Exists(Path.Combine(this.Helper.DirectoryPath, this.Customizations.MapsPath, $"{this.MapSeason}_map.png")))
                        {
                            this.Monitor.Log("Seasonal maps not provided. Defaulted to spring.");
                            this.MapSeason = null; // Set to null so that cache is not invalidated when game season changes
                        }

                        // Replace map page
                        string defaultMapFile = File.Exists(Path.Combine(this.Helper.DirectoryPath, this.Customizations.MapsPath, "spring_map.png")) ? "spring_map.png" : "map.png";
                        string filename = this.MapSeason == null ? defaultMapFile : $"{this.MapSeason}_map.png";

                        bool useRecolor = this.Customizations.MapsPath != null && File.Exists(Path.Combine(this.Helper.DirectoryPath, this.Customizations.MapsPath, filename));
                        Texture2D map = useRecolor
                            ? this.Helper.ModContent.Load<Texture2D>($"{this.Customizations.MapsPath}/{filename}")
                            : this.Helper.ModContent.Load<Texture2D>($"{this.Customizations.MapsRootPath}/_default/{filename}");

                        if (useRecolor)
                            this.Monitor.Log($"Using {Path.Combine(this.Customizations.MapsPath, filename)}.");

                        return map;
                    },
                    AssetLoadPriority.Exclusive
                );
            }
            else if (e.NameWithoutLocale.IsEquivalentTo(this.LocationCustomizationsPath) || e.NameWithoutLocale.IsEquivalentTo(this.NpcCustomizationsPath))
            {
                e.LoadFrom(() => new Dictionary<string, JObject>(), AssetLoadPriority.Exclusive);
            }
        }

        /// <summary>Get the pixel coordinates relative to the top-left corner of the map for an in-world tile position.</summary>
        /// <param name="locationName">The in-world location name.</param>
        /// <param name="tileX">The X tile position within the location, if known.</param>
        /// <param name="tileY">The Y tile position within the location, if known.</param>
        /// <param name="customMapVectors">The custom vectors which map specific in-game tile coordinates to their map pixels.</param>
        /// <param name="locationExclusions">The locations to ignore when scanning locations for players and NPCs.</param>
        public static Vector2 LocationToMap(string locationName, int tileX = -1, int tileY = -1, Dictionary<string, MapVector[]> customMapVectors = null, HashSet<string> locationExclusions = null)
        {
            static Vector2 ScanRecursively(string locationName, int tileX, int tileY, Dictionary<string, MapVector[]> customMapVectors, HashSet<string> locationExclusions, ISet<string> seen, int depth)
            {
                // special case: map generated level to single name
                locationName = LocationUtil.GetLocationNameFromLevel(locationName) ?? locationName;

                if (string.IsNullOrWhiteSpace(locationName))
                    return Unknown;

                // break infinite loops
                if (!seen.Add(locationName))
                    return Unknown;
                if (depth > LocationUtil.MaxRecursionDepth)
                    throw new InvalidOperationException($"Infinite recursion detected in location scan. Technical details:\n{nameof(locationName)}: {locationName}\n{nameof(tileX)}: {tileX}\n{nameof(tileY)}: {tileY}\n\n{Environment.StackTrace}");

                if (locationExclusions?.Contains(locationName) == true || locationName.Contains("WarpRoom"))
                    return Unknown;

                if (FarmBuildings.TryGetValue(locationName, out BuildingMarker marker))
                    return marker.MapPosition;

                // Get location of indoor location by its warp position in the outdoor location
                if (LocationUtil.TryGetContext(locationName, out var loc)
                  && loc.Type != LocationType.Outdoors
                  && loc.Root != null
                  && locationName != "MovieTheater"         // Weird edge cases where the warps are off
                )
                {
                    string building = LocationUtil.GetBuilding(locationName, depth + 1);

                    if (building != null)
                    {
                        var buildingContext = LocationUtil.TryGetContext(building);
                        int doorX = (int)buildingContext.Warp.X;
                        int doorY = (int)buildingContext.Warp.Y;

                        // Slightly adjust warp location to depict being inside the building 
                        var warpPos = ScanRecursively(loc.Root, doorX, doorY, customMapVectors, locationExclusions, seen, depth + 1);
                        return new Vector2(warpPos.X + 1, warpPos.Y - 8);
                    }
                }

                // If we fail to grab the indoor location correctly for whatever reason, fallback to old hard-coded constants

                MapVector[] locVectors = ModEntry.GetMapVectors(locationName, customMapVectors);
                if (locVectors == null)
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

            return ScanRecursively(locationName, tileX, tileY, customMapVectors, locationExclusions, new HashSet<string>(), 1);
        }

        /// <summary>Get whether an NPC is configured to be excluded when rendering markers on the map.</summary>
        /// <param name="name">The NPC name.</param>
        /// <param name="ignoreConfig">Whether to ignore custom configuration from the player.</param>
        public static bool ShouldExcludeNpc(string name, bool ignoreConfig = false)
        {
            return ModEntry.ShouldExcludeNpc(name, out _, ignoreConfig);
        }

        /// <summary>Get whether an NPC is configured to be excluded when rendering markers on the map.</summary>
        /// <param name="name">The NPC name.</param>
        /// <param name="reason">A phrase indicating why the NPC should be excluded, shown by the <see cref="SummaryCommand"/>.</param>
        /// <param name="ignoreConfig">Whether to ignore custom configuration from the player.</param>
        public static bool ShouldExcludeNpc(string name, out string reason, bool ignoreConfig = false)
        {
            // from config override
            if (!ignoreConfig && ModEntry.Config.ForceNpcVisibility != null && ModEntry.Config.ForceNpcVisibility.TryGetValue(name, out bool forceVisible))
            {
                reason = !forceVisible ? "excluded in player settings" : null;
                return forceVisible;
            }

            // from mod override
            if (ModEntry.Globals.ModNpcExclusions.Contains(name))
            {
                reason = "excluded by mod override";
                return true;
            }

            // from defaults
            if (ModEntry.Globals.NpcExclusions.Contains(name))
            {
                reason = "excluded by default";
                return true;
            }

            // not hidden
            reason = null;
            return false;
        }


        /*********
        ** Private methods
        *********/
        /// <inheritdoc cref="IGameLoopEvents.SaveLoaded"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            //
            // Load config and other one-off data
            //

            Config = this.Helper.Data.ReadJsonFile<PlayerConfig>($"config/{Constants.SaveFolderName}.json") ?? new PlayerConfig();

            // Initialize these early for multiplayer sync
            this.NpcMarkers.Value = new Dictionary<string, NpcMarker>();
            this.FarmerMarkers.Value = new Dictionary<long, FarmerMarker>();

            if (!(Context.IsSplitScreen && !Context.IsMainPlayer))
            {
                // Load customizations
                var npcSettings = this.Helper.GameContent.Load<Dictionary<string, JObject>>(this.NpcCustomizationsPath);
                var locationSettings = this.Helper.GameContent.Load<Dictionary<string, JObject>>(this.LocationCustomizationsPath);
                this.Customizations.LoadCustomData(npcSettings, locationSettings);
            }

            // Load farm buildings
            try
            {
                this.BuildingMarkers.Value = this.Helper.ModContent.Load<Texture2D>($"{this.Customizations.MapsPath}/buildings.png");
            }
            catch
            {
                this.BuildingMarkers.Value = File.Exists(Path.Combine("maps/_default", "buildings.png"))
                    ? this.Helper.ModContent.Load<Texture2D>("maps/_default/buildings.png")
                    : null;
            }

            // Get season for map
            this.MapSeason = Globals.UseSeasonalMaps ? Game1.currentSeason : "spring";
            this.Helper.GameContent.InvalidateCache(this.MapFilePath);
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
            LocationUtil.ScanLocationContexts();

            // Log any custom locations not handled in content.json
            string[] unknownLocations = this.GetLocationsWithoutMapVectors().Select(p => p.Name).OrderBy(p => p).ToArray();
            if (unknownLocations.Any())
                this.Monitor.Log($"Unknown locations: {string.Join(", ", unknownLocations)}");

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
                    this.Monitor.Log("Since the server host doesn't have NPC Map Locations installed, NPC locations can't be synced.", LogLevel.Warn);
            }

            // enable minimap
            this.UpdateMinimapVisibility();
        }

        /// <inheritdoc cref="IWorldEvents.BuildingListChanged"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnBuildingListChanged(object sender, BuildingListChangedEventArgs e)
        {
            if (e.Location.IsFarm)
                this.UpdateFarmBuildingLocations();

            LocationUtil.ScanLocationContexts();
        }

        /// <inheritdoc cref="IInputEvents.ButtonPressed"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            //
            // Handle opening mod menu and changing tooltip options
            //

            if (!Context.IsWorldReady)
                return;

            // Minimap dragging
            if (this.ShowMinimap.Value && !Globals.LockMinimapPosition && this.Minimap.Value != null)
            {
                if (this.Minimap.Value.IsHoveringDragZone() && e.Button == SButton.MouseRight)
                {
                    MouseUtil.HandleMouseDown();
                    this.Minimap.Value.HandleMouseDown();
                }
            }

            // Debug DnD
            if (this.DebugMode && e.Button == SButton.MouseRight && this.IsModMapOpen.Value)
                MouseUtil.HandleMouseDown();

            // Minimap toggle
            if (e.Button.ToString().Equals(Globals.MinimapToggleKey) && Game1.activeClickableMenu == null)
            {
                Globals.ShowMinimap = !Globals.ShowMinimap;
                this.UpdateMinimapVisibility();
                this.Helper.Data.WriteJsonFile($"config/{Constants.SaveFolderName}.json", Config);
            }

            // ModMenu
            if (Game1.activeClickableMenu is GameMenu)
                this.HandleInput((GameMenu)Game1.activeClickableMenu, e.Button);

            if (this.DebugMode && !Context.IsMultiplayer && this.Helper.Input.GetState(SButton.LeftControl) == SButtonState.Held && e.Button.Equals(SButton.MouseRight))
                Game1.player.setTileLocation(Game1.currentCursorTile);
        }

        /// <inheritdoc cref="IInputEvents.ButtonReleased"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonReleased(object sender, ButtonReleasedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            if (this.DebugMode && e.Button == SButton.MouseRight && this.IsModMapOpen.Value)
                MouseUtil.HandleMouseRelease();
            else if (this.Minimap.Value != null && !ModEntry.Globals.LockMinimapPosition)
            {
                if (Game1.activeClickableMenu is ModMenu && e.Button == SButton.MouseLeft)
                    this.Minimap.Value.Resize();
                else if (Game1.activeClickableMenu == null && e.Button == SButton.MouseRight)
                {
                    MouseUtil.HandleMouseRelease();
                    this.Minimap.Value.HandleMouseRelease();
                }
            }
        }

        /// <inheritdoc cref="IGameLoopEvents.DayStarted"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnDayStarted(object sender = null, DayStartedEventArgs e = null)
        {
            // Check for traveling merchant day
            if (this.ConditionalNpcs.Value != null)
                this.ConditionalNpcs.Value["Merchant"] = ((Forest)Game1.getLocationFromName("Forest")).travelingMerchantDay;

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

        /// <inheritdoc cref="IGameLoopEvents.UpdateTicked"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            // update and sync markers
            if (e.IsMultipleOf(30) && Game1.currentLocation != null && Game1.player?.currentLocation != null)
            {
                bool updateForMinimap = this.ShowMinimap.Value && this.Minimap.Value != null;
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

            // update for season change (for when it's changed via console)
            if (e.IsOneSecond && Globals.UseSeasonalMaps && this.MapSeason != null && this.MapSeason != Game1.currentSeason && Game1.currentSeason != null)
            {
                this.MapSeason = Game1.currentSeason;

                // Force reload of map for season changes
                try
                {
                    this.Helper.GameContent.InvalidateCache(this.MapFilePath);
                }
                catch (Exception ex)
                {
                    this.Monitor.Log("Failed to update map for current season.", LogLevel.Error);
                    this.Monitor.Log(ex.ToString());
                }

                this.Minimap.Value?.UpdateMapForSeason();
            }

            // enable conditional NPCs who've been talked to
            if (e.IsOneSecond)
            {
                foreach (string npcName in ModConstants.ConditionalNpcs)
                {
                    if (this.ConditionalNpcs.Value[npcName])
                        continue;

                    this.ConditionalNpcs.Value[npcName] = Game1.player.friendshipData.ContainsKey(npcName);
                }
            }

            // handle minimap drag
            if (this.ShowMinimap.Value && this.Minimap.Value?.IsHoveringDragZone() == true && this.Helper.Input.GetState(SButton.MouseRight) == SButtonState.Held)
                this.Minimap.Value.HandleMouseDrag();

            // toggle mod map
            if (Game1.activeClickableMenu is not GameMenu gameMenu)
                this.IsModMapOpen.Value = false;
            else
            {
                this.HasOpenedMap.Value = gameMenu.currentTab == ModConstants.MapTabIndex; // When map accessed by switching GameMenu tab or pressing M
                this.IsModMapOpen.Value = this.HasOpenedMap.Value ? this.IsModMapOpen.Value : this.HasOpenedMap.Value; // When vanilla MapPage is replaced by ModMap

                if (this.HasOpenedMap.Value && !this.IsModMapOpen.Value) // Only run once on map open
                    this.OpenModMap();
            }
        }

        /// <inheritdoc cref="IMultiplayerEvents.PeerConnected"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnPeerConnected(object sender, PeerConnectedEventArgs e)
        {
            if (Context.IsMainPlayer)
                this.Helper.Multiplayer.SendMessage(this.Customizations.Names, ModConstants.MessageIds.SyncedNames, modIDs: new[] { this.ModManifest.UniqueID }, playerIDs: new[] { e.Peer.PlayerID });
        }

        /// <inheritdoc cref="IMultiplayerEvents.ModMessageReceived"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnModMessageReceived(object sender, ModMessageReceivedEventArgs e)
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

        /// <inheritdoc cref="IDisplayEvents.WindowResized"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnWindowResized(object sender, WindowResizedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            this.UpdateMarkers(true);
            this.UpdateFarmBuildingLocations();
            this.Minimap.Value?.OnWindowResized();

            if (this.IsModMapOpen.Value)
                this.OpenModMap();
        }

        /// <inheritdoc cref="IDisplayEvents.MenuChanged"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            MouseUtil.Reset();

            // Check for resize after mod menu closed
            if (e.OldMenu is ModMenu)
                this.Minimap.Value?.Resize();
        }

        /// <inheritdoc cref="IPlayerEvents.Warped"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnWarped(object sender, WarpedEventArgs e)
        {
            if (e.IsLocalPlayer)
            {
                // Hide minimap in blacklisted locations with special case for Mines as usual
                this.UpdateMinimapVisibility(e.NewLocation);
            }
        }

        /// <inheritdoc cref="IDisplayEvents.RenderingHud"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnRenderingHud(object sender, RenderingHudEventArgs e)
        {
            if (Context.IsWorldReady && this.ShowMinimap.Value && Game1.displayHUD)
                this.Minimap.Value?.DrawMiniMap();
        }

        /// <inheritdoc cref="IDisplayEvents.RenderedWorld"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnRenderedWorld(object sender, RenderedWorldEventArgs e)
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

        /// <inheritdoc cref="IDisplayEvents.Rendered"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnRendered(object sender, RenderedEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.player == null)
                return;

            if (this.DebugMode)
                this.ShowDebugInfo();
        }

        /// <summary>Get the outdoor location contexts which don't have any map vectors.</summary>
        private IEnumerable<LocationContext> GetLocationsWithoutMapVectors()
        {
            foreach (var entry in LocationUtil.LocationContexts)
            {
                string name = entry.Key;
                LocationContext context = entry.Value;

                string outdoorName = context.Root ?? name;
                if (!this.MapVectors.Value.ContainsKey(outdoorName) && context.Type is not (LocationType.Building or LocationType.Room))
                    yield return entry.Value;
            }
        }

        /// <summary>Get only relevant villagers for the world map.</summary>
        private List<NPC> GetVillagers()
        {
            var villagers = new List<NPC>();

            Utility.ForEachCharacter(npc =>
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

                return true;
            });

            return villagers;
        }

        /// <summary>Get the map vectors for a location name, if any.</summary>
        /// <param name="locationName">The location name.</param>
        /// <param name="customMapVectors">The custom vectors which map specific in-game tile coordinates to their map pixels.</param>
        private static MapVector[] GetMapVectors(string locationName, IDictionary<string, MapVector[]> customMapVectors)
        {
            // get a key for the specific farm type if known
            string farmKey = null;
            if (locationName == "Farm")
            {
                farmKey = Game1.whichFarm switch
                {
                    Farm.default_layout => "Farm_Default",
                    Farm.riverlands_layout => "Farm_Riverland",
                    Farm.forest_layout => "Farm_Forest",
                    Farm.mountains_layout => "Farm_Hills",
                    Farm.combat_layout => "Farm_Wilderness",
                    Farm.fourCorners_layout => "Farm_FourCorners",
                    Farm.beach_layout => "Farm_Beach",
                    Farm.mod_layout => "Farm_" + Game1.GetFarmTypeID(),
                    _ => null
                };
            }

            // get most specific vectors available
            return
                (
                    from source in new[] { customMapVectors, ModConstants.MapVectors }
                    from key in new[] { farmKey, locationName }

                    where source != null && key != null && source.ContainsKey(key)
                    select source[key]
                )
                .FirstOrDefault(p => p?.Any() == true);
        }

        // For drawing farm buildings on the map 
        // and getting positions relative to the farm 
        private void UpdateFarmBuildingLocations()
        {
            FarmBuildings.Clear();

            foreach (var building in Game1.getFarm().buildings)
            {
                // get building interior
                GameLocation indoors = building?.indoors.Value;
                if (indoors is null && building is GreenhouseBuilding && Game1.MasterPlayer.hasOrWillReceiveMail("ccPantry"))
                    indoors = Game1.getLocationFromName("Greenhouse");
                if (indoors is null)
                    continue;

                // get map marker position
                Vector2 locVector = LocationToMap(
                    "Farm", // Get building position in farm
                    building.tileX.Value,
                    building.tileY.Value,
                    this.Customizations.MapVectors,
                    this.Customizations.LocationExclusions
                );
                if (building.buildingType.Value.Contains("Barn"))
                    locVector.Y += 3;

                // track marker
                FarmBuildings[indoors.NameOrUniqueName] = new BuildingMarker(building.buildingType.Value, locVector);
            }

            // Add FarmHouse
            var farmhouseLoc = LocationToMap("FarmHouse", customMapVectors: this.Customizations.MapVectors);
            farmhouseLoc.X -= 6;
            FarmBuildings["FarmHouse"] = new("FarmHouse", farmhouseLoc);
        }

        // Handle keyboard/controller inputs
        private void HandleInput(GameMenu menu, SButton input)
        {
            if (menu.currentTab != ModConstants.MapTabIndex)
                return;

            if (input.ToString().Equals(Globals.MenuKey) || input is SButton.ControllerY)
                Game1.activeClickableMenu = new ModMenu(this.NpcMarkers.Value, this.ConditionalNpcs.Value, onMinimapToggled: () => this.UpdateMinimapVisibility());

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

        /// <summary>Update the <see cref="ShowMinimap"/> value for the current location.</summary>
        /// <param name="location">The location for which to check visibility, or <c>null</c> for the player's current location.</param>
        private void UpdateMinimapVisibility(GameLocation location = null)
        {
            location ??= Game1.currentLocation;

            this.ShowMinimap.Value = this.IsMinimapEnabledIn(location.Name, location.IsOutdoors);
        }

        /// <summary>Get whether the minimap is enabled in the given location.</summary>
        /// <param name="location">The location name.</param>
        /// <param name="isOutdoors">Whether the location is outdoors.</param>
        private bool IsMinimapEnabledIn(string location, bool isOutdoors)
        {
            if (!Globals.ShowMinimap)
                return false;

            // by exact name
            if (Globals.MinimapExclusions.Contains(location))
                return false;

            // mine entrances
            switch (location.ToLower())
            {
                case "mine" when Globals.MinimapExclusions.Contains("Mines"):
                    return false;

                // skull cavern entrance
                case "skullcave" when Globals.MinimapExclusions.Contains("SkullCavern"):
                    return false;
            }

            // mine levels
            if (location.StartsWith("UndergroundMine") && int.TryParse(location.Substring("UndergroundMine".Length), out int mineLevel))
            {
                if (Globals.MinimapExclusions.Contains(mineLevel > 120 ? "SkullCavern" : "Mines"))
                    return false;
            }

            // Deep Woods mod
            if ((location == "DeepWoods" || location.StartsWith("DeepWoods_")) && Globals.MinimapExclusions.Contains("DeepWoods"))
                return false;

            // indoors/outdoors
            if (Globals.MinimapExclusions.Contains(isOutdoors ? "Outdoors" : "Indoors"))
                return false;

            return true;
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
                            IsBirthday = npc.isBirthday(),
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
            if (this.NpcMarkers.Value == null)
                return;

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
                    string playerLocationName = Game1.player.currentLocation.uniqueName.Value ?? Game1.player.currentLocation.Name;
                    if (locationName == playerLocationName)
                        isSameLocation = true;
                    else if (LocationUtil.TryGetContext(locationName, out var npcLocCtx) && LocationUtil.TryGetContext(playerLocationName, out var playerLocCtx))
                        isSameLocation = npcLocCtx.Root == playerLocCtx.Root;
                }

                // NPCs that won't be shown on the map unless 'Show Hidden NPCs' is checked
                this.SetMarkerHiddenIfNeeded(npcMarker, npc.Name, isSameLocation);

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
                    var npcLocation = LocationToMap(locationName, npc.TilePoint.X, npc.TilePoint.Y, this.Customizations.MapVectors, this.Customizations.LocationExclusions);
                    npcMarker.MapX = (int)npcLocation.X - 16;
                    npcMarker.MapY = (int)npcLocation.Y - 15;
                }
            }
        }

        // Update npc marker properties only relevant to farmhand
        private void UpdateNpcsFarmhand()
        {
            if (this.NpcMarkers.Value == null)
                return;

            foreach (var npcMarker in this.NpcMarkers.Value)
            {
                string name = npcMarker.Key;
                var marker = npcMarker.Value;

                // For show Npcs in player's location option
                bool isSameLocation = false;
                if (Globals.OnlySameLocation)
                {
                    string playerLocationName = Game1.player.currentLocation.uniqueName.Value ?? Game1.player.currentLocation.Name;
                    if (marker.LocationName == playerLocationName)
                        isSameLocation = true;
                    else if (LocationUtil.TryGetContext(marker.LocationName, out var npcLocCtx) && LocationUtil.TryGetContext(playerLocationName, out var playerLocCtx))
                        isSameLocation = npcLocCtx.Root == playerLocCtx.Root;
                }

                // NPCs that won't be shown on the map unless 'Show Hidden NPCs' is checked
                this.SetMarkerHiddenIfNeeded(marker, name, isSameLocation);

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

        /// <summary>Set an NPC marker to hidden if applicable based on the current config.</summary>
        /// <param name="marker">The NPC marker.</param>
        /// <param name="name">The NPC name.</param>
        /// <param name="isSameLocation">Whether the NPC is in the same location as the current player.</param>
        private void SetMarkerHiddenIfNeeded(NpcMarker marker, string name, bool isSameLocation)
        {
            marker.IsHidden = false;
            marker.ReasonHidden = null;

            void Hide(string reason)
            {
                marker.IsHidden = true;
                marker.ReasonHidden = reason;
            }

            if (ModEntry.ShouldExcludeNpc(name, out string reason))
                Hide($"hidden per config ({reason})");
            else if (Config.ImmersionOption == 2 && !Game1.player.hasTalkedToFriendToday(name))
                Hide("hidden per config (didn't talk to them today)");
            else if (Config.ImmersionOption == 3 && Game1.player.hasTalkedToFriendToday(name))
                Hide("hidden per config (talked to them today)");
            else if (Globals.OnlySameLocation && !isSameLocation)
                Hide("hidden per config (not in same location)");
            else if (Config.ByHeartLevel)
            {
                int hearts = Game1.player.getFriendshipHeartLevelForNPC(name);
                if (Config.HeartLevelMin > 0 && hearts < Config.HeartLevelMin)
                    Hide($"hidden per config (less than {Config.HeartLevelMin} hearts)");
                if (Config.HeartLevelMax < PlayerConfig.MaxPossibleHeartLevel && hearts > Config.HeartLevelMax)
                    Hide($"hidden per config (more than {Config.HeartLevelMax} hearts)");
            }
        }

        private void UpdateFarmers()
        {
            foreach (var farmer in Game1.getOnlineFarmers())
            {
                if (farmer?.currentLocation == null) continue;
                string locationName = farmer.currentLocation.uniqueName.Value ?? farmer.currentLocation.Name;

                locationName = LocationUtil.GetLocationNameFromLevel(locationName) ?? locationName;

                long farmerId = farmer.UniqueMultiplayerID;
                var farmerLoc = LocationToMap(
                    locationName,
                    farmer.TilePoint.X,
                    farmer.TilePoint.Y,
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
