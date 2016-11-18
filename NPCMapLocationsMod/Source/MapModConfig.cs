/*
 * Config file for mod settings.
 */
using System.Collections.Generic;

namespace NPCMapLocations
{
    public class MapModConfig
    {
        public int nameTooltipMode { get; set; } = 1;
        public string menuKey { get; set; } = "Tab";
        public int immersionLevel { get; set; } = 1;
        public bool byHeartLevel { get; set; } = false;
        public int heartLevelMin { get; set; } = 0;
        public int heartLevelMax { get; set; } = 12;
        public bool onlySameLocation { get; set; } = false;
        public bool showHiddenVillagers { get; set; } = false;
        public bool markQuests { get; set; } = true;
        public bool showAbigail { get; set; } = true;
        public bool showAlex { get; set; } = true;
        public bool showCaroline { get; set; } = true;
        public bool showClint { get; set; } = true;
        public bool showDemetrius { get; set; } = true;
        public bool showElliott { get; set; } = true;
        public bool showEmily { get; set; } = true;
        public bool showEvelyn { get; set; } = true;
        public bool showGeorge { get; set; } = true;
        public bool showGus { get; set; } = true;
        public bool showHaley { get; set; } = true;
        public bool showHarvey { get; set; } = true;
        public bool showJas { get; set; } = true;
        public bool showJodi { get; set; } = true;
        public bool showKent { get; set; } = true;
        public bool showLeah { get; set; } = true;
        public bool showLewis { get; set; } = true;
        public bool showLinus { get; set; } = true;
        public bool showMarnie { get; set; } = true;
        public bool showMaru { get; set; } = true;
        public bool showPam { get; set; } = true;
        public bool showPenny { get; set; } = true;
        public bool showPierre { get; set; } = true;
        public bool showRobin { get; set; } = true;
        public bool showSam { get; set; } = true;
        public bool showSebastian { get; set; } = true;
        public bool showShane { get; set; } = true;
        public bool showVincent { get; set; } = true;
        public bool showWilly { get; set; } = true;
        public bool showSandy { get; set; } = false;
        public bool showWizard { get; set; } = true;
        public bool showMarlon { get; set; } = false;
        public bool showTravelingMerchant { get; set; } = true;
        public Dictionary<string, int> villagerCrop { get; set; } = new Dictionary<string, int>();
        public bool showCustomNPC1 { get; set; } = true;
        public bool showCustomNPC2 { get; set; } = true;
        public bool showCustomNPC3 { get; set; } = true;
        public bool showCustomNPC4 { get; set; } = true;
        public bool showCustomNPC5 { get; set; } = true;
        public Dictionary<string, Dictionary<string, int>> customNPCs { get; set; } = new Dictionary<string, Dictionary<string, int>>();
    }
}
