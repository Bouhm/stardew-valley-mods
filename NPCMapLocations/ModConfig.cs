/*
 * Config file for mod settings.
 */
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;

namespace NPCMapLocations
{
	public class ModConfig
	{
	  public int NameTooltipMode { get; set; } = 1;
	  public int ImmersionOption { get; set; } = 1;
	  public bool ByHeartLevel { get; set; } = false;
	  public int HeartLevelMin { get; set; } = 0;
	  public int HeartLevelMax { get; set; } = 12;
	  public bool OnlySameLocation { get; set; } = false;
	  public bool ShowHiddenVillagers { get; set; } = false;
	  public bool MarkQuests { get; set; } = true;
	  public bool ShowTravelingMerchant { get; set; } = true;
	  public string MenuKey { get; set; } = "Tab";
	  public string TooltipKey { get; set; } = "Space";
	  public bool ShowFarmBuildings { get; set; } = true;

    public bool ShowMinimap { get; set; } = false;
	  public int MinimapX { get; set; } = 12;
	  public int MinimapY { get; set; } = 12;
	  public int MinimapWidth { get; set; } = 75;
	  public int MinimapHeight { get; set; } = 45;
	  public string MinimapDragKey { get; set; } = "LeftAlt";
	  public string MinimapToggleKey { get; set; } = "OemPipe";
	  public HashSet<string> MinimapBlacklist { get; set; } = new HashSet<string>() {};

    public string MapRecolor { get; set; } = "";
	  public bool UseSeasonalMaps { get; set; } = true;
    public Dictionary<string, int> CustomNpcs { get; set; } = new Dictionary<string, int>();
    public HashSet<string> NpcBlacklist { get; set; } = new HashSet<string>() {};
    public Dictionary<string, JObject[]> CustomMapLocations { get; set; } = new Dictionary<string, JObject[]>();
    public Dictionary<string, JObject> CustomMapMarkers { get; set; } = new Dictionary<string, JObject>();
	  public bool DEBUG_MODE { get; set; } = false;
  }
}