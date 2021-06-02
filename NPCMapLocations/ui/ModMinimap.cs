using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;

namespace NPCMapLocations
{
  internal class ModMinimap
  {
    private readonly Texture2D BuildingMarkers;
    private readonly int borderWidth = 12;
    private readonly bool drawPamHouseUpgrade;
    private readonly bool drawMovieTheaterJoja;
    private readonly bool drawMovieTheater;
    private readonly bool drawIsland;
    private readonly Dictionary<long, FarmerMarker> FarmerMarkers;
    private readonly ModCustomizations Customizations;

    private Vector2 prevCenter;
    private Vector2 center; // Center position of minimap
    private float cropX; // Top-left position of crop on map
    private float cropY; // Top-left position of crop on map
    private int mmWidth; // minimap width
    private int mmHeight; // minimap height
    private Vector2 mmLoc; // minimap location relative to map location
    private int mmX; // top-left position of minimap relative to viewport
    private int mmY; // top-left position of minimap relative to viewport
    private int offset = 0; // offset for minimap if viewport changed
    private readonly Dictionary<string, NpcMarker> NpcMarkers;
    private Vector2 playerLoc;
    private int prevMmX;
    private int prevMmY;
    private int drawDelay = 0;
    private bool dragStarted;

    public ModMinimap(
      Dictionary<string, NpcMarker> npcMarkers,
      Dictionary<string, bool> conditionalNpcs,
      Dictionary<long, FarmerMarker> farmerMarkers,
      Dictionary<string, KeyValuePair<string, Vector2>> farmBuildings,
      Texture2D buildingMarkers,
      ModCustomizations customizations
    )
    {
      // renderTarget = new RenderTarget2D(Game1.graphics.GraphicsDevice,Game1.viewport.Width, Game1.viewport.Height);
      this.NpcMarkers = npcMarkers;
      this.ConditionalNpcs = conditionalNpcs;
      this.FarmerMarkers = farmerMarkers;
      this.FarmBuildings = farmBuildings;
      this.BuildingMarkers = buildingMarkers;
      this.Customizations = customizations;

      drawPamHouseUpgrade = Game1.MasterPlayer.mailReceived.Contains("pamHouseUpgrade");
      drawMovieTheaterJoja = Utility.doesMasterPlayerHaveMailReceivedButNotMailForTomorrow("ccMovieTheaterJoja");
      drawMovieTheater = Utility.doesMasterPlayerHaveMailReceivedButNotMailForTomorrow("ccMovieTheater");
      drawIsland = Game1.MasterPlayer.hasOrWillReceiveMail("Visited_Island");

      mmX = ModMain.Globals.MinimapX;
      mmY = ModMain.Globals.MinimapY;
      mmWidth = ModMain.Globals.MinimapWidth * Game1.pixelZoom;
      mmHeight = ModMain.Globals.MinimapHeight * Game1.pixelZoom;
    }

    private Dictionary<string, bool> ConditionalNpcs { get; }
    private Dictionary<string, KeyValuePair<string, Vector2>> FarmBuildings { get; }

    // Check if cursor is hovering the drag zone (top of minimap)
    public bool isHoveringDragZone()
    {
      return (Game1.getMouseX() >= mmX - borderWidth && Game1.getMouseX() <= mmX + mmWidth + borderWidth &&
          Game1.getMouseY() >= mmY - borderWidth && Game1.getMouseY() < mmY + mmHeight + borderWidth);
    }
    public void HandleMouseDown()
    {
      prevMmX = mmX;
      prevMmY = mmY;
      dragStarted = true;
    }

    public void HandleMouseDrag()
    {
      if (dragStarted)
      {
        // Move minimap with mouse on drag
        mmX = NormalizeToMap(MathHelper.Clamp(prevMmX + Game1.getMouseX() - MouseUtil.BeginMousePosition.X, borderWidth,
          Game1.viewport.Width - mmWidth - borderWidth));
        mmY = NormalizeToMap(MathHelper.Clamp(prevMmY + Game1.getMouseY() - MouseUtil.BeginMousePosition.Y, borderWidth,
          Game1.viewport.Height - mmHeight - borderWidth));
      }
    }

    public void HandleMouseRelease()
    {
      ModMain.Globals.MinimapX = mmX;
      ModMain.Globals.MinimapY = mmY;
      ModMain.Helper.Data.WriteJsonFile($"config/{Constants.SaveFolderName}.json", ModMain.Config);
      dragStarted = false;

      // Delay drawing on minimap after it's moved
      if (isHoveringDragZone())
      {
        drawDelay = 30;
      }
    }

    public void UpdateMapForSeason()
    {
      ModMain.Map = Game1.content.Load<Texture2D>("LooseSprites\\map");
    }

    public void CheckOffsetForMap()
    {
      // When ModMain.Map is smaller than viewport (ex. Bus Stop)
      if (Game1.isOutdoorMapSmallerThanViewport())
      {
        offset = (int)(Game1.viewport.Width - Game1.currentLocation.map.Layers[0].LayerWidth * Game1.tileSize) / 2;

        if (mmX > Math.Max(12, offset))
        {
          offset = 0;
        }
      }
      else
      {
        offset = 0;
      }
    }
    public void Resize()
    {
      mmWidth = ModMain.Globals.MinimapWidth * Game1.pixelZoom;
      mmHeight = ModMain.Globals.MinimapHeight * Game1.pixelZoom;
    }

    public void Update()
    {
      // Note: Absolute positions relative to viewport are scaled 4x (Game1.pixelZoom).
      // Positions relative to the map are not.

      center = ModMain.LocationToMap(Game1.player.currentLocation.uniqueName.Value ?? Game1.player.currentLocation.Name, Game1.player.getTileX(),
        Game1.player.getTileY(), Customizations.MapVectors, Customizations.LocationExclusions, true);

      // Player in unknown location, use previous location as center
      if (center.Equals(ModMain.UNKNOWN) && prevCenter != null)
        center = prevCenter;
      else
        prevCenter = center;
      playerLoc = prevCenter;

      center.X = NormalizeToMap(center.X);
      center.Y = NormalizeToMap(center.Y);

      // Top-left offset for markers, relative to the minimap
      mmLoc =
        new Vector2(mmX - center.X + (float)Math.Floor(mmWidth / 2.0),
          mmY - center.Y + (float)Math.Floor(mmHeight / 2.0));

      // Top-left corner of minimap cropped from the whole map
      // Centered around the player's location on the map
      cropX = center.X - (float)Math.Floor(mmWidth / 2.0);
      cropY = center.Y - (float)Math.Floor(mmHeight / 2.0);

      // Handle cases when reaching edge of map 
      // Change offsets accordingly when player is no longer centered
      if (cropX < 0)
      {
        center.X = mmWidth / 2;
        mmLoc.X = mmX;
        cropX = 0;
      }
      else if (cropX + mmWidth > 1200)
      {
        center.X = 1200 - mmWidth / 2;
        mmLoc.X = mmX - (1200 - mmWidth);
        cropX = 1200 - mmWidth;
      }

      if (cropY < 0)
      {
        center.Y = mmHeight / 2;
        mmLoc.Y = mmY;
        cropY = 0;
      }
      // Actual map is 1200x720 but map.Height includes the farms
      else if (cropY + mmHeight > 720)
      {
        center.Y = 720 - mmHeight / 2;
        mmLoc.Y = mmY - (720 - mmHeight);
        cropY = 720 - mmHeight;
      }
    }

    // Center or the player's position is used as reference; player is not center when reaching edge of map
    public void DrawMiniMap()
    {
      var b = Game1.spriteBatch;
      var IsHoveringMinimap = isHoveringDragZone();
      var offsetMmX = mmX + offset;

      // Make transparent on hover
      var color = IsHoveringMinimap
        ? Color.White * 0.25f
        : Color.White;

      if (IsHoveringMinimap)
      {
        Game1.mouseCursor = 2;
      }

      if (ModMain.Map == null) return;

      b.Draw(ModMain.Map, new Vector2(offsetMmX, mmY),
        new Rectangle((int)Math.Floor(cropX / Game1.pixelZoom),
          (int)Math.Floor(cropY / Game1.pixelZoom), mmWidth / Game1.pixelZoom + 2,
          mmHeight / Game1.pixelZoom + 2), color, 0f, Vector2.Zero,
        4f, SpriteEffects.None, 0.86f);

      // Don't draw markers while being dragged
      if (!(isHoveringDragZone() && ModMain.Helper.Input.GetState(SButton.MouseRight) == SButtonState.Held))
      {
        // When minimap is moved, redraw markers after recalculating & repositioning
        if (drawDelay == 0)
        {
          if (!IsHoveringMinimap)
            DrawMarkers();
        }
        else
          drawDelay--;
      }

      // Border around minimap that will also help mask markers outside of the minimap
      // Which gives more padding for when they are considered within the minimap area
      // Draw border
      DrawLine(b, new Vector2(offsetMmX, mmY - borderWidth), new Vector2(offsetMmX + mmWidth - 2, mmY - borderWidth), borderWidth,
        Game1.menuTexture, new Rectangle(8, 256, 3, borderWidth), color * 1.5f);
      DrawLine(b, new Vector2(offsetMmX + mmWidth + borderWidth, mmY),
        new Vector2(offsetMmX + mmWidth + borderWidth, mmY + mmHeight - 2), borderWidth, Game1.menuTexture,
        new Rectangle(8, 256, 3, borderWidth), color * 1.5f);
      DrawLine(b, new Vector2(offsetMmX + mmWidth, mmY + mmHeight + borderWidth),
        new Vector2(offsetMmX + 2, mmY + mmHeight + borderWidth), borderWidth, Game1.menuTexture,
        new Rectangle(8, 256, 3, borderWidth), color * 1.5f);
      DrawLine(b, new Vector2(offsetMmX - borderWidth, mmHeight + mmY), new Vector2(offsetMmX - borderWidth, mmY + 2), borderWidth,
        Game1.menuTexture, new Rectangle(8, 256, 3, borderWidth), color * 1.5f);

      // Draw the border corners
      b.Draw(Game1.menuTexture, new Rectangle(offsetMmX - borderWidth, mmY - borderWidth, borderWidth, borderWidth),
        new Rectangle(0, 256, borderWidth, borderWidth), color * 1.5f);
      b.Draw(Game1.menuTexture, new Rectangle(offsetMmX + mmWidth, mmY - borderWidth, borderWidth, borderWidth),
        new Rectangle(48, 256, borderWidth, borderWidth), color * 1.5f);
      b.Draw(Game1.menuTexture, new Rectangle(offsetMmX + mmWidth, mmY + mmHeight, borderWidth, borderWidth),
        new Rectangle(48, 304, borderWidth, borderWidth), color * 1.5f);
      b.Draw(Game1.menuTexture, new Rectangle(offsetMmX - borderWidth, mmY + mmHeight, borderWidth, borderWidth),
        new Rectangle(0, 304, borderWidth, borderWidth), color * 1.5f);
    }

    private void DrawMarkers()
    {
      var b = Game1.spriteBatch;
      var color = Color.White;
      var offsetMmX = mmX + offset;
      var offsetMmLoc = new Vector2(mmLoc.X + offset + 2, mmLoc.Y + 2);

      //
      // ===== Farm types =====
      //
      // The farms are always overlayed at (0, 43) on the map
      // The crop position, dimensions, and overlay position must all be adjusted accordingly
      // When any part of the cropped farm is outside of the minimap as player moves
      var farmWidth = 131;
      var farmHeight = 61;
      var farmX = NormalizeToMap(MathHelper.Clamp(offsetMmLoc.X, offsetMmX, offsetMmX + mmWidth));
      var farmY = NormalizeToMap(MathHelper.Clamp(offsetMmLoc.Y + 172, mmY, mmY + mmHeight) + 2);
      var farmCropX = (int)MathHelper.Clamp((offsetMmX - offsetMmLoc.X) / Game1.pixelZoom, 0, farmWidth);
      var farmCropY = (int)MathHelper.Clamp((mmY - offsetMmLoc.Y - 172) / Game1.pixelZoom, 0, farmHeight);

      // Check if farm crop extends outside of minimap
      var farmCropWidth = (farmX / Game1.pixelZoom + farmWidth > (offsetMmX + mmWidth) / Game1.pixelZoom) ? (int)((offsetMmX + mmWidth - farmX) / Game1.pixelZoom) : farmWidth - farmCropX;
      var farmCropHeight = (farmY / Game1.pixelZoom + farmHeight > (mmY + mmHeight) / Game1.pixelZoom) ? (int)((mmY + mmHeight - farmY) / Game1.pixelZoom) : farmHeight - farmCropY;

      // Check if farm crop extends beyond farm size
      if (farmCropX + farmCropWidth > farmWidth)
        farmCropWidth = farmWidth - farmCropX;

      if (farmCropY + farmCropHeight > farmHeight)
        farmCropHeight = farmHeight - farmCropY;

      switch (Game1.whichFarm)
      {
        case 1:
          b.Draw(ModMain.Map, new Vector2(farmX, farmY),
            new Rectangle(0 + farmCropX, 180 + farmCropY, farmCropWidth, farmCropHeight), color,
            0f,
            Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
          break;
        case 2:
          b.Draw(ModMain.Map, new Vector2(farmX, farmY),
            new Rectangle(131 + farmCropX, 180 + farmCropY, farmCropWidth, farmCropHeight), color,
            0f,
            Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
          break;
        case 3:
          b.Draw(ModMain.Map, new Vector2(farmX, farmY),
            new Rectangle(0 + farmCropX, 241 + farmCropY, farmCropWidth, farmCropHeight), color,
            0f,
            Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
          break;
        case 4:
          b.Draw(ModMain.Map, new Vector2(farmX, farmY),
            new Rectangle(131 + farmCropX, 241 + farmCropY, farmCropWidth, farmCropHeight), color,
            0f,
            Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
          break;
        case 5:
          b.Draw(ModMain.Map, new Vector2(farmX, farmY),
             new Rectangle(0 + farmCropX, 302 + farmCropY, farmCropWidth, farmCropHeight), color,
             0f,
            Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
          break;
        case 6:
          b.Draw(ModMain.Map, new Vector2(farmX, farmY),
            new Rectangle(131 + farmCropX, 302 + farmCropY, farmCropWidth, farmCropHeight), color,
            0f,
            Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
          break;
      }

      if (drawPamHouseUpgrade)
      {
        var houseLoc = ModMain.LocationToMap("Trailer_Big");
        if (IsWithinMapArea(houseLoc.X, houseLoc.Y))
          b.Draw(ModMain.Map, new Vector2(NormalizeToMap(offsetMmLoc.X + houseLoc.X - 16), NormalizeToMap(offsetMmLoc.Y + houseLoc.Y - 11)),
            new Rectangle(263, 181, 8, 8), color,
            0f, Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
      }

      if (drawMovieTheater || drawMovieTheaterJoja)
      {
        var theaterLoc = ModMain.LocationToMap("JojaMart");
        if (IsWithinMapArea(theaterLoc.X, theaterLoc.Y))
        {
          b.Draw(ModMain.Map, new Vector2(NormalizeToMap(offsetMmLoc.X + theaterLoc.X - 20), NormalizeToMap(offsetMmLoc.Y + theaterLoc.Y - 11)),
            new Rectangle(275, 181, 15, 11), color,
            0f, Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
        }
      }

      if (drawIsland)
      {
        var islandWidth = 40;
        var islandHeight = 30;
        var islandImageX = 208;
        var islandImageY = 363;
        var mapX = 1040;
        var mapY = 600;

        if (ModMain.Globals.UseDetailedIsland)
        {
          islandWidth = 45;
          islandHeight = 40;
          islandImageX = 248;
          islandImageY = 363;
          mapX = 1020;
          mapY = 560;
        }

        var islandX = NormalizeToMap(MathHelper.Clamp(offsetMmLoc.X + mapX, offsetMmX, offsetMmX + mmWidth));
        var islandY = NormalizeToMap(MathHelper.Clamp(offsetMmLoc.Y + mapY, mmY, mmY + mmHeight) + 2);
        var islandCropX = (int)MathHelper.Clamp((offsetMmX - offsetMmLoc.X - mapX) / Game1.pixelZoom, 0, islandWidth);
        var islandCropY = (int)MathHelper.Clamp((mmY - offsetMmLoc.Y - mapY) / Game1.pixelZoom, 0, islandHeight);

        // Check if island crop extends outside of minimap
        var islandCropWidth = (islandX / Game1.pixelZoom + islandWidth > (offsetMmX + mmWidth) / Game1.pixelZoom) ? (int)((offsetMmX + mmWidth - islandX) / Game1.pixelZoom) : islandWidth - islandCropX;
        var islandCropHeight = (islandY / Game1.pixelZoom + islandHeight > (mmY + mmHeight) / Game1.pixelZoom) ? (int)((mmY + mmHeight - islandY) / Game1.pixelZoom) : islandHeight - islandCropY;

        // Check if island crop extends beyond island size
        if (islandCropX + islandCropWidth > islandWidth)
          islandCropWidth = islandWidth - islandCropX;

        if (islandCropY + islandCropHeight > islandHeight)
          islandCropHeight = islandHeight - islandCropY;

        b.Draw(ModMain.Map, new Vector2(islandX, islandY), new Rectangle(islandImageX + islandCropX, islandImageY + islandCropY, islandCropWidth, islandCropHeight), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
      }

      //
      // ===== Farm buildings =====
      //
      if (ModMain.Globals.ShowFarmBuildings && FarmBuildings != null && BuildingMarkers != null)
      {
        var sortedBuildings = FarmBuildings.ToList();
        sortedBuildings.Sort((x, y) => x.Value.Value.Y.CompareTo(y.Value.Value.Y));

        foreach (var building in sortedBuildings)
        {
          if (ModConstants.FarmBuildingRects.TryGetValue(building.Value.Key, out var buildingRect))
          {
            if (IsWithinMapArea(building.Value.Value.X - buildingRect.Width / 2,
              building.Value.Value.Y - buildingRect.Height / 2))
            {
              //  if (Customizations.MapName == "starblue_map")
              //   buildingRect.Y = 7;
              //  else if (Customizations.MapName == "eemie_recolour_map")
              //    buildingRect.Y = 14;

              b.Draw(
                BuildingMarkers,
                new Vector2(
                  NormalizeToMap(offsetMmLoc.X + building.Value.Value.X - (float)Math.Floor(buildingRect.Width / 2.0)),
                  NormalizeToMap(offsetMmLoc.Y + building.Value.Value.Y - (float)Math.Floor(buildingRect.Height / 2.0))
                ),
                buildingRect, color, 0f, Vector2.Zero, 3f, SpriteEffects.None, 1f
              );
            }
          }
        }
      }

      //
      // ===== Traveling Merchant =====
      //
      if (ModMain.Globals.ShowTravelingMerchant && ConditionalNpcs["Merchant"])
      {
        Vector2 merchantLoc = new Vector2(ModConstants.MapVectors["Merchant"][0].MapX, ModConstants.MapVectors["Merchant"][0].MapY);
        if (IsWithinMapArea(merchantLoc.X - 16, merchantLoc.Y - 16))
          b.Draw(Game1.mouseCursors, new Vector2(NormalizeToMap(offsetMmLoc.X + merchantLoc.X - 16), NormalizeToMap(offsetMmLoc.Y + merchantLoc.Y - 15)), new Rectangle(191, 1410, 22, 21), Color.White, 0f, Vector2.Zero, 1.3f, SpriteEffects.None,
            1f);
      }

      //
      // ===== NPCs =====
      //
      // Sort by drawing order
      if (NpcMarkers != null)
      {
        var sortedMarkers = NpcMarkers.ToList();
        sortedMarkers.Sort((x, y) => x.Value.Layer.CompareTo(y.Value.Layer));

        foreach (var npcMarker in sortedMarkers)
        {
          var name = npcMarker.Key;
          var marker = npcMarker.Value;

          // Skip if no specified location
          if (marker.Sprite == null
              || !IsWithinMapArea(marker.MapX, marker.MapY)
              || ModMain.Globals.NpcExclusions.Contains(name)
              || (!ModMain.Globals.ShowHiddenVillagers && marker.IsHidden)
              || (ConditionalNpcs.ContainsKey(name) && !ConditionalNpcs[name])
          )
            continue;

          var markerColor = marker.IsHidden ? Color.DarkGray * 0.7f : Color.White;

          // Draw NPC marker
          var spriteRect = marker.Type == Character.Horse ? new Rectangle(17, 104, 16, 14) : new Rectangle(0, marker.CropOffset, 16, 15);

          if (marker.Type == Character.Horse)
          {
            b.Draw(marker.Sprite,
              new Rectangle(NormalizeToMap(offsetMmLoc.X + marker.MapX),
                NormalizeToMap(offsetMmLoc.Y + marker.MapY),
                30, 32),
                spriteRect, markerColor);
          }
          else
          {
            b.Draw(marker.Sprite,
              new Rectangle(NormalizeToMap(offsetMmLoc.X + marker.MapX),
                NormalizeToMap(offsetMmLoc.Y + marker.MapY),
                30, 32),
                spriteRect, markerColor);
          }

          // Icons for birthday/quest
          if (ModMain.Globals.ShowQuests)
          {
            if (marker.IsBirthday && (Game1.player.friendshipData.ContainsKey(name) && Game1.player.friendshipData[name].GiftsToday == 0))
              b.Draw(Game1.mouseCursors,
                new Vector2(NormalizeToMap(offsetMmLoc.X + marker.MapX + 20),
                  NormalizeToMap(offsetMmLoc.Y + marker.MapY)),
                new Rectangle(147, 412, 10, 11), markerColor, 0f, Vector2.Zero, 1.8f,
                SpriteEffects.None,
                0f);

            if (marker.HasQuest)
              b.Draw(Game1.mouseCursors,
                new Vector2(NormalizeToMap(offsetMmLoc.X + marker.MapX + 22),
                  NormalizeToMap(offsetMmLoc.Y + marker.MapY - 3)),
                new Rectangle(403, 496, 5, 14), markerColor, 0f, Vector2.Zero, 1.8f, SpriteEffects.None,
                0f);
          }
        }
      }

      if (Context.IsMultiplayer)
      {
        foreach (var farmer in Game1.getOnlineFarmers())
        {
          // Temporary solution to handle desync of farmhand location/tile position when changing location
          if (FarmerMarkers.TryGetValue(farmer.UniqueMultiplayerID, out var farMarker))
          {
            if (IsWithinMapArea(farMarker.MapX - 16, farMarker.MapY - 15) && farMarker.DrawDelay == 0)
            {
              farmer.FarmerRenderer.drawMiniPortrat(b,
                new Vector2(NormalizeToMap(offsetMmLoc.X + farMarker.MapX - 16),
                  NormalizeToMap(offsetMmLoc.Y + farMarker.MapY - 15)),
                0.00011f, 2f, 1, farmer);
            }
          }
        }
      }
      else
      {
        Game1.player.FarmerRenderer.drawMiniPortrat(b,
          new Vector2(NormalizeToMap(offsetMmLoc.X + playerLoc.X - 16), NormalizeToMap(offsetMmLoc.Y + playerLoc.Y - 15)), 0.00011f,
          2f, 1,
          Game1.player);
      }
    }

    // Normalize offset differences caused by map being 4x less precise than ModMain.Map markers 
    // Makes the map and markers move together instead of markers moving more precisely (moving when minimap does not shift)
    private int NormalizeToMap(float n)
    {
      return (int)Math.Floor(n / Game1.pixelZoom) * Game1.pixelZoom;
    }

    // Check if within map
    private bool IsWithinMapArea(float x, float y)
    {
      return x > center.X - mmWidth / 2 - (Game1.tileSize / 4)
             && x < center.X + mmWidth / 2 - (Game1.tileSize / 4)
             && y > center.Y - mmHeight / 2 - (Game1.tileSize / 4)
             && y < center.Y + mmHeight / 2 - (Game1.tileSize / 4);
    }

    // For borders
    private void DrawLine(SpriteBatch b, Vector2 begin, Vector2 end, int width, Texture2D tex, Rectangle srcRect, Color color)
    {
      var r = new Rectangle((int)begin.X, (int)begin.Y, (int)(end - begin).Length() + 2, width);
      var v = Vector2.Normalize(begin - end);
      var angle = (float)Math.Acos(Vector2.Dot(v, -Vector2.UnitX));
      if (begin.Y > end.Y) angle = MathHelper.TwoPi - angle;
      b.Draw(tex, r, srcRect, color, angle, Vector2.Zero, SpriteEffects.None, 0);
    }
  }
}