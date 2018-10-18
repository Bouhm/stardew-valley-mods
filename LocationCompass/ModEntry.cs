using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

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
      constants = this.helper.ReadJsonFile<ModData>("constants.json") ?? new ModData();
      SaveEvents.AfterLoad += SaveEvents_AfterLoad;
      LocationEvents.LocationsChanged += LocationEvents_LocationsChanged;
      GameEvents.UpdateTick += GameEvents_UpdateTick;
      InputEvents.ButtonPressed += InputEvents_ButtonPressed;
      GraphicsEvents.OnPreRenderHudEvent += GraphicsEvents_OnPreRenderHudEvent;
      GraphicsEvents.OnPostRenderEvent += GraphicsEvents_OnPostRenderEvent;
      ControlEvents.KeyPressed += ControlEvents_KeyPressed;
      ControlEvents.KeyReleased += ControlEvents_KeyReleased;
    }

    private void InputEvents_ButtonPressed(object sender, EventArgsInput e)
    {
      if (!Context.IsWorldReady || activeWarpLocators == null) return;
      var a = locators;
      var b = activeWarpLocators;
      if (e.Button.Equals(SButton.MouseLeft) || e.Button.Equals(SButton.ControllerA))
        foreach (var doorLocator in activeWarpLocators)
        {
          if (doorLocator.Value.Characters.Count > 1)
            doorLocator.Value.ReceiveLeftClick();
        }
    }

    private void LocationEvents_LocationsChanged(object sender, EventArgsLocationsChanged e)
    {
      locationContexts = new Dictionary<string, LocationContext>();
      foreach (var location in Game1.locations)
        if (!location.IsOutdoors)
          MapRootLocations(location, null, null, false, new Vector2(-1000, -1000));
    }

    private void SaveEvents_AfterLoad(object sender, EventArgs e)
    {
      locators = new Dictionary<string, List<Locator>>();
      activeWarpLocators = new Dictionary<string, LocatorScroller>();

      locationContexts = new Dictionary<string, LocationContext>();
      foreach (var location in Game1.locations)
        if (!location.IsOutdoors)
          MapRootLocations(location, null, null, false, new Vector2(-1000, -1000));
    }


    // Recursively traverse warps of locations and map locations to root locations (outdoor locations)
    // Traverse in reverse (indoor to outdoor) because warps and doors are not complete subsets of Game1.locations 
    // Which means there will be some rooms left out unless all the locations are iterated
    private string MapRootLocations(GameLocation location, GameLocation prevLocation, string root, bool hasOutdoorWarp,
      Vector2 warpPosition)
    {
      // There can be multiple warps to the same location
      if (location == prevLocation) return root;

      if (!locationContexts.ContainsKey(location.Name))
        locationContexts.Add(location.Name, new LocationContext());

      if (prevLocation != null && warpPosition.X >= 0)
      {
        locationContexts[prevLocation.Name].Warp = warpPosition;

        if (root != location.Name)
          locationContexts[prevLocation.Name].Parent = location.Name;
      }

      // Pass root location back recursively
      if (root != null)
      {
        locationContexts[location.Name].Root = root;
        return root;
      }

      // Root location found, set as root and return
      if (location.isOutdoors)
      {
        locationContexts[location.Name].Type = "outdoors";
        locationContexts[location.Name].Root = location.Name;

        if (prevLocation != null)
        {
          if (locationContexts[location.Name].Children == null)
            locationContexts[location.Name].Children = new List<string> {prevLocation.Name};
          else if (!locationContexts[location.Name].Children.Contains(prevLocation.Name))
            locationContexts[location.Name].Children.Add(prevLocation.Name);
        }

        return location.Name;
      }

      // Iterate warps of current location and traverse recursively
      foreach (var warp in location.warps)
      {
        // If one of the warps is a root location, current location is an indoor building 
        if (Game1.getLocationFromName(warp.TargetName).IsOutdoors)
          hasOutdoorWarp = true;

        // If all warps are indoors, then the current location is a room
        locationContexts[location.Name].Type = hasOutdoorWarp ? "indoors" : "room";

        if (prevLocation != null)
        {
          locationContexts[prevLocation.Name].Parent = location.Name;

          if (locationContexts[location.Name].Children == null)
            locationContexts[location.Name].Children = new List<string> {prevLocation.Name};
          else if (!locationContexts[location.Name].Children.Contains(prevLocation.Name))
            locationContexts[location.Name].Children.Add(prevLocation.Name);
        }

        root = MapRootLocations(Game1.getLocationFromName(warp.TargetName), location, root, hasOutdoorWarp,
          new Vector2(warp.TargetX, warp.TargetY));
        locationContexts[location.Name].Root = root;

        return root;
      }

      return root;
    }

    public string GetTargetIndoor(string playerLoc, string npcLoc)
    {
      var target = locationContexts[npcLoc].Parent;
      if (target == null) return null;
      if (locationContexts[npcLoc].Root == target) return npcLoc;
      if (playerLoc == target) return target;
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
          if (!villagers.Contains(npc) && !excludedNpcs.Contains(npc.Name) && npc.isVillager())
            villagers.Add(npc);
        }
      }

      return villagers;
    }

    private void ControlEvents_KeyReleased(object sender, EventArgsKeyPressed e)
    {
      if (e.KeyPressed.ToString().Equals(config.ShowKeyCode) && !config.Toggle)
        showLocators = false;
    }

    private void ControlEvents_KeyPressed(object sender, EventArgsKeyPressed e)
    {
      if (e.KeyPressed.ToString().Equals(config.ShowKeyCode))
        showLocators = true;
    }

    private void GameEvents_UpdateTick(object sender, EventArgs e)
    {
      if (!Context.IsWorldReady)
        return;

      if (!Game1.paused && showLocators)
        UpdateLocators();
    }

    private void UpdateLocators()
    {
      locators = new Dictionary<string, List<Locator>>();

      foreach (var npc in GetVillagers())
      {
        if (npc.Schedule == null
            && !npc.isMarried()
            || npc.currentLocation == null
            || config.NPCBlacklist.Contains(npc.Name)
        )
          continue;

        locationContexts.TryGetValue(Game1.player.currentLocation.Name, out var playerLocCtx);
        locationContexts.TryGetValue(npc.currentLocation.Name, out var npcLocCtx);

        if (npcLocCtx.Root != playerLocCtx.Root)
          continue;

        var playerLoc = Game1.player.currentLocation;
        var npcLoc = npc.currentLocation;
        var npcPos = new Vector2(-1000, 1000);
        var playerPos =
          new Vector2(Game1.player.position.X + Game1.player.FarmerSprite.SpriteWidth / 2 * Game1.pixelZoom,
            Game1.player.position.Y);
        var IsWarp = false;
        var isOnScreen = false;
        var location = npcLoc.Name;

        if (playerLoc == npcLoc)
        {
          // Don't include locator if NPC is visible on screen
          if (Utility.isOnScreen(new Vector2(npc.position.X, npc.position.Y), Game1.tileSize / 4))
            continue;

          npcPos = new Vector2(npc.position.X + npc.Sprite.SpriteHeight / 2 * Game1.pixelZoom, npc.position.Y);
        }
        // Indoor locations
        else
        {
          var indoor = GetTargetIndoor(playerLoc.Name, npcLoc.Name);
          if (indoor == null) continue;
          if (playerLoc.Name != npcLocCtx.Root && playerLoc.Name != indoor) continue;
          IsWarp = true;
          location = (playerLoc.IsOutdoors || npcLocCtx.Type != "room") ? indoor : location;

          if (!playerLoc.IsOutdoors)
          {
            npcPos = new Vector2(
              npcLocCtx.Warp.X * Game1.tileSize + Game1.tileSize / 2,
              npcLocCtx.Warp.Y * Game1.tileSize - Game1.tileSize * 3 / 2
            );
          }
          else
          {
            npcPos = new Vector2(
              locationContexts[indoor].Warp.X * Game1.tileSize + Game1.tileSize / 2,
              locationContexts[indoor].Warp.Y * Game1.tileSize - Game1.tileSize * 3 / 2
            );
          }

          if (!activeWarpLocators.ContainsKey(location))
          {
            activeWarpLocators.Add(location, new LocatorScroller()
            {
              Location = location,
              Characters = new HashSet<string>() {npc.Name},
              LocatorRect = new Rectangle((int) (npcPos.X - 32), (int) (npcPos.Y - 32),
                64, 64)
            });
          }
          else
            activeWarpLocators[location].Characters.Add(npc.Name);
        }

        isOnScreen = Utility.isOnScreen(npcPos, Game1.tileSize / 4);

        var locator = new Locator
        {
          Name = npc.Name,
          Marker = npc.Sprite.Texture,
          Proximity = GetDistance(playerPos, npcPos),
          IsWarp = IsWarp,
          IsOnScreen = isOnScreen
        };

        var angle = GetPlayerToNPCAngle(playerPos, npcPos);
        var quadrant = GetViewportQuadrant(angle, playerPos);
        var locatorPos = GetLocatorPosition(angle, quadrant, playerPos, npcPos, isOnScreen, IsWarp);

        locator.X = locatorPos.X;
        locator.Y = locatorPos.Y;
        locator.Angle = angle;
        locator.Quadrant = quadrant;

        if (new Rectangle((int) (locator.X - 32), (int) (locator.Y - 32), 64, 64).Contains(Game1.getMouseX(),
          Game1.getMouseY()))
        {
          Game1.mouseCursor = 0;
          Game1.mouseCursorTransparency = 1f;
        }

        if (locators.TryGetValue(location, out var doorLocators))
        {
          if (!doorLocators.Contains(locator))
            doorLocators.Add(locator);
        }
        else
        {
          doorLocators = new List<Locator> {locator};
        }

        locators[location] = doorLocators;
      }
    }

    // Get angle (in radians) to determine which quadrant the NPC is in
    // 0 rad will be at line from player position to (0, viewportHeight/2) relative to viewport
    private double GetPlayerToNPCAngle(Vector2 playerPos, Vector2 npcPos)
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
      // the viewport quadrant and the line to the npc
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

    private void GraphicsEvents_OnPreRenderHudEvent(object sender, EventArgs e)
    {
      if (!Context.IsWorldReady) return;

      if (showLocators && !Game1.paused)
        DrawLocators();
    }

    private void DrawLocators()
    {
      var currLocation = "";

      // Individual locators, onscreen or offscreen
      foreach (var locPair in locators)
      {
        int offsetX;
        int offsetY;

        if (!locPair.Value.FirstOrDefault().IsWarp)
        {
          foreach (var locator in locPair.Value)
          {
            offsetX = 24;
            offsetY = 15;

            var alphaLevel = locator.Proximity > MAX_PROXIMITY
              ? 0.25
              : 0.25 + (MAX_PROXIMITY - locator.Proximity) / MAX_PROXIMITY * 0.75;

            if (!constants.MarkerCrop.TryGetValue(locator.Name, out var cropY))
              cropY = 0;

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
            Game1.spriteBatch.Draw(
              locator.Marker,
              new Vector2(locator.X + offsetX, locator.Y + offsetY),
              new Rectangle(0, cropY, 16, 15),
              Color.White * (float) alphaLevel,
              0f,
              new Vector2(16, 16),
              3f,
              SpriteEffects.None,
              1f
            );

            
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
        // Multiple locators in a location, pointing to its door
        else
        {
          Locator locator = null;
          LocatorScroller activeLocator = null;

          if (activeWarpLocators != null && activeWarpLocators.TryGetValue(locPair.Key, out activeLocator))
          {
            locator = locPair.Value.ElementAt(activeLocator.Index);
            activeLocator.LocatorRect = new Rectangle((int)(locator.X- 32), (int)(locator.Y - 32), 64, 64);
          }
          else
            locator = locPair.Value.FirstOrDefault();

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

          var alphaLevel = locator.Proximity > MAX_PROXIMITY
            ? 0.25
            : 0.25 + (MAX_PROXIMITY - locator.Proximity) / MAX_PROXIMITY * 0.75;

          // Make locators point down at the door
          if (locator.IsOnScreen)
          {
            locator.Angle = 3 * MathHelper.PiOver2;
            locator.Proximity = 0;
          }

          if (!constants.MarkerCrop.TryGetValue(locator.Name, out var cropY))
            cropY = 0;

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
          Game1.spriteBatch.Draw(
            locator.Marker,
            new Vector2(locator.X + offsetX, locator.Y + offsetY),
            new Rectangle(0, cropY, 16, 15),
            Color.White * (float) alphaLevel,
            0f,
            new Vector2(16, 16),
            3f,
            SpriteEffects.None,
            1f
          );

          if (locator.IsOnScreen || new Rectangle((int)(locator.X - 32), (int)(locator.Y - 32), 64, 64).Contains(Game1.getMouseX(),
                Game1.getMouseY()))
          {
            var countString = $"{locPair.Value.Count}";

            // head icon
            Game1.spriteBatch.Draw(
              pointer,
              new Vector2(locator.X + offsetX - 31, locator.Y + offsetY),
              new Rectangle(64, 0, 7, 9),
              Color.White * (float) alphaLevel, 0f, Vector2.Zero,
              1f,
              SpriteEffects.None,
              1f
            );

            DrawText(Game1.tinyFont, countString, new Vector2(locator.X + offsetX - 31, locator.Y + offsetY),
              Color.Black * (float) alphaLevel,
              new Vector2((int) (Game1.tinyFont.MeasureString(countString).X - 24) / 2,
                (float) (Game1.tileSize / 8) + 3), 1f);

          }

            else if (locator.Proximity > 0)
            {
              var distanceString = $"{Math.Round(locator.Proximity / Game1.tileSize, 0)}";
              DrawText(Game1.tinyFont, distanceString, new Vector2(locator.X + offsetX - 24, locator.Y + offsetY - 4),
                Color.Black * (float) alphaLevel,
                new Vector2((int) Game1.tinyFont.MeasureString(distanceString).X / 2,
                  (float) (Game1.tileSize / 4 * 0.5)), 1f);
            
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

    private void GraphicsEvents_OnPostRenderEvent(object sender, EventArgs e)
    {
      if (!DEBUG_MODE || !Context.IsWorldReady || locators == null)
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
    public Texture2D Marker { get; set; }
    public double Proximity { get; set; }
    public int Quadrant { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public double Angle { get; set; }
    public bool IsWarp { get; set; }
    public bool IsOnScreen { get; set; }
  }

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

  internal class LocationContext
  {
    public string Type { get; set; } // outdoors, indoors, or room
    public string Root { get; set; } // Top-most outdoor location
    public string Parent { get; set; } // Level above
    public List<string> Children { get; set; } // Levels below
    public Vector2 Warp { get; set; } // Position of warp
  }
}