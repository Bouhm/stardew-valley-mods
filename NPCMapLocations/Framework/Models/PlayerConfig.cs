using System.Collections.Generic;

namespace NPCMapLocations.Framework.Models;

/// <summary>The model for per-player config options.</summary>
public class PlayerConfig
{
    /*********
    ** Accessors
    *********/
    /// <summary>The NPCs to show/hide on the map regardless of <see cref="ModConfig"/>, indexed by name.</summary>
    public IDictionary<string, bool> ForceNpcVisibility { get; set; } = new Dictionary<string, bool>();
}
