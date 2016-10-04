/*
NPC Map Locations Mod by Bouhm.
Shows NPC locations real-time on the map.
*/

using StardewValley;
using StardewModdingAPI;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using System;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;
using System.Collections.Generic;

namespace NPCMapLocations
{
    public class MapModMain : Mod
    {
        public static Configuration config;
        private static Dictionary<string, NPCMarker> npcMarkers = new Dictionary<string, NPCMarker>();
        // NPC head crops, top left corner (0, y), width = 16, height = 15 
        private static Dictionary<string, int> spriteCrop;
        private static Dictionary<string, string> startingLocations;
        private static Dictionary<string, Double[]> locationVectors;
        private static Dictionary<string, string> indoorLocations;
        private List<ClickableComponent> points = new List<ClickableComponent>();
        private static MapPageTooltips toolTips;
        private static string npcNames;
        private bool showExtras;

        public override void Entry(params object[] objects)
        {
            config = ConfigExtensions.InitializeConfig<Configuration>(new Configuration(), base.BaseConfigPath);
            GameEvents.UpdateTick += GameEvents_UpdateTick;
            GraphicsEvents.OnPostRenderEvent += GraphicsEvents_OnPostRenderEvent;
            KeyboardInput.KeyDown += KeyboardInput_KeyDown;
        }

        // Open menu key
        private void KeyboardInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (Game1.hasLoadedGame && Game1.activeClickableMenu is GameMenu)
            {
                changeKey(e.KeyCode.ToString(), (GameMenu)Game1.activeClickableMenu);
            }
        }

        private void changeKey(string key, GameMenu menu)
        {
            if (menu.currentTab != 3) { return; }
            if (key.Equals(config.menuKey))
            {
                Game1.activeClickableMenu = new MapModMenu(Game1.viewport.Width / 2 - (950 + IClickableMenu.borderWidth * 2) / 2, Game1.viewport.Height / 2 - (750 + IClickableMenu.borderWidth * 2) / 2, 900 + IClickableMenu.borderWidth * 2, 650 + IClickableMenu.borderWidth * 2, showExtras);
            }
        }

        private void GameEvents_UpdateTick(object sender, EventArgs e)
        {
            if (!(Game1.hasLoadedGame && Game1.activeClickableMenu is GameMenu)) { return; }
            spriteCrop = MapModConstants.spriteCrop;
            startingLocations = MapModConstants.startingLocations;
            locationVectors = MapModConstants.locationVectors;
            indoorLocations = MapModConstants.indoorLocations;
            List<string> npcNamesHovered = new List<String>();
            npcNames = "";

            foreach (NPC npc in Utility.getAllCharacters())
            {
                if (npc.Schedule != null || npc.isMarried())
                {
                    bool sameLocation = false;
                    showExtras = (npc.name.Equals("Sandy") || npc.name.Equals("Wizard") || npc.name.Equals("Marlon"));
                    if (config.onlySameLocation)
                    {
                        string indoorLocationNPC;
                        string indoorLocationPlayer;
                        indoorLocations.TryGetValue(npc.currentLocation.name, out indoorLocationNPC);
                        indoorLocations.TryGetValue(Game1.player.currentLocation.name, out indoorLocationPlayer);
                        if (indoorLocationPlayer == null || indoorLocationNPC == null)
                        {
                            sameLocation = false;
                        }
                        else 
                        {
                            sameLocation = indoorLocationNPC.Equals(indoorLocationPlayer);
                        }
                    }
                    if ((sameLocation || !config.onlySameLocation) && 
                       ((config.immersionLevel == 2) ? Game1.player.hasTalkedToFriendToday(npc.name) : true) &&
                       ((config.immersionLevel == 3) ? !Game1.player.hasTalkedToFriendToday(npc.name) : true) &&
                       (showNPC(npc.name)) && 
                       (config.byHeartLevel ? (Game1.player.getFriendshipHeartLevelForNPC(npc.name) >= config.heartLevelMin && Game1.player.getFriendshipHeartLevelForNPC(npc.name) <= config.heartLevelMax) : true))
                    {
                        int offsetX = 0;
                        int offsetY = 0;
                        int x = 0;
                        int y = 0;
                        int width = 0;
                        int height = 0;
                        double[] npcLocation;
                        string currentLocation;
                        // At the start of a new game, for some reason NPCs locations are null until 6:30 AM
                        if (npc.currentLocation == null)
                        {
                            currentLocation = startingLocations[npc.name];
                        }
                        else
                        {
                            currentLocation = npc.currentLocation.name;
                        }

                        locationVectors.TryGetValue(currentLocation, out npcLocation);
                        // So game doesn't crash if I missed a location
                        if (npcLocation == null)
                        {
                            double[] unknown = { -5000, -5000, 0, 0 };
                            npcLocation = unknown;
                        }
                        double mapScaleX = npcLocation[2];
                        double mapScaleY = npcLocation[3];

                        // Partitioning large areas because map page sucks
                        // In addition to all the locations on the map, all of these values were meticulously calculated to make
                        // real-time tracking accurate with a badly scaling map page (only on NPC paths). DO NOT MESS WITH THESE UNLESS YOU CAN DO IT BETTER.

                        // Partitions for Town
                        if (currentLocation.Equals("Town"))
                        {
                            if (npc.getTileX() < 28 && npc.getTileY() < 58 && npc.getTileY() > 53)
                            {
                                offsetX = 5;
                                offsetY = -30;
                            }
                            else if (npc.getTileX() < 31 && npc.getTileX() > 26 && npc.getTileY() > 74 && npc.getTileY() < 90)
                            {
                                offsetX = 10;
                            }
                            else if (npc.getTileX() < 30 && npc.getTileY() > 89 && npc.getTileY() < 98)
                            {
                                offsetY = -5;
                                offsetX = 5;
                            }
                            else if (npc.getTileX() < 57 && npc.getTileY() > 98 && npc.getTileY() < 109)
                            {
                                offsetX = 30;
                            }
                            else if (npc.getTileX() < 78 && npc.getTileY() < 103 && npc.getTileY() > 40)
                            {
                                mapScaleX = 3.01;
                                mapScaleY = 2.94;
                                offsetY = -10;
                            }
                            else if (npc.getTileX() < 85 && npc.getTileY() < 43)
                            {
                                mapScaleX = 2.48;
                                mapScaleY = 2.52;
                                offsetX = -15;
                            }

                            else if (npc.getTileX() > 90 && npc.getTileY() < 41)
                            {
                                offsetX = -20;
                                offsetY = 25;
                            }
                            else if (npc.getTileX() > 77 && npc.getTileY() < 61)
                            {
                                mapScaleX = 3.21;
                                mapScaleY = 2.64;
                                offsetX = -3;
                                offsetY = -3;
                            }
                            else if (npc.getTileX() > 78 && npc.getTileY() > 60)
                            {
                                mapScaleX = 3.21;
                                mapScaleY = 3.34;
                                offsetX = -22;
                                offsetY = -35;
                            }
                        }

                        // Partitions for Forest ------------------------------------------------------------------------------------
                        else if (currentLocation.Equals("Forest"))
                        {
                            if (Game1.player.getTileX() < 20)
                            {
                                mapScaleX = 3.152;
                                mapScaleY = 1.82;
                                offsetX = 47;
                                offsetY = -35;
                            }
                            else if (npc.getTileX() < 66 && npc.getTileY() < 51)
                            {
                                mapScaleX = 3.152;
                                mapScaleY = 1.82;
                                offsetX = 50;
                                offsetY = -10;
                            }
                            else if (npc.getTileX() > 60 && npc.getTileX() < 90 && npc.getTileY() < 23)
                            {
                                mapScaleX = 2.152;
                                mapScaleY = 1.82;
                                offsetX = 110;
                            }
                            else if (npc.getTileX() < 74 && npc.getTileY() < 49)
                            {
                                mapScaleX = 3.152;
                                mapScaleY = 1.82;
                                offsetX = 30;
                            }
                            else if (npc.getTileX() < 120 && npc.getTileY() < 52)
                            {
                                mapScaleX = 3.2;
                                mapScaleY = 1.8;
                                offsetX = 15;
                                offsetY = -10;
                            }
                            else if (npc.getTileX() < 120 && npc.getTileY() < 101)
                            {
                                mapScaleX = 2.101;
                                mapScaleY = 2.208;
                            }
                        }

                        // Partitions for Beach ------------------------------------------------------------------------------------
                        else if (currentLocation.Equals("Beach"))
                        {
                            if (npc.getTileY() < 7)
                            {
                                offsetX = -50;
                                offsetY = 10;
                            }
                            else if (npc.getTileX() < 39 && npc.getTileY() < 22)
                            {
                                mapScaleX = 1.21;
                                mapScaleY = 2.33;
                                offsetX = -20;
                            }
                            else if (npc.getTileX() < 58 && npc.getTileX() > 28 && npc.getTileY() < 27)
                            {
                                mapScaleX = 1.11;
                                mapScaleY = 2.33;
                                offsetX = 15;
                            }

                            else if (npc.getTileX() < 58 && npc.getTileY() < 37)
                            {
                                mapScaleX = 2.745;
                                mapScaleY = 2.833;
                                offsetX = -20;
                            }
                        }

                        // Partitions for Mountain ------------------------------------------------------------------------------------
                        else if (currentLocation.Equals("Mountain"))
                        {
                            if (npc.getTileX() < 41 && npc.getTileY() < 16)
                            {
                                mapScaleX = 2.9;
                                mapScaleY = 2.46;
                                offsetX = -10;

                            }
                            else if (npc.getTileX() < 41 && npc.getTileY() < 41)
                            {
                                mapScaleX = 2.9;
                                mapScaleY = 1.825;
                            }
                            else if (npc.getTileX() < 61 && npc.getTileY() < 41)
                            {
                                mapScaleX = 2.5;
                                mapScaleY = 2.3;
                            }
                        }

                        x = (int)(((Game1.activeClickableMenu.xPositionOnScreen - 160) + (4 + npcLocation[0] + npc.getTileX() * mapScaleX + offsetX)));
                        y = (int)(((Game1.activeClickableMenu.yPositionOnScreen - 20) + (5 + npcLocation[1] + npc.getTileY() * mapScaleY + offsetY)));
                        width = 32;
                        height = 30;

                        if (npcMarkers.ContainsKey(npc.name))
                        {
                            npcMarkers[npc.name].position = new Rectangle(x, y, width, height);
                        }
                        else
                        {
                            npcMarkers.Add(npc.name, new NPCMarker(npc.sprite.Texture, new Rectangle(x, y, width, height)));
                        }

                        if (Game1.getMouseX() >= x + 2 && Game1.getMouseX() <= x - 2 + width && Game1.getMouseY() >= y + 2 && Game1.getMouseY() <= y - 2 + height)
                        {
                            npcNamesHovered.Add(npc.name);
                        }
                    }
                    else
                    {
                        npcMarkers.Remove(npc.name);
                    }
                }
            }

            if (npcNamesHovered != null && npcNamesHovered.Count > 0)
            {
                npcNames = npcNamesHovered[0];
                for (int i = 1; i < npcNamesHovered.Count; i++)
                {
                    npcNames += ", " + npcNamesHovered[i];
                };
            }
            toolTips = new MapPageTooltips(npcNames, config.nameTooltipMode);
            
        }

        // Draw event (when Map Page is opened)
        private void GraphicsEvents_OnPostRenderEvent(object sender, EventArgs e)
        {
            if (Game1.hasLoadedGame && Game1.activeClickableMenu is GameMenu)
            {
                drawMarkers((GameMenu)Game1.activeClickableMenu);

            }
        }

        // Actual draw event
        static void drawMarkers(GameMenu menu)
        {
            if (menu.currentTab == 3)
            {
                /*
                // Player testing
                int offsetX = 0;
                int offsetY = 0;
                int x = 0;
                int y = 0;
                int width = 0;
                int height = 0;
                double[] npcLocation;
                string currentLocation = Game1.player.currentLocation.name;

                locationVectors.TryGetValue(currentLocation, out npcLocation);
                double mapScaleX = npcLocation[2];
                double mapScaleY = npcLocation[3];

                // Partitioning large areas because map page sucks
                // In addition to all the locations on the map, all of these values were meticulously calculated to make
                // real-time tracking accurate with a badly scaling map page (only on NPC paths). DO NOT MESS WITH THESE UNLESS YOU CAN DO IT BETTER.

                // Partitions for Town
                if (currentLocation.Equals("Town"))
                {
                    if (Game1.player.getTileX() < 28 && Game1.player.getTileY() < 58 && Game1.player.getTileY() > 53)
                    {
                        offsetX = 5;
                        offsetY = -30;
                    }
                    else if (Game1.player.getTileX() < 31 && Game1.player.getTileX() > 26 && Game1.player.getTileY() > 74 && Game1.player.getTileY() < 90)
                    {
                        offsetX = 10;
                    }
                    else if (Game1.player.getTileX() < 30 && Game1.player.getTileY() > 89 && Game1.player.getTileY() < 98)
                    {
                        offsetY = -5;
                        offsetX = 5;
                    }
                    else if (Game1.player.getTileX() < 57 && Game1.player.getTileY() > 98 && Game1.player.getTileY() < 109)
                    {
                        offsetX = 30;
                    }
                    else if (Game1.player.getTileX() < 78 && Game1.player.getTileY() < 103 && Game1.player.getTileY() > 40)
                    {
                        mapScaleX = 3.01;
                        mapScaleY = 2.94;
                        offsetY = -10;
                    }
                    else if (Game1.player.getTileX() < 85 && Game1.player.getTileY() < 43)
                    {
                        mapScaleX = 2.48;
                        mapScaleY = 2.52;
                        offsetX = -15;
                    }

                    else if (Game1.player.getTileX() > 90 && Game1.player.getTileY() < 41)
                    {
                        offsetX = -20;
                        offsetY = 25;
                    }
                    else if (Game1.player.getTileX() > 77 && Game1.player.getTileY() < 61)
                    {
                        mapScaleX = 3.21;
                        mapScaleY = 2.64;
                        offsetX = -3;
                        offsetY = -3;
                    }
                    else if (Game1.player.getTileX() > 78 && Game1.player.getTileY() > 60)
                    {
                        mapScaleX = 3.21;
                        mapScaleY = 3.34;
                        offsetX = -22;
                        offsetY = -35;
                    }
                }

                // Partitions for Forest ------------------------------------------------------------------------------------
                else if (currentLocation.Equals("Forest"))
                {
                    if (Game1.player.getTileX() < 20)
                    {
                        mapScaleX = 3.152;
                        mapScaleY = 1.82;
                        offsetX = 47;
                        offsetY = -35;
                    }
                    else if (Game1.player.getTileX() < 66 && Game1.player.getTileY() < 51)
                    {
                        mapScaleX = 3.152;
                        mapScaleY = 1.82;
                        offsetX = 50;
                        offsetY = -10;
                    }
                    else if (Game1.player.getTileX() > 60 && Game1.player.getTileX() < 90 && Game1.player.getTileY() < 23)
                    {
                        mapScaleX = 2.152;
                        mapScaleY = 1.82;
                        offsetX = 110;
                    }
                    else if (Game1.player.getTileX() < 74 && Game1.player.getTileY() < 49)
                    {
                        mapScaleX = 3.152;
                        mapScaleY = 1.82;
                        offsetX = 30;
                    }
                    else if (Game1.player.getTileX() < 120 && Game1.player.getTileY() < 52)
                    {
                        mapScaleX = 3.2;
                        mapScaleY = 1.8;
                        offsetX = 15;
                        offsetY = -10;
                    }
                    else if (Game1.player.getTileX() < 120 && Game1.player.getTileY() < 101)
                    {
                        mapScaleX = 2.101;
                        mapScaleY = 2.208;
                    }
                }

                // Partitions for Beach ------------------------------------------------------------------------------------
                else if (currentLocation.Equals("Beach"))
                {
                    if (Game1.player.getTileY() < 7)
                    {
                        offsetX = -50;
                        offsetY = 10;
                    }
                    else if (Game1.player.getTileX() < 39 && Game1.player.getTileY() < 22)
                    {
                        mapScaleX = 1.21;
                        mapScaleY = 2.33;
                        offsetX = -20;
                    }
                    else if (Game1.player.getTileX() < 58 && Game1.player.getTileX() > 28 && Game1.player.getTileY() < 27)
                    {
                        mapScaleX = 1.11;
                        mapScaleY = 2.33;
                        offsetX = 15;
                    }

                    else if (Game1.player.getTileX() < 58 && Game1.player.getTileY() < 37)
                    {
                        mapScaleX = 2.745;
                        mapScaleY = 2.833;
                        offsetX = -20;
                    }
                }

                // Partitions for Mountain ------------------------------------------------------------------------------------
                else if (currentLocation.Equals("Mountain"))
                {
                    if (Game1.player.getTileX() < 41 && Game1.player.getTileY() < 16)
                    {
                        mapScaleX = 2.9;
                        mapScaleY = 2.46;
                        offsetX = -10;

                    }
                    else if (Game1.player.getTileX() < 41 && Game1.player.getTileY() < 41)
                    {
                        mapScaleX = 2.9;
                        mapScaleY = 1.825;
                    }
                    else if (Game1.player.getTileX() < 61 && Game1.player.getTileY() < 41)
                    {
                        mapScaleX = 2.5;
                        mapScaleY = 2.3;
                    }
                }

                x = (int)(((Game1.activeClickableMenu.xPositionOnScreen - 160) + (4 + npcLocation[0] + Game1.player.getTileX() * mapScaleX + offsetX)));
                y = (int)(((Game1.activeClickableMenu.yPositionOnScreen - 20) + (5 + npcLocation[1] + Game1.player.getTileY() * mapScaleY + offsetY)));
                width = 32;
                height = 30;
                Game1.spriteBatch.Draw(Game1.getCharacterFromName("Kent").sprite.Texture, 
                                       new Rectangle(x, y, width, height), 
                                       new Rectangle?(new Rectangle(0, -1, 16, 15)), 
                                       Color.White);
                */

                foreach (KeyValuePair<string, NPCMarker> entry in npcMarkers)
                {
                    Game1.spriteBatch.Draw(entry.Value.marker, entry.Value.position, new Rectangle?(new Rectangle(0, spriteCrop[entry.Key], 16, 15)), Color.White);
                }
                if (config.showTravelingMerchant &&
                    (Game1.dayOfMonth == 5 || Game1.dayOfMonth == 7 || Game1.dayOfMonth == 12 || Game1.dayOfMonth == 14 || Game1.dayOfMonth == 19 || Game1.dayOfMonth == 21 || Game1.dayOfMonth == 26 || Game1.dayOfMonth == 28)) { 
                    Game1.spriteBatch.Draw(Game1.mouseCursors, new Vector2(Game1.activeClickableMenu.xPositionOnScreen + 140, Game1.activeClickableMenu.yPositionOnScreen + 355), new Rectangle?(new Rectangle(191, 1410, 22, 21)), Color.White, 0f, Vector2.Zero, 1.3f, SpriteEffects.None, 1f);
                }
                toolTips.draw(Game1.spriteBatch);   
                Game1.spriteBatch.Draw(Game1.mouseCursors, new Vector2((float)Game1.getOldMouseX(), (float)Game1.getOldMouseY()), new Rectangle?(Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, (Game1.options.gamepadControls ? 44 : 0), 16, 16)), Color.White, 0f, Vector2.Zero, ((float)Game1.pixelZoom + Game1.dialogueButtonScale / 150f), SpriteEffects.None, 1f);
            }
        }

        private static bool showNPC(string npc) 
        {
            if (npc.Equals("Abigail")) { return config.showAbigail; }
            if (npc.Equals("Alex")) { return config.showAlex; }
            if (npc.Equals("Caroline")) { return config.showCaroline; }
            if (npc.Equals("Clint")) { return config.showClint; }
            if (npc.Equals("Demetrius")) { return config.showDemetrius; }
            if (npc.Equals("Elliott")) { return config.showElliott; }
            if (npc.Equals("Emily")) { return config.showEmily; }
            if (npc.Equals("Evelyn")) { return config.showEvelyn; }
            if (npc.Equals("George")) { return config.showGeorge; }
            if (npc.Equals("Gus")) { return config.showGus; }
            if (npc.Equals("Haley")) { return config.showHaley; }
            if (npc.Equals("Harvey")) { return config.showHarvey; }
            if (npc.Equals("Jas")) { return config.showJas; }
            if (npc.Equals("Jodi")) { return config.showJodi; }
            if (npc.Equals("Kent")) { return config.showKent; }
            if (npc.Equals("Leah")) { return config.showLeah; }
            if (npc.Equals("Lewis")) { return config.showLewis; }
            if (npc.Equals("Linus")) { return config.showLinus; }
            if (npc.Equals("Marnie")) { return config.showMarnie; }
            if (npc.Equals("Maru")) { return config.showMaru; }
            if (npc.Equals("Pam")) { return config.showPam; }
            if (npc.Equals("Penny")) { return config.showPenny; }
            if (npc.Equals("Pierre")) { return config.showPierre; }
            if (npc.Equals("Robin")) { return config.showRobin; }
            if (npc.Equals("Sam")) { return config.showSam; }
            if (npc.Equals("Sebastian")) { return config.showSebastian; }
            if (npc.Equals("Shane")) { return config.showShane; }
            if (npc.Equals("Vincent")) { return config.showVincent; }
            if (npc.Equals("Willy")) { return config.showWilly; }
            if (npc.Equals("Sandy")) { return config.showSandy; }
            if (npc.Equals("Marlon")) { return config.showMarlon; }
            if (npc.Equals("Wizard")) { return config.showWizard; }
            return true;
        }
    }

    // Class for NPC markers
    public class NPCMarker
    {
        public Texture2D marker;
        public Rectangle position;

        public NPCMarker(Texture2D marker, Rectangle position)     
        {
            this.marker = marker;
            this.position = position;
        }
    }

    // For drawing tooltips
    public class MapPageTooltips : IClickableMenu
    {
        private string hoverText = "";
        private Texture2D map;
        private int mapX;
        private int mapY;
        private List<ClickableComponent> points = new List<ClickableComponent>();
        private string names;
        private int nameTooltipMode;

        public MapPageTooltips(string names, int nameTooltipMode)
        {
            this.nameTooltipMode = nameTooltipMode;
            this.names = names;
            this.map = Game1.content.Load<Texture2D>("LooseSprites\\map");
            Vector2 topLeftPositionForCenteringOnScreen = Utility.getTopLeftPositionForCenteringOnScreen(this.map.Bounds.Width * Game1.pixelZoom, 180 * Game1.pixelZoom, 0, 0);
            this.mapX = (int)topLeftPositionForCenteringOnScreen.X;
            this.mapY = (int)topLeftPositionForCenteringOnScreen.Y;
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX, this.mapY, 292, 152), Game1.player.mailReceived.Contains("ccVault") ? "Calico Desert" : "???"));
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 324, this.mapY + 252, 188, 132), Game1.player.farmName + " Farm"));
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 360, this.mapY + 96, 188, 132), "Backwoods"));
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 516, this.mapY + 224, 76, 100), "Bus Stop"));
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 196, this.mapY + 352, 36, 76), "Wizard's Tower"));
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 420, this.mapY + 392, 76, 40), "Marnie's Ranch" + Environment.NewLine + "Open 9:00AM to 4:00PM most days"));
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 452, this.mapY + 436, 32, 24), "Leah's Cottage"));
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 612, this.mapY + 396, 36, 52), "1 Willow Lane" + Environment.NewLine + "Home of Jodi, Kent & Sam"));
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 652, this.mapY + 408, 40, 36), "2 Willow Lane" + Environment.NewLine + "Home of Emily & Haley"));
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 672, this.mapY + 340, 44, 60), "Town Square"));
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 680, this.mapY + 304, 16, 32), "Harvey's Clinic" + Environment.NewLine + "Open 9:00AM to 3:00PM"));
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 696, this.mapY + 296, 28, 40), string.Concat(new string[]
            {
                "Pierre's General Store",
                Environment.NewLine,
                "Home of Pierre, Caroline & Abigail ",
                Environment.NewLine,
                "Open 9:00AM to 6:00PM (Closed Wednesday)"
            })));
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 852, this.mapY + 388, 80, 36), "Blacksmith" + Environment.NewLine + "Open 9:00AM to 4:00PM"));
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 716, this.mapY + 352, 28, 40), "Saloon" + Environment.NewLine + "Open 12:00PM To 12:00AM"));
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 768, this.mapY + 388, 44, 56), "Mayor's Manor"));
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 892, this.mapY + 416, 32, 28), "Stardew Valley Museum & Library" + Environment.NewLine + "Open 8:00AM to 6:00PM"));
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 824, this.mapY + 564, 28, 20), "Elliott's Cabin"));
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 696, this.mapY + 448, 24, 20), "Sewer"));
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 724, this.mapY + 424, 40, 32), "Graveyard"));
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 780, this.mapY + 360, 24, 20), "Trailer"));
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 748, this.mapY + 316, 36, 36), "1 River Road" + Environment.NewLine + "Home of George, Evelyn & Alex"));
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 732, this.mapY + 148, 48, 32), string.Concat(new string[]
            {
                "Carpenter's Shop",
                Environment.NewLine,
                "Home of Robin, Demetrius, Sebastian & Maru",
                Environment.NewLine,
                "Shop open 9:00AM to 5:00PM most days"
            })));
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 784, this.mapY + 128, 12, 16), "Tent"));
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 880, this.mapY + 96, 16, 24), "Mines"));
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 900, this.mapY + 108, 32, 36), (Game1.stats.DaysPlayed >= 5u) ? ("Adventurer's Guild" + Environment.NewLine + "Open 2:00PM to 10:00PM") : "???"));
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 968, this.mapY + 116, 88, 76), Game1.player.mailReceived.Contains("ccCraftsRoom") ? "Quarry" : "???"));
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 872, this.mapY + 280, 52, 52), "JojaMart" + Environment.NewLine + "Open 9:00AM to 11:00PM"));
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 844, this.mapY + 608, 36, 40), "Fish Shop" + Environment.NewLine + "Open 9:00AM to 5:00PM"));
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 576, this.mapY + 60, 48, 36), Game1.isLocationAccessible("Railroad") ? ("Spa" + Environment.NewLine + "Open all day") : "???"));
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX, this.mapY + 272, 196, 176), Game1.player.mailReceived.Contains("beenToWoods") ? "Secret Woods" : "???"));
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 260, this.mapY + 572, 20, 20), "Ruined House"));
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 692, this.mapY + 204, 44, 36), "Community Center"));
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 380, this.mapY + 596, 24, 32), "Sewer Pipe"));
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 644, this.mapY + 64, 16, 8), Game1.isLocationAccessible("Railroad") ? "Railroad" : "???"));
            this.points.Add(new ClickableComponent(new Rectangle(this.mapX + 728, this.mapY + 652, 28, 28), "Lonely Stone"));
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            foreach (ClickableComponent current in this.points)
            {
                string name = current.name;
                if (name == "Lonely Stone")
                {
                    Game1.playSound("stoneCrack");
                }
            }
            if (Game1.activeClickableMenu != null)
            {
                (Game1.activeClickableMenu as GameMenu).changeTab(0);
            }
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
        }

        public override void performHoverAction(int x, int y)
        {
            this.hoverText = "";
            foreach (ClickableComponent current in this.points)
            {
                if (current.containsPoint(x, y))
                {
                    this.hoverText = current.name;
                    return;
                }
            }
        }

        /*
        public void drawMap(SpriteBatch b)
        {
            Game1.drawDialogueBox(this.mapX - Game1.pixelZoom * 8, this.mapY - Game1.pixelZoom * 24, (this.map.Bounds.Width + 16) * Game1.pixelZoom, 212 * Game1.pixelZoom, false, true, null, false);
            b.Draw(this.map, new Vector2((float)this.mapX, (float)this.mapY), new Rectangle?(new Rectangle(0, 0, 300, 180)), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.86f);
            switch (Game1.whichFarm)
            {
                case 1:
                    b.Draw(this.map, new Vector2((float)this.mapX, (float)(this.mapY + 43 * Game1.pixelZoom)), new Rectangle?(new Rectangle(0, 180, 131, 61)), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
                    break;
                case 2:
                    b.Draw(this.map, new Vector2((float)this.mapX, (float)(this.mapY + 43 * Game1.pixelZoom)), new Rectangle?(new Rectangle(131, 180, 131, 61)), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
                    break;
                case 3:
                    b.Draw(this.map, new Vector2((float)this.mapX, (float)(this.mapY + 43 * Game1.pixelZoom)), new Rectangle?(new Rectangle(0, 241, 131, 61)), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
                    break;
                case 4:
                    b.Draw(this.map, new Vector2((float)this.mapX, (float)(this.mapY + 43 * Game1.pixelZoom)), new Rectangle?(new Rectangle(131, 241, 131, 61)), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.861f);
                    break;
            }
        }
        */

        // Draw location tooltips
        public override void draw(SpriteBatch b)
        {
            int x = Game1.getMouseX() + Game1.tileSize / 2;
            int y = Game1.getMouseY() + Game1.tileSize / 2;
            int offsetY = 0;
            this.performHoverAction(x - Game1.tileSize / 2, y - Game1.tileSize / 2);

            if (!this.hoverText.Equals(""))
            {
                int width = (int)Game1.smallFont.MeasureString(hoverText).X + Game1.tileSize / 2;
                int height = (int)Math.Max(60, Game1.smallFont.MeasureString(hoverText).Y + Game1.tileSize / 2);

                if (x + width > Game1.viewport.Width)
                {
                    x = Game1.viewport.Width - width;
                    y += Game1.tileSize / 4;
                }
                if (this.nameTooltipMode == 1)
                {
                    if (y + height > Game1.viewport.Height)
                    {
                        x += Game1.tileSize / 4;
                        y = Game1.viewport.Height - height;
                    }
                    offsetY = 4 - Game1.tileSize;
                }
                else if (this.nameTooltipMode == 2)
                {
                    if (y + height > Game1.viewport.Height)
                    {
                        x += Game1.tileSize / 4;
                        y = Game1.viewport.Height - height;
                    }
                    offsetY = height - 4;
                }
                else
                {
                    if (y + height > Game1.viewport.Height)
                    {
                        x += Game1.tileSize / 4;
                        y = Game1.viewport.Height - height;
                    }
                }
                drawNPCNames(Game1.spriteBatch, this.names, x, y, offsetY, height, this.nameTooltipMode);
                IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, y, width, height, Color.White, 1f, false);
                b.DrawString(Game1.smallFont, hoverText, new Vector2((float)(x + Game1.tileSize / 4), (float)(y + Game1.tileSize / 4 + 4)) + new Vector2(2f, 2f), Game1.textShadowColor);
                b.DrawString(Game1.smallFont, hoverText, new Vector2((float)(x + Game1.tileSize / 4), (float)(y + Game1.tileSize / 4 + 4)) + new Vector2(0f, 2f), Game1.textShadowColor);
                b.DrawString(Game1.smallFont, hoverText, new Vector2((float)(x + Game1.tileSize / 4), (float)(y + Game1.tileSize / 4 + 4)) + new Vector2(2f, 0f), Game1.textShadowColor);
                b.DrawString(Game1.smallFont, hoverText, new Vector2((float)(x + Game1.tileSize / 4), (float)(y + Game1.tileSize / 4 + 4)), Game1.textColor * 0.9f);
                //IClickableMenu.drawHoverText(b, this.hoverText, Game1.smallFont, 0, 0, -1, null, -1, null, null, 0, -1, -1, -1, -1, 1f, null);
            }
            else
            {
                drawNPCNames(Game1.spriteBatch, this.names, x, y, offsetY, height, this.nameTooltipMode);
            }
        }

        // Draw NPC names in bottom left corner of map page
        public static void drawNPCNames(SpriteBatch b, string names, int x, int y, int offsetY, int relocate, int nameTooltipMode)
        {
            if (!(names.Equals("")))
            {
                int height = (int)Math.Max(60, Game1.smallFont.MeasureString(names).Y + Game1.tileSize / 2);
                int width = (int)Game1.smallFont.MeasureString(names).X + Game1.tileSize / 2;

                if (nameTooltipMode == 1)
                {
                    x = Game1.getOldMouseX() + Game1.tileSize / 2;
                    y += offsetY;
                    // If going off screen on the right, move tooltip to below location tooltip so it can stay inside the screen
                    // without the cursor covering the tooltip
                    if (x + width > Game1.viewport.Width)
                    {
                        x = Game1.viewport.Width - width;
                        y += relocate - 8 + Game1.tileSize;
                    }
                }
                else if (nameTooltipMode == 2)
                {
                    y += offsetY;
                    if (x + width > Game1.viewport.Width)
                    {
                        x = Game1.viewport.Width - width;
                    }
                    // If going off screen on the bottom, move tooltip to above location tooltip so it stays visible
                    if (y + height > Game1.viewport.Height)
                    {
                        x = Game1.getOldMouseX() + Game1.tileSize / 2;
                        y += -relocate + 8 - Game1.tileSize;
                    }
                }
                else
                {
                    x = Game1.activeClickableMenu.xPositionOnScreen - 145;
                    y = Game1.activeClickableMenu.yPositionOnScreen + 625;
                }

                drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, y, width, height, Color.White, 1f, true);
                Vector2 vector = new Vector2(x + (float)(Game1.tileSize / 4), y + (float)(Game1.tileSize / 4 + 4));
                b.DrawString(Game1.smallFont, names, vector + new Vector2(2f, 2f), Game1.textShadowColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                b.DrawString(Game1.smallFont, names, vector + new Vector2(0f, 2f), Game1.textShadowColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                b.DrawString(Game1.smallFont, names, vector + new Vector2(2f, 0f), Game1.textShadowColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                b.DrawString(Game1.smallFont, names, vector, Game1.textColor * 0.9f, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            }
        }
    }
}
