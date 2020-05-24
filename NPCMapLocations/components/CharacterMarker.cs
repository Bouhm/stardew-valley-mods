// Class for map markers
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class CharacterMarker
{
  public string Name { get; set; } // For any customized names; Npc.Name would be vanilla names
  public Texture2D Marker { get; set; }
  public Vector2 MapLocation { get; set; }
  public Vector2 PrevMapLocation { get; set; }
  public string SyncedLocationName { get; set; }
  public string PrevLocationName { get; set; }
  public bool IsBirthday { get; set; }
  public bool HasQuest { get; set; }
  public bool IsOutdoors { get; set; }
  public bool IsHidden { get; set; }
  public int Layer { get; set; }
  public int DrawDelay { get; set; }
}