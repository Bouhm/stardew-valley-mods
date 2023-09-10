using System.Collections.Generic;

namespace NPCMapLocations.Framework.Models
{
    /// <summary>The model for per-player config options.</summary>
    public class PlayerConfig
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The maximum heart level that can be configured for <see cref="HeartLevelMin"/> or <see cref="HeartLevelMax"/>.</summary>
        internal const int MaxPossibleHeartLevel = 12;

        /// <summary>Which NPCs to display.</summary>
        public VillagerVisibility ImmersionOption { get; set; } = VillagerVisibility.All;

        /// <summary>Whether to only show villagers whose hearts with the player is between <see cref="HeartLevelMin"/> and <see cref="HeartLevelMax"/>.</summary>
        public bool ByHeartLevel { get; set; } = false;

        /// <summary>The minimum heart level for <see cref="ByHeartLevel"/>.</summary>
        public int HeartLevelMin { get; set; } = 0;

        /// <summary>The maximum heart level for <see cref="ByHeartLevel"/>.</summary>
        public int HeartLevelMax { get; set; } = MaxPossibleHeartLevel;

        /// <summary>The NPCs to show/hide on the map regardless of <see cref="GlobalConfig"/>, indexed by name.</summary>
        public IDictionary<string, bool> ForceNpcVisibility { get; set; } = new Dictionary<string, bool>();
    }
}
