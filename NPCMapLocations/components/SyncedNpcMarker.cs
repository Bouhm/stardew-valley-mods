using Microsoft.Xna.Framework.Graphics;

public class SyncedNpcMarker
{
  public string DisplayName { get; set; }
  public string LocationName { get; set; }
  public int MapX { get; set; }
  public int MapY { get; set; }
  public bool IsBirthday { get; set; }
  public Character Type { get; set; }
  public SyncedNpcMarker()
  {
    DisplayName = null;
    LocationName = null;
    MapX = -9999;
    MapY = -9999;
    IsBirthday = false;
    Type = Character.Villager;
  }
}

