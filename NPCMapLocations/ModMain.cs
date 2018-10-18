/*
NPC Map Locations Mod by Bouhm.
Shows NPC locations on a modified map.
*/

using System;
using System.Collections.Generic;
using System.Linq;
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
	  public static SButton HeldKey;
    private Texture2D BuildingMarkers;
		private ModConfig Config;
		private ModCustomHandler CustomHandler;
		private ModMinimap Minimap;
		private HashSet<MapMarker> NpcMarkers;
		private Dictionary<string, bool> SecondaryNpcs;
	  private Dictionary<string, LocationContext> locationContexts;
	  private const int DRAW_DELAY = 3;

    // Customizations/Custom mods
	  private string MapName;
    private Texture2D CustomLocationMarkers;
	  private Dictionary<string, MapVector[]> CustomLocations;
    private Dictionary<string, Rectangle> CustomLocationRects;
	  private Dictionary<string, string> CustomNames;
	  private Dictionary<string, int> MarkerCropOffsets;

    // Multiplayer
    private Dictionary<long, MapMarker> FarmerMarkers;
		private bool hasOpenedMap;
		private bool isModMapOpen;

		// Debugging
	  private const bool DEBUG_MODE = false;
		private static Dictionary<string, KeyValuePair<string, Vector2>> FarmBuildings;
		private static Vector2 _tileLower;
		private static Vector2 _tileUpper;
	  private static List<string> alertFlags;

  
		// Replace game map with modified map
		public bool CanLoad<T>(IAssetInfo asset)
		{
			return asset.AssetNameEquals(@"LooseSprites\Map");
		}

		public T Load<T>(IAssetInfo asset)
		{
			T map;
			MapName = CustomHandler.LoadMap();
			try
			{
				if (!MapName.Equals("default_map"))
					Monitor.Log($"Using recolored map {CustomHandler.LoadMap()}.", LogLevel.Debug);
       
				map = Helper.Content.Load<T>($@"assets\{MapName}.png"); // Replace map page
			}
			catch
			{
				Monitor.Log($"Unable to find {MapName}; loaded default map instead.", LogLevel.Debug);
				map = Helper.Content.Load<T>($@"assets\default_map.png");
			}

			return map;
		}

		public override void Entry(IModHelper helper)
		{
			Config = Helper.ReadConfig<ModConfig>();
			MarkerCropOffsets = ModConstants.MarkerCropOffsets;
			CustomHandler = new ModCustomHandler(helper, Config, Monitor);
			BuildingMarkers =
				Helper.Content.Load<Texture2D>(@"assets/buildings.png"); // Load farm buildings
		  CustomLocationMarkers =
		    Helper.Content.Load<Texture2D>(@"assets/customLocations.png"); // Load custom location markers

			SaveEvents.AfterLoad += SaveEvents_AfterLoad;
			TimeEvents.AfterDayStarted += TimeEvents_AfterDayStarted;
      LocationEvents.LocationsChanged += LocationEvents_LocationsChanged;
			LocationEvents.BuildingsChanged += LocationEvents_BuildingsChanged;
			InputEvents.ButtonPressed += InputEvents_ButtonPressed;
			InputEvents.ButtonReleased += InputEvents_ButtonReleased;
			GameEvents.HalfSecondTick += GameEvents_HalfSecondTick;
			GameEvents.UpdateTick += GameEvents_UpdateTick;
      MenuEvents.MenuChanged += MenuEvents_MenuChanged;
		  PlayerEvents.Warped += PlayerEvents_Warped;
		  GraphicsEvents.OnPreRenderHudEvent += GraphicsEvents_OnPreRenderHudEvent;
		  GraphicsEvents.OnPostRenderEvent += GraphicsEvents_OnPostRenderEvent;
		  GraphicsEvents.Resize += GraphicsEvents_Resize;
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
		private static void UpdateFarmBuildingLocs()
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
					building.tileY.Value
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
			if (((CommunityCenter) Game1.getLocationFromName("CommunityCenter")).areasComplete[CommunityCenter.AREA_Pantry])
			{
				var locVector = LocationToMap("Greenhouse");
				locVector.X -= 5 / 2 * 3;
				locVector.Y -= 7 / 2 * 3;
				FarmBuildings["Greenhouse"] = new KeyValuePair<string, Vector2>("Greenhouse", locVector);
			}
		}

		private void LocationEvents_BuildingsChanged(object sender, EventArgsLocationBuildingsChanged e)
		{
			if (e.Location.Name.Equals("Farm"))
				UpdateFarmBuildingLocs();
		}

	  private void LocationEvents_LocationsChanged(object sender, EventArgsLocationsChanged e)
	  {
	    locationContexts = new Dictionary<string, LocationContext>();
	    foreach (var location in Game1.locations)
	      MapRootLocations(location, null, false);
	  }

    // Load config and other one-off data
    private void SaveEvents_AfterLoad(object sender, EventArgs e)
    {
      locationContexts = new Dictionary<string, LocationContext>();
      foreach (var location in Game1.locations)
        MapRootLocations(location, null, false);

      SecondaryNpcs = new Dictionary<string, bool>
			{
				{"Kent", false},
				{"Marlon", false},
				{"Merchant", false},
				{"Sandy", false},
				{"Wizard", false}
			};
			CustomHandler.UpdateCustomNpcs();
			CustomNames = CustomHandler.GetNpcNames();
			MarkerCropOffsets = CustomHandler.GetMarkerCropOffsets();
		  CustomLocations = CustomHandler.GetCustomLocations();
      CustomLocationRects = CustomHandler.GetCustomLocationRects();
			UpdateFarmBuildingLocs();
      alertFlags = new List<string>();
		}

		// Handle opening mod menu and changing tooltip options
	  private void InputEvents_ButtonPressed(object sender, EventArgsInput e)
	  {
	    if (!Context.IsWorldReady) return;

      // Minimap dragging
	    if (Config.ShowMinimap && Minimap != null)
	    {
	      if (e.Button.ToString().Equals(Config.MinimapDragKey))
	        HeldKey = e.Button;
	      else if (HeldKey.ToString().Equals(Config.MinimapDragKey) &&
	               (e.Button == SButton.MouseLeft || e.Button == SButton.ControllerA) &&
	               Game1.activeClickableMenu == null)
	      {
          Minimap.HandleMouseDown();
	        if (Minimap.isBeingDragged)
	          e.SuppressButton();
	      }
	    }

      // Minimap toggle
	    if (e.Button.ToString().Equals(Config.MinimapToggleKey) && Game1.activeClickableMenu == null)
	    {
	      Config.ShowMinimap = !Config.ShowMinimap;
	      Helper.WriteConfig(Config);
	    }

	    // ModMenu
	    if (Game1.activeClickableMenu is GameMenu)
				  HandleInput((GameMenu) Game1.activeClickableMenu, e.Button);
		}

		private void InputEvents_ButtonReleased(object sender, EventArgsInput e)
		{
      if (HeldKey.ToString().Equals(Config.MinimapDragKey) && e.Button.ToString().Equals(Config.MinimapDragKey))
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
					SecondaryNpcs,
					CustomNames,
					MarkerCropOffsets,
					Helper,
					Config
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

				Helper.WriteConfig(Config);
			}
			else
			{
				if (--Config.NameTooltipMode < 1) Config.NameTooltipMode = 3;

				Helper.WriteConfig(Config);
			}
		}

		// Handle any checks that need to be made per day
		private void TimeEvents_AfterDayStarted(object sender, EventArgs e)
		{
			var npcEntries = new Dictionary<string, bool>(SecondaryNpcs);
			foreach (var npc in npcEntries)
			{
				var name = npc.Key;
				switch (name)
				{
					case "Kent":
						SecondaryNpcs[name] = Game1.year >= 2;
						break;
					case "Marlon":
						SecondaryNpcs[name] = Game1.player.eventsSeen.Contains(100162);
						break;
					case "Merchant":
						SecondaryNpcs[name] = ((Forest) Game1.getLocationFromName("Forest")).travelingMerchantDay;
						break;
					case "Sandy":
						SecondaryNpcs[name] = Game1.player.mailReceived.Contains("ccVault");
						break;
					case "Wizard":
						SecondaryNpcs[name] = Game1.player.eventsSeen.Contains(112);
						break;
					default: break;
				}
			}

			ResetMarkers(GetVillagers());
      UpdateMarkers(true);
			if (Config.ShowMinimap)
				Minimap = new ModMinimap(
					NpcMarkers,
					SecondaryNpcs,
					FarmerMarkers,
					MarkerCropOffsets,
					FarmBuildings,
					BuildingMarkers,
					Helper,
					Config,
          MapName,
          CustomLocations,
					CustomLocationRects,
          CustomLocationMarkers
				);
		}

		private void ResetMarkers(List<NPC> villagers)
		{
			NpcMarkers = new HashSet<MapMarker>();
			foreach (var npc in villagers)
			{
				// Handle case where Kent appears even though he shouldn't
				if (npc.Name.Equals("Kent") && !SecondaryNpcs["Kent"]) continue;

				var npcMarker = new MapMarker
				{
					Npc = npc,
					IsBirthday = npc.isBirthday(Game1.currentSeason, Game1.dayOfMonth)
				};
				NpcMarkers.Add(npcMarker);
			}

			if (Context.IsMultiplayer)
				FarmerMarkers = new Dictionary<long, MapMarker>();
		}

		// To initialize ModMap quicker for smoother rendering when opening map
		private void GameEvents_UpdateTick(object sender, EventArgs e)
		{
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

		// Map page updates
		private void GameEvents_HalfSecondTick(object sender, EventArgs e)
		{
			if (!Context.IsWorldReady) return;
		  var updateForMinimap = false;

		  if (Config.ShowMinimap)
		  {
		    if (Minimap != null)
		    {
		      Minimap.Update();
		      updateForMinimap = true;
		    }
		  }

      UpdateMarkers(updateForMinimap);
		}

		private void OpenModMap(GameMenu gameMenu)
		{
      isModMapOpen = true;
			UpdateNpcs(true);
			var pages = Helper.Reflection
				.GetField<List<IClickableMenu>>(gameMenu, "pages").GetValue();

			// Changing the page in GameMenu instead of changing Game1.activeClickableMenu
			// allows for better compatibility with other mods that use MapPage
			pages[GameMenu.mapTab] = new ModMapPage(
				NpcMarkers,
				CustomNames,
				SecondaryNpcs,
				FarmerMarkers,
				MarkerCropOffsets,
				FarmBuildings,
				BuildingMarkers,
				Helper,
				Config,
				MapName,
				CustomLocations,
				CustomLocationRects,
        CustomLocationMarkers
      );
		}

    // Recursively traverse warps of locations and map locations to root locations (outdoor locations)
	  private string MapRootLocations(GameLocation location, string root, bool hasOutdoorWarp)
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
        root = MapRootLocations(Game1.getLocationFromName(warp.TargetName), root, hasOutdoorWarp);
	      locationContexts[location.Name].Root = root;
        return root;
	    }

	    return root;
	  }

	  private void UpdateMarkers(bool forceUpdateAll = false)
		{
			if (isModMapOpen || forceUpdateAll)
			{
			  if (Context.IsMainPlayer)
          UpdateNpcs(forceUpdateAll);

				if (Context.IsMultiplayer)
					UpdateFarmers();
			}
		}

		// Update NPC marker data and names on hover
		private void UpdateNpcs(bool forceUpdateAll = false)
		{
			if (NpcMarkers == null) return;

			foreach (var npcMarker in NpcMarkers)
			{
				var npc = npcMarker.Npc;
				string locationName;
				var npcLocation = npc.currentLocation;

				// Handle null locations at beginning of new day
				if (npcLocation == null)
				{
					locationName = npc.DefaultMap;
					npcLocation = Game1.getLocationFromName(locationName);
				}
				else
				{
					locationName = npc.currentLocation.Name;
				}

        // Special case for Mines
			  if (locationName.StartsWith("UndergroundMine"))
			    locationName = getMinesLocationName(locationName);

        if (locationName == null || !ModConstants.MapVectors.TryGetValue(locationName, out var loc))
				{
					if (!alertFlags.Contains("UnknownLocation:" + locationName))
					{
						Monitor.Log($"Unknown location: {locationName}.", LogLevel.Debug);
						alertFlags.Add("UnknownLocation:" + locationName);
					}
					continue;
				}

				// For layering indoor/outdoor NPCs and indoor indicator
				npcMarker.IsOutdoors = npcLocation.IsOutdoors;

        // For show Npcs in player's location option
			  var isSameLocation = false;
			  if (Config.OnlySameLocation)
			  {
			    if (locationName == Game1.player.currentLocation.Name)
			      isSameLocation = true;
          else if (locationContexts.TryGetValue(locationName, out var npcLocCtx) &&
                   locationContexts.TryGetValue(Game1.player.currentLocation.Name, out var playerLocCtx))
			      isSameLocation = npcLocCtx.Root == playerLocCtx.Root;
        }

        // NPCs that won't be shown on the map unless 'Show Hidden NPCs' is checked
        npcMarker.IsHidden = Config.ImmersionOption == 2 && !Game1.player.hasTalkedToFriendToday(npc.Name)
				                     || Config.ImmersionOption == 3 && Game1.player.hasTalkedToFriendToday(npc.Name)
				                     || Config.OnlySameLocation && !isSameLocation
				                     || Config.ByHeartLevel
				                     && !(Game1.player.getFriendshipHeartLevelForNPC(npc.Name)
				                          >= Config.HeartLevelMin && Game1.player.getFriendshipHeartLevelForNPC(npc.Name)
				                          <= Config.HeartLevelMax);

				// NPCs that will be drawn onto the map
				if (!Config.NpcBlacklist.Contains(npc.Name) && (Config.ShowHiddenVillagers || !npcMarker.IsHidden))
				{
					// Check if gifted for birthday
					if (npcMarker.IsBirthday)
						npcMarker.IsBirthday = Game1.player.friendshipData.ContainsKey(npc.Name) &&
						                       Game1.player.friendshipData[npc.Name].GiftsToday == 0;

					// Check for daily quests
					foreach (var quest in Game1.player.questLog)
						if (quest.accepted.Value && quest.dailyQuest.Value && !quest.completed.Value)
							switch (quest.questType.Value)
							{
								case 3:
									npcMarker.HasQuest = ((ItemDeliveryQuest) quest).target.Value == npc.Name;
									break;
								case 4:
									npcMarker.HasQuest = ((SlayMonsterQuest) quest).target.Value == npc.Name;
									break;
								case 7:
									npcMarker.HasQuest = ((FishingQuest) quest).target.Value == npc.Name;
									break;
								case 10:
									npcMarker.HasQuest = ((ResourceCollectionQuest) quest).target.Value == npc.Name;
									break;
								default:
									break;
							}
						else
							npcMarker.HasQuest = false;

					npcMarker.Marker = npc.Sprite.Texture;

					// Establish draw order, higher number infront
					// Layers 4 - 7: Outdoor NPCs in order of hidden, hidden w/ quest/birthday, standard, standard w/ quest/birthday
					// Layers 0 - 3: Indoor NPCs in order of hidden, hidden w/ quest/birthday, standard, standard w/ quest/birthday
					npcMarker.Layer = npcMarker.IsOutdoors ? 6 : 2;
					if (npcMarker.IsHidden) npcMarker.Layer -= 2;

					if (npcMarker.HasQuest || npcMarker.IsBirthday) npcMarker.Layer++;

					/*
					// Only do calculations if NPCs are moving
					if (!forceUpdateAll 
					    && (npcMarker.Location != Rectangle.Empty
					    && (!npcLocation.IsOutdoors // Indoors
					    || !npcMarker.Npc.isMoving()))) // Not moving
					{
					    continue;
					}
					*/

					// Get center of NPC marker 
					var x = (int) GetMapPosition(npcLocation, npc.getTileX(), npc.getTileY()).X - 16;
					var y = (int) GetMapPosition(npcLocation, npc.getTileX(), npc.getTileY()).Y - 15;

					npcMarker.MapLocation = new Vector2(x, y);
				}
				else
				{
					// Set no location so they don't get drawn
					npcMarker.MapLocation = new Vector2(-1000, -1000);
				}
			}
		}

		private void UpdateFarmers()
		{
			foreach (var farmer in Game1.getOnlineFarmers())
			{
				if (farmer?.currentLocation == null) continue;
			  var locationName = farmer.currentLocation.Name;

			  if (locationName.StartsWith("UndergroundMine"))
          locationName = getMinesLocationName(locationName);

				if (!ModConstants.MapVectors.TryGetValue(farmer.currentLocation.Name, out var loc))
				{
					if (!alertFlags.Contains("UnknownLocation:" + farmer.currentLocation.Name))
					{
						Monitor.Log($"Unknown location: {farmer.currentLocation.Name}.", LogLevel.Debug);
						alertFlags.Add("UnknownLocation:" + farmer.currentLocation.Name);
					}
				}

				var farmerId = farmer.UniqueMultiplayerID;
				var farmerLoc = GetMapPosition(farmer.currentLocation, farmer.getTileX(), farmer.getTileY());

				if (FarmerMarkers.TryGetValue(farmer.UniqueMultiplayerID, out var farMarker))
				{
					var deltaX = farmerLoc.X - farMarker.PrevMapLocation.X;
					var deltaY = farmerLoc.Y - farMarker.PrevMapLocation.Y;

					// Location changes before tile position, causing farmhands to blink
					// to the wrong position upon entering new location. Handle this in draw.
					if (farmer.currentLocation.Name == farMarker.PrevLocationName && MathHelper.Distance(deltaX, deltaY) > 15)
						FarmerMarkers[farmerId].DrawDelay = DRAW_DELAY;
					else if (farMarker.DrawDelay > 0)
						FarmerMarkers[farmerId].DrawDelay--;
				}
				else
				{
					var newMarker = new MapMarker
					{
						Name = farmer.Name,
						DrawDelay = 0
					};

					FarmerMarkers.Add(farmerId, newMarker);
				}

				FarmerMarkers[farmerId].MapLocation = farmerLoc;
				FarmerMarkers[farmerId].PrevMapLocation = farmerLoc;
				FarmerMarkers[farmerId].PrevLocationName = farmer.currentLocation.Name;
				FarmerMarkers[farmerId].IsOutdoors = farmer.currentLocation.IsOutdoors;
			}
		}

		// Helper method for LocationToMap
		public static Vector2 GetMapPosition(GameLocation location, int tileX, int tileY)
		{
			if (location == null || tileX < 0 || tileY < 0) return new Vector2(-1000, -1000); ;

			// Handle farm buildings
			// Match currentLocation.Name with buildingType 
			// and use uniqueName to get location of buildings with the same currentLocation.Name
			if (location.IsFarm && !location.Name.Equals("FarmHouse"))
				if (location.uniqueName.Value != null
				    && (FarmBuildings[location.uniqueName.Value].Key.Equals(location.Name)
				        || FarmBuildings[location.uniqueName.Value].Key.Contains("Cabin")))
					return FarmBuildings[location.uniqueName.Value].Value;

			return LocationToMap(location.Name, tileX, tileY);
		}

		// MAIN METHOD FOR PINPOINTING CHARACTERS ON THE MAP
		// Calculated from mapping of game tile positions to pixel coordinates of the map in MapModConstants. 
		// Requires MapModConstants and modified map page in ./assets
		public static Vector2 LocationToMap(string locationName, int tileX = -1, int tileY = -1)
		{
			var mapPagePos =
				Utility.getTopLeftPositionForCenteringOnScreen(300 * Game1.pixelZoom, 180 * Game1.pixelZoom, 0, 0);

		  if (locationName.StartsWith("UndergroundMine"))
		  {
		    var mine = locationName.Substring("UndergroundMine".Length, locationName.Length - "UndergroundMine".Length);
		    if (Int32.TryParse(mine, out var mineLevel))
		    {
		      // Skull cave
		      if (mineLevel > 120)
		        locationName = "SkullCave";
		      // Mines
		      else
		        locationName = "Mine";
		    }
      }
		   

      if (!ModConstants.MapVectors.TryGetValue(locationName, out var locVectors))
				return new Vector2(-1000, -1000);

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
				var hasEqualTile = false;

				// Create rectangle bound from two pre-defined points (lower & upper bound) and calculate map scale for that area
				foreach (var vector in vectors)
				{
					if (lower != null && upper != null)
					{
						if (lower.TileX == upper.TileX || lower.TileY == upper.TileY)
							hasEqualTile = true;
						else
							break;
					}

					if ((lower == null || hasEqualTile) && tileX >= vector.TileX && tileY >= vector.TileY)
					{
						lower = vector;
						continue;
					}

					if ((upper == null || hasEqualTile) && tileX <= vector.TileX && tileY <= vector.TileY) upper = vector;
				}

				// Handle null cases - not enough vectors to calculate using lower/upper bound strategy
				// Uses fallback strategy - get closest points such that lower != upper
				var tilePos = "(" + tileX + ", " + tileY + ")";
				if (lower == null)
					lower = upper == vectors.First() ? vectors.Skip(1).First() : vectors.First();

				if (upper == null)
					upper = lower == vectors.First() ? vectors.Skip(1).First() : vectors.First();

				x = (int) (lower.MapX + (tileX - lower.TileX) / (double) (upper.TileX - lower.TileX) * (upper.MapX - lower.MapX));
				y = (int) (lower.MapY + (tileY - lower.TileY) / (double) (upper.TileY - lower.TileY) * (upper.MapY - lower.MapY));

				if (DEBUG_MODE)
				{
					_tileUpper = new Vector2(upper.TileX, upper.TileY);
					_tileLower = new Vector2(lower.TileX, lower.TileY);
				}
			}

			return new Vector2(x, y);
		}

	  private string getMinesLocationName(string locationName)
	  {
	    var mine = locationName.Substring("UndergroundMine".Length, locationName.Length - "UndergroundMine".Length);
      if (Int32.TryParse(mine, out var mineLevel))
	    {
	      // Skull cave
	      if (mineLevel > 120)
	        return "SkullCave";
	      // Mines
	      else
	        return "Mine";
	    }
	    return null;
	  }

		private void GraphicsEvents_Resize(object sender, EventArgs e)
		{
			if (!Context.IsWorldReady) return;

			UpdateMarkers(true);
			UpdateFarmBuildingLocs();
      Minimap?.CheckOffsetForMap();
		}

		private void MenuEvents_MenuChanged(object sender, EventArgsClickableMenuChanged e)
		{
			if (!Context.IsWorldReady) return;
			if (e.PriorMenu is ModMenu)
				Minimap?.Resize();
		}

	  private void PlayerEvents_Warped(object sender, EventArgsPlayerWarped e)
	  {
	    Minimap?.CheckOffsetForMap();
	  }

    private void GraphicsEvents_OnPreRenderHudEvent(object sender, EventArgs e)
		{
      if (Context.IsWorldReady && Config.ShowMinimap && Game1.displayHUD)
			{
				Minimap?.DrawMiniMap();
			}
		}

		// DEBUG 
		private void GraphicsEvents_OnPostRenderEvent(object sender, EventArgs e)
		{
			if (!Context.IsWorldReady || Game1.player == null) return;

			if (DEBUG_MODE)
				ShowDebugInfo();
		}

		// Show debug info in top left corner
		private static void ShowDebugInfo()
		{
			if (Game1.player.currentLocation == null) return;
    
			// Black background for legible text
			Game1.spriteBatch.Draw(Game1.shadowTexture, new Rectangle(50, 50, 425, 160), new Rectangle(1, 80, 1, 1),
				Color.Black);

			// Show map location and tile positions
			DrawText(
				Game1.player.currentLocation.Name + " (" + Math.Floor(Game1.player.Position.X / Game1.tileSize) + ", " +
				Math.Floor(Game1.player.Position.Y / Game1.tileSize) + ")", new Vector2(Game1.tileSize / 4, Game1.tileSize / 4));

			var currMenu = Game1.activeClickableMenu is GameMenu ? (GameMenu) Game1.activeClickableMenu : null;

			// Show lower & upper bound tiles used for calculations 
			if (currMenu != null && currMenu.currentTab == GameMenu.mapTab)
			{
				DrawText("Lower bound: (" + _tileLower.X + ", " + _tileLower.Y + ")",
					new Vector2(Game1.tileSize / 4, Game1.tileSize * 3 / 4 + 8));
				DrawText("Upper bound: (" + _tileUpper.X + ", " + _tileUpper.Y + ")",
					new Vector2(Game1.tileSize / 4, Game1.tileSize * 5 / 4 + 8 * 2));
			}
			else
			{
				DrawText("Lower bound: (" + _tileLower.X + ", " + _tileLower.Y + ")",
					new Vector2(Game1.tileSize / 4, Game1.tileSize * 3 / 4 + 8), Color.DimGray);
				DrawText("Upper bound: (" + _tileUpper.X + ", " + _tileUpper.Y + ")",
					new Vector2(Game1.tileSize / 4, Game1.tileSize * 5 / 4 + 8 * 2), Color.DimGray);
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
	public class MapMarker
	{
		public string Name { get; set; } // For any customized names; Npc.Name would be vanilla names
		public Texture2D Marker { get; set; }
		public NPC Npc { get; set; }
		public Vector2 MapLocation { get; set; }
		public Vector2 PrevMapLocation { get; set; }
		public string PrevLocationName { get; set; }
		public bool IsBirthday { get; set; }
		public bool HasQuest { get; set; }
		public bool IsOutdoors { get; set; }
		public bool IsHidden { get; set; }
		public int Layer { get; set; }
	  public int DrawDelay { get; set; } = 0;
	}

	// Class for Location Vectors
  // Maps the tileX and tileY in a game location to the location on the map
	public class MapVector
	{
		public int TileX { get; set; } // tileX in a game location
    public int TileY { get; set; } // tileY in a game location
    public int MapX { get; set; } // Absolute position relative to map
    public int MapY { get; set; } // Absolute position relative to map

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
	}

  internal class LocationContext
  {
    public string Type { get; set; } // outdoors, indoors, or room
    public string Root { get; set; } // Top-most location

    public LocationContext()
    {
      Type = null;
      Root = null;
    }
  }
}