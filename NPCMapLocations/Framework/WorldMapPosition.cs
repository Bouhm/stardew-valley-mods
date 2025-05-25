using StardewValley.WorldMaps;

namespace NPCMapLocations.Framework;

/// <summary>A marker position on the world map.</summary>
/// <param name="RegionId">The world map region containing the marker.</param>
/// <param name="X">The marker's pixel X coordinate relative to the top-left corner of the map.</param>
/// <param name="Y">The marker's pixel Y coordinate relative to the top-left corner of the map.</param>
public record WorldMapPosition(string? RegionId, int X, int Y)
{
    /// <summary>An unknown or invalid map position.</summary>
    public static readonly WorldMapPosition Empty = new WorldMapPosition(null, 0, 0);

    /// <summary>Get whether this is an unknown or invalid mpa position.</summary>
    public bool IsEmpty => this.RegionId == null;

    /// <summary>Construct an instance.</summary>
    /// <param name="position">The map area position from the game data.</param>
    public static WorldMapPosition Create(MapAreaPositionWithContext position)
    {
        // note: this can't be a constructor for compatibility with Json.NET, since we can't
        // put [JsonConstructor] on the primary constructor for a record class.

        var pixel = position.GetMapPixelPosition();
        int x = (int)pixel.X;
        int y = (int)pixel.Y;

        return new WorldMapPosition(position.Data.Region.Id, x, y);
    }
}
