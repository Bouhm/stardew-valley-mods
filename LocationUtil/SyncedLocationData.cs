// Synced NPC tile positions for multiplayer
using System.Collections.Generic;

internal class SyncedLocationData
{
  public Dictionary<string, LocationData> SyncedLocations { get; set; }

  public SyncedLocationData()
  {
    SyncedLocations = new Dictionary<string, LocationData>();
  }

  public void AddNpcLocation(string name, LocationData location)
  {
    if (!SyncedLocations.ContainsKey(name))
    {
      SyncedLocations.Add(name, location);
    }
    else
    {
      SyncedLocations[name] = location;
    }
  }
}

// Used for syncing only the necessary data
internal class LocationData
{
  public string LocationName { get; set; }
  public int TileX { get; set; }
  public int TileY { get; set; }

  public LocationData(string locationName, int tileX, int tileY)
  {
    this.LocationName = locationName;
    this.TileX = tileX;
    this.TileY = tileY;
  }
}
