using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace NPCMapLocations
{
  class SyncedLocationData
  {
    public Dictionary<string, MapLocationData> SyncedLocations { get; set; }

    public SyncedLocationData()
    {
      SyncedLocations = new Dictionary<string, MapLocationData>();
    }

    public void AddNpcLocation(string name, MapLocationData mapLocation) 
    {
      if (!SyncedLocations.ContainsKey(name))
      {
        SyncedLocations.Add(name, mapLocation);
      }
      else
      {
        SyncedLocations[name] = mapLocation;
      }
    }
  }

  // Used for syncing only the necessary data
  internal class MapLocationData
  {
    public string LocationName { get; set; }
    public int TileX { get; set; }
    public int TileY { get; set; }

    public MapLocationData(string locationName, int tileX, int tileY)
    {
      this.LocationName = locationName;
      this.TileX = tileX;
      this.TileY = tileY;
    }
  }
}
