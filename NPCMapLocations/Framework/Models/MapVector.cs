using Newtonsoft.Json;

namespace NPCMapLocations.Framework.Models
{
    /// <summary>An in-world tile coordinate and map pixel which represent the same position.</summary>
    public class MapVector
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The X tile coordinate for the in-game position.</summary>
        public int TileX { get; set; }

        /// <summary>The Y tile coordinate for the in-game position.</summary>
        public int TileY { get; set; }

        /// <summary>The X pixel coordinate relative to the top-left corner of the map.</summary>
        public int MapX { get; set; }

        /// <summary>The Y pixel coordinate relative to the top-left corner of the map.</summary>
        public int MapY { get; set; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="x">The X pixel coordinate relative to the top-left corner of the map.</param>
        /// <param name="y">The Y pixel coordinate relative to the top-left corner of the map.</param>
        [JsonConstructor]
        public MapVector(int x, int y)
        {
            this.MapX = x;
            this.MapY = y;
        }

        /// <summary>Construct an instance.</summary>
        /// <param name="x">The X pixel coordinate relative to the top-left corner of the map.</param>
        /// <param name="y">The Y pixel coordinate relative to the top-left corner of the map.</param>
        /// <param name="tileX">The X tile coordinate for the in-game position.</param>
        /// <param name="tileY">The Y tile coordinate for the in-game position.</param>
        [JsonConstructor]
        public MapVector(int x, int y, int tileX, int tileY)
        {
            this.MapX = x;
            this.MapY = y;
            this.TileX = tileX;
            this.TileY = tileY;
        }
    }
}
