using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocationCompass
{
    class ModConfig
    {
        public string HoldKeyCode { get; set; } = "LeftAlt";
        public bool ShowFarmersOnly { get; set; } = true;
        public List<string> NPCBlacklist { get; set; } = new List<string>();
    }
}
