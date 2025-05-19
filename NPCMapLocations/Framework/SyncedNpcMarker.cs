namespace NPCMapLocations.Framework;

/// <summary>The base data for an NPC marker on the map synchronized from the host player.</summary>
public class SyncedNpcMarker
{
    /*********
    ** Accessors
    *********/
    /// <summary>The NPC's translated display name.</summary>
    public string DisplayName { get; set; }

    /// <summary>The name of the location containing the NPC.</summary>
    public string LocationName { get; set; }

    /// <summary>The NPC's marker position on the world map.</summary>
    public WorldMapPosition WorldMapPosition { get; set; }

    /// <summary>Whether the NPC's birthday is today.</summary>
    public bool IsBirthday { get; set; }

    /// <summary>The NPC's character type.</summary>
    public CharacterType Type { get; set; } = CharacterType.Villager;

    /// <summary>The world map region ID, if available.</summary>
    public string WorldMapRegionId => this.WorldMapPosition?.RegionId;

    /// <summary>The marker's pixel X coordinate relative to the top-left corner of the map.</summary>
    public int WorldMapX => this.WorldMapPosition?.X ?? 0;

    /// <summary>The marker's pixel Y coordinate relative to the top-left corner of the map.</summary>
    public int WorldMapY => this.WorldMapPosition?.X ?? 0;
}
