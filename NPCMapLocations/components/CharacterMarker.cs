// Class for map markers
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class CharacterMarker
{
  public string LocationName { get; set; }
  public Texture2D Marker { get; set; }
  public int MapX { get; set; }
  public int MapY { get; set; }
  public bool IsBirthday { get; set; }
  public bool HasQuest { get; set; }
  public bool IsHidden { get; set; }
  public int Layer { get; set; }

  public CharacterMarker()
  {
    LocationName = "";
    Marker = null;
    MapX = -9999;
    MapY = -9999;
    IsBirthday = false;
    HasQuest = false;
    IsHidden = false;
    Layer = 1;
  }
}