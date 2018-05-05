/*
 * Config file for mod settings.
 */
using System.Collections.Generic;

namespace NPCMapLocations
{
    public class ModConfig
    {
        public string MenuKey { get; set; } = "tab";
        public string TooltipKey { get; set; } = "space";
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
        public bool ShowCustomNPC1 { get; set; } = true;
        public bool ShowCustomNPC2 { get; set; } = true;
        public bool ShowCustomNPC3 { get; set; } = true;
        public bool ShowCustomNPC4 { get; set; } = true;
        public bool ShowCustomNPC5 { get; set; } = true;
        public bool ShowFarmBuildings { get; set; } = true;
        public Dictionary<string, Dictionary<string, int>> CustomNPCs { get; set; } = new Dictionary<string, Dictionary<string, int>>();
    }
}
