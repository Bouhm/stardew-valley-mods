// Synced NPC positions for multiplayer
using System.Collections.Generic;

namespace Bouhm.Shared.Locations;

internal class SyncedNpcLocationData
{
    /*********
    ** Accessors
    *********/
    public Dictionary<string, LocationData> Locations { get; set; } = [];
}
