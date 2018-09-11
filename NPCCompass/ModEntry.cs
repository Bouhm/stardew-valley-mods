using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Network;

namespace NPCCompass
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        private IModHelper helper;
        private ModConfig config;
        private bool showLocators;
        public static IMonitor monitor;
        private static Texture2D pointer;
        private static ModData constants;
        private static HashSet<Locator> locators;
        private static Dictionary<string, HashSet<Locator>> doorLocatorMaps;
        private const int MAX_PROXIMITY = 4800;

        private const bool DEBUG_MODE = true;

        public override void Entry(IModHelper helper)
        {
            this.helper = helper;
            monitor = this.Monitor;
            config = helper.ReadConfig<ModConfig>();
            ModEntry.pointer = helper.Content.Load<Texture2D>(@"assets/locator.png", ContentSource.ModFolder); // Load pointer tex
            constants = this.helper.ReadJsonFile<ModData>("constants.json") ?? new ModData();
            GameEvents.UpdateTick += GameEvents_UpdateTick;
            GraphicsEvents.OnPreRenderHudEvent += GraphicsEvents_OnPreRenderHudEvent;
            GraphicsEvents.OnPostRenderEvent += GraphicsEvents_OnPostRenderEvent;
            ControlEvents.KeyPressed += ControlEvents_KeyPressed;
            ControlEvents.KeyReleased += ControlEvents_KeyReleased;
        }

        private void ControlEvents_KeyReleased(object sender, EventArgsKeyPressed e)
        {
            if (e.KeyPressed.ToString().Equals(config.ShowKeyCode) && !config.Toggle)
                showLocators = false;        
        }

        private void ControlEvents_KeyPressed(object sender, EventArgsKeyPressed e)
        {
            if (e.KeyPressed.ToString().Equals(config.ShowKeyCode))
                showLocators = true;
        }

        private void GameEvents_UpdateTick(object sender, EventArgs e)
        {
            if (!Context.IsWorldReady) { return; }
            if (!Game1.paused)
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
        private static Vector2 GetLocatorPosition(double angle, int quadrant, Vector2 playerPos, Vector2 npcPos, bool isOnScreen=false, bool isDoor=false)
        {
            float x = playerPos.X - Game1.viewport.X;
            float y = playerPos.Y - Game1.viewport.Y;

            if (isDoor)
            {
                if (Utility.isOnScreen(npcPos, Game1.tileSize / 4))
                {
                    return new Vector2(npcPos.X - Game1.viewport.X + Game1.tileSize / 2, npcPos.Y - Game1.viewport.Y - Game1.tileSize / 2);
                }
                else
                {
                    x += 3*Game1.tileSize/4;
                }
            }

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
            doorLocatorMaps = new Dictionary<string, HashSet<Locator>>();
            Vector2 playerPos = new Vector2(Game1.player.position.X + Game1.player.FarmerSprite.SpriteWidth / 2 * Game1.pixelZoom, Game1.player.position.Y);
            Vector2 npcPos = Vector2.Zero;
            bool isDoor = false;
            bool isOnScreen = false;

            foreach (NPC npc in Utility.getAllCharacters())
            {
                if ((npc.Schedule == null
                    && !npc.isMarried())
                    || npc.currentLocation == null
                    || config.NPCBlacklist.Contains(npc.Name)
                   ) { continue; }

                if (npc.currentLocation.Equals(Game1.player.currentLocation))
                {
                    // Don't include locator if NPC is visible on screen
                    if (Utility.isOnScreen(new Vector2(npc.position.X, npc.position.Y), Game1.tileSize / 4)) { continue; }

                    npcPos = new Vector2(npc.position.X + npc.Sprite.SpriteHeight / 2 * Game1.pixelZoom, npc.position.Y);
                }
                else
                {
                    foreach (KeyValuePair<Point, string> door in Game1.player.currentLocation.doors.Pairs)
                    {
                        if (door.Value.Equals(npc.currentLocation.Name)) {
                            npcPos = new Vector2(
                                    door.Key.X * Game1.tileSize,
                                    door.Key.Y * Game1.tileSize
                                );
                            isDoor = true;
                            isOnScreen = Utility.isOnScreen(npcPos, Game1.tileSize / 4);
                            break;
                        }
                    }
                    if (npcPos.Equals(Vector2.Zero))
                        continue;
                }

                Locator locator = new Locator
                {
                    Name = npc.Name,
                    Marker = npc.Sprite.Texture,
                    Proximity = GetDistance(playerPos, npcPos),
                    IsDoor = isDoor,
                    IsOnScreen = isOnScreen
                };

                double angle = GetPlayerToNPCAngle(playerPos, npcPos);
                int quadrant = GetViewportQuadrant(angle, playerPos);
                Vector2 locatorPos = GetLocatorPosition(angle, quadrant, playerPos, npcPos, isOnScreen, isDoor);

                locator.X = locatorPos.X;
                locator.Y = locatorPos.Y;
                locator.Angle = angle;
                locator.Quadrant = quadrant;

                if (isDoor && isOnScreen)
                {
                    if (doorLocatorMaps.TryGetValue(npc.currentLocation.Name, out HashSet<Locator> doorLocators)) 
                        doorLocators.Add(locator);
                    else
                        doorLocators = new HashSet<Locator>{locator};

                    doorLocatorMaps[npc.currentLocation.Name] = doorLocators;
                }
                else
                    locators.Add(locator);
            }
        }

        private void GraphicsEvents_OnPreRenderHudEvent(object sender, EventArgs e)
        {
            if (!Context.IsWorldReady) { return; }
            if (showLocators && !Game1.paused)
                DrawLocators();
        }

        private void DrawLocators()
        {

            foreach (KeyValuePair<string, HashSet<Locator>> doorLocators in doorLocatorMaps)
            {
                int count = doorLocators.Value.Count;
                int i = 0;
                double angleStep = 1;
                double rotation = 0;

                foreach (Locator locator in doorLocators.Value)
                {
                    double alphaLevel = locator.Proximity > MAX_PROXIMITY ? 0.25 : 0.25 + ((MAX_PROXIMITY - locator.Proximity) / MAX_PROXIMITY) * 0.75;

                    if (locator.IsOnScreen)
                    {
                        locator.Angle = 3 * MathHelper.PiOver2;
                        locator.Proximity = 0;
                    }

                    if (!constants.MarkerCrop.TryGetValue(locator.Name, out var cropY))
                      cropY = 0;

                    // Pointer texture
                    Game1.spriteBatch.Draw(
                        pointer,
                        new Vector2(locator.X, locator.Y),
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
                        new Vector2(locator.X + 24, locator.Y + 20),
                        new Rectangle?(new Rectangle(0, cropY, 16, 15)),
                        Color.White * (float)alphaLevel,
                        0f,
                        new Vector2(16, 16),
                        3f,
                        SpriteEffects.None,
                        1f
                    );
                }
            }
        
            foreach (Locator locator in locators)
            {
                double alphaLevel = locator.Proximity > MAX_PROXIMITY ? 0.25 : 0.25 + ((MAX_PROXIMITY - locator.Proximity) / MAX_PROXIMITY) * 0.75;
                int offsetX = 0;
              
                // To offset the offsets used to keep all of the locators inside the viewport screen
                if (locator.Quadrant == 2 || locator.Quadrant == 4)
                    offsetX = -16;
                
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
                    new Vector2(locator.X + 24 + offsetX, locator.Y + 20),
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
                    DrawText(distanceString, new Vector2(locator.X + offsetX, locator.Y + 16), Color.White * (float)alphaLevel, new Vector2((int)Game1.dialogueFont.MeasureString(distanceString).X / 2, (float)((Game1.tileSize / 4) * 0.5)), 0.37f);
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
            if (!DEBUG_MODE || !Context.IsWorldReady || locators == null || doorLocatorMaps == null) { return; }
            foreach (Locator locator in locators)
            {
                if (!locator.IsOnScreen) { continue; }
                NPC npc = Game1.getCharacterFromName(locator.Name);
                float viewportX = Game1.player.position.X + Game1.pixelZoom * Game1.player.Sprite.SpriteWidth / 2 - Game1.viewport.X;
                float viewportY = Game1.player.position.Y - Game1.viewport.Y;
                float npcViewportX = npc.position.X + Game1.pixelZoom * npc.Sprite.SpriteWidth/2 - Game1.viewport.X;
                float npcViewportY = npc.position.Y - Game1.viewport.Y;

                // Draw NPC sprite noodle connecting center of screen to NPC for debugging
                DrawLine(Game1.spriteBatch, new Vector2(viewportX, viewportY), new Vector2(npcViewportX, npcViewportY), npc.Sprite.Texture);
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
        public bool IsDoor { get; set; } = true;
        public bool IsOnScreen { get; set; } = true;
    }
}

