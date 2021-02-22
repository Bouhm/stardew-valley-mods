/*
 * Config file for mod settings.
 */
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;

namespace NPCMapLocations
{
	public class PlayerConfig
	{
	  public int ImmersionOption { get; set; } = 1;
	  public bool ByHeartLevel { get; set; } = false;
	  public int HeartLevelMin { get; set; } = 0;
	  public int HeartLevelMax { get; set; } = 12;
  }

  public class GlobalConfig
  {
    public string MenuKey { get; set; } = "Tab";
    public string TooltipKey { get; set; } = "Space";
    public int NameTooltipMode { get; set; } = 1;
    public bool OnlySameLocation { get; set; } = false;
    public bool ShowHiddenVillagers { get; set; } = false;
    public bool ShowQuests { get; set; } = true;
    public bool ShowTravelingMerchant { get; set; } = true;
    public bool ShowMinimap { get; set; } = false;
    public bool ShowFarmBuildings { get; set; } = true;

    public string MinimapToggleKey { get; set; } = "OemPipe";
    public int MinimapX { get; set; } = 12;
    public int MinimapY { get; set; } = 12;
    public int MinimapWidth { get; set; } = 75;
    public int MinimapHeight { get; set; } = 45;

    public HashSet<string> MinimapExclusions { get; set; } = new HashSet<string>() { };
    public bool UseSeasonalMaps { get; set; } = true;
    public bool ShowChildren { get; set; } = false;
    public bool ShowHorse { get; set; } = true;
    public Dictionary<string, int> CustomNpcMarkerOffsets { get; set; } = new Dictionary<string, int>();
    public HashSet<string> NpcExclusions { get; set; } = new HashSet<string>() { };
    public bool UseDetailedIsland { get; set; } = true;
    public bool DEBUG_MODE { get; set; } = false;
  }
}