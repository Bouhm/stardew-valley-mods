using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using Netcode;
using StardewValley.Network;

namespace LocationCompass
{
  /// <summary>The mod entry point.</summary>
  public class ModEntry : Mod
  {
    private IModHelper helper;
    private ModConfig config;
    private bool showLocators;
    private static IMonitor monitor;
    private static Texture2D pointer;
    private static ModData constants;
    private static Dictionary<string, LocationContext> locationContexts; // Mapping of locations to root locations
    private static Dictionary<GameLocation, List<Locator>> locators;
    private static Dictionary<string, LocatorScroller> activeDoorLocators; // Active indices of locators of doors
    private const int MAX_PROXIMITY = 4800;

    private const bool DEBUG_MODE = false;

    public override void Entry(IModHelper helper)
    {
      this.helper = helper;
      monitor = this.Monitor;
      config = helper.ReadConfig<ModConfig>();
      ModEntry.pointer =
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
      if (!Context.IsWorldReady || activeDoorLocators == null) return;
      if (e.Button.Equals(SButton.MouseLeft) || e.Button.Equals(SButton.ControllerA))
      {
        foreach (var doorLocator in activeDoorLocators)
        {
          doorLocator.Value.ReceiveLeftClick(Game1.getMouseX(), Game1.getMouseY());
        }
      }
    }

    private void LocationEvents_LocationsChanged(object sender, EventArgsLocationsChanged e)
    {
      locationContexts = new Dictionary<string, LocationContext>();
      foreach (var location in Game1.locations)
        MapRootLocations(location, null, null, false);
    }

    private void SaveEvents_AfterLoad(object sender, EventArgs e)
    {
      locationContexts = new Dictionary<string, LocationContext>();
      foreach (var location in Game1.locations)
        MapRootLocations(location, null, null, false);
      
    }


    // Recursively traverse warps of locations and map locations to root locations (outdoor locations)
    private string MapRootLocations(GameLocation location, GameLocation prevLocation, string root, bool hasOutdoorWarp)
    {
      if (!locationContexts.ContainsKey(location.Name))
        locationContexts.Add(location.Name, new LocationContext());

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
        return location.Name;
      }

      // Iterate warps of current location and traverse recursively
      foreach (var warp in location.warps)
      {
        // If one of the warps is a root location, current location is an indoor building 
        if (Game1.getLocationFromName(warp.TargetName).isOutdoors)
          hasOutdoorWarp = true;

        // If all warps are indoors, then the current location is a room
        locationContexts[location.Name].Type = hasOutdoorWarp ? "indoors" : "room";
        root = MapRootLocations(Game1.getLocationFromName(warp.TargetName), location, root, hasOutdoorWarp);
        locationContexts[location.Name].Root = root;

        if (prevLocation != null)
          locationContexts[prevLocation.Name].Parent = location.Name;

        return root;
      }

      return root;
    }

    // Get only relevant villagers for map
    private List<NPC> GetVillagers()
    {
      var villagers = new List<NPC>();
      var excludedNpcs = new List<string>()
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
      {
        return;
      }

      if (!Game1.paused)
        UpdateLocators();
    }

    // Get angle (in radians) to determine which quadrant the NPC is in
    // 0 rad will be at line from player position to (0, viewportHeight/2) relative to viewport
    private double GetPlayerToNPCAngle(Vector2 playerPos, Vector2 npcPos)
    {
      // Hypotenuse is the line from player to npc
      float opposite = npcPos.Y - playerPos.Y;
      float adjacent = npcPos.X - playerPos.X;
      double angle = Math.Atan2(opposite, adjacent) + MathHelper.Pi;

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
      else if (angle < MathHelper.Pi - Math.Atan2(playerPos.Y - Game1.viewport.Y,
                 Game1.viewport.X + Game1.viewport.Width - playerPos.X))
        return 2;
      // Right quadrant
      else if (angle < MathHelper.Pi + Math.Atan2(Game1.viewport.Y + Game1.viewport.Height - playerPos.Y,
                 Game1.viewport.X + Game1.viewport.Width - playerPos.X))
        return 3;
      // Bottom quadrant
      else if (angle < MathHelper.TwoPi - Math.Atan2(Game1.viewport.Y + Game1.viewport.Height - playerPos.Y,
                 playerPos.X - Game1.viewport.X))
        return 4;
      // Bottom half of left quadrant
      else
        return 1;
    }

    private double GetDistance(Vector2 begin, Vector2 end)
    {
      return Math.Sqrt(
        Math.Pow((begin.X - end.X), 2) + Math.Pow((begin.Y - end.Y), 2)
      );
    }

    // Get position of location relative to viewport from
    // the viewport quadrant and positions of player/npc relative to map
    private static Vector2 GetLocatorPosition(double angle, int quadrant, Vector2 playerPos, Vector2 npcPos,
      bool isOnScreen = false, bool isDoor = false)
    {
      float x = playerPos.X - Game1.viewport.X;
      float y = playerPos.Y - Game1.viewport.Y;

      if (isDoor)
      {
        if (Utility.isOnScreen(new Vector2(npcPos.X, npcPos.Y - Game1.tileSize * 3 / 2), Game1.tileSize / 4))
          return new Vector2(npcPos.X - Game1.viewport.X + Game1.tileSize / 2,
            npcPos.Y - Game1.viewport.Y - Game1.tileSize*3/2);
        else
          x += 3 * Game1.tileSize / 4;
      }

      // Draw triangle such that the hypotenuse is
      // the line from player to the point of intersection of
      // the viewport quadrant and the line to the npc
      switch (quadrant)
      {
        // Have to split each quadrant in half since player is not always centered in viewport
        case 1:
          // Bottom half
          if (angle > MathHelper.TwoPi - angle)
            y += (playerPos.X - Game1.viewport.X) * (float) Math.Tan(MathHelper.TwoPi - angle);
          // Top half
          else
            y += (playerPos.X - Game1.viewport.X) * (float) Math.Tan(MathHelper.TwoPi - angle);

          y = MathHelper.Clamp(y, pointer.Height / 2, Game1.viewport.Height - pointer.Height / 2);
          return new Vector2(0 + (Game1.tileSize / 2 + 8), y);
        case 2:
          // Left half
          if (angle < MathHelper.PiOver2)
            x -= (playerPos.Y - Game1.viewport.Y) * (float) Math.Tan(MathHelper.PiOver2 - angle);
          // Right half
          else
            x -= (playerPos.Y - Game1.viewport.Y) * (float) Math.Tan(MathHelper.PiOver2 - angle);

          x = MathHelper.Clamp(x, pointer.Width / 2, Game1.viewport.Width - pointer.Width / 2);
          return new Vector2(x, 0 + (Game1.tileSize / 2 + 8));
        case 3:
          // Top half
          if (angle < MathHelper.Pi)
            y -= (Game1.viewport.X + Game1.viewport.Width - playerPos.X) * (float) Math.Tan(MathHelper.Pi - angle);
          // Bottom half
          else
            y -= (Game1.viewport.X + Game1.viewport.Width - playerPos.X) * (float) Math.Tan(MathHelper.Pi - angle);

          y = MathHelper.Clamp(y, pointer.Height / 2, Game1.viewport.Height - pointer.Height / 2);
          return new Vector2(Game1.viewport.Width - (Game1.tileSize / 2 + 8), y);
        case 4:
          // Right half
          if (angle < 3 * MathHelper.PiOver2)
            x += (Game1.viewport.Y + Game1.viewport.Height - playerPos.Y) *
                 (float) Math.Tan(3 * MathHelper.PiOver2 - angle);
          // Left half
          else
            x += (Game1.viewport.Y + Game1.viewport.Height - playerPos.Y) *
                 (float) Math.Tan(3 * MathHelper.PiOver2 - angle);

          x = MathHelper.Clamp(x, pointer.Width / 2, Game1.viewport.Width - pointer.Width / 2);
          return new Vector2(x, Game1.viewport.Height - (Game1.tileSize / 2 + 8));
        default:
          return new Vector2(-1000, -1000);
      }
    }

    private void UpdateLocators()
    {
      locators = new Dictionary<GameLocation, List<Locator>>();
      if (activeDoorLocators == null)
      {
        activeDoorLocators = new Dictionary<string, LocatorScroller>();
      }

      foreach (NPC npc in GetVillagers())
      {
        if ((npc.Schedule == null
             && !npc.isMarried())
            || npc.currentLocation == null
            || config.NPCBlacklist.Contains(npc.Name)
        )
        {
          continue;
        }

        var npcPos = new Vector2(-1000, 1000);
        var playerPos = new Vector2(Game1.player.position.X + Game1.player.FarmerSprite.SpriteWidth / 2 * Game1.pixelZoom, Game1.player.position.Y);
        var isDoor = false;
        var isOnScreen = false;

        if (npc.currentLocation.Equals(Game1.player.currentLocation))
        {
          // Don't include locator if NPC is visible on screen
          if (Utility.isOnScreen(new Vector2(npc.position.X, npc.position.Y), Game1.tileSize / 4))
          {
            continue;
          }

          npcPos = new Vector2(npc.position.X + npc.Sprite.SpriteHeight / 2 * Game1.pixelZoom, npc.position.Y);
        }
        else
        {
          locationContexts.TryGetValue(npc.currentLocation.Name, out var npcLocCtx);
          locationContexts.TryGetValue(Game1.player.currentLocation.Name, out var playerLocCtx);

          if (npcLocCtx.Root == playerLocCtx.Root)
          {
            foreach (KeyValuePair<Point, string> door in Game1.player.currentLocation.doors.Pairs)
            {
              if (door.Value.Equals(npc.currentLocation.Name) || door.Value.Equals(npcLocCtx.Parent))
              {
                npcPos = new Vector2(
                  door.Key.X * Game1.tileSize,
                  door.Key.Y * Game1.tileSize
                );
                isDoor = true;
                isOnScreen = Utility.isOnScreen(npcPos, Game1.tileSize / 4);

                if (!activeDoorLocators.ContainsKey(npc.currentLocation.Name))
                  activeDoorLocators.Add(npc.currentLocation.Name,
                    new LocatorScroller() {Location = npc.currentLocation.Name, Characters = new HashSet<string>(){ npc.Name }});
                else
                  activeDoorLocators[npc.currentLocation.Name].Characters.Add(npc.Name);

                break;
              }
            }
          }

          if (npcPos.X < 0)
            continue;
        }

        var locator = new Locator
        {
          Name = npc.Name,
          Marker = npc.Sprite.Texture,
          Proximity = GetDistance(playerPos, npcPos),
          IsDoor = isDoor,
          IsOnScreen = isOnScreen
        };

        double angle = GetPlayerToNPCAngle(playerPos, npcPos);
        int quadrant = GetViewportQuadrant(angle, playerPos);
        Vector2 locatorPos = GetLocatorPosition(angle, quadrant, playerPos, npcPos, isOnScreen, isDoor);

        locator.X = locatorPos.X;
        locator.Y = locatorPos.Y;
        locator.Angle = angle;
        locator.Quadrant = quadrant;

        if (locators.TryGetValue(npc.currentLocation, out var doorLocators))
          doorLocators.Add(locator);
        else
          doorLocators = new List<Locator> { locator };

        locators[npc.currentLocation] = doorLocators;
      }
    }

    private void GraphicsEvents_OnPreRenderHudEvent(object sender, EventArgs e)
    {
      if (!Context.IsWorldReady)
      {
        return;
      }

      if (showLocators && !Game1.paused)
        DrawLocators();
    }

    private void DrawLocators()
    {
      string currLocation = "";
      int offsetX = 0;
      int offsetY = 0;

      // Individual locators, onscreen or offscreen
      foreach (var locPair in locators)
      {
        if (locPair.Key.isOutdoors)
        {
          foreach (var locator in locPair.Value)
          {
            double alphaLevel = locator.Proximity > MAX_PROXIMITY
              ? 0.25
              : 0.25 + ((MAX_PROXIMITY - locator.Proximity) / MAX_PROXIMITY) * 0.75;

            if (!locator.IsOnScreen)
            {
              switch (locator.Quadrant)
              {
                case 1:
                  offsetX = 16;
                  break;
                case 2:
                  offsetY = 16;
                  break;
                case 3:
                  offsetX = -16;
                  break;
                case 4:
                  offsetY = -16;
                  break;
              }
            }

            if (!constants.MarkerCrop.TryGetValue(locator.Name, out var cropY))
              cropY = 0;

            // Pointer texture
            Game1.spriteBatch.Draw(
              pointer,
              new Vector2(locator.X + offsetX, locator.Y + offsetY),
              new Rectangle?(new Rectangle(0, 0, 64, 64)),
              Color.White * (float)alphaLevel,
              (float)(locator.Angle - 3 * MathHelper.PiOver4),
              new Vector2(pointer.Width / 2, pointer.Height / 2),
              1f,
              SpriteEffects.None,
              0.0f
            );

            // NPC head
            Game1.spriteBatch.Draw(
              locator.Marker,
              new Vector2(locator.X + offsetX + 24, locator.Y + offsetY + 16),
              new Rectangle?(new Rectangle(0, cropY, 16, 15)),
              Color.White * (float)alphaLevel,
              0f,
              new Vector2(16, 16),
              3f,
              SpriteEffects.None,
              1f
            );

            if (locator.Proximity > 0)
            {
              string distanceString = $"{Math.Round(locator.Proximity / Game1.tileSize, 0)}";
              DrawText(Game1.tinyFont, distanceString, new Vector2(locator.X + offsetX, locator.Y + 12),
                Color.Black * (float)alphaLevel,
                new Vector2((int)Game1.tinyFont.MeasureString(distanceString).X / 2,
                  (float)((Game1.tileSize / 4) * 0.5)), 1f);
            }
          }
        }
        // Multiple locators in a location, pointing to its door
        else
        {
          Locator locator = null;
          LocatorScroller activeLocator = null;
         
          if (activeDoorLocators != null && activeDoorLocators.TryGetValue(locPair.Key.Name, out activeLocator))
          {
            locator = locPair.Value.ElementAt(activeLocator.Index);
            activeLocator.UpdatePosition(locator.X, locator.Y);
          }
          else
            locator = locPair.Value.FirstOrDefault();

            double alphaLevel = locator.Proximity > MAX_PROXIMITY
            ? 0.25
            : 0.25 + ((MAX_PROXIMITY - locator.Proximity) / MAX_PROXIMITY) * 0.75;

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
            new Vector2(locator.X + offsetX, locator.Y + offsetY),
            new Rectangle?(new Rectangle(0, 0, 64, 64)),
            Color.White * (float)alphaLevel,
            (float)(locator.Angle - 3 * MathHelper.PiOver4),
            new Vector2(pointer.Width / 2, pointer.Height / 2),
            1f,
            SpriteEffects.None,
            0.0f
          );

          // NPC head
          Game1.spriteBatch.Draw(
            locator.Marker,
            new Vector2(locator.X + offsetX + 24, locator.Y + offsetY + 16),
            new Rectangle?(new Rectangle(0, cropY, 16, 15)),
            Color.White * (float)alphaLevel,
            0f,
            new Vector2(16, 16),
            3f,
            SpriteEffects.None,
            1f
          );

          if (locPair.Value.Count > 1)
          {
            string countString = $"{locPair.Value.Count - 1}";

            // tinyFont doesn't scale the + symbol the same way...
            DrawText(Game1.tinyFont, "+", new Vector2(locator.X, locator.Y + offsetY + 12),
              Color.Black * (float) alphaLevel,
              new Vector2(12, -4), 0.5f);

            DrawText(Game1.tinyFont, countString, new Vector2(locator.X, locator.Y + offsetY + 12),
              Color.Black * (float) alphaLevel,
              new Vector2((int) (Game1.tinyFont.MeasureString(countString).X - 12) / 2,
                (float) ((Game1.tileSize / 4) * 0.5)), 1f);

            if (activeLocator != null)
              activeLocator.Draw((float)alphaLevel);
          }
        }
      }
    }

    // Draw line relative to viewport
    private void DrawLine(SpriteBatch b, Vector2 begin, Vector2 end, Texture2D tex)
    {
      Rectangle r = new Rectangle((int) begin.X, (int) begin.Y, (int) (end - begin).Length() + 2, 2);
      Vector2 v = Vector2.Normalize(begin - end);
      float angle = (float) Math.Acos(Vector2.Dot(v, -Vector2.UnitX));
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
      Game1.spriteBatch.DrawString(font ?? Game1.tinyFont, text, pos, color ?? Color.White, 0f, origin ?? Vector2.Zero, scale,
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

          NPC npc = Game1.getCharacterFromName(locator.Name);
          float viewportX = Game1.player.position.X + Game1.pixelZoom * Game1.player.Sprite.SpriteWidth / 2 -
                            Game1.viewport.X;
          float viewportY = Game1.player.position.Y - Game1.viewport.Y;
          float npcViewportX = npc.position.X + Game1.pixelZoom * npc.Sprite.SpriteWidth / 2 - Game1.viewport.X;
          float npcViewportY = npc.position.Y - Game1.viewport.Y;

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
    public string Name { get; set; } = null;
    public Texture2D Marker { get; set; } = null;
    public double Proximity = 0;
    public int Quadrant = 1;
    public float X { get; set; } = 0;
    public float Y { get; set; } = 0;
    public double Angle { get; set; } = 0.0;
    public bool IsDoor { get; set; } = true;
    public bool IsOnScreen { get; set; } = true;   
  }

  internal class LocatorScroller
  {
    public string Location { get; set; }
    public HashSet<string> Characters { get; set; }
    public int Index { get; set; } = 0;
    public float X { get; set; }
    public float Y { get; set; }

    private Rectangle leftArrowRect;
    private Rectangle rightArrowRect;
    private const int margin = 40;

    public LocatorScroller()
    {
      leftArrowRect = new Rectangle((int)X - margin, (int)Y, 64, 64);
      rightArrowRect = new Rectangle((int)X + margin, (int)Y, 64, 64);
    }

    public void ReceiveLeftClick(int x, int y)
    {
      if (leftArrowRect.Contains(x, y))
      {
        Index--;
        Game1.playSound("drumkit6");

        if (Index < 0)
          Index = Characters.Count - 1;
      }
      else if (rightArrowRect.Contains(x, y))
      {
        Index++;
        Game1.playSound("drumkit6");

        if (Index > Characters.Count - 1)
          Index = 0;
      }
      
    }

    public void UpdatePosition(float x, float y)
    {
      X = x - 6;
      Y = y - 16;
      leftArrowRect.X = (int) X - margin;
      leftArrowRect.Y = (int) Y;
      rightArrowRect.X = (int) X + margin;
      rightArrowRect.Y = (int) Y;
    }

    public void Draw(float opacity)
    {
      var b = Game1.spriteBatch;
      b.Draw(Game1.mouseCursors, new Vector2((float) (leftArrowRect.X), (float) (leftArrowRect.Y)),
        new Rectangle(480, 96, 24, 32), Color.White * opacity, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 1f);
      b.Draw(Game1.mouseCursors, new Vector2((float) (rightArrowRect.X), (float) (rightArrowRect.Y)),
        new Rectangle(448, 96, 24, 32), Color.White * opacity, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 1f);
    }
  }

  internal class LocationContext
  {
    public string Type { get; set; } // outdoors, indoors, or room
    public string Root { get; set; } // Top-most location
    public string Parent { get; set; } // Level above

    public LocationContext()
    {
      Type = null;
      Root = null;
      Parent = null;
    }
  }
}