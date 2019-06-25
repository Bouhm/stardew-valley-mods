using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace NPCMapLocations
{
  // Handles custom maps (recolors of the mod map), custom NPCs, custom sprites, custom names, etc.
  public class ModCustomizations
  {
    private readonly IMonitor Monitor;
    private readonly HashSet<string> NpcCustomizations;

    public Dictionary<string, MapVector[]> MapVectors { get; set; }
    public Dictionary<string, string> Names { get; set; }
    public Texture2D LocationTextures { get; set; }
    public Dictionary<string, CustomLocation> Locations { get; set; }
    public Dictionary<string, int> MarkerCropOffsets { get; set; }
    public List<ClickableComponent> Tooltips { get; set; }
    public string MapName { get; set; }

    public ModCustomizations(IMonitor monitor)
    {
      Monitor = monitor;
      MapVectors = new Dictionary<string, MapVector[]>();
      Names = new Dictionary<string, string>();
      NpcCustomizations = new HashSet<string>();
      Locations = new Dictionary<string, CustomLocation>();
      Tooltips = new List<ClickableComponent>();

      if (ModMain.Helper.ModRegistry.IsLoaded("FlashShifter.StardewValleyExpandedCP")) Monitor.Log("Using SVE customizations.", LogLevel.Debug);

      LoadMap();
      LoadTooltips();
      LoadMarkerCropOffsets();
      LoadCustomNpcs();
      LoadCustomMapLocations();
    }

    // Handles customizations for NPCs
    // Custom NPCs and custom names or sprites for existing NPCs
    private void LoadCustomNpcs()
    {
      foreach (var npc in Utility.getAllCharacters())
      {
        if (npc == null) continue;

        if (!ModConstants.ExcludedVillagers.Contains(npc.Name) && npc.isVillager())
        {
          LoadNpcCrop(npc);
          LoadCustomNames(npc);
        }
      }

      if (NpcCustomizations.Count != 0)
      {
        var names = "Handled custom NPCs: ";
        foreach (var name in NpcCustomizations) names += name + ", ";

        Monitor.Log(names.Substring(0, names.Length - 2), LogLevel.Debug);
      }

      ModMain.Helper.Data.WriteJsonFile($"config/{Constants.SaveFolderName}.json", ModMain.Config);
    }

    // Load recolored map if user has recolor mods
    private void LoadMap()
    {
      // Eemie's Map Recolour
      if (ModMain.Helper.ModRegistry.IsLoaded("minervamaga.CP.eemieMapRecolour"))
        MapName = "eemie_recolour";
      // A Toned Down SDV
      else if (ModMain.Helper.ModRegistry.IsLoaded("Lavender.TonedDownSDV"))
        MapName = "toned_down";
      // Starblue Valley
      else if (ModMain.Helper.ModRegistry.IsLoaded("Lita.StarblueValley"))
        MapName = "starblue_valley";
      // Elle's Dirt and Cliff Recolor
      else if (ModMain.Helper.ModRegistry.IsLoaded("Elle.DirtCliffRecolor"))
        MapName = "elle_recolor";
      // Extended Farm
      else if (ModMain.Helper.ModRegistry.IsLoaded("Forkmaster.ExtendedFarm"))
        MapName = "farm_extended";
      // Default
      else
        MapName = "default";
    }

    private void LoadTooltips()
    {
      foreach (var tooltip in ModMain.Config.CustomMapTooltips)
        Tooltips.Add(new ClickableComponent(
          new Rectangle(
            (int) tooltip.Value.GetValue("X"),
            (int) tooltip.Value.GetValue("Y"),
            (int) tooltip.Value.GetValue("Width"),
            (int) tooltip.Value.GetValue("Height")
          ),
          tooltip.Value.GetValue("PrimaryText") + Environment.NewLine + tooltip.Value.GetValue("SecondaryText"))
        );
    }

    private void LoadMarkerCropOffsets()
    {
      MarkerCropOffsets = ModConstants.MarkerCropOffsets;
    }

    // { "customLocation": [{x, y}] } where x, y are relative to map (for a custom location ex. a new house)
    // { "customRegion": [{x1, y1}, {x2, y2}] where x, y are relative to map (for a custom region ex. a new farm)
    // Any custom locations with given location on the map
    private void LoadCustomMapLocations()
    {
      foreach (var mapVectors in ModMain.Config.CustomMapLocations)
      {
        var mapVectorArr = new MapVector[mapVectors.Value.Length];
        for (var i = 0; i < mapVectors.Value.Length; i++)
        {
          var mapVector = mapVectors.Value[i];

          // Marker doesn't need to specify corresponding Tile position
          if (mapVector.GetValue("TileX") == null || mapVector.GetValue("TileY") == null)
            mapVectorArr[i] = new MapVector(
              (int) mapVector.GetValue("MapX"),
              (int) mapVector.GetValue("MapY")
            );
          // Region must specify corresponding Tile positions for
          // Calculations on movement within location
          else
            mapVectorArr[i] = new MapVector(
              (int) mapVector.GetValue("MapX"),
              (int) mapVector.GetValue("MapY"),
              (int) mapVector.GetValue("TileX"),
              (int) mapVector.GetValue("TileY")
            );
        }

        MapVectors.Add(mapVectors.Key, mapVectorArr);
      }

      foreach (var location in ModMain.Config.CustomMapTextures)
        Locations.Add(location.Key, new CustomLocation(location.Value));

      // Automatically adjust tracking for modded maps that are sized differently from vanilla map
      foreach (var location in Game1.locations)
      {
        var locationName = location.uniqueName.Value ?? location.Name;

        if (!location.IsOutdoors || locationName == "Summit" || MapVectors.ContainsKey(locationName) ||
            !ModConstants.MapVectors.TryGetValue(locationName, out var mapVector)) continue;
        if (mapVector.LastOrDefault().TileX != location.Map.DisplayWidth / Game1.tileSize ||
            mapVector.LastOrDefault().TileY != location.Map.DisplayHeight / Game1.tileSize)
          MapVectors.Add(locationName,
            new[]
            {
              mapVector.FirstOrDefault(),
              new MapVector(
                mapVector.LastOrDefault().MapX,
                mapVector.LastOrDefault().MapY,
                location.Map.DisplayWidth / Game1.tileSize,
                location.Map.DisplayHeight / Game1.tileSize
              )
            });
      }

      var customLocations = MapVectors.Keys.ToArray();
      if (MapVectors.Keys.Count > 0)
      {
        if (customLocations.Length == 1)
        {
          Monitor.Log($"Handled custom location: {customLocations[0]}.", LogLevel.Debug);
        }
        else
        {
          var locationList = "";
          for (var i = 0; i < customLocations.Length; i++)
            locationList += customLocations[i] + (i + 1 == customLocations.Length ? "" : ", ");

          Monitor.Log($"Handled custom locations: {locationList}.", LogLevel.Debug);
        }
      }
    }

    // Handle any modified NPC names 
    // Specifically mods that change names in dialogue files (displayName)
    private void LoadCustomNames(NPC npc)
    {
      if (!Names.TryGetValue(npc.Name, out var customName))
      {
        if (npc.displayName == null)
        {
          Names.Add(npc.Name, npc.Name);
        }
        else
        {
          Names.Add(npc.Name, npc.displayName);
          if (!npc.Name.Equals(npc.displayName) || ModMain.Config.CustomNpcs.ContainsKey(npc.Name))
            NpcCustomizations.Add(npc.Name);
        }
      }
    }

    // Load user-specified NPC crops for custom sprites
    private void LoadNpcCrop(NPC npc)
    {
      if (ModMain.Config.CustomNpcs != null && ModMain.Config.CustomNpcs.Count > 0)
        foreach (var villager in ModMain.Config.CustomNpcs)
          if (npc.Name.Equals(villager.Key))
          {
            MarkerCropOffsets[npc.Name] = villager.Value;
            NpcCustomizations.Add(npc.Name);
          }

      // If custom crop offset is not specified, default to 0
      if (!MarkerCropOffsets.TryGetValue(npc.Name, out var crop)) MarkerCropOffsets[npc.Name] = 0;
    }

    public class CustomLocation
    {
      public Vector2 LocVector;
      public Rectangle SrcRect;

      public CustomLocation(JObject locationRects)
      {
        var fromAreaRect = locationRects.GetValue("FromArea");
        var toAreaRect = locationRects.GetValue("ToArea");

        SrcRect = new Rectangle(fromAreaRect.Value<int>("X"), fromAreaRect.Value<int>("Y"),
          fromAreaRect.Value<int>("Width"), fromAreaRect.Value<int>("Height"));
        LocVector = new Vector2(toAreaRect.Value<int>("X"), toAreaRect.Value<int>("Y"));
      }
    }
  }
}