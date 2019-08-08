// Synced NPC tile positions for multiplayer
using System.Collections.Generic;

internal class SyncedNpcPositions
{
  public Dictionary<string, NpcPosition> SyncedLocations { get; set; }

  public SyncedNpcPositions()
  {
    SyncedLocations = new Dictionary<string, NpcPosition>();
  }

  public void AddNpcLocation(string name, NpcPosition location)
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
internal class NpcPosition
{
  public string LocationName { get; set; }
  public int TileX { get; set; }
  public int TileY { get; set; }

  public NpcPosition(string locationName, int tileX, int tileY)
  {
    this.LocationName = locationName;
    this.TileX = tileX;
    this.TileY = tileY;
  }
}
