using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using Netcode;
using NPCMapLocations;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Quests;

namespace LocationCompass
{
  /// <summary>The mod entry point.</summary>
  public class ModEntry : Mod
  {
    private const int MAX_PROXIMITY = 4800;

    private const bool DEBUG_MODE = false;
    private static IMonitor monitor;
    private static Texture2D pointer;
    private static ModData constants;
    private static List<Character> characters;
    private static SyncedLocationData syncedLocationData;
    private static Dictionary<string, LocationContext> locationContexts; // Mapping of locations to root locations
    private static Dictionary<string, List<Locator>> locators;
    private static Dictionary<string, LocatorScroller> activeWarpLocators; // Active indices of locators of doors
    private ModConfig config;
    private IModHelper helper;
    private bool showLocators;

    public override void Entry(IModHelper helper)
    {
      this.helper = helper;
      monitor = Monitor;
      config = helper.ReadConfig<ModConfig>();
      pointer =
        helper.Content.Load<Texture2D>(@"assets/locator.png", ContentSource.ModFolder); // Load pointer tex
      constants = this.helper.Data.ReadJsonFile<ModData>("constants.json") ?? new ModData();

      Helper.Events.GameLoop.SaveLoaded += GameLoop_SaveLoaded;
      Helper.Events.GameLoop.DayStarted += GameLoop_DayStarted;
      Helper.Events.World.LocationListChanged += World_LocationListChanged;
      Helper.Events.Multiplayer.ModMessageReceived += Multiplayer_ModMessageReceived;
      Helper.Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked;
      Helper.Events.Input.ButtonPressed += Input_ButtonPressed;
      Helper.Events.Input.ButtonReleased += Input_ButtonReleased;
      Helper.Events.Display.Rendered += Display_Rendered;
    }

    private void GameLoop_DayStarted(object sender, DayStartedEventArgs e)
    {
      characters = new List<Character>();

      foreach (var NPC in GetVillagers())
      {
        characters.Add(NPC);
      }

      UpdateLocators();
    }

    private void GetLocationContexts()
    {
      locationContexts = new Dictionary<string, LocationContext>();
      foreach (var location in Game1.locations)
      {
        // Get outdoor neighbors
        if (location.IsOutdoors)
        {
          if (!locationContexts.ContainsKey(location.Name))
            locationContexts.Add(location.Name, new LocationContext());

          foreach (var warp in location.warps)
          {
            if (warp == null || Game1.getLocationFromName(warp.TargetName) == null) continue;
            var warpLocation = Game1.getLocationFromName(warp.TargetName);

            if (warpLocation.IsOutdoors)
            {
              if (!locationContexts[location.Name].Neighbors.ContainsKey(warp.TargetName))
                locationContexts[location.Name].Neighbors.Add(warp.TargetName, new Vector2(warp.X, warp.Y));
            }
          }
        }
        // Get root locations from indoor locations
        else
          MapRootLocations(location, null, null, false, new Vector2(-1000, -1000));
      }

      foreach (var location in Game1.getFarm().buildings)
        MapRootLocations(location.indoors.Value, null, null, false, new Vector2(-1000, -1000));
    }

    private void Input_ButtonPressed(object sender, ButtonPressedEventArgs e)
    {
      if (!Context.IsWorldReady) return;

      // Handle toggle
      if (e.Button.ToString().Equals(config.ToggleKeyCode) && !Game1.paused && Game1.currentMinigame == null &&
          !Game1.eventUp && Game1.activeClickableMenu == null)
      {
        if (config.HoldToToggle)
        {
          // Hide HUD to show locators
          if (Game1.displayHUD)
            showLocators = true;
        }
        else
          showLocators = !showLocators;

        Game1.displayHUD = !showLocators;
      }

      // Configs
      if (activeWarpLocators != null)
      {
        // Handle scroller click
        if (e.Button.Equals(SButton.MouseRight) || e.Button.Equals(SButton.ControllerA))
          foreach (var doorLocator in activeWarpLocators)
          {
            if (doorLocator.Value.Characters.Count > 1)
              doorLocator.Value.ReceiveLeftClick();
          }

        if (showLocators)
        {
          if (e.Button.ToString() == config.SameLocationToggleKey)
            config.SameLocationOnly = !config.SameLocationOnly;
          else if (e.Button.ToString() == config.FarmersOnlyToggleKey)
            config.ShowFarmersOnly = !config.ShowFarmersOnly;
          else if (e.Button.ToString() == config.QuestsOnlyToggleKey)
            config.ShowQuestsAndBirthdaysOnly = !config.ShowQuestsAndBirthdaysOnly;
          else if (e.Button.ToString() == config.HorsesToggleKey)
            config.ShowHorses = !config.ShowHorses;

          helper.Data.WriteJsonFile("config.json", config);
        }
      }
    }

    private void World_LocationListChanged(object sender, LocationListChangedEventArgs e)
    {
      GetLocationContexts();
    }

    private void GameLoop_SaveLoaded(object sender, SaveLoadedEventArgs e)
    {
      activeWarpLocators = new Dictionary<string, LocatorScroller>();
      syncedLocationData = new SyncedLocationData();
      GetLocationContexts();

      // Log warning if host does not have mod installed
      if (Context.IsMultiplayer)
      {
        var hostHasMod = false;

        foreach (IMultiplayerPeer peer in this.Helper.Multiplayer.GetConnectedPlayers())
        {
          if (peer.GetMod("Bouhm.LocationCompass") != null && peer.IsHost)
          {
            hostHasMod = true;
            break;
          }
        }

        if (!hostHasMod)
          Monitor.Log("Since the server host does not have LocationCompass installed, NPC locations cannot be synced and updated.", LogLevel.Warn);
      }
    }

    private void Multiplayer_ModMessageReceived(object sender, ModMessageReceivedEventArgs e)
    {
      if (e.FromModID == ModManifest.UniqueID && e.Type == "SyncedLocationData")
        syncedLocationData = e.ReadAs<SyncedLocationData>();
    }

    // Recursively traverse warps of locations and map locations to root locations (outdoor locations)
    // Traverse in reverse (indoor to outdoor) because warps and doors are not complete subsets of Game1.locations 
    // Which means there will be some rooms left out unless all the locations are iterated
    private string MapRootLocations(GameLocation location, GameLocation prevLocation, string root, bool hasOutdoorWarp, Vector2 warpPosition)
    {
      // There can be multiple warps to the same location
      if (location == prevLocation) return root;

      var currLocationName = location.uniqueName.Value ?? location.Name;
      var prevLocationName = prevLocation?.uniqueName.Value ?? prevLocation?.Name;
     
      if (!locationContexts.ContainsKey(currLocationName))
        locationContexts.Add(currLocationName, new LocationContext());

      if (prevLocation != null && warpPosition.X >= 0)
      {
        locationContexts[prevLocationName].Warp = warpPosition;

        if (root != currLocationName)
          locationContexts[prevLocationName].Parent = currLocationName;
      }

      // Pass root location back recursively
      if (root != null)
      {
        locationContexts[currLocationName].Root = root;
        return root;
      }

      // Root location found, set as root and return
      if (location.IsOutdoors)
      {
        locationContexts[currLocationName].Type = "outdoors";
        locationContexts[currLocationName].Root = currLocationName;

        if (prevLocation != null)
        {
          if (locationContexts[currLocationName].Children == null)
            locationContexts[currLocationName].Children = new List<string> {prevLocationName};
          else if (!locationContexts[currLocationName].Children.Contains(prevLocationName))
            locationContexts[currLocationName].Children.Add(prevLocationName);
        }

        return currLocationName;
      }

      // Iterate warps of current location and traverse recursively
      foreach (var warp in location.warps)
      {
        // Avoid circular loop
        if (currLocationName == warp.TargetName || prevLocationName == warp.TargetName) continue;

        var warpLocation = Game1.getLocationFromName(warp.TargetName);

        // If one of the warps is a root location, current location is an indoor building 
        if (warpLocation.IsOutdoors)
        {

          hasOutdoorWarp = true;
        }

        // If all warps are indoors, then the current location is a room
        locationContexts[currLocationName].Type = hasOutdoorWarp ? "indoors" : "room";

        if (prevLocation != null)
        {
         locationContexts[prevLocationName].Parent = currLocationName;

          if (locationContexts[currLocationName].Children == null)
            locationContexts[currLocationName].Children = new List<string> {prevLocationName};
          else if (!locationContexts[currLocationName].Children.Contains(prevLocationName))
            locationContexts[currLocationName].Children.Add(prevLocationName);
        }

        root = MapRootLocations(warpLocation, location, root, hasOutdoorWarp,
          new Vector2(warp.TargetX, warp.TargetY));
        locationContexts[currLocationName].Root = root;

        return root;
      }

      return root;
    }

    // Finds the upper-most indoor location the player is in
    // Assuming there are warps to get there from the NPC's position
    public string GetTargetIndoor(string playerLoc, string npcLoc)
    {
      if (playerLoc.Contains("UndergroundMine") && npcLoc.Contains("UndergroundMine"))
      {
        return getMineName(playerLoc);
      }

      var target = locationContexts[npcLoc].Parent;

      if (target == null) return null;
      if (target == locationContexts[npcLoc].Root) return npcLoc;
      if (target == playerLoc) return target;
      return GetTargetIndoor(playerLoc, target);
    }

    // Get only relevant villagers for map
    private List<NPC> GetVillagers()
    {
      var villagers = new List<NPC>();
      var excludedNpcs = new List<string>
      {
        "Dwarf",
        "Mister Qi",
        "Bouncer",
        "Henchman",
        "Gunther",
        "Krobus"
      };

      foreach (var location in Game1.locations)
      {
        foreach (var npc in location.characters)
        {
          if (npc == null) continue;
          if (!villagers.Contains(npc) && !excludedNpcs.Contains(npc.Name) && (npc is Horse || npc.isVillager()))
            villagers.Add(npc);
        }
      }
      return villagers;
    }

    private void Input_ButtonReleased(object sender, ButtonReleasedEventArgs e)
    {
      if (!Context.IsWorldReady) return;

      if (e.Button.ToString().Equals(config.ToggleKeyCode) && !Game1.paused && Game1.currentMinigame == null && !Game1.eventUp && Game1.activeClickableMenu == null)
      {
        if (config.HoldToToggle)
        {
          showLocators = false;
          Game1.displayHUD = true;
        }
      }    
    }

    private void GameLoop_UpdateTicked(object sender, UpdateTickedEventArgs e)
    {
      if (!Context.IsWorldReady)
        return;

      // One-second tick
      if (e.IsOneSecond)
      {
        if (characters != null && Context.IsMultiplayer)
        {
          foreach (var farmer in Game1.getOnlineFarmers())
          {
            if (farmer == Game1.player) continue;
            if (!characters.Contains(farmer))
              characters.Add(farmer);
          }
        }

        if (Context.IsMainPlayer && Context.IsMultiplayer && syncedLocationData != null)
        {
          getSyncedLocationData();
          Helper.Multiplayer.SendMessage(syncedLocationData, "SyncedLocationData", modIDs: new[] { ModManifest.UniqueID });
        }
      }

      // Update tick
      if (!Game1.paused && showLocators && syncedLocationData != null)
      {
        if (Context.IsMainPlayer)
        {
          getSyncedLocationData();
        }
        UpdateLocators();
      }
    }

    private void getSyncedLocationData()
    {
      foreach (var npc in GetVillagers())
      {
        if (npc == null || npc.currentLocation == null) continue;
        if (syncedLocationData.SyncedLocations.TryGetValue(npc.Name, out var locationData))
        {
          syncedLocationData.SyncedLocations[npc.Name] = new LocationData(npc.currentLocation.uniqueName.Value ?? npc.currentLocation.Name, (int)npc.Position.X, (int)npc.Position.Y);
        }
        else
        {
          syncedLocationData.AddNpcLocation(npc.Name,
            new LocationData(npc.currentLocation.uniqueName.Value ?? npc.currentLocation.Name, (int)npc.Position.X,
              (int)npc.Position.Y));
        }
      }
    }

    private string getMineName(string locationName)
    {
      var mine = locationName.Substring("UndergroundMine".Length, locationName.Length - "UndergroundMine".Length);
      var mineName = locationName;

      if (Int32.TryParse(mine, out var mineLevel))
      {
        mineName = mineLevel > 120 ? "SkullCave" : "Mine";
      }

      return mineName;
    }

    private void UpdateLocators()
    {
      locators = new Dictionary<string, List<Locator>>();

      foreach (var character in characters)
      {
        if (character.currentLocation == null) continue;
        if (!config.ShowHorses && character is Horse || config.ShowFarmersOnly && (character is NPC && !(character is Horse))) continue;
        if (!syncedLocationData.SyncedLocations.TryGetValue(character.Name, out var npcLoc) && character is NPC) continue;
        if (character is NPC npc && config.ShowQuestsAndBirthdaysOnly) {
          var isBirthday = false;
          var hasQuest = false;
          // Check if gifted for birthday
          if (npc.isBirthday(Game1.currentSeason, Game1.dayOfMonth))
          {
            isBirthday = Game1.player.friendshipData.ContainsKey(npc.Name) && Game1.player.friendshipData[npc.Name].GiftsToday == 0;
          }

          // Check for daily quests
          foreach (var quest in Game1.player.questLog)
          {
            if (quest.accepted.Value && quest.dailyQuest.Value && !quest.completed.Value)
              switch (quest.questType.Value)
              {
                case 3:
                  hasQuest = ((ItemDeliveryQuest)quest).target.Value == npc.Name;
                  break;
                case 4:
                  hasQuest = ((SlayMonsterQuest)quest).target.Value == npc.Name;
                  break;
                case 7:
                  hasQuest = ((FishingQuest)quest).target.Value == npc.Name;
                  break;
                case 10:
                  hasQuest = ((ResourceCollectionQuest)quest).target.Value == npc.Name;
                  break;
              }
          }

          if (!isBirthday && !hasQuest)
            continue;
        }

        var playerLocName = Game1.player.currentLocation.uniqueName.Value ?? Game1.player.currentLocation.Name;
        var charLocName = character is Farmer
          ? character.currentLocation.uniqueName.Value ?? character.currentLocation.Name
          : npcLoc.LocationName;
        var isPlayerLocOutdoors = Game1.player.currentLocation.IsOutdoors;

        LocationContext playerLocCtx;
        LocationContext characterLocCtx;

        // Manually handle mines
        if (isPlayerLocOutdoors && charLocName.Contains("UndergroundMine")) {
            // If inside either mine, show characters as inside same general mine to player outside
            charLocName = getMineName(charLocName); 
        }
        if (playerLocName.Contains("UndergroundMine") && charLocName.Contains("UndergroundMine"))
        {
          // Leave mine levels distinguished in name if player inside mine
          locationContexts.TryGetValue(getMineName(playerLocName), out playerLocCtx);
          locationContexts.TryGetValue(getMineName(charLocName), out characterLocCtx);
        }
        else
        {
          if (!locationContexts.TryGetValue(playerLocName, out playerLocCtx)) continue;
          if (!locationContexts.TryGetValue(charLocName, out characterLocCtx)) continue;
        }

        if (config.SameLocationOnly && characterLocCtx.Root != playerLocCtx.Root)
          continue;

        var characterPos = new Vector2(-1000, 1000);
        var playerPos =
          new Vector2(Game1.player.position.X + Game1.player.FarmerSprite.SpriteWidth / 2 * Game1.pixelZoom,
            Game1.player.position.Y);
        var isWarp = false;
        var isOnScreen = false;
        var isOutdoors = false;
        var isHourse = character is Horse;

        Vector2 charPosition;
        int charSpriteHeight;

        if (character is Farmer)
        {
          charPosition = character.Position;
          var farmer = (Farmer) character;
          charSpriteHeight = farmer.FarmerSprite.SpriteHeight;

        } 
        else
        {
          charPosition = new Vector2(npcLoc.PositionX, npcLoc.PositionY);
          charSpriteHeight = character.Sprite.SpriteHeight;
        }

        // Player and character in same location
        if (playerLocName == charLocName)
        {
          // Don't include locator if character is visible on screen
          if (Utility.isOnScreen(charPosition, Game1.tileSize / 4))
            continue;

          characterPos = new Vector2(charPosition.X + charSpriteHeight / 2 * Game1.pixelZoom, charPosition.Y);
        }
        else
        {
          // Indoor locations
          // Intended behavior is for all characters in a building, including rooms within the building
          // to show up when the player is outside. So even if an character is not in the same location
          // ex. Maru in ScienceHouse, Sebastian in SebastianRoom, Sebastian will be placed in
          // ScienceHouse such that the player will know Sebastian is in that building.
          // Once the player is actually inside, Sebastian will be correctly placed in SebastianRoom.

          // Finds the upper-most indoor location that the player is in
          isWarp = true;
          var indoor = GetTargetIndoor(playerLocName, charLocName);
          if (config.SameLocationOnly)
          {
            if (indoor == null) continue;
            if (playerLocName != characterLocCtx.Root && playerLocName != indoor) continue;
          }
          charLocName = (isPlayerLocOutdoors || characterLocCtx.Type != "room") ? indoor : charLocName;


          // Neighboring outdoor warps
          if (!isPlayerLocOutdoors)
          {
            if (characterLocCtx.Root != playerLocCtx.Root || characterLocCtx.Parent == null)
              continue;

            // Doors that lead to connected rooms to character
            if (characterLocCtx.Parent == playerLocName)
            {
              characterPos = new Vector2(
                characterLocCtx.Warp.X * Game1.tileSize + Game1.tileSize / 2,
                characterLocCtx.Warp.Y * Game1.tileSize - Game1.tileSize * 3 / 2
              );
            }
            else
            {
              characterPos = new Vector2(
                locationContexts[characterLocCtx.Parent].Warp.X * Game1.tileSize + Game1.tileSize / 2,
                locationContexts[characterLocCtx.Parent].Warp.Y * Game1.tileSize - Game1.tileSize * 3 / 2
              );
            }
          }
          else
          {
            if (characterLocCtx.Root == playerLocCtx.Root)
            {
              // Point locators to the neighboring outdoor warps and
              // doors of buildings including nested rooms
              characterPos = new Vector2(
                locationContexts[indoor].Warp.X * Game1.tileSize + Game1.tileSize / 2,
                locationContexts[indoor].Warp.Y * Game1.tileSize - Game1.tileSize * 3 / 2
              );
            }
            else if (!config.SameLocationOnly)
            {
              // Warps to other outdoor locations
              Vector2 warpPos;
              isOutdoors = true;
              if ((characterLocCtx.Root != null && playerLocCtx.Neighbors.TryGetValue(characterLocCtx.Root, out warpPos)) || charLocName != null && playerLocCtx.Neighbors.TryGetValue(charLocName, out warpPos))
              {
                charLocName = characterLocCtx.Root;

                characterPos = new Vector2(
                  warpPos.X * Game1.tileSize + Game1.tileSize / 2,
                  warpPos.Y * Game1.tileSize - Game1.tileSize * 3 / 2
                );
              }
              else
                continue;
            }
          }

          // Add character to the list of locators inside a building
          if (!activeWarpLocators.ContainsKey(charLocName))
          {
            activeWarpLocators.Add(charLocName, new LocatorScroller()
            {
              Location = charLocName,
              Characters = new HashSet<string>() { character.Name },
              LocatorRect = new Rectangle((int)(characterPos.X - 32), (int)(characterPos.Y - 32),
                64, 64)
            });
          }
          else
            activeWarpLocators[charLocName].Characters.Add(character.Name);
        }
        
        isOnScreen = Utility.isOnScreen(characterPos, Game1.tileSize / 4);

        var locator = new Locator
        {
          Name = character.Name,
          Farmer = character is Farmer ? (Farmer)character : null,
          Marker = character is NPC ? character.Sprite.Texture : null,
          Proximity = GetDistance(playerPos, characterPos),
          IsWarp = isWarp,
          IsOutdoors = isOutdoors,
          IsOnScreen = isOnScreen,
          IsHorse = isHourse
        };

        var angle = GetPlayerToTargetAngle(playerPos, characterPos);
        var quadrant = GetViewportQuadrant(angle, playerPos);
        var locatorPos = GetLocatorPosition(angle, quadrant, playerPos, characterPos, isOnScreen, isWarp);

        locator.X = locatorPos.X;
        locator.Y = locatorPos.Y;
        locator.Angle = angle;
        locator.Quadrant = quadrant;

        if (locators.TryGetValue(charLocName, out var warpLocators))
        {
          if (!warpLocators.Contains(locator))
            warpLocators.Add(locator);
        }
        else
        {
          warpLocators = new List<Locator> {locator};
        }

        locators[charLocName] = warpLocators;
      }
    }

    // Get angle (in radians) to determine which quadrant the NPC is in
    // 0 rad will be at line from player position to (0, viewportHeight/2) relative to viewport
    private double GetPlayerToTargetAngle(Vector2 playerPos, Vector2 npcPos)
    {
      // Hypotenuse is the line from player to npc
      var opposite = npcPos.Y - playerPos.Y;
      var adjacent = npcPos.X - playerPos.X;
      var angle = Math.Atan2(opposite, adjacent) + MathHelper.Pi;

      return angle;
    }

    // Get quadrant to draw the locator on based on the angle
    //   _________
    //  | \  2  / |
    //  |   \ /   |
    //  | 1  |  3 |
    //  |   / \   |
    //  | /__4__\_|
    //
    private int GetViewportQuadrant(double angle, Vector2 playerPos)
    {

      // Top half of left quadrant
      if (angle < Math.Atan2(playerPos.Y - Game1.viewport.Y, playerPos.X - Game1.viewport.X))
        return 1;
      // Top quadrant
      if (angle < MathHelper.Pi - Math.Atan2(playerPos.Y - Game1.viewport.Y,
            Game1.viewport.X + Game1.viewport.Width - playerPos.X))
        return 2;
      // Right quadrant
      if (angle < MathHelper.Pi + Math.Atan2(Game1.viewport.Y + Game1.viewport.Height - playerPos.Y,
            Game1.viewport.X + Game1.viewport.Width - playerPos.X))
        return 3;
      // Bottom quadrant
      if (angle < MathHelper.TwoPi - Math.Atan2(Game1.viewport.Y + Game1.viewport.Height - playerPos.Y,
            playerPos.X - Game1.viewport.X))
        return 4;
      // Bottom half of left quadrant
      return 1;
    }

    private double GetDistance(Vector2 begin, Vector2 end)
    {
      return Math.Sqrt(
        Math.Pow(begin.X - end.X, 2) + Math.Pow(begin.Y - end.Y, 2)
      );
    }

    // Get position of location relative to viewport from
    // the viewport quadrant and positions of player/npc relative to map
    private static Vector2 GetLocatorPosition(double angle, int quadrant, Vector2 playerPos, Vector2 npcPos,
      bool isOnScreen = false, bool IsWarp = false)
    {
      var x = playerPos.X - Game1.viewport.X;
      var y = playerPos.Y - Game1.viewport.Y;

      if (IsWarp)
        if (Utility.isOnScreen(new Vector2(npcPos.X, npcPos.Y), Game1.tileSize / 4))
          return new Vector2(npcPos.X - Game1.viewport.X,
            npcPos.Y - Game1.viewport.Y);

      // Draw triangle such that the hypotenuse is
      // the line from player to the point of intersection of
      // the viewport quadrant and the line to the NPC
      switch (quadrant)
      {
        // Have to split each quadrant in half since player is not always centered in viewport
        case 1:
          // Bottom half
          if (angle > MathHelper.TwoPi - angle)
            y += (playerPos.X - Game1.viewport.X) * (float)Math.Tan(MathHelper.TwoPi - angle);
          // Top half
          else
            y += (playerPos.X - Game1.viewport.X) * (float)Math.Tan(MathHelper.TwoPi - angle);

          y = MathHelper.Clamp(y, Game1.tileSize * 3 / 4 + 2, Game1.viewport.Height - (Game1.tileSize * 3 / 4 + 2));
          return new Vector2(0 + Game1.tileSize * 3 / 4 + 2, y);
        case 2:
          // Left half
          if (angle < MathHelper.PiOver2)
            x -= (playerPos.Y - Game1.viewport.Y) * (float)Math.Tan(MathHelper.PiOver2 - angle);
          // Right half
          else
            x -= (playerPos.Y - Game1.viewport.Y) * (float)Math.Tan(MathHelper.PiOver2 - angle);

          x = MathHelper.Clamp(x, Game1.tileSize * 3 / 4 + 2, Game1.viewport.Width - (Game1.tileSize * 3 / 4 + 2));
          return new Vector2(x, 0 + Game1.tileSize * 3 / 4 + 2);
        case 3:
          // Top half
          if (angle < MathHelper.Pi)
            y -= (Game1.viewport.X + Game1.viewport.Width - playerPos.X) * (float)Math.Tan(MathHelper.Pi - angle);
          // Bottom half
          else
            y -= (Game1.viewport.X + Game1.viewport.Width - playerPos.X) * (float)Math.Tan(MathHelper.Pi - angle);

          y = MathHelper.Clamp(y, Game1.tileSize * 3 / 4 + 2, Game1.viewport.Height - (Game1.tileSize * 3 / 4 + 2));
          return new Vector2(Game1.viewport.Width - (Game1.tileSize * 3 / 4 + 2), y);
        case 4:
          // Right half
          if (angle < 3 * MathHelper.PiOver2)
            x += (Game1.viewport.Y + Game1.viewport.Height - playerPos.Y) *
                 (float)Math.Tan(3 * MathHelper.PiOver2 - angle);
          // Left half
          else
            x += (Game1.viewport.Y + Game1.viewport.Height - playerPos.Y) *
                 (float)Math.Tan(3 * MathHelper.PiOver2 - angle);

          x = MathHelper.Clamp(x, Game1.tileSize * 3 / 4 + 2, Game1.viewport.Width - (Game1.tileSize * 3 / 4 + 2));
          return new Vector2(x, Game1.viewport.Height - (Game1.tileSize * 3 / 4 + 2));
        default:
          return new Vector2(-1000, -1000);
      }
    }

    private void DrawLocators()
    {
      var sortedLocators = locators.OrderBy(x => !x.Value.FirstOrDefault().IsOutdoors);

      // Individual locators, onscreen or offscreen
      foreach (var locPair in sortedLocators)
      {
        int offsetX;
        int offsetY;

        // Show outdoor NPCs position in the current location
        if (!locPair.Value.FirstOrDefault().IsWarp)
        {
          foreach (var locator in locPair.Value)
          {
            var isHovering = new Rectangle((int)(locator.X - 32), (int)(locator.Y - 32), 64, 64).Contains(
              Game1.getMouseX(),
              Game1.getMouseY());

            offsetX = 24;
            offsetY = 15;

            // Change opacity based on distance from player
            var alphaLevel = isHovering ? 1f : locator.Proximity > MAX_PROXIMITY
              ? 0.35
              : 0.35 + (MAX_PROXIMITY - locator.Proximity) / MAX_PROXIMITY * 0.65;

            if (!constants.MarkerCrop.TryGetValue(locator.Name, out var cropY))
              cropY = 0;

            var npcSrcRect = locator.IsHorse ? new Rectangle(17, 104, 16, 14) : new Rectangle(0, cropY, 16, 15);

            // Pointer texture
            Game1.spriteBatch.Draw(
              pointer,
              new Vector2(locator.X, locator.Y),
              new Rectangle(0, 0, 64, 64),
              Color.White * (float) alphaLevel,
              (float) (locator.Angle - 3 * MathHelper.PiOver4),
              new Vector2(32, 32),
              1f,
              SpriteEffects.None,
              0.0f
            );

            // NPC head
            if (locator.Marker != null)
            {
              Game1.spriteBatch.Draw(
                locator.Marker,
                new Vector2(locator.X + offsetX, locator.Y + offsetY),
                npcSrcRect,
                Color.White * (float) alphaLevel,
                0f,
                new Vector2(16, 16),
                3f,
                SpriteEffects.None,
                1f
              );
            }
            else
            {
              locator.Farmer.FarmerRenderer.drawMiniPortrat(Game1.spriteBatch, new Vector2(locator.X + offsetX - 48, locator.Y + offsetY - 48), 0f, 3f, 0, locator.Farmer);
            }

            // Draw distance text
            if (locator.Proximity > 0)
            {
              var distanceString = $"{Math.Round(locator.Proximity / Game1.tileSize, 0)}";
              DrawText(Game1.tinyFont, distanceString, new Vector2(locator.X + offsetX - 24, locator.Y + offsetY - 4),
                Color.Black * (float) alphaLevel,
                new Vector2((int) Game1.tinyFont.MeasureString(distanceString).X / 2,
                  (float) (Game1.tileSize / 4 * 0.5)), 1f);
            }
          }
        }
        // Multiple indoor locators in a location, pointing to its door
        else
        {
          Locator locator = null;
          LocatorScroller activeLocator = null;

          if (activeWarpLocators != null && activeWarpLocators.TryGetValue(locPair.Key, out activeLocator))
          {
            locator = locPair.Value.ElementAtOrDefault(activeLocator.Index) ?? locPair.Value.FirstOrDefault();
            activeLocator.LocatorRect = new Rectangle((int)(locator.X - 32), (int)(locator.Y - 32), 64, 64);
          }
          else
            locator = locPair.Value.FirstOrDefault();

          var isHovering = new Rectangle((int)(locator.X - 32), (int)(locator.Y - 32), 64, 64).Contains(
            Game1.getMouseX(),
            Game1.getMouseY());

          offsetX = 24;
          offsetY = 12;

          // Adjust for offsets used to create padding around edges of screen
          if (!locator.IsOnScreen)
            switch (locator.Quadrant)
            {
              case 1:
                offsetY += 2;
                break;
              case 2:
                offsetX -= 0;
                offsetY += 2;
                break;
              case 3:
                offsetY -= 1;
                break;
              case 4:
                break;
            }

          // Change opacity based on distance from player
          var alphaLevel = isHovering ? 1f : (locator.IsOutdoors ? locator.Proximity > MAX_PROXIMITY
            ? 0.3
            : 0.3 + (MAX_PROXIMITY - locator.Proximity) / MAX_PROXIMITY * 0.7 : locator.Proximity > MAX_PROXIMITY
              ? 0.35
              : 0.35 + (MAX_PROXIMITY - locator.Proximity) / MAX_PROXIMITY * 0.65);

          // Make locators point down at the door
          if (locator.IsOnScreen)
          {
            locator.Angle = 3 * MathHelper.PiOver2;
            locator.Proximity = 0;
          }

          if (!constants.MarkerCrop.TryGetValue(locator.Name, out var cropY))
            cropY = 0;

          var compassSrcRect = locator.IsOutdoors ? new Rectangle(64, 0, 64, 64) : new Rectangle(0, 0, 64, 64); // Different locator color for neighboring outdoor locations
          var npcSrcRect = locator.IsHorse ? new Rectangle(17, 104, 16, 14) : new Rectangle(0, cropY, 16, 15);

          // Pointer texture
          Game1.spriteBatch.Draw(
            pointer,
            new Vector2(locator.X, locator.Y),
            compassSrcRect,
            Color.White * (float) alphaLevel,
            (float) (locator.Angle - 3 * MathHelper.PiOver4),
            new Vector2(32, 32),
            1f,
            SpriteEffects.None,
            0.0f
          );

          // NPC head
          if (locator.Marker != null)
          {
            Game1.spriteBatch.Draw(
              locator.Marker,
              new Vector2(locator.X + offsetX, locator.Y + offsetY),
              npcSrcRect,
              Color.White * (float) alphaLevel,
              0f,
              new Vector2(16, 16),
              3f,
              SpriteEffects.None,
              1f
            );
          }
          else
          {
            locator.Farmer.FarmerRenderer.drawMiniPortrat(Game1.spriteBatch, new Vector2(locator.X + offsetX - 48, locator.Y + offsetY - 48), 0f, 3f, 0, locator.Farmer);
          }

          if (locator.IsOnScreen || isHovering)
          {
            if (locPair.Value.Count > 1)
            {
              // Draw NPC count
              var countString = $"{locPair.Value.Count}";
              var headOffset = locPair.Value.Count > 9 ? 37 : 31;

              // head icon
              Game1.spriteBatch.Draw(
                pointer,
                new Vector2(locator.X + offsetX - headOffset, locator.Y + offsetY),
                new Rectangle(128, 0, 7, 9),
                Color.White * (float)alphaLevel, 0f, Vector2.Zero,
                1f,
                SpriteEffects.None,
                1f
              );

              DrawText(Game1.tinyFont, countString, new Vector2(locator.X + offsetX - 31, locator.Y + offsetY),
                Color.Black * (float)alphaLevel,
                new Vector2((int)(Game1.tinyFont.MeasureString(countString).X - 24) / 2,
                  (float)(Game1.tileSize / 8) + 3), 1f);
            }
          }

          // Draw distance text
          else if (locator.Proximity > 0)
          {
            var distanceString = $"{Math.Round(locator.Proximity / Game1.tileSize, 0)}";
            DrawText(Game1.tinyFont, distanceString, new Vector2(locator.X + offsetX - 24, locator.Y + offsetY - 4),
              Color.Black * (float) alphaLevel,
              new Vector2((int) Game1.tinyFont.MeasureString(distanceString).X / 2,
                (float) (Game1.tileSize / 4 * 0.5)), 1f);
          
          }

          if (isHovering)
          {
            if (locPair.Value.Count > 1)
            {
              // Change mouse cursor on hover
              Game1.mouseCursor = -1;
              Game1.mouseCursorTransparency = 1f;
              Game1.spriteBatch.Draw(Game1.mouseCursors, new Vector2((float)Game1.getOldMouseX(), (float)Game1.getOldMouseY()), new Rectangle?(Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 44, 16, 16)), Color.White, 0f, Vector2.Zero, ((float)Game1.pixelZoom + Game1.dialogueButtonScale / 150f), SpriteEffects.None, 1f);
            }
            else
            {
              Game1.spriteBatch.Draw(Game1.mouseCursors, new Vector2((float)Game1.getOldMouseX(), (float)Game1.getOldMouseY()), new Rectangle(0, 0, 8, 10), Color.White, 0f, Vector2.Zero, ((float)Game1.pixelZoom + Game1.dialogueButtonScale / 150f), SpriteEffects.None, 1f);
            }
          }
        }
      }
    }

    // Draw line relative to viewport
    private void DrawLine(SpriteBatch b, Vector2 begin, Vector2 end, Texture2D tex)
    {
      var r = new Rectangle((int) begin.X, (int) begin.Y, (int) (end - begin).Length() + 2, 2);
      var v = Vector2.Normalize(begin - end);
      var angle = (float) Math.Acos(Vector2.Dot(v, -Vector2.UnitX));
      if (begin.Y > end.Y) angle = MathHelper.TwoPi - angle;
      b.Draw(tex, r, null, Color.White, angle, Vector2.Zero, SpriteEffects.None, 0);
    }

    // Draw outlined text
    private static void DrawText(SpriteFont font, string text, Vector2 pos, Color? color = null, Vector2? origin = null,
      float scale = 1f)
    {
      //Game1.spriteBatch.DrawString(font, text, pos + new Vector2(1, 1), Color.Black, 0f, origin ?? Vector2.Zero, scale, SpriteEffects.None, 0f);
      //Game1.spriteBatch.DrawString(font, text, pos + new Vector2(-1, 1), Color.Black, 0f, origin ?? Vector2.Zero, scale, SpriteEffects.None, 0f);
      //Game1.spriteBatch.DrawString(font, text, pos + new Vector2(1, -1), Color.Black, 0f, origin ?? Vector2.Zero, scale, SpriteEffects.None, 0f);
      //Game1.spriteBatch.DrawString(font, text, pos + new Vector2(-1, -1), Color.Black, 0f, origin ?? Vector2.Zero, scale, SpriteEffects.None, 0f);
      Game1.spriteBatch.DrawString(font ?? Game1.tinyFont, text, pos, color ?? Color.White, 0f, origin ?? Vector2.Zero,
        scale,
        SpriteEffects.None, 0f);
    }

    private void Display_Rendered(object sender, RenderedEventArgs e)
    {
      //if (!Context.IsWorldReady || locators == null) return;

      if (showLocators && Game1.activeClickableMenu == null)
        DrawLocators();

      if (!DEBUG_MODE)
        return;

      foreach (var locPair in locators)
      {
        foreach (var locator in locPair.Value)
        {
          if (!locator.IsOnScreen)
            continue;

          var npc = Game1.getCharacterFromName(locator.Name);
          var viewportX = Game1.player.position.X + Game1.pixelZoom * Game1.player.Sprite.SpriteWidth / 2 -
                          Game1.viewport.X;
          var viewportY = Game1.player.position.Y - Game1.viewport.Y;
          var npcViewportX = npc.position.X + Game1.pixelZoom * npc.Sprite.SpriteWidth / 2 - Game1.viewport.X;
          var npcViewportY = npc.position.Y - Game1.viewport.Y;

          // Draw NPC sprite noodle connecting center of screen to NPC for debugging
          DrawLine(Game1.spriteBatch, new Vector2(viewportX, viewportY), new Vector2(npcViewportX, npcViewportY),
            npc.Sprite.Texture);
        }
      }
    }
  }

  // Class for locators AKA the 'needles' of the compass
  internal class Locator
  {
    public string Name { get; set; }
    public Farmer Farmer { get; set; }
    public Texture2D Marker { get; set; }
    public double Proximity { get; set; }
    public int Quadrant { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public double Angle { get; set; }
    public bool IsHorse { get; set; }
    public bool IsWarp { get; set; }
    public bool IsOutdoors { get; set; }
    public bool IsOnScreen { get; set; }
  }

  // Object for keeping track of with NPC to show when
  // there are multiple NPCs in a building
  internal class LocatorScroller
  {
    public string Location { get; set; }
    public HashSet<string> Characters { get; set; }
    public int Index { get; set; }
    public Rectangle LocatorRect { get; set; }

    public void ReceiveLeftClick()
    {
      if (LocatorRect.Contains(Game1.getMouseX(), Game1.getMouseY()))
      {
        Index++;
        Game1.playSound("drumkit6");

        if (Index > Characters.Count - 1)
          Index = 0;
      }
    }
  }

  // Used for determining if an NPC is in the same root location
  // as the player as well as indoor location a room belongs to
  internal class LocationContext
  {
    public string Type { get; set; } // outdoors, indoors, or room
    public string Root { get; set; } // Top-most outdoor location
    public string Parent { get; set; } // Level above
    public Dictionary<string, Vector2> Neighbors { get; set; } = new Dictionary<string, Vector2>(); // Connected outdoor locations
    public List<string> Children { get; set; } // Levels below
    public Vector2 Warp { get; set; } // Position of warp
  }
}