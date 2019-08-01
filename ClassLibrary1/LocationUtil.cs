using System;
using System.Collections.Generic;
using StardewValley;

// Library for methods the map out all the locations in SDV
// and other helpful functions
public class LocationUtil
{
  public static void GetLocationContexts()
  {
    locationContexts = new Dictionary<string, LocationContext>();
    foreach (var location in Game1.locations)
    {
      // Get outdoor neighbors
      if (location.IsOutdoors)
      {
        if (!locationContexts.ContainsKey(location.Name))
          locationContexts.Add(location.Name, new LocationContext());

        foreach (var warp in location.warps)
        {
          if (warp == null || Game1.getLocationFromName(warp.TargetName) == null) continue;
          var warpLocation = Game1.getLocationFromName(warp.TargetName);

          if (warpLocation.IsOutdoors)
          {
            if (!locationContexts[location.Name].Neighbors.ContainsKey(warp.TargetName))
              locationContexts[location.Name].Neighbors.Add(warp.TargetName, new Vector2(warp.X, warp.Y));
          }
        }
      }
      // Get root locations from indoor locations
      else
        MapRootLocations(location, null, null, false, Vector2.Zero);
    }

    foreach (var location in Game1.getFarm().buildings)
      MapRootLocations(location.indoors.Value, null, null, false, Vector2.Zero);
  }

  // Get Mines name from floor level
  public static string GetMinesLocationName(string locationName)
  {
    var mine = locationName.Substring("UndergroundMine".Length, locationName.Length - "UndergroundMine".Length);
    if (int.TryParse(mine, out var mineLevel))
    {
      // Skull cave
      if (mineLevel > 120)
        return "SkullCave";
      // Mines
      return "Mine";
    }

    return null;
  }

  // Used for determining if an NPC is in the same root location
  // as the player as well as indoor location a room belongs to
  internal class LocationContext
  {
    public string Type { get; set; } // outdoors, indoors, or room
    public string Root { get; set; } // Top-most outdoor location
    public string Parent { get; set; } // Level above
    public Dictionary<string, Vector2> Neighbors { get; set; } = new Dictionary<string, Vector2>(); // Connected outdoor locations
    public List<string> Children { get; set; } // Levels below
    public Vector2 Warp { get; set; } // Position of warp
  }
}
