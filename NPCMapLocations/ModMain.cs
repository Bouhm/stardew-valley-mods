using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Quests;
using StardewValley.Characters;
using StardewModdingAPI.Utilities;
using Newtonsoft.Json.Linq;

namespace NPCMapLocations
{
  public class ModMain : Mod, IAssetLoader
  {
    public static PlayerConfig Config;
    public static GlobalConfig Globals;
    public static IModHelper Helper;
    public static IMonitor IMonitor;
    public static Texture2D Map;
    public static Vector2 UNKNOWN = new Vector2(-9999, -9999);
    private static Dictionary<string, KeyValuePair<string, Vector2>> FarmBuildings;

    private readonly PerScreen<Texture2D> BuildingMarkers = new PerScreen<Texture2D>();
    private readonly PerScreen<Dictionary<string, MapVector[]>> MapVectors = new PerScreen<Dictionary<string, MapVector[]>>();
    private readonly PerScreen<ModMinimap> Minimap = new PerScreen<ModMinimap>();
    private readonly PerScreen<Dictionary<string, NpcMarker>> NpcMarkers = new PerScreen<Dictionary<string, NpcMarker>>();
    private readonly PerScreen<Dictionary<string, bool>> ConditionalNpcs = new PerScreen<Dictionary<string, bool>>();
    private readonly PerScreen<bool> hasOpenedMap = new PerScreen<bool>();
    private readonly PerScreen<bool> isModMapOpen = new PerScreen<bool>();

    // External mod settings
    private readonly string MapFilePath = @"LooseSprites\Map";
    private readonly string NpcCustomizationsPath = "Mods/Bouhm.NPCMapLocations/NPCs";
    private readonly string LocationCustomizationsPath = "Mods/Bouhm.NPCMapLocations/Locations";

    // Multiplayer
    private readonly PerScreen<Dictionary<long, FarmerMarker>> FarmerMarkers = new PerScreen<Dictionary<long, FarmerMarker>>();
    private static long hostId;
    private static List<long> playerIds;

    // Customizations/Custom mods
    private string MapSeason;
    private ModCustomizations Customizations;

    // Debugging
    private static bool DEBUG_MODE;

    // Replace game map with modified map
    public bool CanLoad<T>(IAssetInfo asset)
    {
      return (
        asset.AssetNameEquals(MapFilePath) ||
        asset.AssetNameEquals(NpcCustomizationsPath) ||
        asset.AssetNameEquals(LocationCustomizationsPath)
      );
    }

    public T Load<T>(IAssetInfo asset)
    {
      if (asset.AssetNameEquals(MapFilePath))
      {
        T map;

        if (MapSeason == null)
        {
          Monitor.Log("Unable to get current season. Defaulted to spring.", LogLevel.Debug);
          MapSeason = "spring";
        }

        if (!File.Exists(Path.Combine(ModMain.Helper.DirectoryPath, Customizations.MapsPath, $"{MapSeason}_map.png")))
        {
          Monitor.Log("Seasonal maps not provided. Defaulted to spring.", LogLevel.Debug);
          MapSeason = null; // Set to null so that cache is not invalidated when game season changes
        }

        // Replace map page
        string defaultMapFile = File.Exists(Path.Combine(ModMain.Helper.DirectoryPath, Customizations.MapsPath, "spring_map.png")) ? "spring_map.png" : "map.png";
        string filename = MapSeason == null ? defaultMapFile : $"{MapSeason}_map.png";

        bool useRecolor = Customizations.MapsPath != null && File.Exists(Path.Combine(ModMain.Helper.DirectoryPath, Customizations.MapsPath, filename));
        map = useRecolor
          ? Helper.Content.Load<T>(Path.Combine(Customizations.MapsPath, filename))
          : Helper.Content.Load<T>(Path.Combine(Customizations.MapsRootPath, "_default", filename));

        if (useRecolor)
          Monitor.Log($"Using {Path.Combine(Customizations.MapsPath, filename)}.", LogLevel.Debug);

        return map;
      }
      else if (asset.AssetNameEquals(LocationCustomizationsPath) || asset.AssetNameEquals(NpcCustomizationsPath))
      {
        return (T)(object)new Dictionary<string, JObject>();
      }

      return (T)asset;
    }
     
    
  public override void Entry(IModHelper helper)
    {
      if (!Context.IsMainPlayer && Context.IsSplitScreen) return;

      Helper = helper;
      IMonitor = Monitor;
      Globals = Helper.Data.ReadJsonFile<GlobalConfig>("config/globals.json") ?? new GlobalConfig();
      Customizations = new ModCustomizations();

      Helper.Events.GameLoop.SaveLoaded += GameLoop_SaveLoaded;
      Helper.Events.GameLoop.DayStarted += GameLoop_DayStarted;
      Helper.Events.World.BuildingListChanged += World_BuildingListChanged;
      Helper.Events.Input.ButtonPressed += Input_ButtonPressed;
      Helper.Events.Input.ButtonReleased += Input_ButtonReleased;
      Helper.Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked;
      Helper.Events.Player.Warped += Player_Warped;
      Helper.Events.Display.MenuChanged += Display_MenuChanged;
      Helper.Events.Display.RenderingHud += Display_RenderingHud;
      Helper.Events.Display.RenderedWorld += Display_RenderedWorld;
      Helper.Events.Display.Rendered += Display_Rendered;
      Helper.Events.Display.WindowResized += Display_WindowResized;
      Helper.Events.Multiplayer.ModMessageReceived += Multiplayer_ModMessageReceived;
    }

    // Load config and other one-off data
    private void GameLoop_SaveLoaded(object sender, SaveLoadedEventArgs e)
    {
      Config = Helper.Data.ReadJsonFile<PlayerConfig>($"config/{Constants.SaveFolderName}.json") ?? new PlayerConfig();

      if (!Context.IsMainPlayer)
      {
        // Determine host ID
        foreach (IMultiplayerPeer peer in Helper.Multiplayer.GetConnectedPlayers())
        {
          if (peer.IsHost)
          {
            hostId = peer.PlayerID;
            break;
          }
        }
      }

      // Initialize these early for multiplayer sync
      NpcMarkers.Value = new Dictionary<string, NpcMarker>();
      FarmerMarkers.Value = new Dictionary<long, FarmerMarker>();

      // Let host know farmhand is ready to receive updates
      if (Context.IsMultiplayer && !Context.IsMainPlayer)
      {
        Helper.Multiplayer.SendMessage(true, "PlayerReady", modIDs: new string[] { ModManifest.UniqueID }, playerIDs: new long[] { hostId });
      }

      if (!(Context.IsSplitScreen && !Context.IsMainPlayer))
      {
        // Load customizations
        var NpcSettings = Helper.Content.Load<Dictionary<string, JObject>>(NpcCustomizationsPath, ContentSource.GameContent);
        var LocationSettings = Helper.Content.Load<Dictionary<string, JObject>>(LocationCustomizationsPath, ContentSource.GameContent);
        Customizations.LoadCustomData(NpcSettings, LocationSettings);
      }

      // Load farm buildings
      try
      {
        BuildingMarkers.Value = Helper.Content.Load<Texture2D>(Path.Combine(Customizations.MapsPath, "buildings.png"));
      }
      catch
      {
        if (File.Exists(Path.Combine("maps/_default", "buildings.png"))) {
          BuildingMarkers.Value = Helper.Content.Load<Texture2D>(Path.Combine("maps/_default", "buildings.png"));
        }
        else
        {
          BuildingMarkers.Value = null;
        }
      }

      // Get season for map
      MapSeason = Globals.UseSeasonalMaps ? Game1.currentSeason : "spring";
      Helper.Content.InvalidateCache(MapFilePath);
      Map = Game1.content.Load<Texture2D>(MapFilePath);

      // Disable for multiplayer for anti-cheat
      DEBUG_MODE = Globals.DEBUG_MODE && !Context.IsMultiplayer;

      // NPCs that player should meet before being shown
      ConditionalNpcs.Value = new Dictionary<string, bool>();
      foreach (var npcName in ModConstants.ConditionalNpcs)
      {
        ConditionalNpcs.Value[npcName] = Game1.player.friendshipData.ContainsKey(npcName);
      }

      MapVectors.Value = ModConstants.MapVectors;

      // Add custom map vectors from content.json
      foreach (var locVectors in Customizations.MapVectors)
      {
        if (MapVectors.Value.TryGetValue(locVectors.Key, out var mapVectors))
          MapVectors.Value[locVectors.Key] = locVectors.Value;
        else
          MapVectors.Value.Add(locVectors.Key, locVectors.Value);
      }

      // Get context of all locations (indoor, outdoor, relativity)
      LocationUtil.GetLocationContexts();

      // Log any custom locations not handled in content.json
      try
      {
        var alertStr = "Unknown locations:";
        foreach (var locCtx in LocationUtil.LocationContexts)
        {
          if (
            (locCtx.Value.Root == null && !MapVectors.Value.ContainsKey(locCtx.Key))
            || (locCtx.Value.Root != null && !MapVectors.Value.ContainsKey(locCtx.Value.Root))
            && (locCtx.Value.Type != LocationType.Building || locCtx.Value.Type != LocationType.Room)
          )
          {
            if (Customizations.LocationExclusions.Contains(locCtx.Key)) return;
            alertStr += $" {locCtx.Key},";
          }
        }
        Monitor.Log(alertStr.TrimEnd(',') + ".", LogLevel.Debug);
      }
      catch
      {
        Monitor.Log("Too many unknown locations; NPCs in unknown locations will not be visible.", LogLevel.Debug);
      }
     

      UpdateFarmBuildingLocs();

      // Log warning if host does not have mod installed
      if (Context.IsMultiplayer)
      {
        var hostHasMod = false;

        foreach (IMultiplayerPeer peer in Helper.Multiplayer.GetConnectedPlayers())
        {
          if (peer.GetMod("Bouhm.NPCMapLocations") != null && peer.IsHost)
          {
            hostHasMod = true;
            break;
          }
        }

        if (!hostHasMod && !Context.IsMainPlayer)
          Monitor.Log("Since the server host does not have NPCMapLocations installed, NPC locations cannot be synced.", LogLevel.Warn);
      }
    }

    private static bool ShouldTrackNpc(NPC npc)
    {
      return
        (
          !Globals.NpcExclusions.Contains(npc.Name) &&
          !ModConstants.ExcludedNpcs.Contains(npc.Name) &&
          (npc.isVillager()
          | npc.isMarried()
          | (Globals.ShowHorse && npc is Horse)
          | (Globals.ShowChildren && npc is Child))
        );
    }

    // Get only relevant villagers for map
    public static List<NPC> GetVillagers()
    {
      var villagers = new List<NPC>();

      foreach (var location in Game1.locations)
      {
        foreach (var npc in location.characters)
        {
          if (npc == null) continue;
          if (
            !villagers.Contains(npc)
            && ShouldTrackNpc(npc)
          )
          {
            villagers.Add(npc);
          }
        }
      }

      return villagers;
    }

    // For drawing farm buildings on the map 
    // and getting positions relative to the farm 
    private void UpdateFarmBuildingLocs()
    {
      FarmBuildings = new Dictionary<string, KeyValuePair<string, Vector2>>();

      foreach (var building in Game1.getFarm().buildings)
      {
        if (building == null) continue;
        if (building.nameOfIndoorsWithoutUnique == null
            || building.nameOfIndoors == null
            || building.nameOfIndoors.Equals("null")) // Some actually have value of "null"
          continue;

        var locVector = LocationToMap(
          "Farm", // Get building position in farm
          building.tileX.Value,
          building.tileY.Value,
          Customizations.MapVectors,
          Customizations.LocationExclusions
        );

        // Using buildingType instead of nameOfIndoorsWithoutUnique because it is a better subset of currentLocation.Name 
        // since nameOfIndoorsWithoutUnique for Barn/Coop does not use Big/Deluxe but rather the upgrade level
        var commonName = building.buildingType.Value ?? building.nameOfIndoorsWithoutUnique;

        if (commonName.Contains("Barn")) locVector.Y += 3;

        // Format: { uniqueName: { commonName: positionOnFarm } }
        // buildingType will match currentLocation.Name for commonName
        FarmBuildings[building.nameOfIndoors] =
          new KeyValuePair<string, Vector2>(building.buildingType.Value, locVector);
      }

      // Greenhouse unlocked after pantry bundles completed
      if (((CommunityCenter)Game1.getLocationFromName("CommunityCenter")).areasComplete[CommunityCenter.AREA_Pantry])
      {
        var greehouseLoc = LocationToMap("Greenhouse", -1, -1, Customizations.MapVectors);
        greehouseLoc.X -= 5 / 2 * 3;
        greehouseLoc.Y -= 7 / 2 * 3;
        FarmBuildings["Greenhouse"] = new KeyValuePair<string, Vector2>("Greenhouse", greehouseLoc);
      }

      // Add FarmHouse
      var farmhouseLoc = LocationToMap("FarmHouse", -1, -1, Customizations.MapVectors);
      farmhouseLoc.X -= 6;
      FarmBuildings["FarmHouse"] = new KeyValuePair<string, Vector2>("FarmHouse", farmhouseLoc);
    }

    private void World_BuildingListChanged(object sender, BuildingListChangedEventArgs e)
    {
      if (e.Location.IsFarm)
        UpdateFarmBuildingLocs();

      LocationUtil.GetLocationContexts();
    }

    // Handle opening mod menu and changing tooltip options
    private void Input_ButtonPressed(object sender, ButtonPressedEventArgs e)
    {
      if (!Context.IsWorldReady) return;

      // Minimap dragging
      if (Globals.ShowMinimap && Minimap.Value != null)
      {
        if (Minimap.Value.isHoveringDragZone() && e.Button == SButton.MouseRight)
        {
          MouseUtil.HandleMouseDown(() => Minimap.Value.HandleMouseDown());
        }
      }

      // Debug DnD
      if
        (DEBUG_MODE && e.Button == SButton.MouseRight && isModMapOpen.Value)
      {
        MouseUtil.HandleMouseDown();
      }

      // Minimap toggle
      if (e.Button.ToString().Equals(Globals.MinimapToggleKey) && Game1.activeClickableMenu == null)
      {
        Globals.ShowMinimap = !Globals.ShowMinimap;
        Helper.Data.WriteJsonFile($"config/{Constants.SaveFolderName}.json", Config);
      }

      // ModMenu
      if (Game1.activeClickableMenu is GameMenu)
        HandleInput((GameMenu)Game1.activeClickableMenu, e.Button);

      if (DEBUG_MODE && !Context.IsMultiplayer && Helper.Input.GetState(SButton.LeftControl) == SButtonState.Held && e.Button.Equals(SButton.MouseRight))
        Game1.player.setTileLocation(Game1.currentCursorTile);
    }

    private void Input_ButtonReleased(object sender, ButtonReleasedEventArgs e)
    {
      if (!Context.IsWorldReady) return;

      if (DEBUG_MODE && e.Button == SButton.MouseRight && isModMapOpen.Value)
      {
        MouseUtil.HandleMouseRelease();
      }
      else if (Minimap.Value != null)
      {
        if (Game1.activeClickableMenu is ModMenu && e.Button == SButton.MouseLeft) {
          Minimap.Value.Resize();
        }
        else if (Game1.activeClickableMenu == null && e.Button == SButton.MouseRight)
        {
          MouseUtil.HandleMouseRelease(() => Minimap.Value.HandleMouseRelease());
        }
      }
    }

    // Handle keyboard/controller inputs
    private void HandleInput(GameMenu menu, SButton input)
    {
      if (menu.currentTab != ModConstants.MapTabIndex) return;
      if (input.ToString().Equals(Globals.MenuKey) || input is SButton.ControllerY)
        Game1.activeClickableMenu = new ModMenu(
          NpcMarkers.Value,
          ConditionalNpcs.Value
        );

      if (input.ToString().Equals(Globals.TooltipKey) || input is SButton.RightShoulder)
        ChangeTooltipConfig();
      else if (input.ToString().Equals(Globals.TooltipKey) || input is SButton.LeftShoulder) ChangeTooltipConfig(false);
    }

    private void ChangeTooltipConfig(bool incre = true)
    {
      if (incre)
      {
        if (++Globals.NameTooltipMode > 3) Globals.NameTooltipMode = 1;

        Helper.Data.WriteJsonFile($"config/{Constants.SaveFolderName}.json", Config);
      }
      else
      {
        if (--Globals.NameTooltipMode < 1) Globals.NameTooltipMode = 3;

        Helper.Data.WriteJsonFile($"config/{Constants.SaveFolderName}.json", Config);
      }
    }

    // Handle any checks that need to be made per day
    private void GameLoop_DayStarted(object sender = null, DayStartedEventArgs e = null)
    {
      // Check for travelining merchant day
      if (ConditionalNpcs.Value != null && ConditionalNpcs.Value.ContainsKey("Merchant") && ConditionalNpcs.Value["Merchant"]) {
        ConditionalNpcs.Value["Merchant"] = ((Forest)Game1.getLocationFromName("Forest")).travelingMerchantDay;
      }

      ResetMarkers();
      UpdateMarkers(true);

      Minimap.Value = new ModMinimap(
        NpcMarkers.Value,
        ConditionalNpcs.Value,
        FarmerMarkers.Value,
        FarmBuildings,
        BuildingMarkers.Value,
        Customizations
      );
    }

    private bool IsLocationExcluded(string location)
    {
      return Globals.ShowMinimap && Globals.MinimapExclusions.Any(loc => loc != "Farm" && location.StartsWith(loc) || loc == "Farm" && location == "Farm") ||
               ((Globals.MinimapExclusions.Contains("Mine") || Globals.MinimapExclusions.Contains("UndergroundMine")) && location.Contains("Mine"));
    }

    private void ResetMarkers()
    {
      NpcMarkers.Value = new Dictionary<string, NpcMarker>();
      FarmerMarkers.Value = new Dictionary<long, FarmerMarker>();

      if (!Context.IsMultiplayer || Context.IsMainPlayer)
      {
        foreach (var npc in GetVillagers())
        {
          if (!Customizations.Names.TryGetValue(npc.Name, out var name) && !(npc is Horse || npc is Child)) continue;

          var type = Character.Villager;
          if (npc is Horse)
          {
            type = Character.Horse;
          }
          else if (npc is Child)
          {
            type = Character.Child;
          }

          if (!ModMain.Globals.NpcMarkerOffsets.TryGetValue(npc.Name, out var offset))
          {
            offset = 0;
          }

          if (!NpcMarkers.Value.ContainsKey(npc.Name))
          {
            var displayName = (npc.displayName != null && Game1.IsEnglish()) ? npc.displayName : npc.Name;
            var newMarker = new NpcMarker()
            {
              DisplayName = displayName,
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

            NpcMarkers.Value.Add(npc.Name, newMarker);
          }
        }
      }
    }

    // To initialize ModMap quicker for smoother rendering when opening map
    private void GameLoop_UpdateTicked(object sender, UpdateTickedEventArgs e)
    {
      if (!Context.IsWorldReady) return;

      // Half-second tick
      if (e.IsMultipleOf(30))
      {
        var updateForMinimap = Globals.ShowMinimap && Minimap.Value != null;

        if (updateForMinimap)
        {
          Minimap.Value.Update();
        }

        UpdateMarkers(updateForMinimap | Context.IsMainPlayer);

        // Sync multiplayer data
        if (Context.IsMainPlayer && Context.IsMultiplayer && playerIds != null)
        {
          var syncedMarkers = new Dictionary<string, SyncedNpcMarker>();

          foreach (var npcMarker in NpcMarkers.Value)
          {
            syncedMarkers.Add(npcMarker.Key, new SyncedNpcMarker()
            {
              DisplayName = npcMarker.Value.DisplayName,
              LocationName = npcMarker.Value.LocationName,
              MapX = npcMarker.Value.MapX,
              MapY = npcMarker.Value.MapY,
              IsBirthday = npcMarker.Value.IsBirthday,
              Type = npcMarker.Value.Type
            });
          }

          Helper.Multiplayer.SendMessage(syncedMarkers, "SyncedNpcMarkers", modIDs: new string[] { ModManifest.UniqueID }, playerIDs: playerIds.ToArray());
        }
      }

      // One-second tick
      if (e.IsOneSecond)
      {
        // Check season change (for when it's changed via console)
        if (Globals.UseSeasonalMaps && (MapSeason != null && MapSeason != Game1.currentSeason) && Game1.currentSeason != null)
        {
          MapSeason = Game1.currentSeason;

          // Force reload of map for season changes
          try
          {
            Helper.Content.InvalidateCache(MapFilePath);
          }
          catch
          {
            Monitor.Log("Failed to update map for current season.", LogLevel.Error);
          }

          Minimap.Value?.UpdateMapForSeason();
        }

        // Check if conditional NPCs have been talked to
        foreach (var npcName in ModConstants.ConditionalNpcs)
        {
          if (ConditionalNpcs.Value[npcName]) continue;
          
          ConditionalNpcs.Value[npcName] = Game1.player.friendshipData.ContainsKey(npcName);
        }
      }

      // Update tick
      if (Globals.ShowMinimap && Minimap.Value != null && Minimap.Value.isHoveringDragZone() && Helper.Input.GetState(SButton.MouseRight) == SButtonState.Held)
      {
        Minimap.Value.HandleMouseDrag();
      }

      if (Game1.activeClickableMenu == null || !(Game1.activeClickableMenu is GameMenu gameMenu))
      {
        isModMapOpen.Value = false;
        return;
      }

      hasOpenedMap.Value =
        gameMenu.currentTab == ModConstants.MapTabIndex; // When map accessed by switching GameMenu tab or pressing M
      isModMapOpen.Value = hasOpenedMap.Value ? isModMapOpen.Value : hasOpenedMap.Value; // When vanilla MapPage is replaced by ModMap

      if (hasOpenedMap.Value && !isModMapOpen.Value) // Only run once on map open
        OpenModMap();
    }

    private void Multiplayer_PeerDisconnected(object sender, PeerDisconnectedEventArgs e)
    {
      // Remove disconnected peer's ID from list if exists
      if (Context.IsMainPlayer && Context.IsMultiplayer)
      {
        playerIds.Remove(e.Peer.PlayerID);

        if (playerIds.Count == 0)
        {
          // Set list to null and stop listening to disconnections
          playerIds = null;
          Helper.Events.Multiplayer.PeerDisconnected -= Multiplayer_PeerDisconnected;
        }
      }
    }

    private void Multiplayer_ModMessageReceived(object sender, ModMessageReceivedEventArgs e)
    {
      if (e.FromModID == ModManifest.UniqueID)
      {
        switch (e.Type)
        {
          case "PlayerReady":
            if (Context.IsMainPlayer)
            {
              if (playerIds == null)
              {
                // Instantiate list and listen to player disconnects
                playerIds = new List<long>(3);
                Helper.Events.Multiplayer.PeerDisconnected += Multiplayer_PeerDisconnected;
              }
              playerIds.Add(e.FromPlayerID);

              Helper.Multiplayer.SendMessage(Customizations.Names, "SyncedNames", modIDs: new string[] { ModManifest.UniqueID }, playerIDs: playerIds.ToArray());
            }
            break;
          case "SyncedNames":
            if (Customizations != null)
            {
              var syncedNames = e.ReadAs<Dictionary<string, string>>();
              Customizations.Names = syncedNames;
            }
            break;
          case "SyncedNpcMarkers":
            if (NpcMarkers.Value == null) return;

            var syncedNpcMarkers = e.ReadAs<Dictionary<string, SyncedNpcMarker>>();
            foreach (var syncedMarker in syncedNpcMarkers)
            {
              if (!ModMain.Globals.NpcMarkerOffsets.TryGetValue(syncedMarker.Key, out var offset))
              {
                offset = 0;
              }

              if (NpcMarkers.Value.TryGetValue(syncedMarker.Key, out var npcMarker))
              {
                npcMarker.LocationName = syncedMarker.Value.LocationName;
                npcMarker.MapX = syncedMarker.Value.MapX;
                npcMarker.MapY = syncedMarker.Value.MapY;
                npcMarker.DisplayName = syncedMarker.Value.DisplayName;
                npcMarker.CropOffset = offset;
                npcMarker.IsBirthday = syncedMarker.Value.IsBirthday;
                npcMarker.Type = syncedMarker.Value.Type;

                if (!Customizations.Names.TryGetValue(syncedMarker.Key, out var name))
                {
                  name = syncedMarker.Key;
                }

                try
                {
                  if (syncedMarker.Value.Type == Character.Villager)
                  {
                    if (name == "Leo")
                    {
                      npcMarker.Sprite = new AnimatedSprite($"Characters\\ParrotBoy", 0, 16, 32).Texture;
                    }
                    else
                    {
                      npcMarker.Sprite = new AnimatedSprite($"Characters\\{name}", 0, 16, 32).Texture;
                    }
                  }
                  else
                  {
                    var sprite = Game1.getCharacterFromName(syncedMarker.Key, false) != null ? Game1.getCharacterFromName(syncedMarker.Key, false).Sprite.Texture : null;
                    npcMarker.Sprite = sprite;
                  };
                }
                catch
                {
                  npcMarker.Sprite = null;
                }
               
              }
              else 
              {
                var newMarker = new NpcMarker
                {
                  LocationName = syncedMarker.Value.LocationName,
                  MapX = syncedMarker.Value.MapX,
                  MapY = syncedMarker.Value.MapY,
                  DisplayName = syncedMarker.Value.DisplayName,
                  IsBirthday = syncedMarker.Value.IsBirthday,
                  Type = syncedMarker.Value.Type
                };

                if (!Customizations.Names.TryGetValue(syncedMarker.Key, out var name))
                {
                  name = syncedMarker.Key;
                }

                try
                {
                  if (syncedMarker.Value.Type == Character.Villager)
                  {
                    if (name == "Leo")
                    {
                      newMarker.Sprite = new AnimatedSprite($"Characters\\ParrotBoy", 0, 16, 32).Texture;
                    }
                    else
                    {
                      newMarker.Sprite = new AnimatedSprite($"Characters\\{name}", 0, 16, 32).Texture;
                    }
                  }
                  else
                  {
                    var sprite = Game1.getCharacterFromName(syncedMarker.Key, false) != null ? Game1.getCharacterFromName(syncedMarker.Key, false).Sprite.Texture : null;
                    newMarker.Sprite = sprite;
                  };
                }
                catch
                {
                  newMarker.Sprite = null;
                }

                NpcMarkers.Value.Add(syncedMarker.Key, newMarker);
              }
            }
            break;
          default:
            break;
        }
      }
    }

    private void OpenModMap()
    {
      if (!(Game1.activeClickableMenu is GameMenu gameMenu)) return;

      isModMapOpen.Value = true;

      var pages = Helper.Reflection
        .GetField<List<IClickableMenu>>(gameMenu, "pages").GetValue();

      // Changing the page in GameMenu instead of changing Game1.activeClickableMenu
      // allows for better compatibility with other mods that use MapPage
      pages[ModConstants.MapTabIndex] = new ModMapPage(
        NpcMarkers.Value,
        ConditionalNpcs.Value,
        FarmerMarkers.Value,
        FarmBuildings,
        BuildingMarkers.Value,
        Customizations
      );
    }

    private void UpdateMarkers(bool forceUpdate = false)
    {
      if (isModMapOpen.Value || forceUpdate)
      {
        if (!Context.IsMultiplayer || Context.IsMainPlayer)
        {
          UpdateNpcs();
        }
        else
        {
          UpdateNpcsFarmhand();
        }

        if (Context.IsMultiplayer)
          UpdateFarmers();
      }
    }

    // Update NPC marker data and names on hover
    private void UpdateNpcs()
    {
      if (NpcMarkers.Value == null) return;

      List<NPC> npc_list = GetVillagers();

      // If player is riding a horse, add it to list
      if (Game1.player.isRidingHorse())
      {
        npc_list.Add(Game1.player.mount);
      }

      foreach (var npc in npc_list)
      {
        if (!NpcMarkers.Value.TryGetValue(npc.Name, out var npcMarker)
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
        var isSameLocation = false;

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
            switch (quest)
            {
              case ItemDeliveryQuest itemDeliveryQuest:
                npcMarker.HasQuest = itemDeliveryQuest.target.Value == npc.Name;
                break;
              case SlayMonsterQuest slayMonsterQuest:
                npcMarker.HasQuest = slayMonsterQuest.target.Value == npc.Name;
                break;
              case FishingQuest fishingQuest:
                npcMarker.HasQuest = fishingQuest.target.Value == npc.Name;
                break;
              case ResourceCollectionQuest resourceCollectionQuest:
                npcMarker.HasQuest = resourceCollectionQuest.target.Value == npc.Name;
                break;
            }
        }

        // Establish draw order, higher number infront
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
          var npcLocation = LocationToMap(locationName, npc.getTileX(), npc.getTileY(), Customizations.MapVectors, Customizations.LocationExclusions);
          npcMarker.MapX = (int)npcLocation.X - 16;
          npcMarker.MapY = (int)npcLocation.Y - 15;
        }
      }
    }

    // Update npc marker properties only relevant to farmhand
    private void UpdateNpcsFarmhand()
    {
      if (NpcMarkers.Value == null) return;

      foreach (var npcMarker in NpcMarkers.Value)
      {
        var name = npcMarker.Key;
        var marker = npcMarker.Value;

        // For show Npcs in player's location option
        var isSameLocation = false;
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
        foreach (var quest in Game1.player.questLog) { 
          if (quest.accepted.Value && quest.dailyQuest.Value && !quest.completed.Value)
            switch (quest)
            {
              case ItemDeliveryQuest itemDeliveryQuest:
                marker.HasQuest = itemDeliveryQuest.target.Value == name;
                break;
              case SlayMonsterQuest slayMonsterQuest:
                marker.HasQuest = slayMonsterQuest.target.Value == name;
                break;
              case FishingQuest fishingQuest:
                marker.HasQuest = fishingQuest.target.Value == name;
                break;
              case ResourceCollectionQuest resourceCollectionQuest:
                marker.HasQuest = resourceCollectionQuest.target.Value == name;
                break;
            }
        }

        // Establish draw order, higher number infront
        // Layers 4 - 7: Outdoor NPCs in order of hidden, hidden w/ quest/birthday, standard, standard w/ quest/birthday
        // Layers 0 - 3: Indoor NPCs in order of hidden, hidden w/ quest/birthday, standard, standard w/ quest/birthday
        if (marker.Type == Character.Horse || marker.Type == Character.Child)
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
        var locationName = farmer.currentLocation.uniqueName.Value ?? farmer.currentLocation.Name;

        if (locationName.Contains("UndergroundMine"))
        {
          locationName = LocationUtil.GetMinesLocationName(locationName);
        }

        var farmerId = farmer.UniqueMultiplayerID;
        var farmerLoc = LocationToMap(
          locationName,
          farmer.getTileX(),
          farmer.getTileY(),
          Customizations.MapVectors,
          Customizations.LocationExclusions
        );

        if (FarmerMarkers.Value.TryGetValue(farmer.UniqueMultiplayerID, out var farMarker))
        {
          var deltaX = farmerLoc.X - farMarker.PrevMapX;
          var deltaY = farmerLoc.Y - farMarker.PrevMapY;

          // Location changes before tile position, causing farmhands to blink
          // to the wrong position upon entering new location. Handle this in draw.
          if (locationName == farMarker.LocationName && MathHelper.Distance(deltaX, deltaY) > 15)
            FarmerMarkers.Value[farmerId].DrawDelay = 1;
          else if (farMarker.DrawDelay > 0)
            FarmerMarkers.Value[farmerId].DrawDelay--;
        }
        else
        {
          var newMarker = new FarmerMarker
          {
            Name = farmer.Name,
            DrawDelay = 0
          };

          FarmerMarkers.Value.Add(farmerId, newMarker);
        }


        FarmerMarkers.Value[farmerId].MapX = (int)farmerLoc.X;
        FarmerMarkers.Value[farmerId].MapY = (int)farmerLoc.Y;
        FarmerMarkers.Value[farmerId].PrevMapX = (int)farmerLoc.X;
        FarmerMarkers.Value[farmerId].PrevMapY = (int)farmerLoc.Y;
        FarmerMarkers.Value[farmerId].LocationName = locationName;
        FarmerMarkers.Value[farmerId].MapX = (int)farmerLoc.X;
        FarmerMarkers.Value[farmerId].MapY = (int)farmerLoc.Y;
      }
    }

    // MAIN METHOD FOR PINPOINTING CHARACTERS ON THE MAP
    // Calculated from mapping of game tile positions to pixel coordinates of the map in MapModConstants. 
    // Requires MapModConstants and modified map page in /maps
    public static Vector2 LocationToMap(string locationName, int tileX = -1, int tileY = -1,
      Dictionary<string, MapVector[]> CustomMapVectors = null, HashSet<string> LocationExclusions = null, bool isPlayer = false)
    {
      if ((LocationExclusions != null && LocationExclusions.Contains(locationName)) || locationName.Contains("WarpRoom")) return UNKNOWN;

      if (FarmBuildings.TryGetValue(locationName, out var mapLoc)) return mapLoc.Value;

      if (locationName.StartsWith("UndergroundMine"))
      {
        var mine = locationName.Substring("UndergroundMine".Length, locationName.Length - "UndergroundMine".Length);
        if (int.TryParse(mine, out var mineLevel))
        {
          // Skull cave
          if (mineLevel > 120)
            locationName = "SkullCave";
          // Mines
          else
            locationName = "Mine";
        }
      }

      // Get location of indoor location by its warp position in the outdoor location
      if (LocationUtil.LocationContexts.TryGetValue(locationName, out var loc)
        && loc.Type != LocationType.Outdoors
        && loc.Root != null
        && locationName != "MovieTheater"         // Weird edge cases where the warps are off
      )
      {
        var building = LocationUtil.GetBuilding(locationName);

        if (building != null)
        {
          var doorX = (int)LocationUtil.LocationContexts[building].Warp.X;
          var doorY = (int)LocationUtil.LocationContexts[building].Warp.Y;

          // Slightly adjust warp location to depict being inside the building 
          var warpPos = LocationToMap(loc.Root, doorX, doorY, CustomMapVectors, LocationExclusions, isPlayer);
          return new Vector2(warpPos.X + 1, warpPos.Y - 8);
        }
      }

      // If we fail to grab the indoor location correctly for whatever reason, fallback to old hard-coded constants

      MapVector[] locVectors = null;
      bool locationNotFound = false;

      if (locationName == "Farm")
      {
        // Handle different farm types for custom vectors
        var farms = new string[7] { "Farm_Default", "Farm_Riverland", "Farm_Forest", "Farm_Hills", "Farm_Wilderness", "Farm_FourCorners", "Farm_Beach" };
        if (CustomMapVectors != null && (CustomMapVectors.Keys.Any(locName => locName == farms.ElementAtOrDefault(Game1.whichFarm))))
        {
          if (!CustomMapVectors.TryGetValue(farms.ElementAtOrDefault(Game1.whichFarm), out locVectors))
          {
            locationNotFound = !ModConstants.MapVectors.TryGetValue(locationName, out locVectors);
          }
        }
        else
        {
          if (!CustomMapVectors.TryGetValue("Farm", out locVectors))
          {
            locationNotFound = !ModConstants.MapVectors.TryGetValue(farms.ElementAtOrDefault(Game1.whichFarm), out locVectors);
            if(locationNotFound)
            {
              locationNotFound = !ModConstants.MapVectors.TryGetValue(locationName, out locVectors);
            }
          }

        }
      }
      // If not in custom vectors, use default
      else if (!(CustomMapVectors != null && CustomMapVectors.TryGetValue(locationName, out locVectors)))
      {
        locationNotFound = !ModConstants.MapVectors.TryGetValue(locationName, out locVectors);
      }

      if (locVectors == null || locationNotFound) return UNKNOWN;

      int x;
      int y;

      // Precise (static) regions and indoor locations
      if (locVectors.Count() == 1 || tileX == -1 || tileY == -1)
      {
        x = locVectors.FirstOrDefault().MapX;
        y = locVectors.FirstOrDefault().MapY;
      }
      else
      {
        // Sort map vectors by distance to point
        var vectors = locVectors.OrderBy(vector =>
          Math.Sqrt(Math.Pow(vector.TileX - tileX, 2) + Math.Pow(vector.TileY - tileY, 2)));

        MapVector lower = null;
        MapVector upper = null;
        var isSameAxis = false;

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
          {
            upper = vector;
          }
        }

        // Handle null cases - not enough vectors to calculate using lower/upper bound strategy
        // Uses fallback strategy - get closest points such that lower != upper
        var tilePos = "(" + tileX + ", " + tileY + ")";
        if (lower == null)
          lower = upper == vectors.First() ? vectors.Skip(1).First() : vectors.First();

        if (upper == null)
          upper = lower == vectors.First() ? vectors.Skip(1).First() : vectors.First();

        x = (int)MathHelper.Clamp((int)(lower.MapX + (tileX - lower.TileX) / (double)(upper.TileX - lower.TileX) * (upper.MapX - lower.MapX)), 0, 1200);
        y = (int)MathHelper.Clamp((int)(lower.MapY + (tileY - lower.TileY) / (double)(upper.TileY - lower.TileY) * (upper.MapY - lower.MapY)), 0, 720);
      }

      return new Vector2(x, y);
    }

    private void Display_WindowResized(object sender, WindowResizedEventArgs e)
    {
      if (!Context.IsWorldReady) return;

      UpdateMarkers(true);
      UpdateFarmBuildingLocs();
      Minimap.Value?.CheckOffsetForMap();

      if (isModMapOpen.Value)
      {
        OpenModMap();
      }
    }

    private void Display_MenuChanged(object sender, MenuChangedEventArgs e)
    {
      if (!Context.IsWorldReady) return;
      MouseUtil.Reset();

      // Check for resize after mod menu closed
      if (e.OldMenu is ModMenu)
        Minimap.Value?.Resize();
    }

    private void Player_Warped(object sender, WarpedEventArgs e)
    {
      if (e.IsLocalPlayer)
      {
        // Hide minimap in blacklisted locations with special case for Mines as usual
        Globals.ShowMinimap = Globals.ShowMinimap && !IsLocationExcluded(e.NewLocation.Name);

        // Check if map does not fill screen and adjust for black bars (ex. BusStop)
        Minimap.Value?.CheckOffsetForMap();
      }
    }

    private void Display_RenderingHud(object sender, RenderingHudEventArgs e)
    {
      if (Context.IsWorldReady && Globals.ShowMinimap && Game1.displayHUD) Minimap.Value?.DrawMiniMap();
    }

    private void Display_RenderedWorld(object sender, RenderedWorldEventArgs e)
    {
      // Highlight tile for debug mode
      if (DEBUG_MODE)
        Game1.spriteBatch.Draw(Game1.mouseCursors,
          new Vector2(
            Game1.tileSize * (int)Math.Floor(Game1.currentCursorTile.X) - Game1.viewport.X,
            Game1.tileSize * (int)Math.Floor(Game1.currentCursorTile.Y) - Game1.viewport.Y),
          new Rectangle(448, 128, 64, 64), Color.White);
    }

    // DEBUG 
    private void Display_Rendered(object sender, RenderedEventArgs e)
    {
      if (!Context.IsWorldReady || Game1.player == null) return;

      if (DEBUG_MODE)
        ShowDebugInfo();
    }

    // Show debug info in top left corner
    private void ShowDebugInfo()
    {
      if (Game1.player.currentLocation == null) return;
      string locationName = Game1.player.currentLocation.uniqueName.Value ?? Game1.player.currentLocation.Name;
      var textHeight = (int)Game1.dialogueFont
        .MeasureString("()").Y - 6;

      var currMenu = Game1.activeClickableMenu is GameMenu ? (GameMenu)Game1.activeClickableMenu : null;

      // If map is open, show map position at cursor
      if (isModMapOpen.Value)
      {
        int borderWidth = 3;
        float borderOpacity = 0.75f;
        Vector2 mapPos = MouseUtil.GetMapPositionAtCursor();
        Rectangle bounds = MouseUtil.GetDragAndDropArea();

        var tex = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
        tex.SetData(new Color[] { Color.Red });

        // Draw point at cursor on map
        Game1.spriteBatch.Draw(tex,
          new Rectangle(Game1.getMouseX() - (int)(borderWidth / 2), Game1.getMouseY() - (int)(borderWidth / 2), borderWidth, borderWidth),
          Rectangle.Empty, Color.White);

        // Show map pixel position at cursor
        DrawText($"Map position: ({mapPos.X}, {mapPos.Y})",
          new Vector2(Game1.tileSize / 4, Game1.tileSize / 4), Color.White);

        // Draw drag and drop area
        if (Helper.Input.GetState(SButton.MouseRight) == SButtonState.Held)
        {
          // Draw dragging box
          DrawBorder(tex,
           MouseUtil.GetCurrentDraggingArea(), borderWidth, Color.White * borderOpacity);
        }
        else
        {
          if (MouseUtil.BeginMousePosition.X < 0 && MouseUtil.EndMousePosition.X < 0) return;

          // Draw drag and drop box
          DrawBorder(tex,
            bounds,
            borderWidth, Color.White * borderOpacity);

          var mapBounds = MouseUtil.GetRectangleOnMap(bounds);

          if (mapBounds.Width == 0 && mapBounds.Height == 0)
          {
            // Show point
            DrawText($"Point: ({mapBounds.X}, {mapBounds.Y})",
              new Vector2(Game1.tileSize / 4, Game1.tileSize / 4 + textHeight), Color.White);
          }
          else
          {
            // Show first point of DnD box
            DrawText($"Top-left: ({mapBounds.X}, {mapBounds.Y})",
              new Vector2(Game1.tileSize / 4, Game1.tileSize / 4 + textHeight), Color.White);

            // Show second point of DnD box
            DrawText($"Bot-right: ({mapBounds.X + mapBounds.Width}, {mapBounds.Y + mapBounds.Height})",
              new Vector2(Game1.tileSize / 4, Game1.tileSize / 4 + textHeight * 2), Color.White);

            // Show width of DnD box
            DrawText($"Width: {mapBounds.Width}",
              new Vector2(Game1.tileSize / 4, Game1.tileSize / 4 + textHeight * 3), Color.White);

            // Show height of DnD box
            DrawText($"Height: {mapBounds.Height}",
              new Vector2(Game1.tileSize / 4, Game1.tileSize / 4 + textHeight * 4), Color.White);
          }
        }
      }
      else
      {
        // Show tile position of tile at cursor
        var tilePos = MouseUtil.GetTilePositionAtCursor();
        DrawText($"{locationName} ({Game1.currentLocation.Map.DisplayWidth / Game1.tileSize} x {Game1.currentLocation.Map.DisplayHeight / Game1.tileSize})", new Vector2(Game1.tileSize / 4, Game1.tileSize / 4), Color.White);
        DrawText($"Tile position: ({tilePos.X}, {tilePos.Y})", new Vector2(Game1.tileSize / 4, Game1.tileSize / 4 + textHeight), Color.White);
      }
    }

    // Draw outlined text
    private static void DrawText(string text, Vector2 pos, Color? color = null)
    {
      var tex = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
      tex.SetData(new Color[] { Color.Black * 0.75f });

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
    private static void DrawBorder(Texture2D tex, Rectangle rect, int borderWidth, Color color)
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