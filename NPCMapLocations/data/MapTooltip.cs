// Class for custom map tooltips
using Newtonsoft.Json;

public class MapTooltip
{
  public int X { get; set; } // Absolute position relative to map
  public int Y { get; set; } // Absolute position relative to map
  public int Width { get; set; } // Width of area on map
  public int Height { get; set; } // Height of area on map
  public string PrimaryText { get; set; } // Primary text
  public string SecondaryText { get; set; } // Secondary text (second line)

  public MapTooltip(int x, int y, int width, int height, string primaryText)
  {
    X = x;
    Y = y;
    Width = width;
    Height = height;
    PrimaryText = primaryText;
    SecondaryText = "";
  }

  [JsonConstructor]
  public MapTooltip(int x, int y, int width, int height, string primaryText, string secondaryText)
  {
    X = x;
    Y = y;
    Width = width;
    Height = height;
    PrimaryText = primaryText;
    SecondaryText = secondaryText;
  }
}