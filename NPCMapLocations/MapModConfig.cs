/*
 * Config file for mod settings.
 */
using System.Collections.Generic;

namespace NPCMapLocations
{
    public class MapModConfig
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
        public bool ShowAbigail { get; set; } = true;
        public bool ShowAlex { get; set; } = true;
        public bool ShowCaroline { get; set; } = true;
        public bool ShowClint { get; set; } = true;
        public bool ShowDemetrius { get; set; } = true;
        public bool ShowElliott { get; set; } = true;
        public bool ShowEmily { get; set; } = true;
        public bool ShowEvelyn { get; set; } = true;
        public bool ShowGeorge { get; set; } = true;
        public bool ShowGus { get; set; } = true;
        public bool ShowHaley { get; set; } = true;
        public bool ShowHarvey { get; set; } = true;
        public bool ShowJas { get; set; } = true;
        public bool ShowJodi { get; set; } = true;
        public bool ShowKent { get; set; } = true;
        public bool ShowLeah { get; set; } = true;
        public bool ShowLewis { get; set; } = true;
        public bool ShowLinus { get; set; } = true;
        public bool ShowMarnie { get; set; } = true;
        public bool ShowMaru { get; set; } = true;
        public bool ShowPam { get; set; } = true;
        public bool ShowPenny { get; set; } = true;
        public bool ShowPierre { get; set; } = true;
        public bool ShowRobin { get; set; } = true;
        public bool ShowSam { get; set; } = true;
        public bool ShowSebastian { get; set; } = true;
        public bool ShowShane { get; set; } = true;
        public bool ShowVincent { get; set; } = true;
        public bool ShowWilly { get; set; } = true;
        public bool ShowSandy { get; set; } = false;
        public bool ShowWizard { get; set; } = true;
        public bool ShowMarlon { get; set; } = false;
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
