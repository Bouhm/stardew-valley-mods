namespace NPCMapLocations.Framework
{
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

        /// <summary>The marker's pixel X coordinate relative to the top-left corner of the map.</summary>
        public int MapX { get; set; } = -9999;

        /// <summary>The marker's pixel Y coordinate relative to the top-left corner of the map.</summary>
        public int MapY { get; set; } = -9999;

        /// <summary>Whether the NPC's birthday is today.</summary>
        public bool IsBirthday { get; set; }

        /// <summary>The NPC's character type.</summary>
        public CharacterType Type { get; set; } = CharacterType.Villager;
    }
}
