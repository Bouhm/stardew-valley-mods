// Class for map markers
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class NpcMarker
{
  public string DisplayName { get; set; }
  public string LocationName { get; set; }
  public Texture2D Sprite { get; set; }
  public int CropOffset { get; set; }
  public int MapX { get; set; }
  public int MapY { get; set; }
  public bool IsBirthday { get; set; }
  public Character Type { get; set; }
  public bool HasQuest { get; set; }
  public bool IsHidden { get; set; }
  public int Layer { get; set; }

  public NpcMarker()
  {
    DisplayName = null;
    LocationName = null;
    Sprite = null;
    CropOffset = 0;
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