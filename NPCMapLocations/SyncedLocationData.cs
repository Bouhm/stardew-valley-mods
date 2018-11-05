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
    public Dictionary<string, Vector2> SyncedLocations { get; set; }

    public SyncedLocationData()
    {
      SyncedLocations = new Dictionary<string, Vector2>();
    }

    public void AddNpcLocation(string name, Vector2 mapLocation) 
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
}
