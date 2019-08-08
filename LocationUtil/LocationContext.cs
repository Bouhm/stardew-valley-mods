using System.Collections.Generic;
using Microsoft.Xna.Framework;

// Used for determining if an NPC is in the same root location
// as the player as well as indoor location a room belongs to
internal class LocationContext
{
  public string Type { get; set; } // outdoors, building, or room
  public string Root { get; set; } // Top-most outdoor location
  public string Parent { get; set; } // Level above
  public Dictionary<string, Vector2> Neighbors { get; set; } = new Dictionary<string, Vector2>(); // Connected outdoor locations
  public List<string> Children { get; set; } // Levels below
  public Vector2 Warp { get; set; } // Position of warp
}
