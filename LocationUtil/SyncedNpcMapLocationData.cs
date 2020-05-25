// Synced NPC positions for multiplayer
using Microsoft.Xna.Framework;
using System.Collections.Generic;

internal class SyncedNpcMapLocationData
{
  public Dictionary<string, MapLocationData> MapLocations { get; set; }

  public SyncedNpcMapLocationData()
  {
    MapLocations = new Dictionary<string, MapLocationData>();
  }

  public void AddNpcLocation(string name, MapLocationData location)
  {
    if (!MapLocations.ContainsKey(name))
    {
      MapLocations.Add(name, location);
    }
    else
    {
      MapLocations[name] = location;
    }
  }
}

// Used for syncing only the necessary data
internal class MapLocationData
{
  public Vector2 MapLocation { get; set; }
  public bool IsBirthday { get; set; }

  public MapLocationData(Vector2 mapLocation)
  {
    this.MapLocation = mapLocation;
  }
}