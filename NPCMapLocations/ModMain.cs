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

namespace NPCMapLocations
{
  public class ModMain : Mod, IAssetLoader
  {
    public static PlayerConfig Config;
    public static GlobalConfig Globals;
    public static CustomData CustomData;
    public static IModHelper Helper;
    public static IMonitor IMonitor;
    public static SButton HeldKey;
    public static Texture2D Map;
    public static int mapTab;
    public static Vector2 UNKNOWN = new Vector2(-9999, -9999); 

    private const int DRAW_DELAY = 3;
    private Texture2D BuildingMarkers;
    private Dictionary<string, MapVector[]> MapVectors;
    private ModMinimap Minimap;
    private HashSet<CharacterMarker> NpcMarkers;
    private Dictionary<string, bool> ConditionalNpcs;
    private bool hasOpenedMap;
    private bool isModMapOpen;
    private bool shouldShowMinimap;

    // Multiplayer
    private Dictionary<long, CharacterMarker> FarmerMarkers;

    // Customizations/Custom mods
    private string MapSeason;
    private ModCustomizations Customizations;

    // Debugging
    private static bool DEBUG_MODE;
    private static Dictionary<string, KeyValuePair<string, Vector2>> FarmBuildings;
    private static List<string> alertFlags;

    // Replace game map with modified mapinit
    public bool CanLoad<T>(IAssetInfo asset)
    {
      return asset.AssetNameEquals(@"LooseSprites\Map") && Customizations != null;
    }

    public T Load<T>(IAssetInfo asset)
    {
      T map;

      if (MapSeason == null)
      {
        Monitor.Log("Unable to get current season. Defaulted to spring.", LogLevel.Debug);
        MapSeason = "spring";
      }

      if (!File.Exists(Path.Combine(ModMain.Helper.DirectoryPath, Customizations.MapsPath, $"{this.MapSeason}_map.png")))
      {
        Monitor.Log("Seasonal maps not provided. Defaulted to spring.", LogLevel.Debug);
        MapSeason = null; // Set to null so that cache is not invalidate when game season changes
      }

      // Replace map page
      string filename = this.MapSeason == null ? "spring_map.png" : $"{this.MapSeason}_map.png";

      bool useRecolor = Customizations.MapsPath != null && File.Exists(Path.Combine(ModMain.Helper.DirectoryPath, Customizations.MapsPath, filename));
      map = useRecolor
        ? Helper.Content.Load<T>(Path.Combine(Customizations.MapsPath, filename))
        : Helper.Content.Load<T>(Path.Combine(Customizations.MapsRootPath, "_default", filename));

      if (useRecolor)
        Monitor.Log($"Using recolored map {Path.Combine(Customizations.MapsPath, filename)}.", LogLevel.Debug);

      return map;
    }

    public override void Entry(IModHelper helper)
    {
      Helper = helper;
      IMonitor = Monitor;
      Globals = Helper.Data.ReadJsonFile<GlobalConfig>("config/globals.json") ?? new GlobalConfig();

      Helper.Events.GameLoop.SaveLoaded += GameLoop_SaveLoaded;
      Helper.Events.Multiplayer.ModMessageReceived += Multiplayer_ModMessageReceived;
      Helper.Events.GameLoop.DayStarted += GameLoop_DayStarted;
      Helper.Events.World.BuildingListChanged += World_BuildingListChanged;
      Helper.Events.Input.ButtonPressed += Input_ButtonPressed;
      Helper.Events.Input.ButtonReleased += Input_ButtonReleased;
      Helper.Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked;
      Helper.Events.Player.Warped += Player_Warped;
      Helper.Events.Display.MenuChanged += Display_MenuChanged;
      Helper.Events.Display.RenderingHud += Display_RenderingHud;
      Helper.Events.Display.Rendered += Display_Rendered;
      Helper.Events.Display.WindowResized += Display_WindowResized;
    }

    // Load config and other one-off data
    private void GameLoop_SaveLoaded(object sender, SaveLoadedEventArgs e)
    {
      Config = Helper.Data.ReadJsonFile<PlayerConfig>($"config/{Constants.SaveFolderName}.json") ?? new PlayerConfig();

      // Load customizations
      Customizations = new ModCustomizations();
      CustomData = Helper.Data.ReadJsonFile<CustomData>(Path.Combine(Customizations.MapsPath, "customlocations.json")) ?? new CustomData();
      Customizations.LoadCustomData();

      // Load farm buildings
      try
      {
        BuildingMarkers = Helper.Content.Load<Texture2D>(Path.Combine(Customizations.MapsPath, "buildings.png"));
      }
      catch
      {
        BuildingMarkers = null;
      }

      // Get season for map
      MapSeason = Globals.UseSeasonalMaps ? Game1.currentSeason : "spring";
      Helper.Content.InvalidateCache("LooseSprites/Map");
      Map = Game1.content.Load<Texture2D>("LooseSprites\\map");

      // Disable for multiplayer for anti-cheat
      DEBUG_MODE = Globals.DEBUG_MODE && !Context.IsMultiplayer;
      shouldShowMinimap = Config.ShowMinimap;

      // NPCs should be unlocked before showing
      ConditionalNpcs = new Dictionary<string, bool>
      {
        {"Dwarf", false},
        {"Kent", false},
        {"Krobus", false},
        {"Marlon", false},
        {"Merchant", false},
        {"Sandy", false},
        {"Wizard", false}
      };

      MapVectors = ModConstants.MapVectors;

      // Add custom map vectors from customlocations.json
      foreach (var locVectors in Customizations.MapVectors)
      {
        if (MapVectors.TryGetValue(locVectors.Key, out var mapVectors))
          MapVectors[locVectors.Key] = locVectors.Value;
        else
          MapVectors.Add(locVectors.Key, locVectors.Value);
      }

      // Get context of all locations (indoor, outdoor, relativity)
      LocationUtil.GetLocationContexts();
      alertFlags = new List<string>();

      // Log any custom locations not in customlocations.json
      foreach (var locCtx in LocationUtil.LocationContexts)
      {
        if (
          locCtx.Value.Root == null
          || ((!locCtx.Key.Equals("FarmHouse")
          && !locCtx.Key.Contains("Cabin")
          && !locCtx.Key.Contains("UndergroundMine"))
          && !MapVectors.TryGetValue(locCtx.Value.Root, out var loc))
        )
        {
          if (!alertFlags.Contains("UnknownLocation:" + locCtx.Key))
          {
            Monitor.Log($"Unknown location: {locCtx.Key}", LogLevel.Debug);
            alertFlags.Add("UnknownLocation:" + locCtx.Key);
          }
        }
      }

      UpdateFarmBuildingLocs();

      // Find index of MapPage since it's a different value for SDV mobile
      var pages = Helper.Reflection.GetField<List<IClickableMenu>>(new GameMenu(false), "pages").GetValue();

      foreach (var page in pages)
      {
        if (page is MapPage)
        {
          mapTab = pages.IndexOf(page);
        }
      }

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
        !ModConstants.ExcludedNpcs.Contains(npc.Name)
        && npc.GetType().GetProperty("ExcludeFromMap") == null // For other developers to always exclude an npc
        && (
          npc.isVillager()
          | npc.isMarried()
          | (Globals.ShowHorse && npc is Horse)
          | (Globals.ShowChildren && npc is Child)
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
          Customizations.MapVectors
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
      if (Config.ShowMinimap && Minimap != null)
      {
        if (Minimap.isHoveringDragZone() && e.Button == SButton.MouseRight)
        {
          MouseUtil.HandleMouseDown(() => Minimap.HandleMouseDown());
        }
      }

      // Debug DnD
      if
        (DEBUG_MODE && e.Button == SButton.MouseRight && isModMapOpen)
      {
        MouseUtil.HandleMouseDown();
      }

      // Minimap toggle
      if (e.Button.ToString().Equals(Globals.MinimapToggleKey) && Game1.activeClickableMenu == null)
      {
        Config.ShowMinimap = !Config.ShowMinimap;
        Helper.Data.WriteJsonFile($"config/{Constants.SaveFolderName}.json", Config);
      }

      // ModMenu
      if (Game1.activeClickableMenu is GameMenu)
        HandleInput((GameMenu)Game1.activeClickableMenu, e.Button);

      if (DEBUG_MODE && e.Button == SButton.LeftControl) HeldKey = e.Button;

      if (DEBUG_MODE && !Context.IsMultiplayer && HeldKey == SButton.LeftControl && e.Button.Equals(SButton.MouseRight))
        Game1.player.setTileLocation(Game1.currentCursorTile);
    }

    private void Input_ButtonReleased(object sender, ButtonReleasedEventArgs e)
    {
      if (!Context.IsWorldReady) return;

      if (Minimap != null)
      {
        if (Game1.activeClickableMenu is ModMenu && e.Button == SButton.MouseLeft) { 
          Minimap.Resize();
        }
        else if (Game1.activeClickableMenu == null && e.Button == SButton.MouseRight)
        {
          MouseUtil.HandleMouseRelease(() => Minimap.HandleMouseRelease());
        }
      }
      else if (DEBUG_MODE && e.Button == SButton.MouseRight && isModMapOpen)
      {
        MouseUtil.HandleMouseRelease();
      }
    }

    // Handle keyboard/controller inputs
    private void HandleInput(GameMenu menu, SButton input)
    {
      if (menu.currentTab != mapTab) return;
      if (input.ToString().Equals(Globals.MenuKey) || input is SButton.ControllerY)
        Game1.activeClickableMenu = new ModMenu(
          ConditionalNpcs,
          Customizations
        );

      if (input.ToString().Equals(Globals.TooltipKey) || input is SButton.RightShoulder)
        ChangeTooltipConfig();
      else if (input.ToString().Equals(Globals.TooltipKey) || input is SButton.LeftShoulder) ChangeTooltipConfig(false);
    }

    private void ChangeTooltipConfig(bool incre = true)
    {
      if (incre)
      {
        if (++Config.NameTooltipMode > 3) Config.NameTooltipMode = 1;

        Helper.Data.WriteJsonFile($"config/{Constants.SaveFolderName}.json", Config);
      }
      else
      {
        if (--Config.NameTooltipMode < 1) Config.NameTooltipMode = 3;

        Helper.Data.WriteJsonFile($"config/{Constants.SaveFolderName}.json", Config);
      }
    }

    // Handle any checks that need to be made per day
    private void GameLoop_DayStarted(object sender, DayStartedEventArgs e)
    {

      var npcEntries = ConditionalNpcs != null ? new Dictionary<string, bool>(ConditionalNpcs) : new Dictionary<string, bool>(new Dictionary<string, bool>
      {
        {"Dwarf", false},
        {"Kent", false},
        {"Krobus", false},
        {"Marlon", false},
        {"Merchant", false},
        {"Sandy", false},
        {"Wizard", false}
      });

      // Characters that are not always available, for avoiding spoilers
      foreach (var npc in npcEntries)
      {
        var name = npc.Key;
        try
        {
          switch (name)
          {
            case "Dwarf":
              // Marlon cutscene when first entering mines
              ConditionalNpcs[name] = Game1.MasterPlayer.eventsSeen.Contains(100162);
              break;
            case "Kent":
              // Kent returns year 2
              ConditionalNpcs[name] = Game1.year >= 2;
              break;
            case "Krobus":
              // Rusty Key which unlocks sewer
              ConditionalNpcs[name] = Game1.MasterPlayer.hasRustyKey;
              break;
            case "Marlon":
              // Marlon cutscene when first entering mines
              ConditionalNpcs[name] = Game1.MasterPlayer.eventsSeen.Contains(100162);
              break;
            case "Merchant":
              // Merchant schedule
              ConditionalNpcs[name] = ((Forest)Game1.getLocationFromName("Forest")).travelingMerchantDay;
              break;
            case "Sandy":
              // When player meets Sandy for the first time
              ConditionalNpcs[name] = Game1.MasterPlayer.eventsSeen.Contains(67);
              break;
            case "Wizard":
              // Scene for unlocking wizard
              ConditionalNpcs[name] = Game1.MasterPlayer.eventsSeen.Contains(112);
              break;
          }
        }
        catch
        {
          ConditionalNpcs[name] = false;
        }
      }

      ResetMarkers();
      UpdateMarkers(true);

      Minimap = new ModMinimap(
        NpcMarkers,
        ConditionalNpcs,
        FarmerMarkers,
        FarmBuildings,
        BuildingMarkers,
        Customizations
      );

      shouldShowMinimap = !IsLocationBlacklisted(Game1.player.currentLocation.Name);
    }

    private bool IsLocationBlacklisted(string location)
    {
      return Config.ShowMinimap && Globals.MinimapBlacklist.Any(loc => loc != "Farm" && location.StartsWith(loc) || loc == "Farm" && location == "Farm") ||
               ((Globals.MinimapBlacklist.Contains("Mine") || Globals.MinimapBlacklist.Contains("UndergroundMine")) && location.Contains("Mine"));
    }

    private void ResetMarkers()
    {
      NpcMarkers = new HashSet<CharacterMarker>();

      foreach (var npc in GetVillagers())
      {
        if (Customizations.Names.TryGetValue(npc.Name, out var npcName))
        {
          NpcMarkers.Add(new CharacterMarker
          {
            Npc = npc,
            Name = npcName,
            Marker = npc.Sprite.Texture,
            IsBirthday = npc.isBirthday(Game1.currentSeason, Game1.dayOfMonth)
          });
        }
      }

      if (Context.IsMultiplayer) FarmerMarkers = new Dictionary<long, CharacterMarker>();
    }

    // To initialize ModMap quicker for smoother rendering when opening map
    private void GameLoop_UpdateTicked(object sender, UpdateTickedEventArgs e)
    {
      if (!Context.IsWorldReady) return;

      // One-second tick
      if (e.IsOneSecond)
      {
        // Sync multiplayer data
        if (Context.IsMainPlayer && Context.IsMultiplayer)
        {

          var message = new SyncedLocationData();
          foreach (var npc in GetVillagers())
          {
            if (npc == null || npc.currentLocation == null) continue;

            try
            {
              message.AddNpcLocation(npc.Name,
                new LocationData(npc.currentLocation.uniqueName.Value ?? npc.currentLocation.Name, npc.Position.X,
                  npc.Position.Y));
            }
            catch
            {
              IMonitor.Log("Failed to send synced location data.", LogLevel.Error);
            }

          }

          Helper.Multiplayer.SendMessage(message, "SyncedLocationData", modIDs: new string[] { ModManifest.UniqueID });
        }

        // Check season change (for when it's changed via console)
        if (Globals.UseSeasonalMaps && (MapSeason != null && MapSeason != Game1.currentSeason) && Game1.currentSeason != null)
        {
          MapSeason = Game1.currentSeason;

          // Force reload of map for season changes
          try
          {
            Helper.Content.InvalidateCache("LooseSprites/Map");
          }
          catch
          {
            Monitor.Log("Failed to update map for current season.", LogLevel.Error);
          }

          Minimap?.UpdateMapForSeason();
        }
      }

      // Half-second tick
      if (e.IsMultipleOf(30))
      {
        // Map page updates
        var updateForMinimap = false || shouldShowMinimap;

        if (Config.ShowMinimap)
          if (Minimap != null)
          {
            Minimap.Update();
            updateForMinimap = true;
          }

        UpdateMarkers(updateForMinimap);
      }

      // Update tick
      // If minimap is being dragged, suppress mouse behavior
      if (Config.ShowMinimap && Minimap != null && Minimap.isHoveringDragZone() && Helper.Input.GetState(SButton.MouseRight) == SButtonState.Held)
      {
        Minimap.HandleMouseDrag();
      }

      if (Game1.activeClickableMenu == null || !(Game1.activeClickableMenu is GameMenu gameMenu))
      {
        isModMapOpen = false;
        return;
      }

      hasOpenedMap =
        gameMenu.currentTab == mapTab; // When map accessed by switching GameMenu tab or pressing M
      isModMapOpen = hasOpenedMap ? isModMapOpen : hasOpenedMap; // When vanilla MapPage is replaced by ModMap

      if (hasOpenedMap && !isModMapOpen) // Only run once on map open
        OpenModMap();
    }

    private void Multiplayer_ModMessageReceived(object sender, ModMessageReceivedEventArgs e)
    {
      if (NpcMarkers == null) return;

      if (e.FromModID == ModManifest.UniqueID && e.Type == "SyncedLocationData")
      {
        var message = e.ReadAs<SyncedLocationData>();
        foreach (var marker in NpcMarkers)
        {
          if (message.SyncedLocations.TryGetValue(marker.Npc.Name, out var npcLoc))
          {
            marker.SyncedLocationName = npcLoc.LocationName;
            if (!marker.IsHidden)
            {
              var mapLocation = LocationToMap(npcLoc.LocationName, (int)Math.Floor(npcLoc.X / Game1.tileSize), (int)Math.Floor(npcLoc.Y / Game1.tileSize), Customizations.MapVectors);
              marker.MapLocation = new Vector2(mapLocation.X - 16, mapLocation.Y - 15);
            }
          }

          else
          {
            marker.MapLocation = UNKNOWN;
          }
        }
      }
    }

    private void OpenModMap()
    {
      if (!(Game1.activeClickableMenu is GameMenu gameMenu)) return;

      isModMapOpen = true;
      UpdateNpcs(true);
      var pages = Helper.Reflection
        .GetField<List<IClickableMenu>>(gameMenu, "pages").GetValue();

      // Changing the page in GameMenu instead of changing Game1.activeClickableMenu
      // allows for better compatibility with other mods that use MapPage
      pages[mapTab] = new ModMapPage(
        NpcMarkers,
        ConditionalNpcs,
        FarmerMarkers,
        FarmBuildings,
        BuildingMarkers,
        Customizations
      );
    }

    private void UpdateMarkers(bool forceUpdate = false)
    {
      if (isModMapOpen || forceUpdate)
      {
        UpdateNpcs(forceUpdate);

        if (Context.IsMultiplayer)
          UpdateFarmers();
      }
    }

    // Update NPC marker data and names on hover
    private void UpdateNpcs(bool forceUpdate = false)
    {
      if (NpcMarkers == null) return;

      foreach (var npcMarker in NpcMarkers)
      {
        string locationName;
        var npc = npcMarker.Npc;

        if (npcMarker.SyncedLocationName == null)
        {
          // Skip if no location
          if (npc.currentLocation == null)
            continue;

          locationName = npc.currentLocation.uniqueName.Value ?? npc.currentLocation.Name;
        }
        else
        {
          locationName = npcMarker.SyncedLocationName;
        }

        // For layering indoor/outdoor NPCs and indoor indicator
        if (LocationUtil.LocationContexts.TryGetValue(locationName, out var locCtx))
        {
          npcMarker.IsOutdoors = locCtx.Type == "outdoors";
        }
        else
        {
          npcMarker.IsOutdoors = false;
        }

        // For show Npcs in player's location option
        var isSameLocation = false;
        if (Config.OnlySameLocation)
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
        npcMarker.IsHidden = Config.ImmersionOption == 2 && !Game1.player.hasTalkedToFriendToday(npcMarker.Npc.Name)
                             || Config.ImmersionOption == 3 && Game1.player.hasTalkedToFriendToday(npcMarker.Npc.Name)
                             || Config.OnlySameLocation && !isSameLocation
                             || Config.ByHeartLevel
                             && !(Game1.player.getFriendshipHeartLevelForNPC(npcMarker.Npc.Name)
                                  >= Config.HeartLevelMin && Game1.player.getFriendshipHeartLevelForNPC(npcMarker.Npc.Name)
                                  <= Config.HeartLevelMax);

        // NPCs that will be drawn onto the map
        if ((!ModMain.Globals.NpcBlacklist.Contains(npcMarker.Npc.Name)) && (Config.ShowHiddenVillagers || !npcMarker.IsHidden))
        {
          // Check if gifted for birthday
          if (npcMarker.IsBirthday)
          {
            npcMarker.IsBirthday = Game1.player.friendshipData.ContainsKey(npcMarker.Npc.Name) &&
                                   Game1.player.friendshipData[npcMarker.Npc.Name].GiftsToday == 0;

            // Check for daily quests
            foreach (var quest in Game1.player.questLog)
              if (quest.accepted.Value && quest.dailyQuest.Value && !quest.completed.Value)
                switch (quest.questType.Value)
                {
                  case 3:
                    npcMarker.HasQuest = ((ItemDeliveryQuest)quest).target.Value == npcMarker.Npc.Name;
                    break;
                  case 4:
                    npcMarker.HasQuest = ((SlayMonsterQuest)quest).target.Value == npcMarker.Npc.Name;
                    break;
                  case 7:
                    npcMarker.HasQuest = ((FishingQuest)quest).target.Value == npcMarker.Npc.Name;
                    break;
                  case 10:
                    npcMarker.HasQuest = ((ResourceCollectionQuest)quest).target.Value == npcMarker.Npc.Name;
                    break;
                }
          }
          else
          {
            npcMarker.HasQuest = false;
          }

          // Establish draw order, higher number infront
          // Layers 4 - 7: Outdoor NPCs in order of hidden, hidden w/ quest/birthday, standard, standard w/ quest/birthday
          // Layers 0 - 3: Indoor NPCs in order of hidden, hidden w/ quest/birthday, standard, standard w/ quest/birthday
          if (npcMarker.Npc is Horse || npcMarker.Npc is Child)
          {
            npcMarker.Layer = 0;
          }
          else
          {
            npcMarker.Layer = npcMarker.IsOutdoors ? 6 : 2;
            if (npcMarker.IsHidden) npcMarker.Layer -= 2;
          }

          if (npcMarker.HasQuest || npcMarker.IsBirthday) npcMarker.Layer++;

          /*
          // Only do calculations if NPCs are moving
          if (!forceUpdate 
              && (npcMarker.Location != Rectangle.Empty
              && (!npcLocation.IsOutdoors // Indoors
              || !npcMarker.Npc.isMoving()))) // Not moving
          {
              continue;
          }
          */

          if (npcMarker.SyncedLocationName == null)
          {
            // Get center of NPC marker
            var npcLocation = LocationToMap(locationName, npc.getTileX(), npc.getTileY(), Customizations.MapVectors);
            var x = (int)npcLocation.X - 16;
            var y = (int)npcLocation.Y - 15;
            npcMarker.MapLocation = new Vector2(x, y);
          }
        }
        else
        {
          // Set no location so they don't get drawn
          npcMarker.MapLocation = UNKNOWN;
        }
      }
    }

    private void UpdateFarmers()
    {
      foreach (var farmer in Game1.getOnlineFarmers())
      {
        if (farmer?.currentLocation == null) continue;
        var locationName = farmer.currentLocation.uniqueName.Value ?? farmer.currentLocation.Name;

        if (locationName.Contains("UndergroundMine"))
          locationName = LocationUtil.GetMinesLocationName(locationName);

        var farmerId = farmer.UniqueMultiplayerID;
        var farmerLocationName = farmer.currentLocation.uniqueName.Value ?? farmer.currentLocation.Name;
        var farmerLoc = LocationToMap(farmerLocationName,
          farmer.getTileX(), farmer.getTileY(), Customizations.MapVectors);

        if (FarmerMarkers.TryGetValue(farmer.UniqueMultiplayerID, out var farMarker))
        {
          var deltaX = farmerLoc.X - farMarker.PrevMapLocation.X;
          var deltaY = farmerLoc.Y - farMarker.PrevMapLocation.Y;

          // Location changes before tile position, causing farmhands to blink
          // to the wrong position upon entering new location. Handle this in draw.
          if (farmerLocationName == farMarker.PrevLocationName && MathHelper.Distance(deltaX, deltaY) > 15)
            FarmerMarkers[farmerId].DrawDelay = DRAW_DELAY;
          else if (farMarker.DrawDelay > 0)
            FarmerMarkers[farmerId].DrawDelay--;
        }
        else
        {
          var newMarker = new CharacterMarker
          {
            Name = farmer.Name,
            DrawDelay = 0
          };

          FarmerMarkers.Add(farmerId, newMarker);
        }

        FarmerMarkers[farmerId].MapLocation = farmerLoc;
        FarmerMarkers[farmerId].PrevMapLocation = farmerLoc;
        FarmerMarkers[farmerId].PrevLocationName = farmer.currentLocation.uniqueName.Value ?? farmer.currentLocation.Name;
        FarmerMarkers[farmerId].IsOutdoors = farmer.currentLocation.IsOutdoors;
      }
    }

    // MAIN METHOD FOR PINPOINTING CHARACTERS ON THE MAP
    // Calculated from mapping of game tile positions to pixel coordinates of the map in MapModConstants. 
    // Requires MapModConstants and modified map page in /maps
    public static Vector2 LocationToMap(string locationName, int tileX = -1, int tileY = -1,
      Dictionary<string, MapVector[]> CustomMapVectors = null, bool isPlayer = false)
    {
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
      if (LocationUtil.LocationContexts.TryGetValue(locationName, out var loc) && loc.Type != "outdoors" && loc.Root != null)
      {
        var building = LocationUtil.GetBuilding(locationName);

        if (building != null)
        {
          var doorX = (int)LocationUtil.LocationContexts[building].Warp.X;
          var doorY = (int)LocationUtil.LocationContexts[building].Warp.Y;

          // Slightly adjust warp location to depict being inside the building 
          var warpPos = LocationToMap(loc.Root, doorX, doorY, CustomMapVectors, isPlayer);
          return new Vector2(warpPos.X + 1, warpPos.Y - 8);
        }
      }

      // If we fail to grab the indoor location correctly for whatever reason, fallback to old hard-coded constants

      MapVector[] locVectors = null;
      bool locationNotFound = false;

      if (locationName == "Farm")
      {
        // Handle different farm types for custom vectors
        var farms = new string[6] { "Farm_Default", "Farm_Riverland", "Farm_Forest", "Farm_Hills", "Farm_Wilderness", "Farm_FourCorners" };
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
            locationNotFound = !ModConstants.MapVectors.TryGetValue(locationName, out locVectors);
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

        //        if (DEBUG_MODE && isPlayer)
        //        {
        //          _tileUpper = new Vector2(upper.TileX, upper.TileY);
        //          _tileLower = new Vector2(lower.TileX, lower.TileY);
        //        }
      }

      return new Vector2(x, y);
    }

    private void Display_WindowResized(object sender, WindowResizedEventArgs e)
    {
      if (!Context.IsWorldReady) return;

      UpdateMarkers(true);
      UpdateFarmBuildingLocs();
      Minimap?.CheckOffsetForMap();

      if (isModMapOpen)
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
        Minimap?.Resize();
    }

    private void Player_Warped(object sender, WarpedEventArgs e)
    {
      if (!e.IsLocalPlayer) return;

      // Hide minimap in blacklisted locations with special case for Mines as usual
      shouldShowMinimap = !IsLocationBlacklisted(e.NewLocation.Name);

      // Check if map does not fill screen and adjust for black bars (ex. BusStop)
      Minimap?.CheckOffsetForMap();
    }

    private void Display_RenderingHud(object sender, RenderingHudEventArgs e)
    {
      if (Context.IsWorldReady && Config.ShowMinimap && shouldShowMinimap && Game1.displayHUD) Minimap?.DrawMiniMap();

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
      if (isModMapOpen)
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
        if (Helper.Input.GetState(SButton.MouseLeft) == SButtonState.Held)
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

          // Make points more distinct
          //          Game1.spriteBatch.Draw(tex,
          //            new Rectangle((int)MouseUtil.BeginMousePosition.X, (int)MouseUtil.BeginMousePosition.Y, borderWidth, borderWidth),
          //            Rectangle.Empty, Color.White);
          //
          //          Game1.spriteBatch.Draw(tex,
          //            new Rectangle((int)MouseUtil.EndMousePosition.X, (int)MouseUtil.EndMousePosition.Y, borderWidth, borderWidth),
          //            Rectangle.Empty, Color.White);

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

  // Class for map markers
  public class CharacterMarker
  {
    public string Name { get; set; } // For any customized names; Npc.Name would be vanilla names
    public NPC Npc { get; set; }
    public Texture2D Marker { get; set; }
    public Vector2 MapLocation { get; set; }
    public Vector2 PrevMapLocation { get; set; }
    public string SyncedLocationName { get; set; }
    public string PrevLocationName { get; set; }
    public bool IsBirthday { get; set; }
    public bool HasQuest { get; set; }
    public bool IsOutdoors { get; set; }
    public bool IsHidden { get; set; }
    public int Layer { get; set; }
    public int DrawDelay { get; set; }
  }

  // Class for Location Vectors
  // Maps the tileX and tileY in a game location to the location on the map
  public class MapVector
  {
    public MapVector(int x, int y)
    {
      MapX = x;
      MapY = y;
    }

    public MapVector(int x, int y, int tileX, int tileY)
    {
      MapX = x;
      MapY = y;
      TileX = tileX;
      TileY = tileY;
    }

    public int TileX { get; set; } // tileX in a game location
    public int TileY { get; set; } // tileY in a game location
    public int MapX { get; set; } // Absolute position relative to map
    public int MapY { get; set; } // Absolute position relative to map
  }
}