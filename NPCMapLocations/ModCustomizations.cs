using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NPCMapLocations
{
  // Handles custom maps (recolors of the mod map), custom NPCs, custom sprites, custom names, etc.
  public class ModCustomizations
  {
    private readonly HashSet<string> NpcCustomizations;
    public readonly string MapsRootPath = "maps";

    public Dictionary<string, MapVector[]> MapVectors { get; set; }
    public Dictionary<string, string> Names { get; set; }
    public Dictionary<string, CustomLocation> Locations { get; set; }
    public Dictionary<string, int> NpcMarkerOffsets { get; set; }
    public List<ClickableComponent> Tooltips { get; set; }
    public string MapsPath { get; set; }

    public ModCustomizations()
    {
      MapVectors = new Dictionary<string, MapVector[]>();
      Names = new Dictionary<string, string>();
      NpcCustomizations = new HashSet<string>();
      Locations = new Dictionary<string, CustomLocation>();
      Tooltips = new List<ClickableComponent>();
      MapsPath = GetCustomMapFolderName();
      if (MapsPath != null)
        MapsPath = Path.Combine(MapsRootPath, MapsPath);
      else
        MapsPath = Path.Combine(MapsRootPath, "_default");
    }

    // Handles customizations for NPCs
    // Custom NPCs and custom names or sprites for existing NPCs
    private void LoadCustomNpcs()
    {
      foreach (var npc in ModMain.GetVillagers())
      {
        GetNpcCrop(npc);
        GetCustomName(npc);
      }

      // Handle duplicate displayName -- custom NPCs that replaces villagers
      Dictionary<string, string> dupes = Names
        .Where(n1 => Names.Any(n2 => n2.Key != n1.Key && n2.Value == n1.Value))
        .ToDictionary(n => n.Key, n => n.Value);

      // Properly replace the villager with custom NPC
      foreach (var dupe in dupes)
      {
        if (dupe.Key != dupe.Value)
        {
          Names[dupe.Key] = dupe.Value;
        }
        else
        {
          Names.Remove(dupe.Key);
        }
      }

      if (NpcCustomizations.Count != 0)
      {
        var names = "Adjusted markers for ";
        foreach (var name in NpcCustomizations) names += name + ", ";

        ModMain.IMonitor.Log(names.Substring(0, names.Length - 2), LogLevel.Debug);
      }

      ModMain.Helper.Data.WriteJsonFile($"config/{Constants.SaveFolderName}.json", ModMain.Config);
    }

    /// <summary>Get the folder from which to load tilesheet overrides for compatibility with other mods, if applicable.</summary>
    /// <remarks>This selects a folder in assets/tilesheets by checking the folder name against the installed mod IDs. Each folder can optionally be comma-delimited to require multiple mods, with ~ separating alternative IDs. For example "A ~ B, C" means "if the player has (A OR B) AND C installed". If multiple folders match, the first one sorted alphabetically which matches the most mods is used.</remarks>
    private string GetCustomMapFolderName()
    {
      // get root compatibility folder
      DirectoryInfo compatFolder = new DirectoryInfo(Path.Combine(ModMain.Helper.DirectoryPath, MapsRootPath));
      if (!compatFolder.Exists)
        return null;

      // get tilesheet subfolder matching the highest number of installed mods
      string folderName = null;
      {
        int modsMatched = 0;
        foreach (DirectoryInfo folder in compatFolder.GetDirectories().OrderBy(p => p.Name))
        {
          if (folder.Name == "_default")
            continue;

          // get mod ID groups
          string[] modGroups = folder.Name.Split(',');
          if (modGroups.Length <= modsMatched)
            continue;

          // check if all mods are installed
          bool matched = true;
          foreach (string group in modGroups)
          {
            string[] modIDs = group.Split('~');
            if (!modIDs.Any(id => ModMain.Helper.ModRegistry.IsLoaded(id.Trim())))
            {
              matched = false;
              break;
            }
          }

          if (matched)
          {
            folderName = folder.Name;
            modsMatched = modGroups.Length;
          }
        }
      }

      return folderName;
    }

    public void LoadCustomData()
    {
      LoadTooltips();
      LoadMarkerCropOffsets();
      LoadCustomNpcs();
      LoadCustomMapLocations();
    }

    private void LoadTooltips()
    {
      foreach (var tooltip in ModMain.CustomData.CustomMapTooltips)
      {
        string text = tooltip.Value.GetValue("SecondaryText") != null
          ? (string) tooltip.Value.GetValue("PrimaryText") + Environment.NewLine + tooltip.Value.GetValue("SecondaryText")
          : (string) tooltip.Value.GetValue("PrimaryText");

        Tooltips.Add(new ClickableComponent(
          new Rectangle(
            (int) tooltip.Value.GetValue("X"),
            (int) tooltip.Value.GetValue("Y"),
            (int) tooltip.Value.GetValue("Width"),
            (int) tooltip.Value.GetValue("Height")
          ),
          text
        ));
      }
    }

    private void LoadMarkerCropOffsets()
    {
      NpcMarkerOffsets = ModConstants.NpcMarkerOffsets;
    }

    // Any custom locations with given location on the map
    private void LoadCustomMapLocations()
    {
      foreach (var mapVectors in ModMain.CustomData.CustomMapLocations)
      {
        var mapVectorArr = new MapVector[mapVectors.Value.Length];
        for (var i = 0; i < mapVectors.Value.Length; i++)
        {
          var mapVector = mapVectors.Value[i];

          // Marker doesn't need to specify corresponding Tile position
          if (mapVector.GetValue("TileX") == null || mapVector.GetValue("TileY") == null)
            mapVectorArr[i] = new MapVector(
              (int)mapVector.GetValue("MapX"),
              (int)mapVector.GetValue("MapY")
            );
          // Region must specify corresponding Tile positions for
          // Calculations on movement within location
          else
            mapVectorArr[i] = new MapVector(
              (int)mapVector.GetValue("MapX"),
              (int)mapVector.GetValue("MapY"),
              (int)mapVector.GetValue("TileX"),
              (int)mapVector.GetValue("TileY")
            );      
        }

        MapVectors.Add(mapVectors.Key, mapVectorArr);
      }

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
          ModMain.IMonitor.Log($"Handled tracking for custom location: {customLocations[0]}.", LogLevel.Debug);
        }
        else
        {
          var locationList = "";
          for (var i = 0; i < customLocations.Length; i++)
          {
            locationList += customLocations[i] + (i + 1 == customLocations.Length ? "" : ", ");
          }

          ModMain.IMonitor.Log($"Handled tracking for custom locations: {locationList}.", LogLevel.Debug);
        }
      }
    }

    // Handle any modified NPC names 
    // Specifically mods that change names in dialogue files (displayName)
    public void GetCustomName(NPC npc)
    {
      if (npc is Horse) return;

      if (!Names.TryGetValue(npc.Name, out var customName))
      {
        if (npc.displayName == null)
        {
          Names.Add(npc.Name, npc.Name);
        }
        else
        {
          Names.Add(npc.Name, npc.displayName);
          if (!npc.Name.Equals(npc.displayName) || ModMain.Globals.CustomNpcMarkerOffsets.ContainsKey(npc.Name))
          {
            NpcCustomizations.Add(npc.Name);
          }
        }
      }
    }

    // Load user-specified NPC crops for custom sprites
    private void GetNpcCrop(NPC npc)
    {
      var CustomNpcMarkerOffsets = ModMain.Globals.CustomNpcMarkerOffsets;

      if (CustomNpcMarkerOffsets != null && CustomNpcMarkerOffsets.Count > 0)
        foreach (var villager in CustomNpcMarkerOffsets)
          if (npc.Name.Equals(villager.Key))
          {
            NpcMarkerOffsets[npc.Name] = villager.Value;
            NpcCustomizations.Add(npc.Name);
          }

      // If custom crop offset is not specified, default to 0
      if (!NpcMarkerOffsets.TryGetValue(npc.Name, out var crop)) NpcMarkerOffsets[npc.Name] = 0;
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

  public class CustomData
  {
    public Dictionary<string, JObject[]> CustomMapLocations { get; set; } = new Dictionary<string, JObject[]>();
    public Dictionary<string, JObject> CustomMapTooltips { get; set; } = new Dictionary<string, JObject>();
  }
}