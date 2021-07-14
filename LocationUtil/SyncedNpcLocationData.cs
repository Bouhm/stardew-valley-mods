// Synced NPC positions for multiplayer
using System.Collections.Generic;

internal class SyncedNpcLocationData
{
    public Dictionary<string, LocationData> Locations { get; set; }

    public SyncedNpcLocationData()
    {
        this.Locations = new Dictionary<string, LocationData>();
    }

    public void AddLocation(string name, LocationData location)
    {
        if (!this.Locations.ContainsKey(name))
            this.Locations.Add(name, location);
        else
            this.Locations[name] = location;
    }
}
