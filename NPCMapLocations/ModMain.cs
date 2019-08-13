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

namespace NPCMapLocations
{
  public class ModMain : Mod, IAssetLoader
  {
    public static ModConfig Config;
    public static IModHelper Helper;
    public static SButton HeldKey;
    public static bool IsSVE;

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
    private string Season;
    private ModCustomizations Customizations;

    // Debugging
    private static bool DEBUG_MODE;
    private static Dictionary<string, KeyValuePair<string, Vector2>> FarmBuildings;
    private static Vector2 _tileLower;
    private static Vector2 _tileUpper;
    private static List<string> alertFlags;

    // Replace game map with modified map
    public bool CanLoad<T>(IAssetInfo asset)
    {
      return asset.AssetNameEquals(@"LooseSprites\Map") && Customizations != null;
    }

    public T Load<T>(IAssetInfo asset)
    {
      T map;

      if (Season == null)
      {
        Monitor.Log("Unable to get current season. Defaulted to spring.", LogLevel.Debug);
        Season = "spring";
      }

      // Replace map page
      string filename = $"{this.Season}_map.png";
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
      Config = Helper.Data.ReadJsonFile<ModConfig>($"config/default.json") ?? new ModConfig();
      // Load farm buildings
      try
      {
        BuildingMarkers = Helper.Content.Load<Texture2D>(@"assets/buildings.png");
      }
      catch
      {
        BuildingMarkers = null;
      }

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
      Config = Helper.Data.ReadJsonFile<ModConfig>($"config/{Constants.SaveFolderName}.json") ?? Config;
      IsSVE = Helper.ModRegistry.IsLoaded("FlashShifter.StardewValleyExpandedCP");
      Customizations = new ModCustomizations(Monitor)
      {
        LocationTextures = File.Exists(@"assets/customLocations.png") ? Helper.Content.Load<Texture2D>(@"assets/customLocations.png") : null
      };
      Season = Config.UseSeasonalMaps ? Game1.currentSeason : "spring";
      Helper.Content.InvalidateCache("LooseSprites/Map");

      DEBUG_MODE = Config.DEBUG_MODE;
      shouldShowMinimap = Config.ShowMinimap;

      LocationUtil.LocationContexts = new Dictionary<string, LocationContext>();
      foreach (var location in Game1.locations) { 
        LocationUtil.MapRootLocations(location, null, null, false, Vector2.Zero);
      }

      ConditionalNpcs = new Dictionary<string, bool>
      {
        {"Kent", false},
        {"Marlon", false},
        {"Merchant", false},
        {"Sandy", false},
        {"Wizard", false}
      };

      MapVectors = ModConstants.MapVectors;

      foreach (var locVectors in Customizations.MapVectors)
      {
        if (MapVectors.TryGetValue(locVectors.Key, out var mapVectors))
          MapVectors[locVectors.Key] = locVectors.Value;
        else
          MapVectors.Add(locVectors.Key, locVectors.Value);
      }

      UpdateFarmBuildingLocs();
      alertFlags = new List<string>();

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

    // Get only relevant villagers for map
    private List<NPC> GetVillagers()
    {
      var villagers = new List<NPC>();

      foreach (var location in Game1.locations)
      {
        foreach (var npc in location.characters)
        {
          if (npc == null) continue;
          if (!villagers.Contains(npc) && !ModConstants.ExcludedVillagers.Contains(npc.Name) && npc.isVillager())
            villagers.Add(npc);
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
        var locVector = LocationToMap("Greenhouse", -1, -1, Customizations.MapVectors);
        locVector.X -= 5 / 2 * 3;
        locVector.Y -= 7 / 2 * 3;
        FarmBuildings["Greenhouse"] = new KeyValuePair<string, Vector2>("Greenhouse", locVector);
      }
    }

    private void World_BuildingListChanged(object sender, BuildingListChangedEventArgs e)
    {
      if (e.Location.IsFarm)
        UpdateFarmBuildingLocs();

      LocationUtil.LocationContexts = new Dictionary<string, LocationContext>();
      foreach (var location in Game1.locations)
        LocationUtil.MapRootLocations(location, null, null, false, Vector2.Zero);
    }

    // Handle opening mod menu and changing tooltip options
    private void Input_ButtonPressed(object sender, ButtonPressedEventArgs e)
    {
      if (!Context.IsWorldReady) return;

      // Minimap dragging
      if (Config.ShowMinimap && Minimap != null)
      {
        if (e.Button.ToString().Equals(Config.MinimapDragKey))
        {
          HeldKey = e.Button;
        }
        else if (HeldKey.ToString().Equals(Config.MinimapDragKey) &&
                 (e.Button == SButton.MouseLeft || e.Button == SButton.ControllerA) &&
                 Game1.activeClickableMenu == null)
        {
          Minimap.HandleMouseDown();
          if (Minimap.isBeingDragged)
            Helper.Input.Suppress(e.Button);
        }
      }

      // Minimap toggle
      if (e.Button.ToString().Equals(Config.MinimapToggleKey) && Game1.activeClickableMenu == null)
      {
        Config.ShowMinimap = !Config.ShowMinimap;
        Helper.Data.WriteJsonFile($"config/{Constants.SaveFolderName}.json", Config);
      }

      // ModMenu
      if (Game1.activeClickableMenu is GameMenu)
        HandleInput((GameMenu) Game1.activeClickableMenu, e.Button);

      if (DEBUG_MODE && e.Button == SButton.LeftAlt) HeldKey = e.Button;

      if (DEBUG_MODE && !Context.IsMultiplayer && HeldKey == SButton.LeftAlt && e.Button.Equals(SButton.MouseRight))
        Game1.player.setTileLocation(Game1.currentCursorTile);
    }

    private void Input_ButtonReleased(object sender, ButtonReleasedEventArgs e)
    {
      if (!Context.IsWorldReady) return;
      if (HeldKey.ToString().Equals(Config.MinimapDragKey) && e.Button.ToString().Equals(Config.MinimapDragKey) ||
          HeldKey == SButton.LeftAlt && e.Button != SButton.MouseRight)
        HeldKey = SButton.None;

      if (Minimap != null && Context.IsWorldReady && e.Button == SButton.MouseLeft)
      {
        if (Game1.activeClickableMenu == null)
          Minimap.HandleMouseRelease();
        else if (Game1.activeClickableMenu is ModMenu)
          Minimap.Resize();
      }
    }

    // Handle keyboard/controller inputs
    private void HandleInput(GameMenu menu, SButton input)
    {
      if (menu.currentTab != GameMenu.mapTab) return;
      if (input.ToString().Equals(Config.MenuKey) || input is SButton.ControllerY)
        Game1.activeClickableMenu = new ModMenu(
          ConditionalNpcs,
          Customizations
        );

      if (input.ToString().Equals(Config.TooltipKey) || input is SButton.RightShoulder)
        ChangeTooltipConfig();
      else if (input.ToString().Equals(Config.TooltipKey) || input is SButton.LeftShoulder) ChangeTooltipConfig(false);
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
      var npcEntries = new Dictionary<string, bool>(ConditionalNpcs);

      // Characters that are not always available, for avoiding spoilers
      foreach (var npc in npcEntries)
      {
        var name = npc.Key;
        switch (name)
        {
          case "Kent":
            ConditionalNpcs[name] = Game1.year >= 2;
            break;
          case "Marlon":
            ConditionalNpcs[name] = Game1.player.eventsSeen.Contains(100162);
            break;
          case "Merchant":
            ConditionalNpcs[name] = ((Forest) Game1.getLocationFromName("Forest")).travelingMerchantDay;
            break;
          case "Sandy":
            ConditionalNpcs[name] = Game1.player.mailReceived.Contains("ccVault");
            break;
          case "Wizard":
            ConditionalNpcs[name] = Game1.player.eventsSeen.Contains(112);
            break;
        }
      }

      ResetMarkers(GetVillagers());
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
      return Config.ShowMinimap && Config.MinimapBlacklist.Any(loc => loc != "Farm" && location.StartsWith(loc) || loc == "Farm" && location == "Farm") ||
               ((Config.MinimapBlacklist.Contains("Mine") || Config.MinimapBlacklist.Contains("UndergroundMine")) && location.Contains("Mine"));
    }

    private void ResetMarkers(List<NPC> villagers)
    {
      NpcMarkers = new HashSet<CharacterMarker>();
      foreach (var npc in villagers)
      {
        // Handle case where Kent appears even though he shouldn't
        if (npc.Name.Equals("Kent") && !ConditionalNpcs["Kent"]) continue;
        if (!Customizations.Names.TryGetValue(npc.Name, out var npcName))
        {
          npcName = npc.displayName ?? npc.Name;
          Customizations.Names.Add(npc.Name, npcName);
        }

        var npcMarker = new CharacterMarker
        {
          Npc = npc,
          Name = npcName,
          Marker = npc.Sprite.Texture,
          IsBirthday = npc.isBirthday(Game1.currentSeason, Game1.dayOfMonth)
        };
        NpcMarkers.Add(npcMarker);
      }

      
      if (Context.IsMultiplayer)
        FarmerMarkers = new Dictionary<long, CharacterMarker>();
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
            message.AddNpcLocation(npc.Name,
              new LocationData(npc.currentLocation.uniqueName.Value ?? npc.currentLocation.Name, npc.getTileX(),
                npc.getTileY()));
          }

          Helper.Multiplayer.SendMessage(message, "SyncedLocationData", modIDs: new string[] { ModManifest.UniqueID });
        }
       
        // Check season change (for when it's changed via console)
        if (Config.UseSeasonalMaps && Season != Game1.currentSeason && Game1.currentSeason != null)
        {
          Season = Game1.currentSeason;

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
      if (Game1.activeClickableMenu == null || !(Game1.activeClickableMenu is GameMenu gameMenu))
      {
        isModMapOpen = false;
        return;
      }

      hasOpenedMap =
        gameMenu.currentTab == GameMenu.mapTab; // When map accessed by switching GameMenu tab or pressing M
      isModMapOpen = hasOpenedMap ? isModMapOpen : hasOpenedMap; // When vanilla MapPage is replaced by ModMap

      if (hasOpenedMap && !isModMapOpen) // Only run once on map open
        OpenModMap(gameMenu);
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
              var mapLocation = LocationToMap(npcLoc.LocationName, npcLoc.TileX, npcLoc.TileY, Customizations.MapVectors);
              marker.MapLocation = new Vector2(mapLocation.X - 16, mapLocation.Y - 15);
            }
          }
          else
          {
            marker.MapLocation = Vector2.Zero;
          }
        }
      }
    }

    private void OpenModMap(GameMenu gameMenu)
    {
      isModMapOpen = true;
      UpdateNpcs(true);
      var pages = Helper.Reflection
        .GetField<List<IClickableMenu>>(gameMenu, "pages").GetValue();

      var mapTab = GameMenu.mapTab;

      // Find index of MapPage since it's a different value for SDV mobile
      foreach (var page in pages)
      {
        if (page is MapPage)
        {
          mapTab = pages.IndexOf(page);
        }
      }

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
          // Handle null locations at beginning of new day
          if (npc.currentLocation == null)
            locationName = npc.DefaultMap;
          else
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
        if (!Config.NpcBlacklist.Contains(npcMarker.Npc.Name) && (Config.ShowHiddenVillagers || !npcMarker.IsHidden))
        {
          // Check if gifted for birthday
          if (npcMarker.IsBirthday)
            npcMarker.IsBirthday = Game1.player.friendshipData.ContainsKey(npcMarker.Npc.Name) &&
                                   Game1.player.friendshipData[npcMarker.Npc.Name].GiftsToday == 0;

          // Check for daily quests
          foreach (var quest in Game1.player.questLog)
            if (quest.accepted.Value && quest.dailyQuest.Value && !quest.completed.Value)
              switch (quest.questType.Value)
              {
                case 3:
                  npcMarker.HasQuest = ((ItemDeliveryQuest) quest).target.Value == npcMarker.Npc.Name;
                  break;
                case 4:
                  npcMarker.HasQuest = ((SlayMonsterQuest) quest).target.Value == npcMarker.Npc.Name;
                  break;
                case 7:
                  npcMarker.HasQuest = ((FishingQuest) quest).target.Value == npcMarker.Npc.Name;
                  break;
                case 10:
                  npcMarker.HasQuest = ((ResourceCollectionQuest) quest).target.Value == npcMarker.Npc.Name;
                  break;
              }
            else
              npcMarker.HasQuest = false;

          // Establish draw order, higher number infront
          // Layers 4 - 7: Outdoor NPCs in order of hidden, hidden w/ quest/birthday, standard, standard w/ quest/birthday
          // Layers 0 - 3: Indoor NPCs in order of hidden, hidden w/ quest/birthday, standard, standard w/ quest/birthday
          npcMarker.Layer = npcMarker.IsOutdoors ? 6 : 2;
          if (npcMarker.IsHidden) npcMarker.Layer -= 2;

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
            var x = (int) LocationToMap(locationName, npc.getTileX(), npc.getTileY(), Customizations.MapVectors).X - 16;
            var y = (int) LocationToMap(locationName, npc.getTileX(), npc.getTileY(), Customizations.MapVectors).Y - 15;
            npcMarker.MapLocation = new Vector2(x, y);
          }
        }
        else
        {
          // Set no location so they don't get drawn
          npcMarker.MapLocation = Vector2.Zero;
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

        if ((!locationName.Contains("Cabin") && !locationName.Contains("UndergroundMine")) &&
            !MapVectors.TryGetValue(locationName, out var loc))
        {
          if (!alertFlags.Contains("UnknownLocation:" + locationName))
          {
            Monitor.Log($"Unknown location: {locationName}.", LogLevel.Debug);
            alertFlags.Add("UnknownLocation:" + locationName);
          }
        }

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
    // Requires MapModConstants and modified map page in ./assets
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

      MapVector[] locVectors = null;
      bool locationNotFound = false;

      if (locationName == "Farm")
      {
        // Handle different farm types for custom vectors
        var farms = new string[5] { "Farm_Default", "Farm_Riverland", "Farm_Forest", "Farm_Hills", "Farm_Wilderness" };
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

      if (locVectors == null || locationNotFound) return Vector2.Zero;
      
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

        x = (int) MathHelper.Clamp((int)(lower.MapX + (tileX - lower.TileX) / (double) (upper.TileX - lower.TileX) * (upper.MapX - lower.MapX)), 0, 1200);
        y = (int) MathHelper.Clamp((int)(lower.MapY + (tileY - lower.TileY) / (double) (upper.TileY - lower.TileY) * (upper.MapY - lower.MapY)), 0, 720);

        if (DEBUG_MODE && isPlayer)
        {
          _tileUpper = new Vector2(upper.TileX, upper.TileY);
          _tileLower = new Vector2(lower.TileX, lower.TileY);
        }
      }

      return new Vector2(x, y);
    }

    private void Display_WindowResized(object sender, WindowResizedEventArgs e)
    {
      if (!Context.IsWorldReady) return;

      UpdateMarkers(true);
      UpdateFarmBuildingLocs();
      Minimap?.CheckOffsetForMap();
    }

    private void Display_MenuChanged(object sender, MenuChangedEventArgs e)
    {
      if (!Context.IsWorldReady) return;

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
      if (DEBUG_MODE && HeldKey == SButton.LeftAlt)
        Game1.spriteBatch.Draw(Game1.mouseCursors,
          new Vector2(
            Game1.tileSize * (int) Math.Floor(Game1.currentCursorTile.X) - Game1.viewport.X,
            Game1.tileSize * (int) Math.Floor(Game1.currentCursorTile.Y) - Game1.viewport.Y),
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
      string locationText =
        $"{locationName} ({Game1.currentLocation.Map.DisplayWidth / Game1.tileSize} x {Game1.currentLocation.Map.DisplayHeight / Game1.tileSize})";

      // Black background for legible text
      Game1.spriteBatch.Draw(Game1.shadowTexture, new Rectangle(0, 0, 200 + (int)Game1.smallFont
                                                                        .MeasureString(locationText).X, 200), new Rectangle(3, 0, 1, 1),
        Color.Black);

      // Show map location and tile positions
      DrawText(
        locationText,
        new Vector2(Game1.tileSize / 4, Game1.tileSize / 4), Color.White);
      DrawText(
        "Position: (" + Math.Ceiling(Game1.player.Position.X / Game1.tileSize) + ", " +
        Math.Ceiling(Game1.player.Position.Y / Game1.tileSize) + ")",
        new Vector2(Game1.tileSize / 4, Game1.tileSize * 3 / 4 + 8),
        _tileUpper != Vector2.Zero && (Game1.player.Position.X / Game1.tileSize > _tileUpper.X ||
                                       Game1.player.Position.Y / Game1.tileSize > _tileUpper.Y)
          ? Color.Red
          : Color.White);

      var currMenu = Game1.activeClickableMenu is GameMenu ? (GameMenu) Game1.activeClickableMenu : null;

      // Show lower & upper bound tiles used for calculations
      if (!(_tileLower == Vector2.Zero && _tileUpper == Vector2.Zero))
      {
        if (isModMapOpen || Config.ShowMinimap)
        {
          DrawText("Lower bound: (" + _tileLower.X + ", " + _tileLower.Y + ")",
            new Vector2(Game1.tileSize / 4, Game1.tileSize * 5 / 4 + 8 * 2));
          DrawText("Upper bound: (" + _tileUpper.X + ", " + _tileUpper.Y + ")",
            new Vector2(Game1.tileSize / 4, Game1.tileSize * 7 / 4 + 8 * 3));
        }
        else
        {
          DrawText("Lower bound: (" + _tileLower.X + ", " + _tileLower.Y + ")",
            new Vector2(Game1.tileSize / 4, Game1.tileSize * 5 / 4 + 8 * 2), Color.DimGray);
          DrawText("Upper bound: (" + _tileUpper.X + ", " + _tileUpper.Y + ")",
            new Vector2(Game1.tileSize / 4, Game1.tileSize * 7 / 4 + 8 * 3), Color.DimGray);
        }
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