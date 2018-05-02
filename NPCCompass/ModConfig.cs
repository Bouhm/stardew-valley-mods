using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPCCompass
{
    class ModConfig
    {
        public bool Toggle { get; set; } = false;
        public string ShowKeyCode { get; set; } = "OemQuotes"; // Quotes key
        public List<string> NPCBlacklist { get; set; } = new List<string>();
    }
}
