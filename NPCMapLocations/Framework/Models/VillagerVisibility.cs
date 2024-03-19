namespace NPCMapLocations.Framework.Models
{
    /// <summary>Which villagers should be visible on the world map.</summary>
    public enum VillagerVisibility
    {
        /// <summary>Show all villagers on the map.</summary>
        All = 1,

        /// <summary>Show villagers the player has talked to today.</summary>
        TalkedTo,

        /// <summary>Show villagers the player has not talked to today.</summary>
        NotTalkedTo
    }
}
