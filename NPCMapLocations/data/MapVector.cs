// Class for Location Vectors
// Maps the tileX and tileY in a game location to the location on the map
public class MapVector
{
  public MapVector(int x, int y)
  {
    MapX = x;
    MapY = y;
  }

  public MapVector(int x, int y, int tileX, int tileY)
  {
    MapX = x;
    MapY = y;
    TileX = tileX;
    TileY = tileY;
  }

  public int TileX { get; set; } // tileX in a game location
  public int TileY { get; set; } // tileY in a game location
  public int MapX { get; set; } // Absolute position relative to map
  public int MapY { get; set; } // Absolute position relative to map
}