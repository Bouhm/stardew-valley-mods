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
        private ModData constants;
        private HashSet<Locator> locators;

        // For debug info
        private const bool DEBUG_MODE = false;

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

        private void GetPlayerToNPCAngle(int playerX, int playerY, int npcX, int npcY)
        {

        }

        private void GetViewportSide(double rad, int length, int width)
        {

        }

        private void GetLocatorPosition()
        {

        }

        private void UpdateLocators()
        {
            locators = new HashSet<Locator>();
            foreach (NPC npc in Utility.getAllCharacters())
            {
                if (!npc.isVillager() || npc.currentLocation == null) { continue; }
                if (constants.Location[npc.currentLocation.name] == null || !constants.Location[npc.currentLocation.name].Equals(Game1.player.currentLocation.name)) { continue; }
                if (Utility.isOnScreen(new Vector2(npc.position.X, npc.position.Y), 2 * Game1.tileSize)) { continue; }

                Locator locator = new Locator
                {
                    Name = npc.name,
                    Marker = npc.sprite.Texture,
                    Proximity = Math.Sqrt(
                        Math.Pow((Game1.player.position.X - npc.position.X), 2) + Math.Pow((Game1.player.position.Y - npc.position.Y), 2)
                    )
                };

                locator.X = npc.position.X;
                locator.Y = npc.position.Y;
            }
        }

        private void GraphicsEvents_OnPreRenderHudEvent(object sender, EventArgs e)
        {
            if (!Context.IsWorldReady) { return; }
            foreach (Locator locator in locators)
            {
                Game1.spriteBatch.Draw(locator.Marker, new Vector2(locator.X / 4, locator.Y / 4), new Rectangle?(new Rectangle(0, constants.MarkerCrop[locator.Name], 16, 15)), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
            }
        }

        private void DrawLine(SpriteBatch b, Vector2 begin, Vector2 end, Texture2D tex)
        {
            Rectangle r = new Rectangle((int)begin.X, (int)begin.Y, (int)(end - begin).Length() + 2, 2);
            Vector2 v = Vector2.Normalize(begin - end);
            float angle = (float)Math.Acos(Vector2.Dot(v, -Vector2.UnitX));
            if (begin.Y > end.Y) angle = MathHelper.TwoPi - angle;
            b.Draw(tex, r, null, Color.White, angle, Vector2.Zero, SpriteEffects.None, 0);
        }

        private void GraphicsEvents_OnPostRenderEvent(object sender, EventArgs e)
        {
            if (!Context.IsWorldReady) { return; }
            foreach (NPC npc in Utility.getAllCharacters())
            {
                if (!npc.isVillager() || npc.currentLocation == null) { continue; }
                if (constants.Location[npc.currentLocation.name] == null || !constants.Location[npc.currentLocation.name].Equals(Game1.player.currentLocation.name)) { continue; }

                float viewportX = Game1.viewport.Width/2;
                float viewportY = Game1.viewport.Height/2;
                float npcViewportX = npc.position.X - Game1.viewport.X;
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
        public float X { get; set; } = 0;
        public float Y { get; set; } = 0;
    }
}

