using System;
using System.Collections.Generic;

namespace NPCMapLocations.Framework;

/// <summary>The model for NPC Map Location's internal data file.</summary>
internal class DataModel
{
    /*********
    ** Accessors
    *********/
    /// <summary>The NPC C# types which should be ignored by the mod.</summary>
    public HashSet<string> IgnoreNpcTypes { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="ignoreNpcTypes"><inheritdoc cref="IgnoreNpcTypes" path="/summary"/></param>
    public DataModel(string[]? ignoreNpcTypes)
    {
        this.IgnoreNpcTypes = new HashSet<string>(ignoreNpcTypes ?? [], StringComparer.OrdinalIgnoreCase);
    }
}
