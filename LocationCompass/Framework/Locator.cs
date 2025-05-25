using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace LocationCompass.Framework;

// Class for locators AKA the 'needles' of the compass
internal record Locator(string Name, Farmer? Farmer, Texture2D? Marker, double Proximity, int Quadrant, float X, float Y, double Angle, bool IsHorse, bool IsWarp, bool IsOutdoors, bool IsOnScreen)
{
    /*********
    ** Accessors
    *********/
    public double Proximity { get; set; } = Proximity;
    public double Angle { get; set; } = Angle;
}
