// Class for Location Vectors
// Maps the tileX and tileY in a game location to the location on the map
using Newtonsoft.Json;

public class MapVector
{
    [JsonConstructor]
    public MapVector(int x, int y)
    {
        this.MapX = x;
        this.MapY = y;
    }

    [JsonConstructor]
    public MapVector(int x, int y, int tileX, int tileY)
    {
        this.MapX = x;
        this.MapY = y;
        this.TileX = tileX;
        this.TileY = tileY;
    }

    public int TileX { get; set; } // tileX in a game location
    public int TileY { get; set; } // tileY in a game location
    public int MapX { get; set; } // Absolute position relative to map
    public int MapY { get; set; } // Absolute position relative to map
}
