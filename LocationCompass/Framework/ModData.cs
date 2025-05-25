using System.Collections.Generic;
using System.Runtime.Serialization;

namespace LocationCompass.Framework;

internal class ModData
{
    /*********
    ** Accessors
    *********/
    public Dictionary<string, int> MarkerCrop { get; set; } = [];


    /*********
    ** Public methods
    *********/
    // <summary>Normalize the model after it's deserialized.</summary>
    /// <param name="context">The deserialization context.</param>
    [OnDeserialized]
    internal void OnDeserialized(StreamingContext context)
    {
        this.MarkerCrop ??= [];
    }
}
