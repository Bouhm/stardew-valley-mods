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

        /// <summary>The marker's pixel X coordinate relative to the top-left corner of the map.</summary>
        public int MapX { get; set; }

        /// <summary>The marker's pixel Y coordinate relative to the top-left corner of the map.</summary>
        public int MapY { get; set; }

        /// <summary>The <see cref="MapX"/> value during the previous update tick.</summary>
        public int PrevMapX { get; set; }

        /// <summary>The <see cref="MapY"/> value during the previous update tick.</summary>
        public int PrevMapY { get; set; }

        /// <summary>The name of the location containing the player.</summary>
        public string LocationName { get; set; }

        /// <summary>The number of ticks until the marker should become visible.</summary>
        public int DrawDelay { get; set; }
    }
}
