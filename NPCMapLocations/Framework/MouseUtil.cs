using System;
using Microsoft.Xna.Framework;
using StardewValley;

namespace NPCMapLocations.Framework
{
    /// <summary>Provides utilities for calculating tile positions, map positions, or drag &amp; drop positions based on the cursor position.</summary>
    internal static class MouseUtil
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The cursor position where the player first pressed the mouse button, if it's still down.</summary>
        public static Vector2 BeginMousePosition { get; set; }

        /// <summary>The cursor position where the player released the mouse button, if it's released.</summary>
        public static Vector2 EndMousePosition { get; set; }


        /*********
        ** Public methods
        *********/
        /// <summary>Reset the drag tracking positions.</summary>
        public static void Reset()
        {
            BeginMousePosition = new Vector2(-1000, -1000);
            EndMousePosition = new Vector2(-1000, -1000);
        }

        /// <summary>Get the cursor's tile coordinate.</summary>
        public static Vector2 GetTilePositionAtCursor()
        {
            return Game1.currentCursorTile;
        }

        /// <summary>Get the pixel position of the cursor on the map, relative to the top-left corner of the map.</summary>
        public static Vector2 GetMapPositionAtCursor()
        {
            Vector2 mapPos = Utility.getTopLeftPositionForCenteringOnScreen(ModEntry.Map.Bounds.Width * 4, 720);
            return new Vector2((int)(Math.Ceiling(Game1.getMousePosition().X - mapPos.X)), (int)(Math.Ceiling(Game1.getMousePosition().Y - mapPos.Y)));
        }

        /// <summary>Track the start of a possible mouse click &amp; drag.</summary>
        public static void HandleMouseDown()
        {
            BeginMousePosition = new Vector2(Game1.getMouseX(), Game1.getMouseY());
        }

        /// <summary>Track the end of a possible mouse click &amp; drag.</summary>
        public static void HandleMouseRelease()
        {
            EndMousePosition = new Vector2(Game1.getMouseX(), Game1.getMouseY());
        }

        /// <summary>Get the area which the player is currently clicking &amp; dragging.</summary>
        /// <remarks>This assumes <see cref="HandleMouseDown"/> was called and the player is still dragging.</remarks>
        public static Rectangle GetCurrentDraggingArea()
        {
            return new((int)BeginMousePosition.X, (int)BeginMousePosition.Y, (int)(Game1.getMouseX() - BeginMousePosition.X), (int)(Game1.getMouseY() - BeginMousePosition.Y));
        }

        /// <summary>Get the area which the player last clicked &amp; dragged.</summary>
        /// <remarks>This assumes <see cref="HandleMouseDown"/> and then <see cref="HandleMouseRelease"/> were called.</remarks>
        public static Rectangle GetDragAndDropArea()
        {
            return new((int)BeginMousePosition.X, (int)BeginMousePosition.Y, (int)(EndMousePosition.X - BeginMousePosition.X), (int)(EndMousePosition.Y - BeginMousePosition.Y));
        }

        // Convert absolute positions to map positions
        /// <summary>Get a pixel map area relative to the top-left corner of the map.</summary>
        /// <param name="rect">The absolute pixel area relative to the top-left corner of the screen.</param>
        public static Rectangle GetRectangleOnMap(Rectangle rect)
        {
            Vector2 mapBounds = Utility.getTopLeftPositionForCenteringOnScreen(ModEntry.Map.Bounds.Width * 4, 720);
            return new Rectangle((int)(rect.X - mapBounds.X), (int)(rect.Y - mapBounds.Y), rect.Width, rect.Height);
        }
    }
}
