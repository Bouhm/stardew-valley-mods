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
        private static ModData constants;
        private static HashSet<Locator> locators;

        // For debug info
        private static double _angle;
        private static Vector2 _viewportPos;
        private static Vector2 _npcPos;
        private static Vector2 _playerPos;
        private static Vector2 _locatorPos;

        private const bool DEBUG_MODE = true;

        public override void Entry(IModHelper helper)
        {
            this.helper = helper;
            monitor = this.Monitor;
            constants = this.helper.ReadJsonFile<ModData>("constants.json") ?? new ModData();
            GameEvents.UpdateTick += GameEvents_UpdateTick;
            GraphicsEvents.OnPostRenderHudEvent += GraphicsEvents_OnPreRenderHudEvent;
            GraphicsEvents.OnPostRenderEvent += GraphicsEvents_OnPostRenderEvent;
        }

        private void GameEvents_UpdateTick(object sender, EventArgs e)
        {
            if (!Context.IsWorldReady) { return; }
            UpdateLocators();
        }

        // Get angle (in radians) to determine which quadrant the NPC is in
        // 0 rad will be at line from center of viewport to (0, viewportHeight/2) relative to viewport
        private double GetPlayerToNPCAngle(Vector2 playerPos, Vector2 npcPos)
        {
            // Hypotenuse is the line from player to npc
            float opposite = npcPos.Y - playerPos.Y;
            float adjacent = npcPos.X - playerPos.X;
            double angle = Math.Atan(opposite / adjacent);

            if (opposite > 0 && adjacent > 0 || opposite < 0 && adjacent > 0)
                angle += MathHelper.Pi;
            else if (opposite > 0 && adjacent < 0)
                angle += MathHelper.TwoPi;

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
        private int GetViewportQuadrant(double angle)
        {
            float length = Game1.viewport.Height / 2; // Game1.player.position.X - Game1.viewport.X;
            float width = Game1.viewport.Width / 2; // Game1.player.position.Y - Game1.viewport.Y;
            //monitor.Log(Math.Atan(length / width) + ", " + length +", " + width);
            // Top half of left quadrant
            if (angle < Math.Atan(length/width))
                return 1;
            // Top quadrant
            else if (angle < MathHelper.Pi - Math.Atan(length/width))
                return 2;
            // Right quadrant
            else if (angle < 3*MathHelper.PiOver2 - Math.Atan(length/width))
                return 3;
            // Bottom quadrant
            else if (angle < MathHelper.TwoPi - Math.Atan(length/width))
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
        private Vector2 GetLocatorPosition(double angle, int quadrant)
        {
            float x;
            float y;

            // Draw triangle such that the hypotenuse is
            // the line from player to the point of intersection of
            // the viewport quadrant and the line to the npc
            switch (quadrant)
            {
                case 1:
                    y = Game1.viewport.Height/2 - (Game1.viewport.Width/2 * (float)Math.Tan(angle));
                    return new Vector2(0, y);
                case 2:
                    x = Game1.viewport.Width/2 - (Game1.viewport.Height/2 * (float)Math.Tan(MathHelper.PiOver2 - angle));
                    return new Vector2(x, 0);
                case 3:
                    y = Game1.viewport.Height/2 - (Game1.viewport.Width/2 * (float)Math.Tan(MathHelper.Pi - angle));
                    return new Vector2(Game1.viewport.Width, y);
                case 4:
                    x = Game1.viewport.Width/2 - (Game1.viewport.Height/2 * (float)Math.Tan(3 * MathHelper.PiOver2 - angle));
                    return new Vector2(x, Game1.viewport.Height);
                default:
                    return Vector2.Zero;
            }
        }

        private void UpdateLocators()
        {
            locators = new HashSet<Locator>();
            foreach (NPC npc in Utility.getAllCharacters())
            {
                if ((npc.Schedule == null 
                    && !npc.isMarried()) 
                    || npc.currentLocation == null 
                    || !constants.Location.ContainsKey(npc.currentLocation.name)
                   ) { continue; }
                if (constants.Location[npc.currentLocation.name] == null 
                    || !constants.Location[npc.currentLocation.name].Equals(Game1.player.currentLocation.name)
                   ) { continue; }
                //if (Utility.isOnScreen(new Vector2(npc.position.X, npc.position.Y), Game1.tileSize)) { continue; }

                Vector2 playerPos = new Vector2(Game1.player.position.X + Game1.pixelZoom * Game1.player.sprite.spriteWidth / 2, Game1.player.position.Y);
                Vector2 npcPos = new Vector2(npc.position.X + Game1.pixelZoom * npc.sprite.spriteWidth / 2, npc.position.Y);
                Locator locator = new Locator
                {
                    Name = npc.name,
                    Marker = npc.sprite.Texture,
                    Proximity = GetDistance(playerPos, npcPos)
                };

                double angle = GetPlayerToNPCAngle(playerPos, npcPos);
                int quadrant = GetViewportQuadrant(angle);
                Vector2 locatorPos = GetLocatorPosition(angle, quadrant);

                locator.X = locatorPos.X; 
                locator.Y = locatorPos.Y; 
                locators.Add(locator);

                if (DEBUG_MODE && locators.Count == 1)
                {
                    ModEntry._angle = angle;
                    ModEntry._viewportPos = new Vector2(Game1.viewport.X, Game1.viewport.Y);
                    ModEntry._playerPos = playerPos;
                    ModEntry._npcPos = npcPos;
                    ModEntry._locatorPos = locatorPos;
                }
            }
        }

        private void GraphicsEvents_OnPreRenderHudEvent(object sender, EventArgs e)
        {
            if (!Context.IsWorldReady) { return; }
            foreach (Locator locator in locators)
            {
                Game1.spriteBatch.Draw(
                    locator.Marker,
                    new Vector2(locator.X - 32, locator.Y - 30), 
                    new Rectangle?(new Rectangle(0, constants.MarkerCrop[locator.Name], 16, 15)), 
                    Color.White, 0f, 
                    Vector2.Zero, 
                    Game1.pixelZoom, 
                    SpriteEffects.None, 
                    1f
                );
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

        // Show debug info in top left corner
        private static void ShowDebugInfo()
        {
            // Black background for legible text
            Game1.spriteBatch.Draw(Game1.shadowTexture, new Rectangle(0, 0, 500, 286), new Rectangle(6, 3, 1, 1), Color.Black);

            Color color = ModEntry.locators.Count == 0 ? Color.DimGray : Color.White;

            DrawText("Angle (rad): " + Math.Round(ModEntry._angle, 2), new Vector2(Game1.tileSize / 4, Game1.tileSize / 4), color);
            DrawText("Locator: (" + (int)ModEntry._locatorPos.X + ", " + (int)ModEntry._locatorPos.Y + ")", new Vector2(Game1.tileSize / 4, Game1.tileSize * 3 / 4 + 8), color);
            DrawText("Player: (" + (int)ModEntry._playerPos.X + ", " + (int)ModEntry._playerPos.Y + ")", new Vector2(Game1.tileSize / 4, Game1.tileSize * 5 / 4 + 8 * 2), color);
            DrawText("NPC: (" + (int)ModEntry._npcPos.X + ", " + (int)ModEntry._npcPos.Y + ")", new Vector2(Game1.tileSize / 4, Game1.tileSize * 7 / 4 + 8 * 3), color);
            DrawText("Viewport: (" + (int)ModEntry._viewportPos.X + ", " + (int)ModEntry._viewportPos.Y + ")", new Vector2(Game1.tileSize / 4, Game1.tileSize * 9 / 4 + 8 * 4), color);
            DrawText(Game1.viewport.Width + "x" + Game1.viewport.Height, new Vector2(Game1.tileSize / 4, Game1.tileSize * 11 / 4 + 8 * 5), color);
        }

        // Draw outlined text
        private static void DrawText(string text, Vector2 pos, Color? color = null)
        {
            Game1.spriteBatch.DrawString(Game1.dialogueFont, text, pos + new Vector2(1, 1), Color.Black);
            Game1.spriteBatch.DrawString(Game1.dialogueFont, text, pos + new Vector2(-1, 1), Color.Black);
            Game1.spriteBatch.DrawString(Game1.dialogueFont, text, pos + new Vector2(1, -1), Color.Black);
            Game1.spriteBatch.DrawString(Game1.dialogueFont, text, pos + new Vector2(-1, -1), Color.Black);
            Game1.spriteBatch.DrawString(Game1.dialogueFont, text, pos, color ?? Color.White);
        }

        private void GraphicsEvents_OnPostRenderEvent(object sender, EventArgs e)
        {
            if (!DEBUG_MODE || !Context.IsWorldReady) { return; }
            foreach (NPC npc in Utility.getAllCharacters())
            {
                if ((npc.Schedule == null
                    && !npc.isMarried())
                    || npc.currentLocation == null
                    || !constants.Location.ContainsKey(npc.currentLocation.name)
                   ) { continue; }
                if (constants.Location[npc.currentLocation.name] == null
                    || !constants.Location[npc.currentLocation.name].Equals(Game1.player.currentLocation.name)
                   ) { continue; }

                float viewportX = Game1.player.position.X + Game1.pixelZoom * Game1.player.sprite.spriteWidth/2 - Game1.viewport.X;
                float viewportY = Game1.player.position.Y - Game1.viewport.Y;
                float npcViewportX = npc.position.X + Game1.pixelZoom * npc.sprite.spriteWidth/2 - Game1.viewport.X;
                float npcViewportY = npc.position.Y - Game1.viewport.Y;
                // Draw NPC sprite noodle connecting center of screen to NPC for debugging
                DrawLine(Game1.spriteBatch, new Vector2(viewportX, viewportY), new Vector2(npcViewportX, npcViewportY), npc.sprite.Texture);
            }
            if (locators.Count == 1)
                ShowDebugInfo();
        }
    }

    // Class for locators AKA the 'needles' of the compass
    internal class Locator
    {
        public string Name { get; set; } = "";
        public Texture2D Marker { get; set; } = null;
        public double Proximity = 0;
        public float X { get; set; } = 0;
        public float Y { get; set; } = 0;
    }
}

