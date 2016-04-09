using StardewModdingAPI;
using System.Collections.Generic;

namespace NPCMapLocations
{
    public class Configuration : Config
    {
        public int nameTooltipMode { get; set; }
        public string menuKey { get; set; }
        public int immersionLevel { get; set; }
        public bool onlySameLocation { get; set; }
        public bool showAbigail { get; set; }
        public bool showAlex { get; set; }
        public bool showCaroline { get; set; }
        public bool showClint { get; set; }
        public bool showDemetrius { get; set; }
        public bool showElliott { get; set; }
        public bool showEmily { get; set; }
        public bool showEvelyn { get; set; }
        public bool showGeorge { get; set; }
        public bool showGus { get; set; }
        public bool showHaley { get; set; }
        public bool showHarvey { get; set; }
        public bool showJas { get; set; }
        public bool showJodi { get; set; }
        public bool showKent { get; set; }
        public bool showLeah { get; set; }
        public bool showLewis { get; set; }
        public bool showLinus { get; set; }
        public bool showMarnie { get; set; }
        public bool showMaru { get; set; }
        public bool showPam { get; set; }
        public bool showPenny { get; set; }
        public bool showPierre { get; set; }
        public bool showRobin { get; set; }
        public bool showSam { get; set; }
        public bool showSebastian { get; set; }
        public bool showShane { get; set; }
        public bool showVincent { get; set; }
        public bool showWilly { get; set; }


        public override T GenerateDefaultConfig<T>()
        {
            this.nameTooltipMode = 1;
            this.menuKey = "Tab";
            this.immersionLevel = 1;
            this.onlySameLocation = false;
            this.showAbigail = true;
            this.showAlex = true;
            this.showCaroline = true;
            this.showClint = true;
            this.showDemetrius = true;
            this.showElliott = true;
            this.showEmily = true;
            this.showEvelyn = true;
            this.showGeorge = true;
            this.showGus = true;
            this.showHaley = true;
            this.showHarvey = true;
            this.showJas = true;
            this.showJodi = true;
            this.showKent = true;
            this.showLeah = true;
            this.showLewis = true;
            this.showLinus = true;
            this.showMarnie = true;
            this.showMaru = true;
            this.showPam = true;
            this.showPenny = true;
            this.showPierre = true;
            this.showRobin = true;
            this.showSam = true;
            this.showSebastian = true;
            this.showShane = true;
            this.showVincent = true;
            this.showWilly = true;
            return this as T;
        }
    }
}
    