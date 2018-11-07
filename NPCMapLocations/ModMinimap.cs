using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace NPCMapLocations
{
	internal class ModMinimap
	{
		private readonly Texture2D BuildingMarkers;
    private readonly ModConfig Config;
		private readonly int borderWidth = 12;
		private readonly bool drawPamHouseUpgrade;
		private readonly Dictionary<long, CharacterMarker> FarmerMarkers;
		private readonly IModHelper Helper;
	  private readonly string MapName;

	  private readonly Dictionary<string, MapVector[]> CustomMapLocations;
	  private readonly Texture2D CustomMarkerTex;

    public bool isBeingDragged;
		private readonly Texture2D map;
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
		private readonly HashSet<CharacterMarker> NpcMarkers;
		private Vector2 playerLoc;
		private int prevMmX;
		private int prevMmY;
		private int prevMouseX;
		private int prevMouseY;
	  private int drawDelay = 0;

		public ModMinimap(
			HashSet<CharacterMarker> npcMarkers,
			Dictionary<string, bool> secondaryNpcs,
			Dictionary<long, CharacterMarker> farmerMarkers,
			Dictionary<string, int> MarkerCropOffsets,
			Dictionary<string, KeyValuePair<string, Vector2>> farmBuildings,
			Texture2D buildingMarkers,
			IModHelper helper,
			ModConfig config,
      string mapName = null,
      Dictionary<string, MapVector[]> customMapLocations = null,
			Texture2D CustomMarkerTex = null
    )
		{
			this.NpcMarkers = npcMarkers;
		  this.SecondaryNpcs = secondaryNpcs;
		  this.FarmerMarkers = farmerMarkers;
			this.MarkerCropOffsets = MarkerCropOffsets;
		  this.FarmBuildings = farmBuildings;
		  this.BuildingMarkers = buildingMarkers;
		  this.Helper = helper;
		  this.Config = config;
		  this.MapName = mapName;
		  this.CustomMapLocations = customMapLocations;
		  this.CustomMarkerTex = CustomMarkerTex;

      map = Game1.content.Load<Texture2D>("LooseSprites\\map");
			drawPamHouseUpgrade = Game1.MasterPlayer.mailReceived.Contains("pamHouseUpgrade");

			mmX = Config.MinimapX;
			mmY = Config.MinimapY;
			mmWidth = Config.MinimapWidth * Game1.pixelZoom;
			mmHeight = Config.MinimapHeight * Game1.pixelZoom;
		}

		private Dictionary<string, bool> SecondaryNpcs { get; }
		private Dictionary<string, int> MarkerCropOffsets { get; }
		private Dictionary<string, KeyValuePair<string, Vector2>> FarmBuildings { get; }

		public void HandleMouseDown()
		{
		  if (Game1.getMouseX() > mmX - borderWidth && Game1.getMouseX() < mmX + mmWidth + borderWidth &&
		      Game1.getMouseY() > mmY - borderWidth && Game1.getMouseY() < mmY + mmHeight + borderWidth)
		  {
		    isBeingDragged = true;
		    prevMmX = mmX;
		    prevMmY = mmY;
		    prevMouseX = Game1.getMouseX();
		    prevMouseY = Game1.getMouseY();
		  }
		}

		public void HandleMouseRelease()
		{
		  if (Game1.getMouseX() > mmX - borderWidth && Game1.getMouseX() < mmX + mmWidth + borderWidth &&
		      Game1.getMouseY() > mmY - borderWidth && Game1.getMouseY() < mmY + mmHeight + borderWidth)
		  {
		    isBeingDragged = false;
		    Config.MinimapX = mmX;
		    Config.MinimapY = mmY;
		    Helper.WriteJsonFile($"config/{Constants.SaveFolderName}.json", Config);
		    drawDelay = 30;
		  }
		}

	  public void CheckOffsetForMap()
	  {
	    // When map is smaller than viewport (ex. Bus Stop)
	    if (Game1.isOutdoorMapSmallerThanViewport())
	      offset = (int)(Game1.viewport.Width - Game1.currentLocation.map.Layers[0].LayerWidth * Game1.tileSize) / 2;
	    else
	      offset = 0;
    }

		public void Resize()
		{
			mmWidth = Config.MinimapWidth * Game1.pixelZoom;
			mmHeight = Config.MinimapHeight * Game1.pixelZoom;
		}

    public void Update()
		{
      // Note: Absolute positions relative to viewport are scaled 4x (Game1.pixelZoom).
      // Positions relative to the map (the map image) are not.

		  center = ModMain.LocationToMap(Game1.player.currentLocation.uniqueName.Value ?? Game1.player.currentLocation.Name, Game1.player.getTileX(),
				Game1.player.getTileY(), CustomMapLocations, true);

      // Player in unknown location, use previous location as center
		  if (center.X < 0 && prevCenter != null)
		    center = prevCenter;
		  else
		    prevCenter = center;
			playerLoc = prevCenter;

			center.X = NormalizeToMap(center.X);
			center.Y = NormalizeToMap(center.Y);

			// Top-left offset for markers, relative to the minimap
			mmLoc =
				new Vector2(mmX - center.X + (float) Math.Floor(mmWidth / 2.0),
					mmY - center.Y + (float) Math.Floor(mmHeight / 2.0));

			// Top-left corner of minimap cropped from the whole map
			// Centered around the player's location on the map
			cropX = center.X - (float) Math.Floor(mmWidth / 2.0);
			cropY = center.Y - (float) Math.Floor(mmHeight / 2.0);

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
		  var IsHoveringMinimap = false;
		  var offsetMmX = mmX + offset;

      // Move minimap along with mouse when held down
      if (Game1.getMouseX() > offsetMmX - borderWidth && Game1.getMouseX() < offsetMmX + mmWidth + borderWidth &&
			    Game1.getMouseY() > mmY - borderWidth && Game1.getMouseY() < mmY + mmHeight + borderWidth)
			{
        IsHoveringMinimap = true;

        if (ModMain.HeldKey.ToString().Equals(Config.MinimapDragKey))
			    Game1.mouseCursor = 2;

        if (isBeingDragged)
				{
					mmX = NormalizeToMap(MathHelper.Clamp(prevMmX + Game1.getMouseX() - prevMouseX, borderWidth,
						Game1.viewport.Width - mmWidth - borderWidth));
					mmY = NormalizeToMap(MathHelper.Clamp(prevMmY + Game1.getMouseY() - prevMouseY, borderWidth,
						Game1.viewport.Height - mmHeight - borderWidth));
				}
			}

      // Make transparent on hover
			var b = Game1.spriteBatch;
			var color = IsHoveringMinimap
				? Color.White * 0.5f
				: Color.White;

      // Draw map
			b.Draw(map, new Vector2(offsetMmX, mmY),
				new Rectangle((int) Math.Floor(cropX / Game1.pixelZoom),
					(int) Math.Floor(cropY / Game1.pixelZoom), mmWidth / Game1.pixelZoom + 2,
					mmHeight / Game1.pixelZoom + 2), color, 0f, Vector2.Zero,
				4f, SpriteEffects.None, 0.86f);

      // Don't draw markers while being dragged
		  if (!isBeingDragged)
		  {
		    // When minimap is moved, redraw markers after recalculating & repositioning
		    if (drawDelay == 0)
		    {
		      if (!IsHoveringMinimap)
		        DrawMarkers(b);
		    }
		    else
		      drawDelay--;
		  }

		  // Border around minimap that will also help mask markers outside of the minimap
			// Which gives more padding for when they are considered within the minimap area
			// Draw border
			DrawLine(b, new Vector2(offsetMmX, mmY - borderWidth), new Vector2(offsetMmX + mmWidth - 2, mmY - borderWidth), borderWidth,
				Game1.menuTexture, new Rectangle(8, 256, 3, borderWidth), color*1.5f);
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

		private void DrawMarkers(SpriteBatch b)
		{
			var color = Color.White;
		  var offsetMmX = mmX + offset;
      var offsetMmLoc = new Vector2(mmLoc.X + offset, mmLoc.Y);

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
		  var farmCropX = (int)MathHelper.Clamp((offsetMmX - offsetMmLoc.X)/Game1.pixelZoom, 0, farmWidth);
		  var farmCropY = (int)MathHelper.Clamp((mmY - offsetMmLoc.Y - 172)/Game1.pixelZoom, 0, farmHeight);

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
		        b.Draw(map, new Vector2(farmX, farmY),
		          new Rectangle(0 + farmCropX, 180 + farmCropY, farmCropWidth, farmCropHeight), color,
		          0f,
		          Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
		        break;
		      case 2:
		        b.Draw(map, new Vector2(farmX, farmY),
		          new Rectangle(131 + farmCropX, 180 + farmCropY, farmCropWidth, farmCropHeight), color,
		          0f,
		          Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
		        break;
		      case 3:
		        b.Draw(map, new Vector2(farmX, farmY),
		          new Rectangle(0 + farmCropX, 241 + farmCropY, farmCropWidth, farmCropHeight), color,
		          0f,
		          Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
		        break;
		      case 4:
		        b.Draw(map, new Vector2(farmX, farmY),
		          new Rectangle(131 + farmCropX,
		            241 + farmCropY, farmCropWidth, farmCropHeight), color,
		          0f,
		          Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
		        break;
		    }

      //
      // ===== Pam house upgrade =====
      //
      if (drawPamHouseUpgrade)
			{
				var pamHouseX = ModConstants.MapVectors["Trailer_Big"][0].MapX;
				var pamHouseY = ModConstants.MapVectors["Trailer_Big"][0].MapY;
				if (IsWithinMapArea(pamHouseX, pamHouseY))
					b.Draw(map, new Vector2(NormalizeToMap(offsetMmLoc.X + pamHouseX - 13), NormalizeToMap(offsetMmLoc.Y + pamHouseY - 16)),
						new Rectangle(263, 181, 8, 8), color,
						0f, Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
			}

      //
      // ===== Farm buildings =====
      //
      if (Config.ShowFarmBuildings && FarmBuildings != null)
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
			        if (MapName == "starblue_map")
			          buildingRect.Y = 7;
			        else if (MapName == "eemie_recolour_map")
			          buildingRect.Y = 14;

			        b.Draw(
			          BuildingMarkers,
			          new Vector2(
			            NormalizeToMap(offsetMmLoc.X + building.Value.Value.X - (float) Math.Floor(buildingRect.Width / 2.0)),
			            NormalizeToMap(offsetMmLoc.Y + building.Value.Value.Y - (float) Math.Floor(buildingRect.Height / 2.0))
			          ),
			          buildingRect, color, 0f, Vector2.Zero, 3f, SpriteEffects.None, 1f
			        );
			      }
			    }
			  }
			}

      //
      // ===== Custom locations =====
      //
      if (Config.CustomMapMarkers != null)
		  {
		    foreach (var location in Config.CustomMapMarkers)
		    {
		      if (CustomMapLocations.TryGetValue(location.Key, out var locationVector) && Config.CustomMapMarkers.TryGetValue(location.Key, out var locationRects))
		      {
		        var fromAreaRect = locationRects.GetValue("FromArea");
		        var toAreaRect = locationRects.GetValue("ToArea");
            var srcRect = new Rectangle(fromAreaRect.Value<int>("X"), fromAreaRect.Value<int>("Y"),
		          fromAreaRect.Value<int>("Width"), fromAreaRect.Value<int>("Height"));
		        var locationX = toAreaRect.Value<int>("X");
		        var locationY = toAreaRect.Value<int>("Y");

            // If only one Vector specified, treat it as a marker
            // Markers are centered based on width/height
            if (locationVector.Length == 1)
		        {
		          b.Draw(
		            CustomMarkerTex,
		            new Vector2(offsetMmLoc.X + locationX, offsetMmLoc.Y + locationY),
		            srcRect, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f
		          );

		        }
		        // If more than one Vector, treat it as a region with lower & upper bound
		        // Regions are draw by the top-left corner
		        else if (locationVector.Length > 1)
            {
		          var regionWidth = srcRect.Width;
		          var regionHeight = srcRect.Height;
		          var regionX = NormalizeToMap(MathHelper.Clamp(offsetMmLoc.X + locationX, offsetMmX, offsetMmX + mmWidth));
		          var regionY = NormalizeToMap(MathHelper.Clamp(offsetMmLoc.Y + locationY, mmY, mmY + mmHeight) + 2);
		          var regionCropX = (int)MathHelper.Clamp((offsetMmX - offsetMmLoc.X - locationX) / Game1.pixelZoom, 0, farmWidth);
		          var regionCropY = (int)MathHelper.Clamp((mmY - offsetMmLoc.Y - locationY) / Game1.pixelZoom, 0, farmHeight);

		          // Check if region crop extends outside of minimap
		          var regionCropWidth = (regionX / Game1.pixelZoom + regionWidth > (offsetMmX + mmWidth) / Game1.pixelZoom) ? (int)((offsetMmX + mmWidth - regionX) / Game1.pixelZoom) : regionWidth - regionCropX;
		          var regionCropHeight = (regionY / Game1.pixelZoom + regionHeight > (mmY + mmHeight) / Game1.pixelZoom) ? (int)((mmY + mmHeight - regionY) / Game1.pixelZoom) : regionHeight - regionCropY;

		          // Check if region crop extends beyond region size
		          if (regionCropX + regionCropWidth > regionWidth)
		            regionCropWidth = regionWidth - regionCropX;

		          if (regionCropY + regionCropHeight > regionHeight)
		            regionCropHeight = regionHeight - regionCropY;

		          b.Draw(CustomMarkerTex, new Vector2(regionX, regionY),
		            new Rectangle(srcRect.X + regionCropX, srcRect.Y + regionCropY, regionCropWidth, regionCropHeight), color,
		            0f,
		            Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
		        }
		      }
        }
		  }

      //
      // ===== Traveling Merchant =====
      //
      if (Config.ShowTravelingMerchant && SecondaryNpcs["Merchant"])
			{
			  Vector2 merchantLoc = new Vector2(ModConstants.MapVectors["Merchant"][0].MapX, ModConstants.MapVectors["Merchant"][0].MapY);
        if (IsWithinMapArea(merchantLoc.X - 16, merchantLoc.Y - 16))
					b.Draw(Game1.mouseCursors, new Vector2(NormalizeToMap(offsetMmLoc.X + merchantLoc.X - 16), NormalizeToMap(offsetMmLoc.Y + merchantLoc.Y - 15)), new Rectangle(191, 1410, 22, 21), Color.White, 0f, Vector2.Zero, 1.3f, SpriteEffects.None,
						1f);
			}

				if (Context.IsMultiplayer)
				{
					foreach (var farmer in Game1.getOnlineFarmers())
					{
						// Temporary solution to handle desync of farmhand location/tile position when changing location
						if (FarmerMarkers.TryGetValue(farmer.UniqueMultiplayerID, out var farMarker))
						{
						  if (farMarker.MapLocation.X < 0) continue;
							if (farMarker.DrawDelay == 0 &&
							    IsWithinMapArea(farMarker.MapLocation.X - 16, farMarker.MapLocation.Y - 15))
							{
								farmer.FarmerRenderer.drawMiniPortrat(b,
									new Vector2(NormalizeToMap(offsetMmLoc.X + farMarker.MapLocation.X - 16),
										NormalizeToMap(offsetMmLoc.Y + farMarker.MapLocation.Y - 15)),
									0.00011f, 2f, 1, farmer);
							}
						}
					}
				}
				else
				{
          if (playerLoc.X >= 0)
					  Game1.player.FarmerRenderer.drawMiniPortrat(b,
						  new Vector2(NormalizeToMap(offsetMmLoc.X + playerLoc.X - 16), NormalizeToMap(offsetMmLoc.Y + playerLoc.Y - 15)), 0.00011f,
						  2f, 1,
						  Game1.player);
				}
        
      //
      // ===== NPCs =====
      //
      // Sort by drawing order
		  if (NpcMarkers != null)
		  {
        var sortedMarkers = NpcMarkers.ToList();
        sortedMarkers.Sort((x, y) => x.Layer.CompareTo(y.Layer));

        foreach (var npcMarker in sortedMarkers)
        {
          // Skip if no specified location
          if (npcMarker.MapLocation.X < 0 || npcMarker.Marker == null ||
              !MarkerCropOffsets.ContainsKey(npcMarker.Npc.Name) ||
              !IsWithinMapArea(npcMarker.MapLocation.X, npcMarker.MapLocation.Y))
            continue;

          // Tint/dim hidden markers
          if (npcMarker.IsHidden)
          {
            b.Draw(npcMarker.Marker,
              new Rectangle(NormalizeToMap(offsetMmLoc.X + npcMarker.MapLocation.X),
                NormalizeToMap(offsetMmLoc.Y + npcMarker.MapLocation.Y),
                32, 30),
              new Rectangle(0, MarkerCropOffsets[npcMarker.Npc.Name], 16, 15), Color.DimGray * 0.7f);
            if (npcMarker.IsBirthday)
              b.Draw(Game1.mouseCursors,
                new Vector2(NormalizeToMap(offsetMmLoc.X + npcMarker.MapLocation.X + 20),
                  NormalizeToMap(offsetMmLoc.Y + npcMarker.MapLocation.Y)),
                new Rectangle(147, 412, 10, 11), Color.DimGray * 0.7f, 0f, Vector2.Zero, 1.8f,
                SpriteEffects.None, 0f);

            if (npcMarker.HasQuest)
              b.Draw(Game1.mouseCursors,
                new Vector2(NormalizeToMap(offsetMmLoc.X + npcMarker.MapLocation.X + 22),
                  NormalizeToMap(offsetMmLoc.Y + npcMarker.MapLocation.Y - 3)),
                new Rectangle(403, 496, 5, 14), Color.DimGray * 0.7f, 0f, Vector2.Zero, 1.8f,
                SpriteEffects.None, 0f);
          }
          else
          {
            b.Draw(npcMarker.Marker,
              new Rectangle(NormalizeToMap(offsetMmLoc.X + npcMarker.MapLocation.X),
                NormalizeToMap(offsetMmLoc.Y + npcMarker.MapLocation.Y),
                30, 32),
              new Rectangle(0, MarkerCropOffsets[npcMarker.Npc.Name], 16, 15), Color.White);
            if (npcMarker.IsBirthday)
              b.Draw(Game1.mouseCursors,
                new Vector2(NormalizeToMap(offsetMmLoc.X + npcMarker.MapLocation.X + 20),
                  NormalizeToMap(offsetMmLoc.Y + npcMarker.MapLocation.Y)),
                new Rectangle(147, 412, 10, 11), Color.White, 0f, Vector2.Zero, 1.8f,
                SpriteEffects.None,
                0f);

            if (npcMarker.HasQuest)
              b.Draw(Game1.mouseCursors,
                new Vector2(NormalizeToMap(offsetMmLoc.X + npcMarker.MapLocation.X + 22),
                  NormalizeToMap(offsetMmLoc.Y + npcMarker.MapLocation.Y - 3)),
                new Rectangle(403, 496, 5, 14), Color.White, 0f, Vector2.Zero, 1.8f, SpriteEffects.None,
                0f);
          }
        }
      }
		}

		// Normalize offset differences caused by map being 4x less precise than map markers 
		// Makes the map and markers move together instead of markers moving more precisely (moving when minimap does not shift)
		private int NormalizeToMap(float n)
		{
			return (int) Math.Floor(n / Game1.pixelZoom) * Game1.pixelZoom;
		}

		// Check if within map
		private bool IsWithinMapArea(float x, float y)
		{
			return x > center.X - mmWidth / 2 - (Game1.tileSize / 4 + 2)
			       && x < center.X + mmWidth / 2 - (Game1.tileSize / 4 + 2)
			       && y > center.Y - mmHeight / 2 - (Game1.tileSize / 4 + 2)
			       && y < center.Y + mmHeight / 2 - (Game1.tileSize / 4 + 2);
		}

		// For borders
		private void DrawLine(SpriteBatch b, Vector2 begin, Vector2 end, int width, Texture2D tex, Rectangle srcRect, Color color)
		{
			var r = new Rectangle((int) begin.X, (int) begin.Y, (int) (end - begin).Length() + 2, width);
			var v = Vector2.Normalize(begin - end);
			var angle = (float) Math.Acos(Vector2.Dot(v, -Vector2.UnitX));
			if (begin.Y > end.Y) angle = MathHelper.TwoPi - angle;
			b.Draw(tex, r, srcRect, color, angle, Vector2.Zero, SpriteEffects.None, 0);
		}
	}
}