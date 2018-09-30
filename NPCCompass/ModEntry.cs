using System;
using System.Collections.Generic;
using System.Linq;
using LocationCompass;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Events;
using StardewValley.Menus;
using StardewValley.Network;

namespace NPCCompass
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
    private static HashSet<Locator> locators;
    private static Dictionary<string, string> OutdoorLocations; // Mapping of corresponding buildings to outdoor locations
    private static Dictionary<string, string> IndoorLocations; // Mapping of corresponding rooms to buildings
    private static Dictionary<string, HashSet<Locator>> doorLocatorSets;
    public static Dictionary<string, int> activeDoorLocators; // Active indices of locators of doors
    private const int MAX_PROXIMITY = 4800;

    private const bool DEBUG_MODE = true;

    public override void Entry(IModHelper helper)
    {
      this.helper = helper;
      monitor = this.Monitor;
      config = helper.ReadConfig<ModConfig>();
      ModEntry.pointer =
        helper.Content.Load<Texture2D>(@"assets/locator.png", ContentSource.ModFolder); // Load pointer tex
      constants = this.helper.ReadJsonFile<ModData>("constants.json") ?? new ModData();
      TimeEvents.AfterDayStarted += TimeEvents_AfterDayStarted;
      GameEvents.UpdateTick += GameEvents_UpdateTick;
      GraphicsEvents.OnPreRenderHudEvent += GraphicsEvents_OnPreRenderHudEvent;
      GraphicsEvents.OnPostRenderEvent += GraphicsEvents_OnPostRenderEvent;
      ControlEvents.KeyPressed += ControlEvents_KeyPressed;
      ControlEvents.KeyReleased += ControlEvents_KeyReleased;
    }

    private void TimeEvents_AfterDayStarted(object sender, EventArgs e)
    {
      TraverseDoors();
      locators = new HashSet<Locator>();
      activeDoorLocators = new Dictionary<string, int>();

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

        var npcPos = Vector2.Zero;
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
          foreach (KeyValuePair<Point, string> door in Game1.player.currentLocation.doors.Pairs)
          {
            if (door.Value.Equals(npc.currentLocation.Name))
            {
              npcPos = new Vector2(
                door.Key.X * Game1.tileSize,
                door.Key.Y * Game1.tileSize
              );
              isDoor = true;
              isOnScreen = Utility.isOnScreen(npcPos, Game1.tileSize / 4);
              break;
            }
          }

          if (npcPos.Equals(Vector2.Zero))
            continue;
        }

        Locator locator = new Locator
        {
          Npc = npc,
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

        if (isDoor && isOnScreen)
        {
          if (doorLocatorSets.TryGetValue(npc.currentLocation.Name, out HashSet<Locator> doorLocators))
            doorLocators.Add(locator);
          else
            doorLocators = new HashSet<Locator> { locator };

          doorLocatorSets[npc.currentLocation.Name] = doorLocators;
        }
        else
          locators.Add(locator);

        if (!activeDoorLocators.ContainsKey(npc.currentLocation.Name))
          activeDoorLocators[npc.currentLocation.Name] = 0;
      }
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

    // Recursively traverse all doors
    private void TraverseDoors()
    {
      foreach (var location in Game1.locations)
      {
        var a = location;
        var b = location.doors;
        var c = location.doors.Pairs;
        foreach (var door in location.doors.Pairs)
        {

        }
     
      }
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
        if (Utility.isOnScreen(npcPos, Game1.tileSize / 4))
          return new Vector2(npcPos.X - Game1.viewport.X + Game1.tileSize / 2,
            npcPos.Y - Game1.viewport.Y - Game1.tileSize / 2);
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
          return new Vector2(0 + (Game1.tileSize / 2 + 8), y);
        case 2:
          // Left half
          if (angle < MathHelper.PiOver2)
            x -= (playerPos.Y - Game1.viewport.Y) * (float) Math.Tan(MathHelper.PiOver2 - angle);
          // Right half
          else
            x -= (playerPos.Y - Game1.viewport.Y) * (float) Math.Tan(MathHelper.PiOver2 - angle);
          return new Vector2(x, 0 + (Game1.tileSize / 2 + 8));
        case 3:
          // Top half
          if (angle < MathHelper.Pi)
            y -= (Game1.viewport.X + Game1.viewport.Width - playerPos.X) * (float) Math.Tan(MathHelper.Pi - angle);
          // Bottom half
          else
            y -= (Game1.viewport.X + Game1.viewport.Width - playerPos.X) * (float) Math.Tan(MathHelper.Pi - angle);
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
          return new Vector2(x, Game1.viewport.Height - (Game1.tileSize / 2 + 8));
        default:
          return new Vector2(-5000, -5000);
      }
    }

    private void UpdateLocators()
    {
      foreach (Locator locator in locators)
      {
        var npc = locator.Npc;
        var npcPos = Vector2.Zero;
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
          foreach (KeyValuePair<Point, string> door in Game1.player.currentLocation.doors.Pairs)
          {
            if (door.Value.Equals(npc.currentLocation.Name))
            {
              npcPos = new Vector2(
                door.Key.X * Game1.tileSize,
                door.Key.Y * Game1.tileSize
              );
              isDoor = true;
              isOnScreen = Utility.isOnScreen(npcPos, Game1.tileSize / 4);
              break;
            }
          }

          if (npcPos.Equals(Vector2.Zero))
            continue;
        }

        locator.Proximity = GetDistance(playerPos, npcPos);
        locator.IsDoor = isDoor;
        locator.IsOnScreen = isOnScreen;

        double angle = GetPlayerToNPCAngle(playerPos, npcPos);
        int quadrant = GetViewportQuadrant(angle, playerPos);
        Vector2 locatorPos = GetLocatorPosition(angle, quadrant, playerPos, npcPos, isOnScreen, isDoor);

        locator.X = locatorPos.X;
        locator.Y = locatorPos.Y;
        locator.Angle = angle;
        locator.Quadrant = quadrant;

        if (isDoor && isOnScreen)
        {
          if (doorLocatorSets.TryGetValue(npc.currentLocation.Name, out HashSet<Locator> doorLocators))
            doorLocators.Add(locator);
          else
            doorLocators = new HashSet<Locator> { locator };

          doorLocatorSets[npc.currentLocation.Name] = doorLocators;
        }
        else
          locators.Add(locator);
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
      int offsetY = -64;

      foreach (KeyValuePair<string, HashSet<Locator>> doorLocators in doorLocatorSets)
      {
        var locators = doorLocators.Value.ToList();
        Locator locator;

        if (activeDoorLocators != null && activeDoorLocators.TryGetValue(doorLocators.Key, out var activeIndex))
          locator = locators.ElementAt(activeDoorLocators[doorLocators.Key]);
        else
          locator = locators.FirstOrDefault();
        
        double alphaLevel = locator.Proximity > MAX_PROXIMITY
          ? 0.25
          : 0.25 + ((MAX_PROXIMITY - locator.Proximity) / MAX_PROXIMITY) * 0.75;

        if (locator.IsOnScreen)
        {
          locator.Angle = 3 * MathHelper.PiOver2;
          locator.Proximity = 0;
        }

        if (!constants.MarkerCrop.TryGetValue(locator.Npc.Name, out var cropY))
          cropY = 0;

        // Pointer texture
        Game1.spriteBatch.Draw(
          pointer,
          new Vector2(locator.X, locator.Y + offsetY),
          new Rectangle?(new Rectangle(0, 0, 64, 64)),
          Color.White * (float) alphaLevel,
          (float) (locator.Angle - 3 * MathHelper.PiOver4),
          new Vector2(pointer.Width / 2, pointer.Height / 2),
          1f,
          SpriteEffects.None,
          0.0f
        );

        // NPC head
        Game1.spriteBatch.Draw(
          locator.Npc.Sprite.Texture,
          new Vector2(locator.X + 24, locator.Y + offsetY + 16),
          new Rectangle?(new Rectangle(0, cropY, 16, 15)),
          Color.White * (float) alphaLevel,
          0f,
          new Vector2(16, 16),
          3f,
          SpriteEffects.None,
          1f
        );

        if (doorLocators.Value.Count > 1)
        {
          string countString = $"{doorLocators.Value.Count-1}";

          // tinyFont doesn't scale the + symbol the same way...
          DrawText(Game1.tinyFont, "+", new Vector2(locator.X, locator.Y + offsetY + 12),
            Color.Black * (float)alphaLevel,
            new Vector2(12, -4), 0.5f);

          DrawText(Game1.tinyFont, countString, new Vector2(locator.X, locator.Y + offsetY + 12),
            Color.Black * (float)alphaLevel,
            new Vector2((int)(Game1.tinyFont.MeasureString(countString).X-12) / 2,
              (float)((Game1.tileSize / 4) * 0.5)), 1f);
        }
      }

      foreach (Locator locator in locators)
      {
        double alphaLevel = locator.Proximity > MAX_PROXIMITY
          ? 0.25
          : 0.25 + ((MAX_PROXIMITY - locator.Proximity) / MAX_PROXIMITY) * 0.75;

        // To offset the offsets used to keep all of the locators inside the viewport screen
        //if (locator.Quadrant == 2 || locator.Quadrant == 4)
        //  offsetX = -16;

        if (!constants.MarkerCrop.TryGetValue(locator.Npc.Name, out var cropY))
          cropY = 0;

        // Pointer texture
        Game1.spriteBatch.Draw(
          pointer,
          new Vector2(locator.X + offsetX, locator.Y),
          new Rectangle?(new Rectangle(0, 0, 64, 64)),
          Color.White * (float) alphaLevel,
          (float) (locator.Angle - 3 * MathHelper.PiOver4),
          new Vector2(pointer.Width / 2, pointer.Height / 2),
          1f,
          SpriteEffects.None,
          0.0f
        );

        // NPC head
        Game1.spriteBatch.Draw(
          locator.Npc.Sprite.Texture,
          new Vector2(locator.X + 24 + offsetX, locator.Y + 16),
          new Rectangle?(new Rectangle(0, cropY, 16, 15)),
          Color.White * (float) alphaLevel,
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
            Color.Black * (float) alphaLevel,
            new Vector2((int) Game1.tinyFont.MeasureString(distanceString).X / 2,
              (float) ((Game1.tileSize / 4) * 0.5)), 1f);
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
      if (!DEBUG_MODE || !Context.IsWorldReady || locators == null || doorLocatorSets == null)
        return;

      foreach (Locator locator in locators)
      {
        if (!locator.IsOnScreen)
          continue;

        NPC npc = Game1.getCharacterFromName(locator.Npc.Name);
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

  // Class for locators AKA the 'needles' of the compass
  internal class Locator
  {
    public NPC Npc { get; set; } = null;
    public double Proximity = 0;
    public int Quadrant = 1;
    public float X { get; set; } = 0;
    public float Y { get; set; } = 0;
    public double Angle { get; set; } = 0.0;
    public bool IsDoor { get; set; } = true;
    public bool IsOnScreen { get; set; } = true;
  }

  internal class ArrowButtons : OptionsElement
  {
    private Dictionary<string, int> activeLocators;
    private Rectangle leftArrowRect;
    private Rectangle rightArrowRect;
    private int count;
    private const int margin = 32;

    public ArrowButtons(Dictionary<string, int> activeLocators, string label, int whichOption, int count, int x, int y)
      : base(label, x, y, 28, 28, whichOption)
    {
      this.activeLocators = activeLocators;
      this.count = count;
      leftArrowRect = new Rectangle(x - margin, y, 64, 64);
      rightArrowRect = new Rectangle(x + margin, y, 64, 64);
    }

    public override void receiveLeftClick(int x, int y)
    {
      if (leftArrowRect.Contains(x, y))
      {
        activeLocators[label]--;
        Game1.playSound("drumkit6");

        if (activeLocators[label] < 0)
          activeLocators[label] = count - 1;
      }
      else if (rightArrowRect.Contains(x, y))
      {
        activeLocators[label]++;
        Game1.playSound("drumkit6");

        if (activeLocators[label] > count)
          activeLocators[label] = 0;
      }
    }

    public override void draw(SpriteBatch b, int slotX, int slotY)
    {
      b.Draw(Game1.mouseCursors, new Vector2((float) (leftArrowRect.X), (float) (leftArrowRect.Y)),
        new Rectangle(448, 96, 24, 32), Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.4f);
      b.Draw(Game1.mouseCursors, new Vector2((float) (rightArrowRect.X), (float) (rightArrowRect.Y)),
        new Rectangle(480, 86, 24, 32), Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.4f);
    }
  }
}