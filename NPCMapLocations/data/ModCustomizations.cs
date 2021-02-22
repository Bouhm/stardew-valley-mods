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
    public Dictionary<string, MapVector[]> MapVectors { get; set; }
    public Dictionary<string, string> Names { get; set; }
    public HashSet<string> LocationExclusions { get; set; }
    public Dictionary<string, MapTooltip> Tooltips { get; set; }

    public readonly string MapsRootPath = "maps";
    public string MapsPath { get; set; }

    public ModCustomizations()
    {
      MapsPath = GetCustomMapFolderName();
      if (MapsPath != null)
        MapsPath = Path.Combine(MapsRootPath, MapsPath);
      else
        MapsPath = Path.Combine(MapsRootPath, "_default");

      Names = new Dictionary<string, string>();
      MapVectors = new Dictionary<string, MapVector[]>();
      Tooltips = new Dictionary<string, MapTooltip>();
      LocationExclusions = new HashSet<string>();
    }

    public void LoadCustomData(Dictionary<string, JObject> CustomNpcJson, Dictionary<string, JObject> CustomLocationJson)
    {
      LoadCustomLocations(CustomLocationJson);
      LoadCustomNpcs(CustomNpcJson);
    }

    private void LoadCustomLocations(Dictionary<string, JObject> customLocationJson)
    {
      foreach (var locationData in customLocationJson)
      {
        var location = locationData.Value;
        if (location.ContainsKey("MapVectors"))
        {
          AddCustomMapLocation(locationData.Key, (JArray)location.GetValue("MapVectors"));
        }
        if (location.ContainsKey("MapTooltip"))
        {
          AddTooltip(locationData.Key, (JObject)location.GetValue("MapTooltip"));
        }
        if (location.ContainsKey("Exclude"))
        {
          if ((bool) location.GetValue("Exclude"))
          {
            LocationExclusions.Add(locationData.Key);
          }
        }
      }
    }

    // Handles customizations for NPCs
    // Custom NPCs and custom names or sprites for existing NPCs
    private void LoadCustomNpcs(Dictionary<string, JObject> customNpcJson)
    {
      var npcMarkerOffsets = ModConstants.NpcMarkerOffsets;
      var npcExclusions = ModMain.Globals.NpcExclusions;

      foreach (var npcData in customNpcJson)
      {
        var npc = npcData.Value;

        if (npc.ContainsKey("Exclude"))
        {
          if ((bool)npc.GetValue("Exclude"))
          {
            npcExclusions.Add(npcData.Key);
            continue;
          }
        }

        if (npc.ContainsKey("MarkerCropOffset"))
        {
          npcMarkerOffsets[npcData.Key] = (int)npc.GetValue("MarkerCropOffset");
        }
        else
        {
          var gameNpc = Game1.getCharacterFromName(npcData.Key);
          if (gameNpc != null)
          {
            // If custom crop offset is not specified, default to 0
            if (!npcMarkerOffsets.TryGetValue(gameNpc.Name, out var crop)) npcMarkerOffsets[gameNpc.Name] = 0;

            // Children sprites are short so give them a booster seat
            if (gameNpc is Child)
            {
              npcMarkerOffsets[gameNpc.Name] += 7;
            }
          }
        }
      }

      // Merge customizations into globals config
      ModMain.Globals.NpcMarkerOffsets = MergeDictionaries(npcMarkerOffsets, ModMain.Globals.NpcMarkerOffsets);
      ModMain.Globals.NpcExclusions = npcExclusions;

      foreach (var character in Utility.getAllCharacters())
      {
        // Handle any modified NPC names 
        // Specifically mods that change names in dialogue files (displayName)
        if (!Names.TryGetValue(character.Name, out var customName))
        {
          var displayName = (character.displayName != null && Game1.IsEnglish()) ? character.displayName : character.Name;

          Names.Add(character.Name, displayName);
        }
      }

      // For farmhands, custom NPCs can't be found so rely on config
      if (Context.IsMultiplayer && !Context.IsMainPlayer)
      {
        var NpcMarkerOffsets = ModMain.Globals.NpcMarkerOffsets;

        if (NpcMarkerOffsets != null && NpcMarkerOffsets.Count > 0)
        {
          foreach (var villager in NpcMarkerOffsets)
          {
            NpcMarkerOffsets[villager.Key] = villager.Value;
          }
        }
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

      ModMain.Helper.Data.WriteJsonFile($"config/{Constants.SaveFolderName}.json", ModMain.Config);
      ModMain.Helper.Data.WriteJsonFile($"config/globals.json", ModMain.Globals);
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

    private void AddTooltip(string locationName, JObject tooltip)
    {
      Tooltips[locationName] = new MapTooltip(
        (int)tooltip.GetValue("X"),
        (int)tooltip.GetValue("Y"),
        (int)tooltip.GetValue("Width"),
        (int)tooltip.GetValue("Height"),
        (string)tooltip.GetValue("PrimaryText"),
        (string)tooltip.GetValue("SecondaryText")
      );

      if (tooltip.ContainsKey("SecondaryText"))
      {
        Tooltips[locationName].SecondaryText = (string)tooltip.GetValue("SecondaryText");
      }
    }

    // Any custom locations with given location on the map
    private void AddCustomMapLocation(string locationName, JArray mapLocations)
    {
      var mapVectors = mapLocations.ToObject<JObject[]>();
      var mapVectorArr = new MapVector[mapVectors.Length];
      for (var i = 0; i < mapVectors.Length; i++)
      {
        var mapVector = mapVectors[i];

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

      MapVectors.Add(locationName, mapVectorArr);
    }

    // Merge dictionaries, in case of key conflict d1 takes precendence
    private Dictionary<string, T> MergeDictionaries<T>(Dictionary<string, T> d1, Dictionary<string, T> d2)
    {
      var dictionaries = new Dictionary<string, T>[] { d1, d2 };
      return dictionaries.SelectMany(dict => dict)
              .ToLookup(pair => pair.Key, pair => pair.Value)
              .ToDictionary(group => group.Key, group => group.First());
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