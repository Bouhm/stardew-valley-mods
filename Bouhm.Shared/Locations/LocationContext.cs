using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Bouhm.Shared.Locations
{
    /// <summary>A location in the location graph with metadata and links to neighboring locations.</summary>
    internal class LocationContext
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The location type (e.g. outdoors, building, or room).</summary>
        public LocationType Type { get; set; }

        /// <summary>The name of the root outdoor location which directly or indirectly contains this location, if any.</summary>
        public string Root { get; set; }

        /// <summary>The name of the immediate parent which contains this location, if any.</summary>
        public string Parent { get; set; }

        /// <summary>The names of locations directly reachable via outgoing warps from this location, with the target tile position for each warp.</summary>
        public Dictionary<string, Vector2> Neighbors { get; set; } = new();

        /// <summary>The names of locations directly contained by this location.</summary>
        public List<string> Children { get; set; }

        /// <summary>The default entry position for incoming warps to this location.</summary>
        public Vector2 Warp { get; set; }
    }
}
