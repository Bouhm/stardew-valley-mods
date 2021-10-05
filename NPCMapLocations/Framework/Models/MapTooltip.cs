using Newtonsoft.Json;

namespace NPCMapLocations.Framework.Models
{
    /// <summary>A tooltip to display on the map.</summary>
    public class MapTooltip
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The pixel X coordinate relative to the top-left corner of the map which triggers the tooltip.</summary>
        public int X { get; set; }

        /// <summary>The pixel Y coordinate relative to the top-left corner of the map which triggers the tooltip.</summary>
        public int Y { get; set; }

        /// <summary>The pixel width of the area which triggers the tooltip.</summary>
        public int Width { get; set; }

        /// <summary>The pixel height of the area which triggers the tooltip.</summary>
        public int Height { get; set; }

        /// <summary>The main text of the tooltip.</summary>
        public string PrimaryText { get; set; }

        /// <summary>The secondary text (shown on the second line) of the tooltip, if any.</summary>
        public string SecondaryText { get; set; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="x">The pixel X coordinate relative to the top-left corner of the map which triggers the tooltip.</param>
        /// <param name="y">The pixel Y coordinate relative to the top-left corner of the map which triggers the tooltip.</param>
        /// <param name="width">The pixel width of the area which triggers the tooltip.</param>
        /// <param name="height">The pixel height of the area which triggers the tooltip.</param>
        /// <param name="primaryText">The main text of the tooltip.</param>
        public MapTooltip(int x, int y, int width, int height, string primaryText)
        {
            this.X = x;
            this.Y = y;
            this.Width = width;
            this.Height = height;
            this.PrimaryText = primaryText;
            this.SecondaryText = "";
        }

        /// <summary>Construct an instance.</summary>
        /// <param name="x">The pixel X coordinate relative to the top-left corner of the map which triggers the tooltip.</param>
        /// <param name="y">The pixel Y coordinate relative to the top-left corner of the map which triggers the tooltip.</param>
        /// <param name="width">The pixel width of the area which triggers the tooltip.</param>
        /// <param name="height">The pixel height of the area which triggers the tooltip.</param>
        /// <param name="primaryText">The main text of the tooltip.</param>
        /// <param name="secondaryText">The secondary text (shown on the second line) of the tooltip, if any.</param>
        [JsonConstructor]
        public MapTooltip(int x, int y, int width, int height, string primaryText, string secondaryText)
        {
            this.X = x;
            this.Y = y;
            this.Width = width;
            this.Height = height;
            this.PrimaryText = primaryText;
            this.SecondaryText = secondaryText;
        }
    }
}
