using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.WorldMaps;

namespace NPCMapLocations.Framework
{
    /// <summary>A marker position on the world map.</summary>
    /// <param name="RegionId">The world map region containing the marker.</param>
    /// <param name="X">The marker's pixel X coordinate relative to the top-left corner of the map.</param>
    /// <param name="Y">The marker's pixel Y coordinate relative to the top-left corner of the map.</param>
    public record WorldMapPosition(string RegionId, int X, int Y)
    {
        /// <summary>An unknown or invalid map position.</summary>
        public static readonly WorldMapPosition Empty = new WorldMapPosition(null, 0, 0);

        /// <summary>Get whether this is an unknown or invalid mpa position.</summary>
        public bool IsEmpty => this.RegionId == null;

        /// <summary>Construct an instance.</summary>
        /// <param name="data">The map area position from the game data.</param>
        /// <param name="location">The in-world location to match.</param>
        /// <param name="tile">The in-world tile coordinate within the <paramref name="location"/> to match.</param>
        public WorldMapPosition(MapAreaPosition data, GameLocation location, Point tile)
            : this(data.Region.Id, 0, 0)
        {
            var pixel = data.GetMapPixelPosition(location, tile);

            this.X = (int)pixel.X;
            this.Y = (int)pixel.Y;
        }
    }
}
