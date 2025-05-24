namespace NPCMapLocations.Framework;

/// <summary>A player marker on the map.</summary>
public class FarmerMarker
{
    /*********
    ** Accessors
    *********/
    /// <summary>The player name.</summary>
    public string Name { get; set; }

    /// <summary>The NPC's marker position on the world map.</summary>
    public WorldMapPosition WorldMapPosition { get; set; }

    /// <summary>The name of the location containing the player.</summary>
    public string LocationName { get; set; }

    /// <summary>The number of ticks until the marker should become visible.</summary>
    public int DrawDelay { get; set; }

    /// <summary>The world map region ID, if available.</summary>
    public string WorldMapRegionId => this.WorldMapPosition?.RegionId;

    /// <summary>The marker's pixel X coordinate relative to the top-left corner of the map.</summary>
    public int WorldMapX => this.WorldMapPosition?.X ?? 0;

    /// <summary>The marker's pixel Y coordinate relative to the top-left corner of the map.</summary>
    public int WorldMapY => this.WorldMapPosition?.Y ?? 0;
}
