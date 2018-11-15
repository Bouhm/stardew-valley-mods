using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocationCompass
{
    class ModConfig
    {
    public bool HoldToToggle { get; set; } = true;
    public string ToggleKeyCode { get; set; } = "LeftAlt";
    public bool SameLocationOnly { get; set; } = true;
    public string SameLocationToggleKey { get; set; } = "H";
    public bool ShowQuestsAndBirthdaysOnly { get; set; } = false;
    public string QuestsOnlyToggleKey { get; set; } = "J";
    public bool ShowFarmersOnly { get; set; } = false;
    public string FarmersOnlyToggleKey { get; set; } = "K";
    public bool ShowHorses { get; set; } = false;
    public string HorsesToggleKey { get; set; } = "L";
    public List<string> NPCBlacklist { get; set; } = new List<string>();
    }
}
