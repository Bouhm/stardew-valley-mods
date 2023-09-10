using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace NPCMapLocations.Framework.Menus
{
    /// <summary>A minimap UI drawn to the screen.</summary>
    internal class ModMinimap
    {
        /*********
        ** Fields
        *********/
        /// <summary>The pixel width of the border drawn around the minimap, before pixel zoom.</summary>
        private const int BorderWidth = 12;

        private readonly Rectangle BorderSourceRect = new(8, 256, 3, BorderWidth);

        /// <summary>The pixel position and size of the minimap, adjusted automatically to fit on the screen.</summary>
        private readonly ScreenBounds ScreenBounds;

        /// <summary>Create the map view to show in the minimap for the current location and position.</summary>
        private readonly Func<ModMapPage> CreateMapPage;

        /// <summary>The minimap's pixel position on screen before the player started dragging it.</summary>
        private Point PositionBeforeDrag;

        /// <summary>Whether the player is currently dragging the minimap to a new position.</summary>
        private bool IsDragging;

        /// <summary>The region for which the <see cref="MapPage"/> was opened.</summary>
        /// <remarks>We deliberately don't check <see cref="StardewValley.Menus.MapPage.mapRegion"/> here, to avoid an infinite loop if the opened map sets a different region for some reason.</remarks>
        private string MapRegionId;

        /// <summary>The underlying map view to show in the minimap area.</summary>
        private ModMapPage MapPage;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="createMapPage">Create the map view to show in the minimap for the current location and position.</param>
        public ModMinimap(Func<ModMapPage> createMapPage)
        {
            this.CreateMapPage = createMapPage;

            this.ScreenBounds = new(
                x: ModEntry.Globals.MinimapX,
                y: ModEntry.Globals.MinimapY,
                width: ModEntry.Globals.MinimapWidth * Game1.pixelZoom,
                height: ModEntry.Globals.MinimapHeight * Game1.pixelZoom
            );
        }

        /// <summary>Get whether dragging the minimap is allowed and the cursor is hovering on top of the minimap.</summary>
        public bool IsHoveringDragZone()
        {
            return
                !ModEntry.Globals.LockMinimapPosition
                && this.ScreenBounds.Contains(MouseUtil.GetScreenPoint());
        }

        /// <summary>Handle the player starting to press the right mouse button.</summary>
        public void HandleMouseRightDown()
        {
            if (Context.IsPlayerFree)
            {
                this.PositionBeforeDrag = new Point(this.ScreenBounds.X, this.ScreenBounds.Y);
                this.IsDragging = true;
            }
        }

        /// <summary>Handle the player holding the right mouse button.</summary>
        public void HandleMouseRightDrag()
        {
            if (this.IsDragging)
            {
                // stop if dragging should no longer be allowed
                if (!Context.IsPlayerFree)
                    this.HandleMouseRightRelease();

                // else move minimap
                else
                {
                    int borderWidth = ModMinimap.BorderWidth;
                    Vector2 mousePos = MouseUtil.GetScreenPosition();

                    this.ScreenBounds.SetDesiredBounds(
                        x: (int)MathHelper.Clamp(this.PositionBeforeDrag.X + mousePos.X - MouseUtil.BeginMousePosition.X, borderWidth, Game1.uiViewport.Width - this.ScreenBounds.Width - borderWidth),
                        y: (int)MathHelper.Clamp(this.PositionBeforeDrag.Y + mousePos.Y - MouseUtil.BeginMousePosition.Y, borderWidth, Game1.uiViewport.Height - this.ScreenBounds.Height - borderWidth)
                    );
                }
            }
        }

        /// <summary>Handle the player releasing the right mouse button.</summary>
        public void HandleMouseRightRelease()
        {
            ModEntry.Globals.MinimapX = this.ScreenBounds.X;
            ModEntry.Globals.MinimapY = this.ScreenBounds.Y;
            ModEntry.StaticHelper.Data.WriteJsonFile("config/globals.json", ModEntry.Globals);
            this.IsDragging = false;
        }

        /// <summary>Adjust the minimap draw bounds to fit on-screen when the window size changes.</summary>
        public void OnWindowResized()
        {
            this.ScreenBounds.Recalculate();
        }

        public void Resize()
        {
            this.ScreenBounds.SetDesiredBounds(
                width: ModEntry.Globals.MinimapWidth * Game1.pixelZoom,
                height: ModEntry.Globals.MinimapHeight * Game1.pixelZoom
            );
        }

        /// <summary>Update the minimap data if needed. Calls to this method are throttled.</summary>
        public void Update()
        {
            if (this.MapPage is null || this.MapRegionId != this.GetRegionId())
                this.UpdateMapPage();

            this.UpdateMapPosition();
        }

        /// <summary>Draw the minimap to the screen.</summary>
        public void Draw()
        {
            // update map if needed
            if (this.MapPage.mapRegion.Id != this.MapRegionId)
            {
                this.UpdateMapPage();
                this.UpdateMapPosition();
            }

            // get minimap dimensions
            var screenBounds = this.ScreenBounds;
            var mapArea = new Rectangle(screenBounds.X, screenBounds.Y, screenBounds.Width, screenBounds.Height);

            bool isHoveringMinimap = this.IsHoveringDragZone();

            // Make transparent on hover
            var color = isHoveringMinimap
              ? Color.White * 0.25f
              : Color.White;

            if (isHoveringMinimap)
                Game1.mouseCursor = 2;

            // draw map
            var spriteBatch = Game1.spriteBatch;
            {
                GraphicsDevice device = spriteBatch.GraphicsDevice;
                Rectangle oldScissorRect = device.ScissorRectangle;

                try
                {
                    device.ScissorRectangle = mapArea;
                    using SpriteBatch clippedBatch = new SpriteBatch(device);

                    clippedBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, null, new RasterizerState { ScissorTestEnable = true });

                    clippedBatch.Draw(Game1.staminaRect, mapArea, new Rectangle(0, 0, 1, 1), Color.Black); // black background in case map doesn't fill minimap view

                    if (!(this.IsHoveringDragZone() && ModEntry.StaticHelper.Input.GetState(SButton.MouseRight) == SButtonState.Held)) // don't draw map while it's being dragged
                    {
                        this.MapPage.drawMap(clippedBatch, false);
                        this.MapPage.drawMiniPortraits(clippedBatch);
                    }

                    clippedBatch.End();
                }
                finally
                {
                    device.ScissorRectangle = oldScissorRect;
                }
            }

            // draw frame
            {
                int x = mapArea.X;
                int y = mapArea.Y;
                int width = mapArea.Width;
                int height = mapArea.Height;

                // draw edges
                int borderWidth = ModMinimap.BorderWidth;
                this.DrawLine(spriteBatch, new Vector2(x, y - borderWidth), new Vector2(x + width - 2, y - borderWidth), borderWidth, Game1.menuTexture, this.BorderSourceRect, color * 1.5f);
                this.DrawLine(spriteBatch, new Vector2(x + width + borderWidth, y), new Vector2(x + width + borderWidth, y + height - 2), borderWidth, Game1.menuTexture, this.BorderSourceRect, color * 1.5f);
                this.DrawLine(spriteBatch, new Vector2(x + width, y + height + borderWidth), new Vector2(x + 2, y + height + borderWidth), borderWidth, Game1.menuTexture, this.BorderSourceRect, color * 1.5f);
                this.DrawLine(spriteBatch, new Vector2(x - borderWidth, height + y), new Vector2(x - borderWidth, y + 2), borderWidth, Game1.menuTexture, this.BorderSourceRect, color * 1.5f);

                // Draw corners
                spriteBatch.Draw(Game1.menuTexture, new Rectangle(x - borderWidth, y - borderWidth, borderWidth, borderWidth), new Rectangle(0, 256, borderWidth, borderWidth), color * 1.5f);
                spriteBatch.Draw(Game1.menuTexture, new Rectangle(x + width, y - borderWidth, borderWidth, borderWidth), new Rectangle(48, 256, borderWidth, borderWidth), color * 1.5f);
                spriteBatch.Draw(Game1.menuTexture, new Rectangle(x + width, y + height, borderWidth, borderWidth), new Rectangle(48, 304, borderWidth, borderWidth), color * 1.5f);
                spriteBatch.Draw(Game1.menuTexture, new Rectangle(x - borderWidth, y + height, borderWidth, borderWidth), new Rectangle(0, 304, borderWidth, borderWidth), color * 1.5f);
            }
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Recreate the underlying map view.</summary>
        [MemberNotNull(nameof(MapPage))]
        private void UpdateMapPage()
        {
            this.MapPage = this.CreateMapPage();
            this.MapRegionId = this.MapPage.mapRegion.Id;
        }

        /// <summary>Get the player's current world map position.</summary>
        private WorldMapPosition GetWorldMapPosition()
        {
            return ModEntry.GetWorldMapPosition(
                Game1.player.currentLocation.uniqueName.Value ?? Game1.player.currentLocation.Name,
                Math.Max(0, Game1.player.TilePoint.X),
                Math.Max(0, Game1.player.TilePoint.Y)
            );
        }

        /// <summary>Get the world map region which contains the player.</summary>
        private string GetRegionId()
        {
            return this.GetWorldMapPosition()?.RegionId ?? "Valley";
        }

        /// <summary>Update the position of the underlying map view so the player is centered within the minimap.</summary>
        private void UpdateMapPosition()
        {
            // get view dimensions
            var screenBounds = this.ScreenBounds;
            int viewX = screenBounds.X;
            int viewY = screenBounds.Y;
            int viewWidth = screenBounds.Width;
            int viewHeight = screenBounds.Height;
            var viewCenter = new Point(
                viewX + (viewWidth / 2),
                viewX + (viewHeight / 2)
            );

            // calculate top-left position of full map which would center player within the minimap
            WorldMapPosition mapPos = this.GetWorldMapPosition();
            var mapBounds = this.MapPage.mapBounds;
            int mapX = viewCenter.X - mapPos.X;
            int mapY = viewCenter.Y - mapPos.Y;
            int mapWidth = mapBounds.Width * Game1.pixelZoom;
            int mapHeight = mapBounds.Height * Game1.pixelZoom;

            // don't slide past edge of map if possible
            if (mapHeight > viewHeight)
            {
                if (mapY > viewY)
                    mapY = viewY;
                else if ((mapY + mapHeight) < (viewY + viewHeight))
                    mapY = viewY - mapHeight + viewHeight;
            }
            if (mapWidth > viewWidth)
            {
                if (mapX > viewX)
                    mapX = viewX;
                else if ((mapX + mapWidth) < (viewX + viewWidth))
                    mapX = viewX - mapWidth + viewWidth;
            }

            // update map bounds
            if (mapBounds.X != mapX || mapBounds.Y != mapY)
                this.MapPage.mapBounds = mapBounds with { X = mapX, Y = mapY };
        }

        /// <summary>Draw a minimap border.</summary>
        /// <param name="b">The sprite batch being drawn.</param>
        /// <param name="begin">The pixel coordinate for the line's starting endpoint.</param>
        /// <param name="end">The pixel coordinate for the line's ending endpoint.</param>
        /// <param name="width">The pixel width of the line.</param>
        /// <param name="texture">The texture to draw.</param>
        /// <param name="sourceRect">The pixel area within the <paramref name="texture"/> to draw.</param>
        /// <param name="color">The color to draw.</param>
        private void DrawLine(SpriteBatch b, Vector2 begin, Vector2 end, int width, Texture2D texture, Rectangle sourceRect, Color color)
        {
            var destRect = new Rectangle((int)begin.X, (int)begin.Y, (int)(end - begin).Length() + 2, width);
            var length = Vector2.Normalize(begin - end);

            float angle = (float)Math.Acos(Vector2.Dot(length, -Vector2.UnitX));
            if (begin.Y > end.Y)
                angle = MathHelper.TwoPi - angle;

            b.Draw(texture, destRect, sourceRect, color, angle, Vector2.Zero, SpriteEffects.None, 0);
        }
    }
}
