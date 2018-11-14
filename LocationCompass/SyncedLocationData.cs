using System.Collections.Generic;

namespace NPCMapLocations
{
  class SyncedLocationData
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
    public int PositionX { get; set; }
    public int PositionY { get; set; }

    public LocationData(string locationName, int positionX, int positionY)
    {
      this.LocationName = locationName;
      this.PositionX = positionX;
      this.PositionY = positionY;
    }
  }
}
