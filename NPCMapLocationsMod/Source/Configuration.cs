using StardewModdingAPI;

namespace NPCMapLocations
{
    public class Configuration : Config
    {
        public bool configStartup { get; set; }
        public int nameTooltipMode { get; set; }
        public string modeChangeKey { get; set; }

        public override T GenerateDefaultConfig<T>()
        {
            this.configStartup = false;
            this.nameTooltipMode = 1;
            this.modeChangeKey = "Tab";
            return this as T;
        }
    }
}
