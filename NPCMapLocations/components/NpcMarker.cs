// Class for map markers
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class NpcMarker
{
  public string Name { get; set; }
  public string LocationName { get; set; }
  public Texture2D Marker { get; set; }
  public int MapX { get; set; }
  public int MapY { get; set; }
  public bool IsBirthday { get; set; }
  public Character Type { get; set; }
  public bool HasQuest { get; set; }
  public bool IsHidden { get; set; }
  public int Layer { get; set; }

  public NpcMarker()
  {
    Name = null;
    LocationName = null;
    Marker = null;
    MapX = -9999;
    MapY = -9999;
    IsBirthday = false;
    HasQuest = false;
    IsHidden = false;
    Layer = 4;
    Type = Character.Villager;
  }
}

public enum Character
{
  Villager,
  Child,
  Horse
}