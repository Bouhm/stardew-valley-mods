namespace NPCMapLocations.Framework
{
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
    }
}
