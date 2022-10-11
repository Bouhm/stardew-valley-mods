using System;
using Microsoft.Xna.Framework;
using StardewValley;

namespace NPCMapLocations.Framework.Menus
{
    /// <summary>A rectangle which is automatically adjusted to fit on-screen.</summary>
    internal class ScreenBounds
    {
        /*********
        ** Fields
        *********/
        /// <summary>The backing field for <see cref="ActualBounds"/>.</summary>
        private Rectangle? ActualBoundsImpl;

        /// <summary>The intended bounds configured through the public interface.</summary>
        private Rectangle ConfiguredBounds;

        /// <summary>The <see cref="ConfiguredBounds"/> adjusted to fit on-screen.</summary>
        private Rectangle ActualBounds => this.ActualBoundsImpl ??= this.GetActualBounds(this.ConfiguredBounds);


        /*********
        ** Accessors
        *********/
        /// <summary>The X pixel position adjusted to fit on-screen.</summary>
        public int X => this.ActualBounds.X;

        /// <summary>The Y pixel position adjusted to fit on-screen.</summary>
        public int Y => this.ActualBounds.Y;

        /// <summary>The pixel width adjusted to fit on-screen.</summary>
        public int Width => this.ActualBounds.Width;

        /// <summary>The pixel height adjusted to fit on-screen.</summary>
        public int Height => this.ActualBounds.Height;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="x">The X pixel position.</param>
        /// <param name="y">The Y pixel position.</param>
        /// <param name="width">The pixel width.</param>
        /// <param name="height">The pixel height.</param>
        public ScreenBounds(int x, int y, int width, int height)
        {
            this.ConfiguredBounds = new(x, y, width, height);
        }

        /// <summary>Set the underlying bounds which will be adjusted for the screen size.</summary>
        /// <param name="x">The X pixel position, or <c>null</c> to keep the current value.</param>
        /// <param name="y">The Y pixel position, or <c>null</c> to keep the current value.</param>
        /// <param name="width">The pixel width, or <c>null</c> to keep the current value.</param>
        /// <param name="height">The pixel height, or <c>null</c> to keep the current value.</param>
        public void SetDesiredBounds(int? x = null, int? y = null, int? width = null, int? height = null)
        {
            bool changed = false;

            if (x?.Equals(this.ConfiguredBounds.X) is false)
            {
                this.ConfiguredBounds.X = x.Value;
                changed = true;
            }

            if (y?.Equals(this.ConfiguredBounds.Y) is false)
            {
                this.ConfiguredBounds.Y = y.Value;
                changed = true;
            }

            if (width?.Equals(this.ConfiguredBounds.Width) is false)
            {
                this.ConfiguredBounds.Width = width.Value;
                changed = true;
            }

            if (height?.Equals(this.ConfiguredBounds.Height) is false)
            {
                this.ConfiguredBounds.Height = height.Value;
                changed = true;
            }

            if (changed)
                this.ActualBoundsImpl = null;
        }

        /// <summary>Get whether the adjusted screen bounds contain the given pixel position.</summary>
        /// <param name="position">The pixel position.</param>
        public bool Contains(Point position)
        {
            return this.ActualBounds.Contains(position);
        }

        /// <summary>Get whether the adjusted screen bounds contain the given pixel position.</summary>
        /// <param name="x">The pixel X position.</param>
        /// <param name="y">The pixel Y position.</param>
        public bool Contains(int x, int y)
        {
            return this.ActualBounds.Contains(x, y);
        }

        /// <summary>Reset the calculated screen bounds.</summary>
        public void Recalculate()
        {
            this.ActualBoundsImpl = null;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Recalculate the actual bounds to fit on-screen.</summary>
        /// <param name="configured">The configured bounds.</param>
        private Rectangle GetActualBounds(Rectangle configured)
        {
            // get screen size
            int screenWidth = Game1.uiViewport.Width;
            int screenHeight = Game1.uiViewport.Height;

            // get valid position & size values
            Rectangle bounds = new(
                x: Math.Clamp(configured.X, 0, screenWidth),
                y: Math.Clamp(configured.Y, 0, screenHeight),
                width: Math.Clamp(configured.Width, 0, screenWidth),
                height: Math.Clamp(configured.Height, 0, screenHeight)
            );

            // adjust if they extend past screen edge
            if (bounds.Right > screenWidth)
            {
                bounds.X = Math.Max(screenWidth - bounds.Width, 0);
                if (bounds.Right > screenWidth)
                    bounds.Width = screenWidth;
            }
            if (bounds.Bottom > screenHeight)
            {
                bounds.Y = Math.Max(screenHeight - bounds.Height, 0);
                if (bounds.Bottom > screenHeight)
                    bounds.Height = screenHeight;
            }

            return bounds;
        }
    }
}
