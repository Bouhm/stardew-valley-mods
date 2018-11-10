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
      public bool ShowFarmersOnly { get; set; } = false;
      public bool ShowHorses { get; set; } = false;
      public List<string> NPCBlacklist { get; set; } = new List<string>();
    }
}
