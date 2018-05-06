/*
 * Config file for mod settings.
 */
using System.Collections.Generic;

namespace NPCMapLocations
{
    public class ModConfig
    {
        public string MenuKey { get; set; } = "Tab";
        public string TooltipKey { get; set; } = "Space";
        public int NameTooltipMode { get; set; } = 1;
        public int ImmersionOption { get; set; } = 1;
        public bool ByHeartLevel { get; set; } = false;
        public int HeartLevelMin { get; set; } = 0;
        public int HeartLevelMax { get; set; } = 12;
        public bool OnlySameLocation { get; set; } = false;
        public bool ShowHiddenVillagers { get; set; } = false;
        public bool MarkQuests { get; set; } = true;
        public HashSet<string> NPCBlacklist { get; set; } = new HashSet<string>() { "Marlon", "Sandy" };
        public bool ShowTravelingMerchant { get; set; } = true;
        public Dictionary<string, int> VillagerCrop { get; set; } = new Dictionary<string, int>();
        public List<string> CustomNPCBlacklist = new List<string>();
        public bool ShowFarmBuildings { get; set; } = true;
        public Dictionary<string, object> CustomNPCs { get; set; } = new Dictionary<string, object>();
    }
}
