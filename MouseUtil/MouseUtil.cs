using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System;

// Library for methods that get tile position, map position, drag and drop position
// based on the location of the cursor
internal class MouseUtil
{
  public static Vector2 BeginMousePosition { get; set; }
  public static Vector2 EndMousePosition { get; set; }
  public static bool IsMouseHeldDown { get; set; }
  public static Texture2D Map { get; set; }

  // Return Vector2 position of tile at cursor
  public static Vector2 GetTilePositionAtCursor()
  {
    return Game1.currentCursorTile;
  }

  // Return Vector2 position of pixel position on the map at cursor
  public static Vector2 GetMapPositionAtCursor()
  {
    Vector2 mapPos = Utility.getTopLeftPositionForCenteringOnScreen(Map.Bounds.Width * 4, 720, 0, 0);
    return new Vector2((int)Game1.getMousePosition().X - mapPos.X, (int)Game1.getMousePosition().Y - mapPos.Y);
  }

  // Handle mouse down for beginning of drag and drop action
  // Accepts a callback function as an argument
  public static void HandleMouseDown(Action fn = null)
  {
    IsMouseHeldDown = true;
    BeginMousePosition = new Vector2(Game1.getMouseX(), Game1.getMouseY());
    fn();
  }

  // Handle mouse release for end of drag and drop action
  // Accepts a callback function as an argument
  public static void HandleMouseRelease(Action fn = null)
  {
    IsMouseHeldDown = false;
    EndMousePosition = new Vector2(Game1.getMouseX(), Game1.getMouseY());
    fn();
  }

  // Return Rectangle of drag and drop area
  public static Rectangle GetDragAndDropArea()
  {
    return new Rectangle((int)BeginMousePosition.X, (int)BeginMousePosition.Y, (int)(EndMousePosition.X - BeginMousePosition.X), (int)(EndMousePosition.Y - BeginMousePosition.Y));
  }
}
