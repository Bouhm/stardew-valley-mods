// Synced NPC positions for multiplayer
using System.Collections.Generic;

internal class SyncedNpcLocationData
{
  public Dictionary<string, LocationData> Locations { get; set; }

  public SyncedNpcLocationData()
  {
    Locations = new Dictionary<string, LocationData>();
  }

  public void AddLocation(string name, LocationData location)
  {
    if (!Locations.ContainsKey(name))
    {
      Locations.Add(name, location);
    }
    else
    {
      Locations[name] = location;
    }
  }
}

// Used for syncing only the necessary data
internal class LocationData
{
  public string LocationName { get; set; }
  public float X { get; set; }
  public float Y { get; set; }

  public LocationData(string locationName, float x, float y)
  {
    this.LocationName = locationName;
    this.X = x;
    this.Y = y;
  }
}