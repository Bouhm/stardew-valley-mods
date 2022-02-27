using System.Collections.Generic;

namespace NPCMapLocations.Framework.Models
{
    /// <summary>The model for per-player config options.</summary>
    public class PlayerConfig
    {
        /*********
        ** Accessors
        *********/
        /// <summary>Which NPCs to display. The possible values are <c>1</c> (all villagers), <c>2</c> (villagers player has talked to), and <c>3</c> (villagers player has not talked to).</summary>
        public int ImmersionOption { get; set; } = 1;

        /// <summary>Whether to only show villagers whose hearts with the player is between <see cref="HeartLevelMin"/> and <see cref="HeartLevelMax"/>.</summary>
        public bool ByHeartLevel { get; set; } = false;

        /// <summary>The minimum heart level for <see cref="ByHeartLevel"/>.</summary>
        public int HeartLevelMin { get; set; } = 0;

        /// <summary>The maximum heart level for <see cref="ByHeartLevel"/>.</summary>
        public int HeartLevelMax { get; set; } = 12;

        /// <summary>The NPCs to show/hide on the map regardless of <see cref="GlobalConfig"/>, indexed by name.</summary>
        public IDictionary<string, bool> ForceNpcVisibility { get; set; } = new Dictionary<string, bool>();
    }
}
