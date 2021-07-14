using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace LocationCompass.Framework
{
    // Class for locators AKA the 'needles' of the compass
    internal class Locator
    {
        public string Name { get; set; }
        public Farmer Farmer { get; set; }
        public Texture2D Marker { get; set; }
        public double Proximity { get; set; }
        public int Quadrant { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public double Angle { get; set; }
        public bool IsHorse { get; set; }
        public bool IsWarp { get; set; }
        public bool IsOutdoors { get; set; }
        public bool IsOnScreen { get; set; }
    }
}
