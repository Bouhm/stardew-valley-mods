// Synced NPC positions for multiplayer
using System.Collections.Generic;

namespace Bouhm.Shared.Locations
{
    internal class SyncedNpcLocationData
    {
        public Dictionary<string, LocationData> Locations { get; set; } = new();
    }
}
