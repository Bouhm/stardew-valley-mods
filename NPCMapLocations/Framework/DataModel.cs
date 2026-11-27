using System;
using System.Collections.Generic;
using StardewValley.Extensions;

namespace NPCMapLocations.Framework;

/// <summary>The model for NPC Map Location's internal data file.</summary>
internal class DataModel
{
    /*********
    ** Accessors
    *********/
    /// <summary>The NPC C# types which should be ignored by the mod.</summary>
    public HashSet<string> IgnoreNpcTypes { get; }

    /// <summary>The overrides to apply to specific NPCs by default.</summary>
    public Dictionary<string, DataNpcModel> Npcs { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="ignoreNpcTypes"><inheritdoc cref="IgnoreNpcTypes" path="/summary"/></param>
    /// <param name="npcOverrides"><inheritdoc cref="Npcs" path="/summary"/></param>
    public DataModel(string[]? ignoreNpcTypes, Dictionary<string, DataNpcModel>? npcOverrides)
    {
        this.IgnoreNpcTypes = new HashSet<string>(ignoreNpcTypes ?? [], StringComparer.OrdinalIgnoreCase);

        this.Npcs = new Dictionary<string, DataNpcModel>(npcOverrides ?? [], StringComparer.OrdinalIgnoreCase);
        this.Npcs.RemoveWhere(p => p.Value is null);
    }
}
