using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Bouhm.Shared;
using Bouhm.Shared.Integrations.GenericModConfigMenu;
using Bouhm.Shared.Locations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using NPCMapLocations.Framework;
using NPCMapLocations.Framework.Menus;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Quests;
using StardewValley.WorldMaps;

namespace NPCMapLocations;

/// <summary>The mod entry class.</summary>
public class ModEntry : Mod
{
    /*********
    ** Fields
    *********/
    /// <summary>The map markers for farm buildings, indexed by the interior unique name.</summary>
    private static readonly Dictionary<string, BuildingMarker> FarmBuildings = [];

    private readonly PerScreen<Texture2D?> BuildingMarkers = new();
    private readonly PerScreen<ModMinimap?> Minimap = new();
    private readonly PerScreen<Dictionary<string, NpcMarker>> NpcMarkers = new(() => []);
    private readonly PerScreen<bool> HasOpenedMap = new();
    private readonly PerScreen<bool> IsModMapOpen = new();
    private readonly PerScreen<bool> IsFirstDay = new();

    /// <summary>Whether to show the minimap.</summary>
    private readonly PerScreen<bool> ShowMinimap = new();

    /// <summary>The integration with the Better Game Menu mod.</summary>
    private BetterGameMenuIntegration? BetterGameMenuIntegration;

    /// <summary>Scans and maps locations in the game world.</summary>
    private static LocationUtil LocationUtil = null!; // set in Entry

    /// <summary>The internal mod data.</summary>
    private DataModel Data = null!; // set in Entry

    // External mod settings
    private readonly string NpcCustomizationsPath = "Mods/Bouhm.NPCMapLocations/NPCs";
    private readonly string LocationCustomizationsPath = "Mods/Bouhm.NPCMapLocations/Locations";

    // Multiplayer
    private readonly PerScreen<Dictionary<long, FarmerMarker>> FarmerMarkers = new(() => []);

    // Customizations/Custom mods
    private ModCustomizations Customizations = null!; // set in Entry


    /*********
    ** Accessors
    *********/
    /// <summary>The mod settings.</summary>
    public static ModConfig Config = null!;        // set in Entry
    public static IModHelper StaticHelper = null!; // set in Entry


    /*********
    ** Public methods
    *********/
    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        // init
        this.MigrateLegacyFiles();
        I18n.Init(helper.Translation);
        ModEntry.LocationUtil = new(this.Monitor);
        StaticHelper = helper;
        Config = helper.ReadConfig<ModConfig>();
        this.Customizations = new ModCustomizations(this.OnConfigEdited);

        // read data file
        const string dataPath = "assets/data.json";
        try
        {
            DataModel? data = this.Helper.Data.ReadJsonFile<DataModel>(dataPath);
            if (data == null)
            {
                data = new DataModel(null, null);
                this.Monitor.Log($"The {dataPath} file seems to be missing or invalid. You can reinstall the mod to fix that.", LogLevel.Warn);
            }
            this.Data = data;
        }
        catch (Exception ex)
        {
            this.Data = new DataModel(null, null);
            this.Monitor.Log($"The {dataPath} file seems to be missing or invalid. You can reinstall the mod to fix that.", LogLevel.Warn);
            this.Monitor.Log(ex.ToString());
        }

        // hook events
        helper.Events.Content.AssetRequested += this.OnAssetRequested;
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.GameLoop.DayStarted += this.OnDayStarted;
        helper.Events.World.BuildingListChanged += this.OnBuildingListChanged;
        helper.Events.Input.ButtonsChanged += this.OnButtonsChanged;
        helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        helper.Events.Input.ButtonReleased += this.OnButtonReleased;
        helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
        helper.Events.Player.Warped += this.OnWarped;
        helper.Events.Display.MenuChanged += this.OnMenuChanged;
        helper.Events.Display.RenderingHud += this.OnRenderingHud;
        helper.Events.Display.WindowResized += this.OnWindowResized;
        helper.Events.Multiplayer.PeerConnected += this.OnPeerConnected;
        helper.Events.Multiplayer.ModMessageReceived += this.OnModMessageReceived;

        // add console command
        helper.ConsoleCommands.Add(SummaryCommand.Name, SummaryCommand.GetDescription(), (_, _) => SummaryCommand.Handle(
            monitor: this.Monitor,
            locationUtil: ModEntry.LocationUtil,
            customizations: this.Customizations,
            npcMarkers: this.NpcMarkers.Value,
            locationsWithoutMapPositions: this.GetLocationsWithoutMapPosition()
        ));
    }

    /// <inheritdoc cref="IGameLoopEvents.SaveLoaded"/>
    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(this.LocationCustomizationsPath) || e.NameWithoutLocale.IsEquivalentTo(this.NpcCustomizationsPath))
        {
            e.LoadFrom(() => new Dictionary<string, JObject>(), AssetLoadPriority.Exclusive);
        }
    }

    /// <summary>Get the world map position which matches an in-world tile coordinate.</summary>
    /// <param name="locationName">The in-world location name.</param>
    /// <param name="tileX">The X tile position within the location to match.</param>
    /// <param name="tileY">The Y tile position within the location to match.</param>
    /// <param name="locationExclusions">The locations to ignore when scanning locations for players and NPCs.</param>
    public static WorldMapPosition GetWorldMapPosition(string? locationName, int tileX = 0, int tileY = 0, HashSet<string>? locationExclusions = null)
    {
        string? originalLocationName = locationName;
        HashSet<string> seen = [];

        try
        {
            Point tile = new Point(tileX, tileY);
            int depth = 0;

            while (!string.IsNullOrWhiteSpace(locationName))
            {
                // special case: map generated level to single name
                locationName = LocationUtil.GetLocationNameFromLevel(locationName) ?? locationName;
                if (locationName is "VolcanoDungeon0")
                    locationName = "Caldera"; // avoid generating volcano level

                // break infinite loops
                if (!seen.Add(locationName))
                    return WorldMapPosition.Empty;
                if (++depth > LocationUtil.MaxRecursionDepth)
                    throw new InvalidOperationException($"Infinite recursion detected in location scan. Technical details:\n{nameof(locationName)}: {locationName}\n{nameof(tileX)}: {tileX}\n{nameof(tileY)}: {tileY}\n\n{Environment.StackTrace}");

                // special case: inside farm building
                if (FarmBuildings.TryGetValue(locationName, out BuildingMarker? marker))
                    return marker.WorldMapPosition;

                // get location
                GameLocation location = Game1.getLocationFromName(locationName);
                if (location is null)
                    return WorldMapPosition.Empty;

                // get map pixel from game data if found
                MapAreaPositionWithContext? mapAreaPos = WorldMapManager.GetPositionData(location, tile);
                if (mapAreaPos != null)
                    return WorldMapPosition.Create(mapAreaPos.Value);

                // else try from parent, unless this location is blacklisted
                if (locationExclusions?.Contains(locationName) != true && !locationName.Contains("WarpRoom") && LocationUtil.TryGetContext(locationName, out var loc))
                {
                    locationName = loc.Parent;
                    tile = Utility.Vector2ToPoint(loc.Warp);
                }
                else
                    break; // not found
            }

            return WorldMapPosition.Empty;
        }
        catch (Exception ex)
        {
            string locationPath;
            if (seen.Count == 0)
                locationPath = $"'{locationName}'";
            else
            {
                locationPath = $"'{string.Join("' > '", seen)}'";
                if (originalLocationName != seen.First())
                    locationPath = $"'{originalLocationName}' -> {locationPath}";
            }

            throw new InvalidOperationException($"Failed getting world map position for location {locationPath} ({tileX}, {tileY}).", ex);
        }
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
    public static bool ShouldExcludeNpc(string name, [NotNullWhen(true)] out string? reason, bool ignoreConfig = false)
    {
        // from config override
        if (!ignoreConfig && ModEntry.Config.NpcVisibility.TryGetValue(name, out bool forceVisibility))
        {
            if (!forceVisibility)
            {
                reason = "excluded in player settings";
                return true;
            }
        }

        // from mod override
        else if (ModEntry.Config.ModNpcExclusions.Contains(name))
        {
            reason = "excluded by mod override";
            return true;
        }

        // not hidden
        reason = null;
        return false;
    }


    /*********
    ** Private methods
    *********/
    /// <inheritdoc cref="IGameLoopEvents.GameLaunched"/>
    [EventPriority(EventPriority.Low)]
    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        // register config UI
        this.RegisterConfigMenu();

        // register map page with Better Game Menu
        this.BetterGameMenuIntegration = new BetterGameMenuIntegration(this.CreateMapPage)
            .Register(this.Helper.ModRegistry, this.Monitor);
    }

    /// <inheritdoc cref="IGameLoopEvents.SaveLoaded"/>
    [EventPriority(EventPriority.Low)]
    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        //
        // Load config and other one-off data
        //

        this.IsFirstDay.Value = true;
        this.NpcMarkers.Value.Clear();
        this.FarmerMarkers.Value.Clear();

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
            this.BuildingMarkers.Value = this.Helper.ModContent.Load<Texture2D>("assets/buildings.png");
        }
        catch
        {
            this.BuildingMarkers.Value = null;
        }

        // Get context of all locations (indoor, outdoor, relativity)
        LocationUtil.ScanLocationContexts();

        this.UpdateFarmBuildingLocations();

        // Log warning if host does not have mod installed
        if (!Context.IsMainPlayer)
        {
            bool hostHasMod = false;

            foreach (IMultiplayerPeer peer in this.Helper.Multiplayer.GetConnectedPlayers())
            {
                if (peer.IsHost)
                {
                    hostHasMod = peer.GetMod(this.ModManifest.UniqueID) != null;
                    break;
                }
            }

            if (!hostHasMod && !Context.IsMainPlayer)
                this.Monitor.Log("Since the server host doesn't have NPC Map Locations installed, NPC locations can't be synced.", LogLevel.Warn);
        }

        // enable minimap
        this.UpdateMinimapVisibility();

        // update
        this.RegisterConfigMenu();
    }

    /// <inheritdoc cref="IWorldEvents.BuildingListChanged"/>
    private void OnBuildingListChanged(object? sender, BuildingListChangedEventArgs e)
    {
        if (e.Location.IsFarm)
            this.UpdateFarmBuildingLocations();

        LocationUtil.ScanLocationContexts();
    }

    /// <inheritdoc cref="IInputEvents.ButtonsChanged"/>
    private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        // toggle minimap
        if (Config.MinimapToggleKey.JustPressed() && Game1.activeClickableMenu is null)
        {
            Config.ShowMinimap = !Config.ShowMinimap;
            this.UpdateMinimapVisibility();
            this.Helper.WriteConfig(Config);
        }

        // check map keybinds
        else if (Game1.activeClickableMenu is GameMenu menu && menu.currentTab == ModConstants.MapTabIndex && menu.GetChildMenu() is null)
        {
            // open config UI
            if (Config.MenuKey.JustPressed())
            {
                if (this.RegisterConfigMenu() is { } configMenu)
                    configMenu.OpenModMenuAsChildMenu(this.ModManifest);
                else
                    this.Monitor.LogOnce("You must install Generic Mod Config Menu to configure this mod.", LogLevel.Warn);
            }

            // change tooltip mode
            else if (Config.TooltipKey.JustPressed() || e.Pressed.Contains(SButton.RightShoulder))
                this.ChangeTooltipConfig();
            else if (e.Pressed.Contains(SButton.LeftShoulder))
                this.ChangeTooltipConfig(false);
        }
    }

    /// <inheritdoc cref="IInputEvents.ButtonPressed"/>
    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        // start minimap dragging
        if (this.ShowMinimap.Value && !Config.LockMinimapPosition && this.Minimap.Value != null)
        {
            if (this.Minimap.Value.IsHoveringDragZone() && e.Button == SButton.MouseRight)
            {
                MouseUtil.HandleMouseDown();
                this.Minimap.Value.HandleMouseRightDown();
            }
        }
    }

    /// <inheritdoc cref="IInputEvents.ButtonReleased"/>
    private void OnButtonReleased(object? sender, ButtonReleasedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        // stop minimap dragging
        if (this.Minimap.Value != null && !ModEntry.Config.LockMinimapPosition)
        {
            if (Game1.activeClickableMenu == null && e.Button == SButton.MouseRight)
            {
                MouseUtil.HandleMouseRelease();
                this.Minimap.Value.HandleMouseRightRelease();
            }
        }
    }

    /// <inheritdoc cref="IGameLoopEvents.DayStarted"/>
    [EventPriority(EventPriority.Low)] // let other mods initialize their locations/NPCs first
    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        // Log any custom locations not handled in content.json
        if (this.IsFirstDay.Value)
        {
            this.IsFirstDay.Value = false;

            string[] unknownLocations = this.GetLocationsWithoutMapPosition().Select(p => p.Name).OrderBy(p => p).ToArray();
            if (unknownLocations.Any())
                this.Monitor.Log($"Unknown locations: {string.Join(", ", unknownLocations)}");
        }

        this.ResetMarkers();
        this.UpdateMarkers(true);

        this.Minimap.Value?.Dispose();
        this.Minimap.Value = new ModMinimap(this.CreateMapPage);
    }

    /// <inheritdoc cref="IGameLoopEvents.UpdateTicked"/>
    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        // update and sync markers
        if (Game1.currentLocation != null && Game1.player.currentLocation != null)
        {
            bool? hasMinimap = null;

            // update local minimap display
            if (e.IsMultipleOf(Config.MiniMapCacheTicks))
            {
                if (this.ShowMinimap.Value && this.Minimap.Value != null)
                {
                    hasMinimap = true;
                    this.Minimap.Value.Update();
                }
                else
                    hasMinimap = false;
            }

            // update & sync NPC markers
            if (e.IsMultipleOf(Config.NpcCacheTicks))
            {
                hasMinimap ??= this.ShowMinimap.Value && this.Minimap.Value != null;

                this.UpdateMarkers(hasMinimap.Value || Context.IsMainPlayer);

                // Sync multiplayer data
                if (Context.IsMainPlayer && Context.IsMultiplayer)
                {
                    Dictionary<string, SyncedNpcMarker> syncedMarkers = [];

                    foreach (var npcMarker in this.NpcMarkers.Value)
                    {
                        syncedMarkers.Add(npcMarker.Key, new SyncedNpcMarker
                        {
                            DisplayName = npcMarker.Value.DisplayName,
                            LocationName = npcMarker.Value.LocationName,
                            IsOutdoors = LocationUtil.IsOutdoors(npcMarker.Value.LocationName),
                            WorldMapPosition = npcMarker.Value.WorldMapPosition,
                            IsBirthday = npcMarker.Value.IsBirthday,
                            Type = npcMarker.Value.Type
                        });
                    }

                    this.Helper.Multiplayer.SendMessage(syncedMarkers, ModConstants.MessageIds.SyncedNpcMarkers, modIDs: [this.ModManifest.UniqueID]);
                }
            }
        }

        // handle minimap drag
        if (this.ShowMinimap.Value && this.Minimap.Value?.IsHoveringDragZone() == true && this.Helper.Input.GetState(SButton.MouseRight) == SButtonState.Held)
            this.Minimap.Value.HandleMouseRightDrag();

        // toggle mod map
        if (Game1.activeClickableMenu is not GameMenu gameMenu)
            this.IsModMapOpen.Value = this.BetterGameMenuIntegration?.IsMenuOpen ?? false;
        else
        {
            this.HasOpenedMap.Value = gameMenu.currentTab == ModConstants.MapTabIndex; // When map accessed by switching GameMenu tab or pressing M
            this.IsModMapOpen.Value = this.HasOpenedMap.Value ? this.IsModMapOpen.Value : this.HasOpenedMap.Value; // When vanilla MapPage is replaced by ModMap

            if (this.HasOpenedMap.Value && !this.IsModMapOpen.Value) // Only run once on map open
                this.OpenModMap();
        }
    }

    /// <inheritdoc cref="IMultiplayerEvents.PeerConnected"/>
    private void OnPeerConnected(object? sender, PeerConnectedEventArgs e)
    {
        if (Context.IsMainPlayer)
            this.Helper.Multiplayer.SendMessage(this.Customizations.Names, ModConstants.MessageIds.SyncedNames, modIDs: [this.ModManifest.UniqueID], playerIDs: [e.Peer.PlayerID]);
    }

    /// <inheritdoc cref="IMultiplayerEvents.ModMessageReceived"/>
    private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
        if (!Context.IsMainPlayer && e.FromModID == this.ModManifest.UniqueID && e.FromPlayerID == Game1.MasterPlayer.UniqueMultiplayerID)
        {
            switch (e.Type)
            {
                case ModConstants.MessageIds.SyncedNames:
                    this.Customizations.Names = e.ReadAs<Dictionary<string, string>>();
                    break;

                case ModConstants.MessageIds.SyncedNpcMarkers:
                    var syncedNpcMarkers = e.ReadAs<Dictionary<string, SyncedNpcMarker>>();
                    foreach ((string internalName, SyncedNpcMarker syncedMarker) in syncedNpcMarkers)
                    {
                        int offset = ModEntry.Config.NpcMarkerOffsets.GetValueOrDefault(internalName, 0);

                        if (!this.NpcMarkers.Value.TryGetValue(internalName, out var npcMarker))
                        {
                            npcMarker = new NpcMarker();
                            this.NpcMarkers.Value.Add(internalName, npcMarker);
                        }

                        npcMarker.LocationName = syncedMarker.LocationName;
                        npcMarker.IsOutdoors = syncedMarker.IsOutdoors;
                        npcMarker.WorldMapPosition = syncedMarker.WorldMapPosition;
                        npcMarker.DisplayName = syncedMarker.DisplayName;
                        npcMarker.CropOffset = offset;
                        npcMarker.IsBirthday = syncedMarker.IsBirthday;
                        npcMarker.Type = syncedMarker.Type;

                        try
                        {
                            if (syncedMarker.Type is CharacterType.Villager or CharacterType.Raccoon)
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
    private void OnWindowResized(object? sender, WindowResizedEventArgs e)
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
    private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        MouseUtil.Reset();
    }

    /// <inheritdoc cref="IPlayerEvents.Warped"/>
    private void OnWarped(object? sender, WarpedEventArgs e)
    {
        if (e.IsLocalPlayer)
        {
            // Hide minimap in blacklisted locations with special case for Mines as usual
            this.UpdateMinimapVisibility(e.NewLocation);
        }
    }

    /// <inheritdoc cref="IDisplayEvents.RenderingHud"/>
    private void OnRenderingHud(object? sender, RenderingHudEventArgs e)
    {
        if (Context.IsWorldReady && this.ShowMinimap.Value && Game1.displayHUD && !Game1.game1.takingMapScreenshot)
            this.Minimap.Value?.Draw();
    }

    /// <summary>Register or update the config UI with Generic Mod Config Menu.</summary>
    private IGenericModConfigMenuApi? RegisterConfigMenu()
    {
        var configMenu = new GenericModConfigMenuIntegration(this.ModManifest, this.Helper.ModRegistry, this.ResetConfig, this.OnConfigEdited, () => this.NpcMarkers.Value);
        configMenu.Register();
        return configMenu.ConfigMenu;
    }

    /// <summary>Get the outdoor location contexts which don't have any map vectors.</summary>
    private IEnumerable<LocationContext> GetLocationsWithoutMapPosition()
    {
        foreach ((string name, LocationContext context) in LocationUtil.LocationContexts)
        {
            if (GetWorldMapPosition(name).IsEmpty && context.Type is not (LocationType.Building or LocationType.Room))
                yield return context;
        }
    }

    /// <summary>Get only relevant villagers for the world map.</summary>
    private List<NPC> GetVillagers()
    {
        List<NPC> villagers = [];

        Utility.ForEachCharacter(npc =>
        {
            bool shouldTrack =
                npc is { IsInvisible: false }
                && (
                    npc.IsVillager
                    || npc.isMarried()
                    || (Config.ShowHorse && npc is Horse)
                    || (Config.ShowChildren && npc is Child)
                );

            if (shouldTrack && !villagers.Contains(npc))
                villagers.Add(npc);

            return true;
        });

        return villagers;
    }

    // For drawing farm buildings on the map
    // and getting positions relative to the farm
    private void UpdateFarmBuildingLocations()
    {
        FarmBuildings.Clear();

        foreach (Building? building in Game1.getFarm().buildings)
        {
            if (building is null)
                continue;

            // get building interior
            GameLocation? indoors = building.indoors.Value;
            if (indoors is null && building is GreenhouseBuilding && Game1.MasterPlayer.hasOrWillReceiveMail("ccPantry"))
                indoors = Game1.getLocationFromName("Greenhouse");
            if (indoors is null)
                continue;

            // get map marker position
            WorldMapPosition position = GetWorldMapPosition("Farm", building.tileX.Value, building.tileY.Value, this.Customizations.LocationExclusions); // Get building position in farm
            if (building.buildingType.Value.Contains("Barn"))
                position = position with { Y = position.Y + 3 };

            // track marker
            FarmBuildings[indoors.NameOrUniqueName] = new BuildingMarker(building.buildingType.Value, position);
        }

        // Add FarmHouse
        {
            WorldMapPosition position = GetWorldMapPosition("FarmHouse");
            position = position with { X = position.X - 6 };
            FarmBuildings["FarmHouse"] = new("FarmHouse", position);
        }
    }

    private void ChangeTooltipConfig(bool increment = true)
    {
        if (increment)
        {
            if (++Config.NameTooltipMode > 3)
                Config.NameTooltipMode = 1;
        }
        else
        {
            if (--Config.NameTooltipMode < 1)
                Config.NameTooltipMode = 3;
        }

        this.Helper.WriteConfig(Config);
    }

    /// <summary>Update the <see cref="ShowMinimap"/> value for the current location.</summary>
    /// <param name="location">The location for which to check visibility, or <c>null</c> for the player's current location.</param>
    private void UpdateMinimapVisibility(GameLocation? location = null)
    {
        if (!Context.IsWorldReady)
            return;

        location ??= Game1.currentLocation;

        this.ShowMinimap.Value = this.IsMinimapEnabledIn(location.Name, location.IsOutdoors);
    }

    /// <summary>Get whether the minimap is enabled in the given location.</summary>
    /// <param name="location">The location name.</param>
    /// <param name="isOutdoors">Whether the location is outdoors.</param>
    private bool IsMinimapEnabledIn(string location, bool isOutdoors)
    {
        if (!Config.ShowMinimap)
            return false;

        // by exact name
        if (Config.MinimapExclusions.Contains(location))
            return false;

        // mine entrances
        switch (location.ToLower())
        {
            case "mine" when Config.MinimapExclusions.Contains("Mines"):
                return false;

            // skull cavern entrance
            case "skullcave" when Config.MinimapExclusions.Contains("SkullCavern"):
                return false;
        }

        // mine levels
        if (location.StartsWith("UndergroundMine") && int.TryParse(location.Substring("UndergroundMine".Length), out int mineLevel))
        {
            if (Config.MinimapExclusions.Contains(mineLevel > 120 ? "SkullCavern" : "Mines"))
                return false;
        }

        // Deep Woods mod
        if ((location == "DeepWoods" || location.StartsWith("DeepWoods_")) && Config.MinimapExclusions.Contains("DeepWoods"))
            return false;

        // indoors/outdoors
        if (Config.MinimapExclusions.Contains(isOutdoors ? "Outdoors" : "Indoors"))
            return false;

        return true;
    }

    /// <summary>Scan the world for NPCs and initialize the map markers for the day.</summary>
    private void ResetMarkers()
    {
        this.NpcMarkers.Value.Clear();
        this.FarmerMarkers.Value.Clear();

        if (Context.IsMainPlayer)
        {
            // bookseller
            if (ModEntry.Config.ShowBookseller && Utility.getDaysOfBooksellerThisSeason().Contains(Game1.dayOfMonth))
            {
                WorldMapPosition mapPos = GetWorldMapPosition("Town", 108, 25); // hardcoded in Town.draw

                this.NpcMarkers.Value.Add("Bookseller", new NpcMarker
                {
                    DisplayName = I18n.MarkerNames_Bookseller(),
                    LocationName = "Town",
                    IsOutdoors = true,
                    CropOffset = 0,
                    Sprite = Game1.mouseCursors_1_6,
                    SpriteSourceRect = new Rectangle(180, 490, 14, 18),
                    SpriteZoom = 1.5f,
                    WorldMapPosition = mapPos
                });
            }

            // traveling cart
            if (ModEntry.Config.ShowTravelingMerchant && Game1.RequireLocation<Forest>("Forest").ShouldTravelingMerchantVisitToday())
            {
                Forest forest = Game1.RequireLocation<Forest>("Forest");
                Point cartTile = forest.GetTravelingMerchantCartTile();
                WorldMapPosition mapPos = GetWorldMapPosition("Forest", cartTile.X + 4, cartTile.Y);

                this.NpcMarkers.Value.Add("Merchant", new NpcMarker
                {
                    DisplayName = I18n.MarkerNames_Merchant(),
                    LocationName = "Forest",
                    IsOutdoors = true,
                    CropOffset = 0,
                    Sprite = Game1.mouseCursors,
                    SpriteSourceRect = new Rectangle(191, 1410, 22, 21),
                    SpriteZoom = 1.3f,
                    WorldMapPosition = mapPos
                });
            }

            // villagers
            foreach (NPC npc in this.GetVillagers())
                this.ResetMarker(npc);
        }
    }

    /// <summary>Add or reset the map marker for an NPC, if it's valid.</summary>
    /// <returns>Returns the created NPC marker, or <c>null</c> if the NPC should be ignored.</returns>
    private NpcMarker? ResetMarker(NPC npc)
    {
        if (npc.SimpleNonVillagerNPC)
            return null;

        // get type name
        string? typeName = npc.GetType().FullName;
        if (typeName is null || this.Data.IgnoreNpcTypes.Contains(typeName))
            return null;

        // get type
        CharacterType type = npc switch
        {
            Horse => CharacterType.Horse,
            Child => CharacterType.Child,
            Raccoon => CharacterType.Raccoon,
            _ => CharacterType.Villager
        };

        // get display name
        string displayName = npc.Name switch
        {
            "Raccoon" when type is CharacterType.Raccoon => I18n.MarkerNames_MisterRaccoon(),
            "MrsRaccoon" when type is CharacterType.Raccoon => I18n.MarkerNames_MrsRaccoon(),
            _ => string.IsNullOrWhiteSpace(npc.displayName) ? npc.Name : npc.displayName
        };

        // get texture
        Texture2D? texture = null;
        try
        {
            texture = new AnimatedSprite(npc.Sprite.loadedTexture, 0, 16, 32).Texture;
        }
        catch (Exception ex)
        {
            this.Monitor.Log($"Couldn't load marker for NPC '{npc.Name}'.", LogLevel.Warn);
            this.Monitor.Log(ex.ToString());
        }

        // get crop area
        int offset = ModEntry.Config.NpcMarkerOffsets.GetValueOrDefault(npc.Name, 0);
        Rectangle? vanillaMugShotSourceRect = type is CharacterType.Villager
            ? npc.getMugShotSourceRect()
            : null;

        // build marker
        NpcMarker newMarker = new()
        {
            DisplayName = displayName,
            CropOffset = offset,
            IsBirthday = npc.isBirthday(),
            Sprite = texture,
            VanillaMugShotSourceRect = vanillaMugShotSourceRect,
            Type = type
        };
        this.NpcMarkers.Value[npc.Name] = newMarker;
        return newMarker;
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
        pages[ModConstants.MapTabIndex] = this.CreateMapPage(gameMenu);
    }

    /// <summary>Create the map page for a menu instance.</summary>
    /// <param name="menu">The game menu for which to create a map view.</param>
    private ModMapPage CreateMapPage(IClickableMenu menu)
    {
        return new ModMapPage(
            menu.xPositionOnScreen,
            menu.yPositionOnScreen,
            menu.width,
            menu.height,

            this.NpcMarkers.Value,
            this.FarmerMarkers.Value,
            FarmBuildings,
            this.BuildingMarkers.Value,
            this.Customizations,
            LocationUtil
        );
    }

    /// <summary>Create a standalone map page.</summary>
    private ModMapPage CreateMapPage()
    {
        GameMenu menu = new GameMenu(playOpeningSound: false);
        return this.CreateMapPage(menu);
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
        if (this.NpcMarkers.Value.Count == 0)
            return;

        // get NPCs in the world
        List<NPC> npcList = this.GetVillagers();
        if (Game1.player.isRidingHorse())
            npcList.Add(Game1.player.mount); // add horse if player is riding it

        // update each NPC
        foreach (NPC npc in npcList)
        {
            // handle NPCs added later
            if (!this.NpcMarkers.Value.TryGetValue(npc.Name, out NpcMarker? npcMarker))
            {
                // If an NPC appears after we initialized markers, add it to the list now.
                // This mainly affects custom NPCs added to the game outside the normal game lifecycle (e.g. via Little
                // NPCs).
                if (Context.IsMainPlayer)
                    npcMarker = this.ResetMarker(npc);
            }
            if (npcMarker is null || npc.currentLocation is null)
                continue;

            // update info
            string locationName = npc.currentLocation.uniqueName.Value ?? npc.currentLocation.Name;
            npcMarker.LocationName = locationName;
            npcMarker.IsOutdoors = LocationUtil.IsOutdoors(locationName);
            npcMarker.HasQuest = this.HasQuest(npc.Name);
            if (locationName != null)
                npcMarker.WorldMapPosition = GetWorldMapPosition(locationName, npc.TilePoint.X, npc.TilePoint.Y, this.Customizations.LocationExclusions);

            // apply NPC conditions
            if (!Config.ShowHiddenVillagers)
            {
                if (this.Data.Npcs.TryGetValue(npc.Name, out DataNpcModel? data) && data.Visible != null)
                {
                    bool hidden = !GameStateQuery.CheckConditions(data.Visible);
                    this.SetMarkerHiddenIfNeeded(npcMarker, npc.Name, hidden);
                }
            }

            // apply 'show NPCs in location' option
            {
                bool isSameLocation = false;

                if (Config.OnlySameLocation)
                {
                    string playerLocationName = Game1.player.currentLocation.uniqueName.Value ?? Game1.player.currentLocation.Name;
                    if (locationName == playerLocationName)
                        isSameLocation = true;
                    else if (LocationUtil.TryGetContext(locationName, out var npcLocCtx) && LocationUtil.TryGetContext(playerLocationName, out var playerLocCtx))
                        isSameLocation = npcLocCtx.Root == playerLocCtx.Root;
                }

                this.SetMarkerHiddenIfNeeded(npcMarker, npc.Name, isSameLocation);
            }

            // set draw layer
            npcMarker.RecalculateDrawLayer();
        }
    }

    // Update npc marker properties only relevant to farmhand
    private void UpdateNpcsFarmhand()
    {
        foreach ((string name, NpcMarker marker) in this.NpcMarkers.Value)
        {
            // apply NPC conditions
            if (!Config.ShowHiddenVillagers)
            {
                if (this.Data.Npcs.TryGetValue(name, out DataNpcModel? data) && data.Visible != null)
                {
                    bool hidden = !GameStateQuery.CheckConditions(data.Visible);
                    this.SetMarkerHiddenIfNeeded(marker, name, hidden);
                }
            }

            // apply 'show NPCs in location' option
            {
                bool isSameLocation = false;

                if (Config.OnlySameLocation)
                {
                    string playerLocationName = Game1.player.currentLocation.uniqueName.Value ?? Game1.player.currentLocation.Name;
                    if (marker.LocationName == playerLocationName)
                        isSameLocation = true;
                    else if (LocationUtil.TryGetContext(marker.LocationName, out var npcLocCtx) && LocationUtil.TryGetContext(playerLocationName, out var playerLocCtx))
                        isSameLocation = npcLocCtx.Root == playerLocCtx.Root;
                }

                this.SetMarkerHiddenIfNeeded(marker, name, isSameLocation);
            }

            // update info
            marker.HasQuest = this.HasQuest(name);
            marker.RecalculateDrawLayer();
        }
    }

    /// <summary>Get whether an NPC has an active quest.</summary>
    /// <param name="npcName">The NPC internal name to match.</param>
    private bool HasQuest(string npcName)
    {
        foreach (Quest quest in Game1.player.questLog)
        {
            if (quest.accepted.Value && quest.dailyQuest.Value && !quest.completed.Value)
            {
                string? questTarget = quest switch
                {
                    ItemDeliveryQuest itemDeliveryQuest => itemDeliveryQuest.target.Value,
                    SlayMonsterQuest slayMonsterQuest => slayMonsterQuest.target.Value,
                    FishingQuest fishingQuest => fishingQuest.target.Value,
                    ResourceCollectionQuest resourceCollectionQuest => resourceCollectionQuest.target.Value,
                    _ => null
                };

                if (questTarget == npcName)
                    return true;
            }
        }

        return false;
    }

    /// <summary>Set an NPC marker to hidden if applicable based on the current config.</summary>
    /// <param name="marker">The NPC marker.</param>
    /// <param name="name">The NPC name.</param>
    /// <param name="isSameLocation">Whether the NPC is in the same location as the current player.</param>
    private void SetMarkerHiddenIfNeeded(NpcMarker marker, string name, bool isSameLocation)
    {
        marker.IsHidden = false;
        marker.ReasonHidden = null;

        void Hide(string setReason)
        {
            marker.IsHidden = true;
            marker.ReasonHidden = setReason;
        }

        bool shownForQuest = ModEntry.Config.ShowQuests && (marker.HasQuest || marker.IsBirthday);

        if (ModEntry.ShouldExcludeNpc(name, out string? reason))
            Hide($"hidden per config ({reason})");
        else if (!shownForQuest && Config.FilterNpcsSpokenTo != null && Config.FilterNpcsSpokenTo != Game1.player.hasTalkedToFriendToday(name))
            Hide($"hidden per config ({(Config.FilterNpcsSpokenTo is true ? "didn't talk" : "talked")} to them today)");
        else if (Config.OnlySameLocation && !isSameLocation)
            Hide("hidden per config (not in same location)");
        else if (Config.HeartLevelMin > 0 || Config.HeartLevelMax < ModConfig.MaxPossibleHeartLevel)
        {
            int hearts = Game1.player.getFriendshipHeartLevelForNPC(name);
            if (Config.HeartLevelMin > 0 && hearts < Config.HeartLevelMin)
                Hide($"hidden per config (less than {Config.HeartLevelMin} hearts)");
            if (Config.HeartLevelMax < ModConfig.MaxPossibleHeartLevel && hearts > Config.HeartLevelMax)
                Hide($"hidden per config (more than {Config.HeartLevelMax} hearts)");
        }
    }

    private void UpdateFarmers()
    {
        foreach (var farmer in Game1.getOnlineFarmers())
        {
            if (farmer?.currentLocation == null)
                continue;

            string locationName = farmer.currentLocation.uniqueName.Value ?? farmer.currentLocation.Name;
            locationName = LocationUtil.GetLocationNameFromLevel(locationName) ?? locationName;
            if (locationName == "VolcanoDungeon0")
                locationName = "Caldera"; // avoid generating volcano dungeon level

            long farmerId = farmer.UniqueMultiplayerID;
            var farmerLoc = GetWorldMapPosition(
                locationName,
                farmer.TilePoint.X,
                farmer.TilePoint.Y,
                this.Customizations.LocationExclusions
            );

            if (this.FarmerMarkers.Value.TryGetValue(farmer.UniqueMultiplayerID, out var farMarker))
            {
                float deltaX = farmerLoc.X - farMarker.WorldMapX;
                float deltaY = farmerLoc.Y - farMarker.WorldMapY;

                // Location changes before tile position, causing farmhands to blink
                // to the wrong position upon entering new location. Handle this in draw.
                if (locationName == farMarker.LocationName && farmerLoc.RegionId == farMarker.WorldMapRegionId && MathHelper.Distance(deltaX, deltaY) > 15)
                    this.FarmerMarkers.Value[farmerId].DrawDelay = 1;
                else if (farMarker.DrawDelay > 0)
                    this.FarmerMarkers.Value[farmerId].DrawDelay--;
            }
            else
            {
                var newMarker = new FarmerMarker(farmer.Name);

                this.FarmerMarkers.Value.Add(farmerId, newMarker);
            }

            this.FarmerMarkers.Value[farmerId].WorldMapPosition = farmerLoc;
            this.FarmerMarkers.Value[farmerId].LocationName = locationName;
        }
    }

    /// <summary>Reset the mod settings to default.</summary>
    private void ResetConfig()
    {
        ModEntry.Config = new ModConfig();
        this.OnConfigEdited();
    }

    /// <summary>Handle the config being edited through Generic Mod Config Menu.</summary>
    private void OnConfigEdited()
    {
        ModEntry.StaticHelper.WriteConfig(ModEntry.Config);

        this.UpdateMinimapVisibility();
        this.Minimap.Value?.ApplyConfig();
    }

    /// <summary>Migrate files from older versions of the mod.</summary>
    private void MigrateLegacyFiles()
    {
        IModHelper helper = this.Helper;
        string dirPath = helper.DirectoryPath;

        // 2.11.4: PDB embedded into assembly
        CommonHelper.RemoveObsoleteFiles(this, "NPCMapLocations.pdb");

        // 3.4.0: `config/globals.json` moved to `config.json`
        {
            ModConfig? config = helper.Data.ReadJsonFile<ModConfig>("config/globals.json");
            if (config != null)
                helper.WriteConfig(config);
            CommonHelper.RemoveObsoleteFiles(this, "config/globals.json");
        }

        // 3.4.0: per-save config files no longer used
        {
            string configDirPath = Path.Combine(dirPath, "config");
            if (Directory.Exists(configDirPath))
            {
                if (Directory.GetFileSystemEntries(configDirPath).Length > 0)
                {
                    this.Monitor.Log("The 'config' folder is no longer used. Renaming to 'config (unused)' to avoid confusion.");

                    string unusedConfigPath = Path.Combine(dirPath, "config (unused)");
                    if (!Directory.Exists(unusedConfigPath))
                        Directory.Move(configDirPath, unusedConfigPath);
                }
                else
                {
                    this.Monitor.Log("The 'config' folder is no longer used. Deleting the empty folder to avoid confusion.");

                    Directory.Delete(configDirPath);
                }
            }
        }
    }
}
