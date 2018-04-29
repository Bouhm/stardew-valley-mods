using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace NPCCompass
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        private IModHelper helper;
        public static IMonitor monitor;
        private static Texture2D pointer;
        private static ModData constants;
        private static Dictionary<string, Point> buildings;
        private static HashSet<Locator> locators;
        private const int MAX_PROXIMITY = 4800;

        private const bool DEBUG_MODE = true;

        public override void Entry(IModHelper helper)
        {
            this.helper = helper;
            monitor = this.Monitor;
            ModEntry.pointer = helper.Content.Load<Texture2D>(@"assets/locator", ContentSource.ModFolder); // Load pointer tex
            constants = this.helper.ReadJsonFile<ModData>("constants.json") ?? new ModData();
            GameEvents.UpdateTick += GameEvents_UpdateTick;
            GraphicsEvents.OnPreRenderHudEvent += GraphicsEvents_OnPreRenderHudEvent;
            GraphicsEvents.OnPostRenderEvent += GraphicsEvents_OnPostRenderEvent;
            LocationEvents.CurrentLocationChanged += LocationEvents_CurrentLocationChanged;
        }

        private void LocationEvents_CurrentLocationChanged(object sender, EventArgsCurrentLocationChanged e)
        {
            // Buildings in player's location
            buildings = new Dictionary<string, Point>();
            GameLocation location = e.NewLocation;
            foreach (KeyValuePair<Point, string> door in location.doors)
            {
                if (!buildings.ContainsKey(door.Value))
                    buildings.Add(door.Value, door.Key);
            }
        }

        private void GameEvents_UpdateTick(object sender, EventArgs e)
        {
            if (!Context.IsWorldReady) { return; }
            UpdateLocators();
        }

        // Get angle (in radians) to determine which quadrant the NPC is in
        // 0 rad will be at line from player position to (0, viewportHeight/2) relative to viewport
        private double GetPlayerToNPCAngle(Vector2 playerPos, Vector2 npcPos)
        {
            // Hypotenuse is the line from player to npc
            float opposite = npcPos.Y - playerPos.Y;
            float adjacent = npcPos.X - playerPos.X;
            double angle = Math.Atan2(opposite, adjacent) + MathHelper.Pi;

            return angle;
        }

        // Get quadrant to draw the locator on based on the angle
        //   _________
        //  | \  2  / |
        //  |   \ /   |
        //  | 1  |  3 |
        //  |   / \   |
        //  | /__4__\_|
        //
        private int GetViewportQuadrant(double angle, Vector2 playerPos)
        {
            // Top half of left quadrant
            if (angle < Math.Atan2(playerPos.Y - Game1.viewport.Y, playerPos.X - Game1.viewport.X))
                return 1;
            // Top quadrant
            else if (angle < MathHelper.Pi - Math.Atan2(playerPos.Y - Game1.viewport.Y, Game1.viewport.X + Game1.viewport.Width - playerPos.X))
                return 2;
            // Right quadrant
            else if (angle < MathHelper.Pi + Math.Atan2(Game1.viewport.Y + Game1.viewport.Height - playerPos.Y, Game1.viewport.X + Game1.viewport.Width - playerPos.X))
                return 3;
            // Bottom quadrant
            else if (angle < MathHelper.TwoPi - Math.Atan2(Game1.viewport.Y + Game1.viewport.Height - playerPos.Y, playerPos.X - Game1.viewport.X))
                return 4;
            // Bottom half of left quadrant
            else
                return 1;
        }

        private double GetDistance(Vector2 begin, Vector2 end)
        {
            return Math.Sqrt(
                Math.Pow((begin.X - end.X), 2) + Math.Pow((begin.Y - end.Y), 2)
            );
        }

        // Get position of location relative to viewport from
        // the viewport quadrant and positions of player/npc relative to map
        private Vector2 GetLocatorPosition(double angle, int quadrant, Vector2 playerPos)
        {
            float x = playerPos.X - Game1.viewport.X;
            float y = playerPos.Y - Game1.viewport.Y;

            // Draw triangle such that the hypotenuse is
            // the line from player to the point of intersection of
            // the viewport quadrant and the line to the npc
            switch (quadrant)
            {
                // Have to split each quadrant in half since player is not always centered in viewport
                case 1:
                    // Bottom half
                    if (angle > MathHelper.TwoPi - angle)
                        y += (playerPos.X - Game1.viewport.X) * (float)Math.Tan(MathHelper.TwoPi - angle);
                    // Top half
                    else
                        y += (playerPos.X - Game1.viewport.X) * (float)Math.Tan(MathHelper.TwoPi - angle);
                    return new Vector2(0 + (Game1.tileSize/2 + 8), y);
                case 2:
                    // Left half
                    if (angle < MathHelper.PiOver2)
                        x -= (playerPos.Y - Game1.viewport.Y) * (float)Math.Tan(MathHelper.PiOver2 - angle);
                    // Right half
                    else
                        x -= (playerPos.Y - Game1.viewport.Y) * (float)Math.Tan(MathHelper.PiOver2 - angle);
                    return new Vector2(x, 0 + (Game1.tileSize/2 + 8));
                case 3:
                    // Top half
                    if (angle < MathHelper.Pi)
                        y -= (Game1.viewport.X + Game1.viewport.Width - playerPos.X) * (float)Math.Tan(MathHelper.Pi - angle);
                    // Bottom half
                    else
                        y -= (Game1.viewport.X + Game1.viewport.Width - playerPos.X) * (float)Math.Tan(MathHelper.Pi - angle);
                    return new Vector2(Game1.viewport.Width - (Game1.tileSize/2 + 8), y);
                case 4:
                    // Right half
                    if (angle < 3 * MathHelper.PiOver2)
                        x += (Game1.viewport.Y + Game1.viewport.Height - playerPos.Y) * (float)Math.Tan(3 * MathHelper.PiOver2 - angle);
                    // Left half
                    else
                        x += (Game1.viewport.Y + Game1.viewport.Height - playerPos.Y) * (float)Math.Tan(3 * MathHelper.PiOver2 - angle);
                    return new Vector2(x, Game1.viewport.Height - (Game1.tileSize/2 + 8));
                default:
                    return new Vector2(-5000, -5000);
            }
        }

        private void UpdateLocators()
        {
            locators = new HashSet<Locator>();
            Vector2 playerPos = new Vector2(Game1.player.position.X + Game1.player.FarmerSprite.spriteWidth / 2 * Game1.pixelZoom, Game1.player.position.Y);
            Vector2 npcPos = Vector2.Zero;
            bool isIndoors = false;

            foreach (NPC npc in Utility.getAllCharacters())
            {
                if ((npc.Schedule == null
                    && !npc.isMarried())
                    || npc.currentLocation == null
                   ) { continue; }

                if (npc.currentLocation.Equals(Game1.player.currentLocation))
                {
                    // Don't include locator if NPC is visible on screen
                    if (Utility.isOnScreen(new Vector2(npc.position.X, npc.position.Y), Game1.tileSize / 4)) { continue; }

                    npcPos = new Vector2(npc.position.X + npc.sprite.spriteHeight / 2 * Game1.pixelZoom, npc.position.Y);
                }
                else
                {
                    if (buildings.ContainsKey(npc.currentLocation.Name))
                    {
                        npcPos = new Vector2(
                                buildings[npc.currentLocation.Name].X,
                                buildings[npc.currentLocation.Name].Y
                            );
                        isIndoors = true;
                    }
                    else
                        continue;
                }

                Locator locator = new Locator
                {
                    Name = npc.name,
                    Marker = npc.sprite.Texture,
                    Proximity = GetDistance(playerPos, npcPos),
                    IsIndoors = isIndoors
                };

                double angle = GetPlayerToNPCAngle(playerPos, npcPos);
                int quadrant = GetViewportQuadrant(angle, playerPos);
                Vector2 locatorPos = GetLocatorPosition(angle, quadrant, playerPos);

                locator.X = locatorPos.X;
                locator.Y = locatorPos.Y;
                locator.Angle = angle;
                locator.Quadrant = quadrant;
                locators.Add(locator);
            }
        }

        private void GraphicsEvents_OnPreRenderHudEvent(object sender, EventArgs e)
        {
            if (!Context.IsWorldReady) { return; }
            DrawLocators();
        }

        private void DrawLocators()
        {
            foreach (Locator locator in locators)
            {
                double alphaLevel = locator.Proximity > MAX_PROXIMITY ? 0.25 : 0.25 + ((MAX_PROXIMITY - locator.Proximity) / MAX_PROXIMITY) * 0.75;

                // To offset the offsets used to keep all of the locators inside the viewport screen
                int offsetX = 0;
                if (locator.Quadrant == 2 || locator.Quadrant == 4)
                    offsetX = -16;

                // Special case for marking doors of buildings with NPCs inside
                // And the door is within viewport
                if (locator.IsIndoors 
                    && (locator.X + offsetX > 0
                    && locator.X + offsetX < Game1.viewport.Width)
                    && (locator.Y < 0
                    && locator.Y > Game1.viewport.Height))
                {
                    locator.Angle = 3 * MathHelper.PiOver2;
                    locator.Proximity = 0;
                }

                // Pointer texture
                Game1.spriteBatch.Draw(
                    pointer,
                    new Vector2(locator.X + offsetX, locator.Y),
                    new Rectangle?(new Rectangle(0, 0, 64, 64)),
                    Color.White * (float)alphaLevel,
                    (float)(locator.Angle - 3 * MathHelper.PiOver4),
                    new Vector2(pointer.Width / 2, pointer.Height / 2),
                    1f,
                    SpriteEffects.None,
                    0.0f
                );

                // NPC head
                Game1.spriteBatch.Draw(
                    locator.Marker,
                    new Vector2(locator.X + 24 + offsetX, locator.Y + 12),
                    new Rectangle?(new Rectangle(0, constants.MarkerCrop[locator.Name], 16, 15)),
                    Color.White * (float)alphaLevel,
                    0f,
                    new Vector2(16, 16),
                    3f,
                    SpriteEffects.None,
                    1f
                );

                if (locator.Proximity > 0)
                {
                    string distanceString = Math.Round(locator.Proximity / Game1.tileSize, 0).ToString();
                    DrawText(distanceString, new Vector2(locator.X + offsetX, locator.Y + 12), Color.White * (float)alphaLevel, new Vector2((int)Game1.dialogueFont.MeasureString(distanceString).X / 2, (float)((Game1.tileSize / 4) * 0.5)), 0.37f);
                }
            }
        }

        // Draw line relative to viewport
        private void DrawLine(SpriteBatch b, Vector2 begin, Vector2 end, Texture2D tex)
        {
            Rectangle r = new Rectangle((int)begin.X, (int)begin.Y, (int)(end - begin).Length() + 2, 2);
            Vector2 v = Vector2.Normalize(begin - end);
            float angle = (float)Math.Acos(Vector2.Dot(v, -Vector2.UnitX));
            if (begin.Y > end.Y) angle = MathHelper.TwoPi - angle;
            b.Draw(tex, r, null, Color.White, angle, Vector2.Zero, SpriteEffects.None, 0);
        }

        // Draw outlined text
        private static void DrawText(string text, Vector2 pos, Color? color = null, Vector2? origin = null, float scale = 1f)
        {
            Game1.spriteBatch.DrawString(Game1.dialogueFont, text, pos + new Vector2(1, 1), Color.Black, 0f, origin ?? Vector2.Zero, scale, SpriteEffects.None, 0f);
            Game1.spriteBatch.DrawString(Game1.dialogueFont, text, pos + new Vector2(-1, 1), Color.Black, 0f, origin ?? Vector2.Zero, scale, SpriteEffects.None, 0f);
            Game1.spriteBatch.DrawString(Game1.dialogueFont, text, pos + new Vector2(1, -1), Color.Black, 0f, origin ?? Vector2.Zero, scale, SpriteEffects.None, 0f);
            Game1.spriteBatch.DrawString(Game1.dialogueFont, text, pos + new Vector2(-1, -1), Color.Black, 0f, origin ?? Vector2.Zero, scale, SpriteEffects.None, 0f);
            Game1.spriteBatch.DrawString(Game1.dialogueFont, text, pos, color ?? Color.White, 0f, origin ?? Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        private void GraphicsEvents_OnPostRenderEvent(object sender, EventArgs e)
        {
            if (!DEBUG_MODE || !Context.IsWorldReady) { return; }
            foreach (Locator locator in locators)
            {
                NPC npc = Game1.getCharacterFromName(locator.Name);
                float viewportX = Game1.player.position.X + Game1.pixelZoom * Game1.player.Sprite.spriteWidth / 2 - Game1.viewport.X;
                float viewportY = Game1.player.position.Y - Game1.viewport.Y;
                float npcViewportX = npc.position.X + Game1.pixelZoom * npc.sprite.spriteWidth/2 - Game1.viewport.X;
                float npcViewportY = npc.position.Y - Game1.viewport.Y;

                // Draw NPC sprite noodle connecting center of screen to NPC for debugging
                DrawLine(Game1.spriteBatch, new Vector2(viewportX, viewportY), new Vector2(npcViewportX, npcViewportY), npc.sprite.Texture);
            }
        }
    }

    // Class for locators AKA the 'needles' of the compass
    internal class Locator
    {
        public string Name { get; set; } = "";
        public Texture2D Marker { get; set; } = null;
        public double Proximity = 0;
        public int Quadrant = 1;
        public float X { get; set; } = 0;
        public float Y { get; set; } = 0;
        public double Angle { get; set; } = 0.0;
        public bool IsIndoors { get; set; } = true;
    }
}

