// Class for map markers
using Microsoft.Xna.Framework.Graphics;

public class FarmerMarker
{
  public string Name { get; set; }
  public int MapX { get; set; }
  public int MapY { get; set; }
  public int PrevMapX { get; set; }
  public int PrevMapY { get; set; }
  public string LocationName { get; set; }
  public int DrawDelay { get; set; }
}
