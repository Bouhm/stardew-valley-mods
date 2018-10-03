using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocationCompass
{
    class ModConfig
    {
        public bool Toggle { get; set; } = false;
        public string ShowKeyCode { get; set; } = "LeftAlt";
        public List<string> NPCBlacklist { get; set; } = new List<string>();
    }
}
