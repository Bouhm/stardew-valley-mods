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

        /// <summary>Track the start of a possible mouse click &amp; drag.</summary>
        public static void HandleMouseDown()
        {
            BeginMousePosition = MouseUtil.GetScreenPosition();
        }

        /// <summary>Track the end of a possible mouse click &amp; drag.</summary>
        public static void HandleMouseRelease()
        {
            EndMousePosition = MouseUtil.GetScreenPosition();
        }

        /// <summary>Get the current mouse position on the screen as a vector.</summary>
        /// <param name="uiScale">Whether to apply UI scaling to the position.</param>
        public static Vector2 GetScreenPosition(bool uiScale = true)
        {
            return new Vector2(Game1.getMouseX(uiScale), Game1.getMouseY(uiScale));
        }

        /// <summary>Get the current mouse position on the screen as a point.</summary>
        /// <param name="uiScale">Whether to apply UI scaling to the position.</param>
        public static Point GetScreenPoint(bool uiScale = true)
        {
            return Game1.getMousePosition(uiScale);
        }
    }
}
