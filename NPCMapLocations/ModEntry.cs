﻿using System;
using System.Collections.Generic;
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
using StardewValley.WorldMaps;

namespace NPCMapLocations;

/// <summary>The mod entry class.</summary>
public class ModEntry : Mod
{
    /*********
    ** Fields
    *********/
    /// <summary>The map markers for farm buildings, indexed by the interior unique name.</summary>
    private static readonly Dictionary<string, BuildingMarker> FarmBuildings = new();

    private readonly PerScreen<Texture2D> BuildingMarkers = new();
    private readonly PerScreen<ModMinimap> Minimap = new();
    private readonly PerScreen<Dictionary<string, NpcMarker>> NpcMarkers = new();
    private readonly PerScreen<Dictionary<string, bool>> ConditionalNpcs = new();
    private readonly PerScreen<bool> HasOpenedMap = new();
    private readonly PerScreen<bool> IsModMapOpen = new();
    private readonly PerScreen<bool> IsFirstDay = new();

    /// <summary>Whether to show the minimap.</summary>
    private readonly PerScreen<bool> ShowMinimap = new();

    /// <summary>An integration with Better Game Menu to use NPC Map Locations.</summary>
    private BetterGameMenuIntegration BetterGameMenuIntegration;

    /// <summary>Scans and maps locations in the game world.</summary>
    private static LocationUtil LocationUtil;

    // External mod settings
    private readonly string NpcCustomizationsPath = "Mods/Bouhm.NPCMapLocations/NPCs";
    private readonly string LocationCustomizationsPath = "Mods/Bouhm.NPCMapLocations/Locations";

    // Multiplayer
    private readonly PerScreen<Dictionary<long, FarmerMarker>> FarmerMarkers = new();

    // Customizations/Custom mods
    private ModCustomizations Customizations;


    /*********
    ** Accessors
    *********/
    public static PlayerConfig Config;
    public static GlobalConfig Globals;
    public static IModHelper StaticHelper;


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
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.GameLoop.DayStarted += this.OnDayStarted;
        helper.Events.World.BuildingListChanged += this.OnBuildingListChanged;
        helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        helper.Events.Input.ButtonReleased += this.OnButtonReleased;
        helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
        helper.Events.Player.Warped += this.OnWarped;
        helper.Events.Display.MenuChanged += this.OnMenuChanged;
        helper.Events.Display.RenderingHud += this.OnRenderingHud;
        helper.Events.Display.WindowResized += this.OnWindowResized;
        helper.Events.Multiplayer.PeerConnected += this.OnPeerConnected;
        helper.Events.Multiplayer.ModMessageReceived += this.OnModMessageReceived;

        helper.ConsoleCommands.Add(SummaryCommand.Name, SummaryCommand.GetDescription(), (_, _) => SummaryCommand.Handle(
            monitor: this.Monitor,
            locationUtil: ModEntry.LocationUtil,
            customizations: this.Customizations,
            npcMarkers: this.NpcMarkers.Value,
            locationsWithoutMapPositions: this.GetLocationsWithoutMapPosition()
        ));
    }

    /// <inheritdoc cref="IGameLoopEvents.SaveLoaded"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event data.</param>
    private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
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
    public static WorldMapPosition GetWorldMapPosition(string locationName, int tileX = 0, int tileY = 0, HashSet<string> locationExclusions = null)
    {
        Point tile = new Point(tileX, tileY);

        ISet<string> seen = new HashSet<string>();
        int depth = 0;

        while (!string.IsNullOrWhiteSpace(locationName))
        {
            // special case: map generated level to single name
            locationName = LocationUtil.GetLocationNameFromLevel(locationName) ?? locationName;

            // break infinite loops
            if (!seen.Add(locationName))
                return WorldMapPosition.Empty;
            if (++depth > LocationUtil.MaxRecursionDepth)
                throw new InvalidOperationException($"Infinite recursion detected in location scan. Technical details:\n{nameof(locationName)}: {locationName}\n{nameof(tileX)}: {tileX}\n{nameof(tileY)}: {tileY}\n\n{Environment.StackTrace}");

            // special case: inside farm building
            if (FarmBuildings.TryGetValue(locationName, out BuildingMarker marker))
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
    /// <inheritdoc cref="IGameLoopEvents.GameLaunched"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event data.</param>
    private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
    {
        new GenericModConfigMenuIntegration(this.ModManifest, this.Helper.ModRegistry)
            .Register();
        this.BetterGameMenuIntegration = new BetterGameMenuIntegration(this.CreateMapPage, this.Monitor, this.Helper.ModRegistry);
    }

    /// <inheritdoc cref="IGameLoopEvents.SaveLoaded"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event data.</param>
    private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
    {
        //
        // Load config and other one-off data
        //

        Config = this.Helper.Data.ReadJsonFile<PlayerConfig>($"config/{Constants.SaveFolderName}.json") ?? new PlayerConfig();
        this.IsFirstDay.Value = true;

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
            this.BuildingMarkers.Value = this.Helper.ModContent.Load<Texture2D>("assets/buildings.png");
        }
        catch
        {
            this.BuildingMarkers.Value = null;
        }

        // NPCs that player should meet before being shown
        this.ConditionalNpcs.Value = new Dictionary<string, bool>();
        foreach (string npcName in ModConstants.ConditionalNpcs)
            this.ConditionalNpcs.Value[npcName] = Game1.player.friendshipData.ContainsKey(npcName);

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
                this.Minimap.Value.HandleMouseRightDown();
            }
        }

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
    }

    /// <inheritdoc cref="IInputEvents.ButtonReleased"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event data.</param>
    private void OnButtonReleased(object sender, ButtonReleasedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        if (this.Minimap.Value != null && !ModEntry.Globals.LockMinimapPosition)
        {
            if (Game1.activeClickableMenu is ModMenu && e.Button == SButton.MouseLeft)
                this.Minimap.Value.Resize();
            else if (Game1.activeClickableMenu == null && e.Button == SButton.MouseRight)
            {
                MouseUtil.HandleMouseRelease();
                this.Minimap.Value.HandleMouseRightRelease();
            }
        }
    }

    /// <inheritdoc cref="IGameLoopEvents.DayStarted"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event data.</param>
    [EventPriority(EventPriority.Low)] // let other mods initialize their locations/NPCs first
    private void OnDayStarted(object sender = null, DayStartedEventArgs e = null)
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

        this.Minimap.Value = new ModMinimap(this.CreateMapPage);
    }

    /// <inheritdoc cref="IGameLoopEvents.UpdateTicked"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event data.</param>
    private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        // update and sync markers
        if (Game1.currentLocation != null && Game1.player.currentLocation != null)
        {
            bool? hasMinimap = null;

            // update local minimap display
            if (e.IsMultipleOf(Globals.MiniMapCacheTicks))
            {
                hasMinimap = this.ShowMinimap.Value && this.Minimap.Value != null;
                if (hasMinimap.Value)
                    this.Minimap.Value.Update();
            }

            // update & sync NPC markers
            if (e.IsMultipleOf(Globals.NpcCacheTicks))
            {
                hasMinimap ??= this.ShowMinimap.Value && this.Minimap.Value != null;

                this.UpdateMarkers(hasMinimap.Value || Context.IsMainPlayer);

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
                            WorldMapPosition = npcMarker.Value.WorldMapPosition,
                            IsBirthday = npcMarker.Value.IsBirthday,
                            Type = npcMarker.Value.Type
                        });
                    }

                    this.Helper.Multiplayer.SendMessage(syncedMarkers, ModConstants.MessageIds.SyncedNpcMarkers, modIDs: new[] { this.ModManifest.UniqueID });
                }
            }
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
                        npcMarker.WorldMapPosition = syncedMarker.Value.WorldMapPosition;
                        npcMarker.DisplayName = syncedMarker.Value.DisplayName;
                        npcMarker.CropOffset = offset;
                        npcMarker.IsBirthday = syncedMarker.Value.IsBirthday;
                        npcMarker.Type = syncedMarker.Value.Type;

                        try
                        {
                            if (syncedMarker.Value.Type is CharacterType.Villager or CharacterType.Raccoon)
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
        if (Context.IsWorldReady && this.ShowMinimap.Value && Game1.displayHUD && !Game1.game1.takingMapScreenshot)
            this.Minimap.Value?.Draw();
    }

    /// <summary>Get the outdoor location contexts which don't have any map vectors.</summary>
    private IEnumerable<LocationContext> GetLocationsWithoutMapPosition()
    {
        foreach (var entry in LocationUtil.LocationContexts)
        {
            string name = entry.Key;
            LocationContext context = entry.Value;

            if (GetWorldMapPosition(name).IsEmpty && context.Type is not (LocationType.Building or LocationType.Room))
                yield return context;
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
                && !npc.IsInvisible
                && !ModConstants.ExcludedNpcs.Contains(npc.Name) // note: don't check Globals.NPCExclusions here, so player can still reenable them in the map options UI
                && (
                    npc.IsVillager
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
            // book seller
            if (ModEntry.Globals.ShowBookseller && Utility.getDaysOfBooksellerThisSeason().Contains(Game1.dayOfMonth))
            {
                WorldMapPosition mapPos = GetWorldMapPosition("Town", 108, 25); // hardcoded in Town.draw

                this.NpcMarkers.Value.Add("Bookseller", new NpcMarker
                {
                    DisplayName = I18n.MarkerNames_Bookseller(),
                    LocationName = "Town",
                    CropOffset = 0,
                    Sprite = Game1.mouseCursors_1_6,
                    SpriteSourceRect = new Rectangle(181, 490, 12, 18),
                    SpriteZoom = 1.5f,
                    WorldMapPosition = mapPos
                });
            }

            // traveling cart
            if (ModEntry.Globals.ShowTravelingMerchant && Game1.RequireLocation<Forest>("Forest").ShouldTravelingMerchantVisitToday())
            {
                Forest forest = Game1.RequireLocation<Forest>("Forest");
                Point cartTile = forest.GetTravelingMerchantCartTile();
                WorldMapPosition mapPos = GetWorldMapPosition("Forest", cartTile.X + 4, cartTile.Y);

                this.NpcMarkers.Value.Add("Merchant", new NpcMarker
                {
                    DisplayName = I18n.MarkerNames_Merchant(),
                    LocationName = "Forest",
                    CropOffset = 0,
                    Sprite = Game1.mouseCursors,
                    SpriteSourceRect = new Rectangle(191, 1410, 22, 21),
                    SpriteZoom = 1.3f,
                    WorldMapPosition = mapPos
                });
            }

            // villagers
            foreach (var npc in this.GetVillagers())
            {
                var type = npc switch
                {
                    Horse => CharacterType.Horse,
                    Child => CharacterType.Child,
                    Raccoon => CharacterType.Raccoon,
                    _ => CharacterType.Villager
                };

                int offset = ModEntry.Globals.NpcMarkerOffsets.GetValueOrDefault(npc.Name, 0);

                if (!this.NpcMarkers.Value.ContainsKey(npc.Name))
                {
                    var newMarker = new NpcMarker
                    {
                        DisplayName = string.IsNullOrWhiteSpace(npc.displayName) ? npc.Name : npc.displayName,
                        CropOffset = offset,
                        IsBirthday = npc.isBirthday(),
                        Type = type
                    };

                    try
                    {
                        newMarker.Sprite = new AnimatedSprite(npc.Sprite.loadedTexture, 0, 16, 32).Texture;
                    }
                    catch (Exception ex)
                    {
                        this.Monitor.Log($"Couldn't load marker for NPC '{npc.Name}'.", LogLevel.Warn);
                        this.Monitor.Log(ex.ToString());
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
            this.ConditionalNpcs.Value,
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
                var mapPos = GetWorldMapPosition(locationName, npc.TilePoint.X, npc.TilePoint.Y, this.Customizations.LocationExclusions);
                mapPos = mapPos with { X = mapPos.X - 16, Y = mapPos.Y - 15 };
                npcMarker.WorldMapPosition = mapPos;
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

        bool shownForQuest = ModEntry.Globals.ShowQuests && (marker.HasQuest || marker.IsBirthday);

        if (ModEntry.ShouldExcludeNpc(name, out string reason))
            Hide($"hidden per config ({reason})");
        else if (!shownForQuest && Config.ImmersionOption == VillagerVisibility.TalkedTo && !Game1.player.hasTalkedToFriendToday(name))
            Hide("hidden per config (didn't talk to them today)");
        else if (!shownForQuest && Config.ImmersionOption == VillagerVisibility.NotTalkedTo && Game1.player.hasTalkedToFriendToday(name))
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
            if (farmer?.currentLocation == null)
                continue;

            string locationName = farmer.currentLocation.uniqueName.Value ?? farmer.currentLocation.Name;
            locationName = LocationUtil.GetLocationNameFromLevel(locationName) ?? locationName;

            long farmerId = farmer.UniqueMultiplayerID;
            var farmerLoc = GetWorldMapPosition(
                locationName,
                farmer.TilePoint.X,
                farmer.TilePoint.Y,
                this.Customizations.LocationExclusions
            );

            if (this.FarmerMarkers.Value.TryGetValue(farmer.UniqueMultiplayerID, out var farMarker))
            {
                float deltaX = farmerLoc.X - farMarker.WorldMapPosition.X;
                float deltaY = farmerLoc.Y - farMarker.WorldMapPosition.Y;

                // Location changes before tile position, causing farmhands to blink
                // to the wrong position upon entering new location. Handle this in draw.
                if (locationName == farMarker.LocationName && farmerLoc.RegionId == farMarker.WorldMapPosition.RegionId && MathHelper.Distance(deltaX, deltaY) > 15)
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

            this.FarmerMarkers.Value[farmerId].WorldMapPosition = farmerLoc;
            this.FarmerMarkers.Value[farmerId].LocationName = locationName;
        }
    }
}
