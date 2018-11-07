/*
MapPage for the mod that handles logic for tooltips
and drawing everything.
Based on regurgitated game code.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NPCMapLocations
{
	public class ModMapPage : MapPage
	{
		private readonly IModHelper Helper;
		private readonly ModConfig Config;
		private readonly Dictionary<string, string> CustomNames;
		private Dictionary<string, bool> SecondaryNpcs { get; }
		private HashSet<CharacterMarker> NpcMarkers;
		private Dictionary<long, CharacterMarker> FarmerMarkers;
		private Dictionary<string, int> MarkerCropOffsets { get; }
		private Dictionary<string, KeyValuePair<string, Vector2>> FarmBuildings { get; }
    private readonly Texture2D BuildingMarkers;
	  private readonly string MapName;
		private string hoveredNames = "";
		private string hoveredLocationText = "";
		private Texture2D map;
		private int mapX;
		private int mapY;
		private bool hasIndoorCharacter;
		private Vector2 indoorIconVector;
		private bool drawPamHouseUpgrade;

	  private readonly Dictionary<string, MapVector[]> CustomMapLocations;
	  private readonly Texture2D CustomMarkerTex;

    // Map menu that uses modified map page and modified component locations for hover
    public ModMapPage(
			HashSet<CharacterMarker> npcMarkers,
			Dictionary<string, string> npcNames,
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
    ) : base(Game1.viewport.Width / 2 - (800 + IClickableMenu.borderWidth * 2) / 2,
			Game1.viewport.Height / 2 - (600 + IClickableMenu.borderWidth * 2) / 2, 800 + IClickableMenu.borderWidth * 2,
			600 + IClickableMenu.borderWidth * 2)
		{
			this.NpcMarkers = npcMarkers;
			this.CustomNames = npcNames;
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
			Vector2 center = Utility.getTopLeftPositionForCenteringOnScreen(map.Bounds.Width * 4, 720, 0, 0);
			mapX = (int) center.X;
			mapY = (int) center.Y;

			var regionRects = RegionRects().ToList();
			for (int i = 0; i < this.points.Count; i++)
			{
				var rect = regionRects.ElementAt(i);
				this.points[i].bounds = new Rectangle(
					// Snaps the cursor to the center instead of bottom right (default)
					(int) (mapX + ModMain.LocationToMap(rect.Key).X - rect.Value.Width / 2),
					(int) (mapY + ModMain.LocationToMap(rect.Key).Y - rect.Value.Height / 2),
					rect.Value.Width,
					rect.Value.Height
				);
			}

		  var a = this.points;
		}

		public override void performHoverAction(int x, int y)
		{
			var f = points;
			hoveredLocationText = "";
			hoveredNames = "";
			hasIndoorCharacter = false;
			foreach (ClickableComponent current in points)
			{
				if (current.containsPoint(x, y))
				{
					hoveredLocationText = current.name;
					break;
				}
			}

			List<string> hoveredList = new List<string>();

			const int markerWidth = 32;
			const int markerHeight = 30;

			// Have to use special character to separate strings for Chinese
			string separator = LocalizedContentManager.CurrentLanguageCode.Equals(LocalizedContentManager.LanguageCode.zh)
				? "，"
				: ", ";

		  if (NpcMarkers != null)
		  {
		    foreach (CharacterMarker npcMarker in this.NpcMarkers)
		    {
		      Vector2 npcLocation = new Vector2(mapX + npcMarker.MapLocation.X, mapY + npcMarker.MapLocation.Y);
		      if (Game1.getMouseX() >= npcLocation.X && Game1.getMouseX() <= npcLocation.X + markerWidth &&
		          Game1.getMouseY() >= npcLocation.Y && Game1.getMouseY() <= npcLocation.Y + markerHeight)
		      {
		        if (this.CustomNames.ContainsKey(npcMarker.Npc.Name) && !npcMarker.IsHidden)
		          hoveredList.Add(this.CustomNames[npcMarker.Npc.Name]);

		        if (!npcMarker.IsOutdoors && !hasIndoorCharacter)
		          hasIndoorCharacter = true;
		      }
		    }
		  }

		  if (Context.IsMultiplayer)
			{
				foreach (CharacterMarker farMarker in FarmerMarkers.Values)
				{
					Vector2 farmerLocation = new Vector2(mapX + farMarker.MapLocation.X, mapY + farMarker.MapLocation.Y);
             if (Game1.getMouseX() >= farmerLocation.X - markerWidth / 2
					    && Game1.getMouseX() <= farmerLocation.X + markerWidth / 2
					    && Game1.getMouseY() >= farmerLocation.Y - markerHeight / 2
					    && Game1.getMouseY() <= farmerLocation.Y + markerHeight / 2)
					{
						hoveredList.Add(farMarker.Name);

						if (!farMarker.IsOutdoors && !hasIndoorCharacter)
							hasIndoorCharacter = true;
					}
				}
			}

			if (hoveredList.Count > 0)
			{
				hoveredNames = hoveredList[0];
				for (int i = 1; i < hoveredList.Count; i++)
				{
					var lines = hoveredNames.Split('\n');
					if ((int) Game1.smallFont.MeasureString(lines[lines.Length - 1] + separator + hoveredList[i]).X >
					    (int) Game1.smallFont.MeasureString("Home of Robin, Demetrius, Sebastian & Maru").X) // Longest string
					{
						hoveredNames += separator + Environment.NewLine;
						hoveredNames += hoveredList[i];
					}
					else
					{
						hoveredNames += separator + hoveredList[i];
					}
				}
			}
		}

		// Draw location and name tooltips
		public override void draw(SpriteBatch b)
		{
			DrawMap(b);
			DrawMarkers(b);

      int x = Game1.getMouseX() + Game1.tileSize / 2;
      int y = Game1.getMouseY() + Game1.tileSize / 2;
      int width;
      int height;
      int offsetY = 0;

      this.performHoverAction(x - Game1.tileSize / 2, y - Game1.tileSize / 2);

      if (!hoveredLocationText.Equals(""))
      {
        IClickableMenu.drawHoverText(b, hoveredLocationText, Game1.smallFont, 0, 0, -1, null, -1, null, null, 0, -1, -1,
          -1, -1, 1f, null);
        int textLength = (int)Game1.smallFont.MeasureString(hoveredLocationText).X + Game1.tileSize / 2;
        width = Math.Max((int)Game1.smallFont.MeasureString(hoveredLocationText).X + Game1.tileSize / 2, textLength);
        height = (int)Math.Max(60, Game1.smallFont.MeasureString(hoveredLocationText).Y + Game1.tileSize / 2);
        if (x + width > Game1.viewport.Width)
        {
          x = Game1.viewport.Width - width;
          y += Game1.tileSize / 4;
        }

        if (this.Config.NameTooltipMode == 1)
        {
          if (y + height > Game1.viewport.Height)
          {
            x += Game1.tileSize / 4;
            y = Game1.viewport.Height - height;
          }

          offsetY = 2 - Game1.tileSize;
        }
        else if (this.Config.NameTooltipMode == 2)
        {
          if (y + height > Game1.viewport.Height)
          {
            x += Game1.tileSize / 4;
            y = Game1.viewport.Height - height;
          }

          offsetY = height - 4;
        }
        else
        {
          if (y + height > Game1.viewport.Height)
          {
            x += Game1.tileSize / 4;
            y = Game1.viewport.Height - height;
          }
        }

        // Draw name tooltip positioned around location tooltip
        DrawNames(b, hoveredNames, x, y, offsetY, height, this.Config.NameTooltipMode);

        // Draw location tooltip
        IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, y, width, height,
          Color.White, 1f, false);
        b.DrawString(Game1.smallFont, hoveredLocationText,
          new Vector2((float)(x + Game1.tileSize / 4), (float)(y + Game1.tileSize / 4 + 4)) + new Vector2(2f, 2f),
          Game1.textShadowColor);
        b.DrawString(Game1.smallFont, hoveredLocationText,
          new Vector2((float)(x + Game1.tileSize / 4), (float)(y + Game1.tileSize / 4 + 4)) + new Vector2(0f, 2f),
          Game1.textShadowColor);
        b.DrawString(Game1.smallFont, hoveredLocationText,
          new Vector2((float)(x + Game1.tileSize / 4), (float)(y + Game1.tileSize / 4 + 4)) + new Vector2(2f, 0f),
          Game1.textShadowColor);
        b.DrawString(Game1.smallFont, hoveredLocationText,
          new Vector2((float)(x + Game1.tileSize / 4), (float)(y + Game1.tileSize / 4 + 4)), Game1.textColor * 0.9f);
      }
      else
      {
        // Draw name tooltip only
        DrawNames(Game1.spriteBatch, hoveredNames, x, y, offsetY, this.height, this.Config.NameTooltipMode);
      }

      

      // Draw indoor icon
      if (hasIndoorCharacter && !String.IsNullOrEmpty(hoveredNames))
				b.Draw(Game1.mouseCursors, indoorIconVector, new Rectangle?(new Rectangle(448, 64, 32, 32)), Color.White, 0f,
					Vector2.Zero, 0.75f, SpriteEffects.None, 0f);

			// Cursor
			if (!Game1.options.hardwareCursor)
				b.Draw(Game1.mouseCursors, new Vector2(Game1.getOldMouseX(), Game1.getOldMouseY()),
					new Rectangle?(Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors,
						(Game1.options.gamepadControls ? 44 : 0), 16, 16)), Color.White, 0f, Vector2.Zero,
					Game1.pixelZoom + Game1.dialogueButtonScale / 150f, SpriteEffects.None, 1f);
		}

		// Draw map to cover base rendering 
		public void DrawMap(SpriteBatch b)
		{
			int boxY = mapY - 96;
			int mY = mapY;
			Game1.drawDialogueBox(mapX - 32, boxY, (map.Bounds.Width + 16) * 4, 848, false, true, null, false);
			b.Draw(map, new Vector2((float) mapX, (float) mY), new Rectangle(0, 0, 300, 180), Color.White, 0f, Vector2.Zero,
				4f, SpriteEffects.None, 0.86f);

			switch (Game1.whichFarm)
			{
				case 1:
					b.Draw(map, new Vector2((float) mapX, (float) (mY + 172)), new Rectangle(0, 180, 131, 61), Color.White, 0f,
						Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
					break;
				case 2:
					b.Draw(map, new Vector2((float) mapX, (float) (mY + 172)), new Rectangle(131, 180, 131, 61), Color.White, 0f,
						Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
					break;
				case 3:
					b.Draw(map, new Vector2((float) mapX, (float) (mY + 172)), new Rectangle(0, 241, 131, 61), Color.White, 0f,
						Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
					break;
				case 4:
					b.Draw(map, new Vector2((float) mapX, (float) (mY + 172)), new Rectangle(131, 241, 131, 61), Color.White, 0f,
						Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
					break;
			}

			if (drawPamHouseUpgrade)
			{
				b.Draw(map,
					new Vector2((float) (mapX + ModConstants.MapVectors["Trailer_Big"][0].MapX - 13),
						(float) (mapY + ModConstants.MapVectors["Trailer_Big"][0].MapY - 16)), new Rectangle(263, 181, 8, 8), Color.White,
					0f, Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
			}

			var player = Game1.player;
			int x = player.getTileX();
			int y = player.getTileY();
			string playerLocationName = null;
			switch (player.currentLocation.Name)
			{
				case "Saloon":
					playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11172");
					break;
				case "Beach":
					playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11174");
					break;
				case "Mountain":
					if (x < 38)
						playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11176");
					else if (x < 96)
						playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11177");
					else
						playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11178");
					break;
				case "Tunnel":
				case "Backwoods":
					playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11180");
					break;
				case "FarmHouse":
				case "Barn":
				case "Big Barn":
				case "Deluxe Barn":
				case "Coop":
				case "Big Coop":
				case "Deluxe Coop":
				case "Cabin":
				case "Slime Hutch":
				case "Greenhouse":
				case "FarmCave":
				case "Shed":
				case "Farm":
					playerLocationName =
						Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11064", player.farmName.Value);
					break;
				case "Forest":
					if (y > 51)
						playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11186");
					else if (x < 58)
						playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11186");
					else
						playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11188");
					break;
				case "Town":
					if (x > 84 && y < 68)
						playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11190");
					else if (x > 80 && y >= 68)

						playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11190");
					else if (y <= 42)
						playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11190");
					else if (y > 42 && y < 76)
						playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11190");
					else
						playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11190");
					break;
				case "Temp":
					if (player.currentLocation.Map.Id.Contains("Town"))
					{
						if (x > 84 && y < 68)
							playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11190");
						else if (x > 80 && y >= 68)
							playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11190");
						else if (y <= 42)
							playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11190");
						else if (y > 42 && y < 76)
							playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11190");
						else
							playerLocationName = Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11190");
					}

					break;
			}

			if (playerLocationName != null)
			{
				SpriteText.drawStringWithScrollCenteredAt(b, playerLocationName, base.xPositionOnScreen + base.width / 2,
					base.yPositionOnScreen + base.height + 32 + 16, "", 1f, -1, 0, 0.88f, false);
			}
		}

		// Draw event
		// Subtractions within location vectors are to set the origin to the center of the sprite
		public void DrawMarkers(SpriteBatch b)
		{
			if (Config.ShowFarmBuildings && FarmBuildings != null)
			{
				var sortedBuildings = FarmBuildings.ToList();
				sortedBuildings.Sort((x, y) => x.Value.Value.Y.CompareTo(y.Value.Value.Y));

				foreach (KeyValuePair<string, KeyValuePair<string, Vector2>> building in sortedBuildings)
				{
					if (ModConstants.FarmBuildingRects.TryGetValue(building.Value.Key, out Rectangle buildingRect))
					  if (MapName == "starblue_map")
					    buildingRect.Y = 7;
					  else if (MapName == "eemie_recolour_map")
					    buildingRect.Y = 14;

          b.Draw(
							BuildingMarkers,
							new Vector2(
								mapX + building.Value.Value.X - buildingRect.Width / 2,
								mapY + building.Value.Value.Y - buildingRect.Height / 2
							),
							new Rectangle?(buildingRect), Color.White, 0f, Vector2.Zero, 3f, SpriteEffects.None, 1f
						);
				}
			}

      // ===== Custom locations =====
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

            b.Draw(CustomMarkerTex, new Vector2(mapX + toAreaRect.Value<int>("X"), mapY + toAreaRect.Value<int>("Y")),
              srcRect, Color.White,
              0f,
              Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
          }
        }
      }

      // Traveling Merchant
      if (Config.ShowTravelingMerchant && SecondaryNpcs["Merchant"])
			{
				Vector2 merchantLoc = new Vector2(ModConstants.MapVectors["Merchant"][0].MapX, ModConstants.MapVectors["Merchant"][0].MapY);
        b.Draw(Game1.mouseCursors, new Vector2(mapX + merchantLoc.X - 16, mapY + merchantLoc.Y - 15),
					new Rectangle?(new Rectangle(191, 1410, 22, 21)), Color.White, 0f, Vector2.Zero, 1.3f, SpriteEffects.None,
					1f);
			}

			// Farmers
			if (Context.IsMultiplayer)
			{
				foreach (Farmer farmer in Game1.getOnlineFarmers())
				{
					// Temporary solution to handle desync of farmhand location/tile position when changing location
					if (FarmerMarkers.TryGetValue(farmer.UniqueMultiplayerID, out CharacterMarker farMarker))
            if (farMarker == null || farMarker.MapLocation.X < 0)
					    continue;
				    if (farMarker != null && farMarker.DrawDelay == 0)
				    {
				      farmer.FarmerRenderer.drawMiniPortrat(b,
				        new Vector2(mapX + farMarker.MapLocation.X - 16, mapY + farMarker.MapLocation.Y - 15),
				        0.00011f, 2f, 1, farmer);
				    }
				}
			}
			else
			{
				Vector2 playerLoc = ModMain.LocationToMap(Game1.player.currentLocation.uniqueName.Value ?? Game1.player.currentLocation.Name, Game1.player.getTileX(),
					Game1.player.getTileY(), CustomMapLocations, true);
        if (playerLoc.X >= 0)
				  Game1.player.FarmerRenderer.drawMiniPortrat(b,
					  new Vector2(mapX + playerLoc.X - 16, mapY + playerLoc.Y - 15), 0.00011f, 2f, 1,
					  Game1.player);
			}

			// NPCs
			// Sort by drawing order
		  if (NpcMarkers != null)
		  {
        var sortedMarkers = NpcMarkers.ToList();
        sortedMarkers.Sort((x, y) => x.Layer.CompareTo(y.Layer));

        foreach (CharacterMarker npcMarker in sortedMarkers)
        {
          // Skip if no specified location
          if (npcMarker.MapLocation.X < 0 || npcMarker.Marker == null ||
              !MarkerCropOffsets.ContainsKey(npcMarker.Npc.Name))
          {
            continue;
          }

          // Tint/dim hidden markers
          if (npcMarker.IsHidden)
          {
            b.Draw(npcMarker.Marker,
              new Rectangle((int)(mapX + npcMarker.MapLocation.X), (int)(mapY + npcMarker.MapLocation.Y),
                32, 30),
              new Rectangle?(new Rectangle(0, MarkerCropOffsets[npcMarker.Npc.Name], 16, 15)), Color.DimGray * 0.7f);
            if (npcMarker.IsBirthday)
            {
              // Gift icon
              b.Draw(Game1.mouseCursors,
                new Vector2(mapX + npcMarker.MapLocation.X + 20, mapY + npcMarker.MapLocation.Y),
                new Rectangle?(new Rectangle(147, 412, 10, 11)), Color.DimGray * 0.7f, 0f, Vector2.Zero, 1.8f,
                SpriteEffects.None, 0f);
            }

            if (npcMarker.HasQuest)
            {
              // Quest icon
              b.Draw(Game1.mouseCursors,
                new Vector2(mapX + npcMarker.MapLocation.X + 22, mapY + npcMarker.MapLocation.Y - 3),
                new Rectangle?(new Rectangle(403, 496, 5, 14)), Color.DimGray * 0.7f, 0f, Vector2.Zero, 1.8f,
                SpriteEffects.None, 0f);
            }
          }
          else
          {
            b.Draw(npcMarker.Marker,
              new Rectangle((int)(mapX + npcMarker.MapLocation.X), (int)(mapY + npcMarker.MapLocation.Y),
                32, 30),
              new Rectangle?(new Rectangle(0, MarkerCropOffsets[npcMarker.Npc.Name], 16, 15)), Color.White);
            if (npcMarker.IsBirthday)
            {
              // Gift icon
              b.Draw(Game1.mouseCursors,
                new Vector2(mapX + npcMarker.MapLocation.X + 20, mapY + npcMarker.MapLocation.Y),
                new Rectangle?(new Rectangle(147, 412, 10, 11)), Color.White, 0f, Vector2.Zero, 1.8f, SpriteEffects.None,
                0f);
            }

            if (npcMarker.HasQuest)
            {
              // Quest icon
              b.Draw(Game1.mouseCursors,
                new Vector2(mapX + npcMarker.MapLocation.X + 22, mapY + npcMarker.MapLocation.Y - 3),
                new Rectangle?(new Rectangle(403, 496, 5, 14)), Color.White, 0f, Vector2.Zero, 1.8f, SpriteEffects.None,
                0f);
            }
          }
        }
      }
		}

		// Draw NPC name tooltips map page
		public void DrawNames(SpriteBatch b, string names, int x, int y, int offsetY, int relocate, int nameTooltipMode)
		{
			if (hoveredNames.Equals("")) return;

			indoorIconVector = new Vector2(-1000, -1000);
			var lines = names.Split('\n');
			int height = (int) Math.Max(60, Game1.smallFont.MeasureString(names).Y + Game1.tileSize / 2);
			int width = (int) Game1.smallFont.MeasureString(names).X + Game1.tileSize / 2;

			if (nameTooltipMode == 1)
			{
				x = Game1.getOldMouseX() + Game1.tileSize / 2;
				if (lines.Length > 1)
				{
					y += offsetY - ((int) Game1.smallFont.MeasureString(names).Y) + Game1.tileSize / 2;
				}
				else
				{
					y += offsetY;
				}

				// If going off screen on the right, move tooltip to below location tooltip so it can stay inside the screen
				// without the cursor covering the tooltip
				if (x + width > Game1.viewport.Width)
				{
					x = Game1.viewport.Width - width;
					if (lines.Length > 1)
					{
						y += relocate - 8 + ((int) Game1.smallFont.MeasureString(names).Y) + Game1.tileSize / 2;
					}
					else
					{
						y += relocate - 8 + Game1.tileSize;
					}
				}
			}
			else if (nameTooltipMode == 2)
			{
				y += offsetY;
				if (x + width > Game1.viewport.Width)
				{
					x = Game1.viewport.Width - width;
				}

				// If going off screen on the bottom, move tooltip to above location tooltip so it stays visible
				if (y + height > Game1.viewport.Height)
				{
					x = Game1.getOldMouseX() + Game1.tileSize / 2;
					if (lines.Length > 1)
					{
						y += -relocate + 8 - ((int) Game1.smallFont.MeasureString(names).Y) + Game1.tileSize / 2;
					}
					else
					{
						y += -relocate + 6 - Game1.tileSize;
					}
				}
			}
			else
			{
				x = Game1.activeClickableMenu.xPositionOnScreen - 145;
				y = Game1.activeClickableMenu.yPositionOnScreen + 650 - height / 2;
			}

			if (hasIndoorCharacter)
			{
				indoorIconVector = new Vector2(x - Game1.tileSize / 8 + 2, y - Game1.tileSize / 8 + 2);
			}

			Vector2 vector = new Vector2(x + (float) (Game1.tileSize / 4), y + (float) (Game1.tileSize / 4 + 4));

			drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, y, width, height, Color.White, 1f, true);
			b.DrawString(Game1.smallFont, names, vector + new Vector2(2f, 2f), Game1.textShadowColor, 0f, Vector2.Zero, 1f,
				SpriteEffects.None, 0f);
			b.DrawString(Game1.smallFont, names, vector + new Vector2(0f, 2f), Game1.textShadowColor, 0f, Vector2.Zero, 1f,
				SpriteEffects.None, 0f);
			b.DrawString(Game1.smallFont, names, vector + new Vector2(2f, 0f), Game1.textShadowColor, 0f, Vector2.Zero, 1f,
				SpriteEffects.None, 0f);
			b.DrawString(Game1.smallFont, names, vector, Game1.textColor * 0.9f, 0f, Vector2.Zero, 1f, SpriteEffects.None,
				0f);
		}

		/// <summary>Get the map points to display on a map.</summary>
		private Dictionary<string, Rectangle> RegionRects() => new Dictionary<string, Rectangle>()
		{
			{"Desert_Region", new Rectangle(-1, -1, 261, 175)},
			{"Farm_Region", new Rectangle(-1, -1, 188, 148)},
			{"Backwoods_Region", new Rectangle(-1, -1, 148, 120)},
			{"BusStop_Region", new Rectangle(-1, -1, 76, 100)},
			{"WizardHouse", new Rectangle(-1, -1, 36, 76)},
			{"AnimalShop", new Rectangle(-1, -1, 76, 40)},
			{"LeahHouse", new Rectangle(-1, -1, 32, 24)},
			{"SamHouse", new Rectangle(-1, -1, 36, 52)},
			{"HaleyHouse", new Rectangle(-1, -1, 40, 36)},
			{"TownSquare", new Rectangle(-1, -1, 48, 45)},
			{"Hospital", new Rectangle(-1, -1, 16, 32)},
			{"SeedShop", new Rectangle(-1, -1, 28, 40)},
			{"Blacksmith", new Rectangle(-1, -1, 80, 36)},
			{"Saloon", new Rectangle(-1, -1, 28, 40)},
			{"ManorHouse", new Rectangle(-1, -1, 44, 56)},
			{"ArchaeologyHouse", new Rectangle(-1, -1, 32, 28)},
			{"ElliottHouse", new Rectangle(-1, -1, 28, 20)},
			{"Sewer", new Rectangle(-1, -1, 24, 20)},
			{"Graveyard", new Rectangle(-1, -1, 40, 32)},
			{"Trailer", new Rectangle(-1, -1, 20, 12)},
			{"AlexHouse", new Rectangle(-1, -1, 36, 36)},
			{"ScienceHouse", new Rectangle(-1, -1, 48, 32)},
			{"Tent", new Rectangle(-1, -1, 12, 16)},
			{"Mine", new Rectangle(-1, -1, 16, 24)},
			{"AdventureGuild", new Rectangle(-1, -1, 32, 36)},
			{"Quarry", new Rectangle(-1, -1, 88, 76)},
			{"JojaMart", new Rectangle(-1, -1, 52, 52)},
			{"FishShop", new Rectangle(-1, -1, 36, 40)},
			{"Spa", new Rectangle(-1, -1, 48, 36)},
			{"Woods", new Rectangle(-1, -1, 196, 176)},
			{"RuinedHouse", new Rectangle(-1, -1, 20, 20)},
			{"CommunityCenter", new Rectangle(-1, -1, 44, 36)},
			{"SewerPipe", new Rectangle(-1, -1, 24, 32)},
			{"Railroad_Region", new Rectangle(-1, -1, 200, 69)},
			{"LonelyStone", new Rectangle(-1, -1, 28, 28)},
		};
	}
}