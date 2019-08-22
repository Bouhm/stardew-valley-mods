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
	  public string MinimapDragKey { get; set; } = "LeftControl";
	  public string MinimapToggleKey { get; set; } = "OemPipe";
	  public HashSet<string> MinimapBlacklist { get; set; } = new HashSet<string>() {};

	  public bool UseSeasonalMaps { get; set; } = true;
    public HashSet<string> NpcBlacklist { get; set; } = new HashSet<string>() {};
  }
}