using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPCMapLocations
{
    // Uses chatbox to broadcast location data to other players
    internal class ModBroadcaster
    {
        public static string locationData;

        public ModBroadcaster(IModHelper helper)
        {
            var messages = helper.Reflection.GetField<List<ChatMessage>>(Game1.chatBox, "messages").GetValue();
        }

        public static void BroadcastData(string data)
        {

        }
    }
}
