using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace NPCMapLocations
{

    public class MapModMapPage : IClickableMenu
    {
        private readonly Dictionary<string, Rect> regionRects = MapModConstants.RegionRects;
        private string hoverText = "";
        private Texture2D map;
        private int mapX;
        private int mapY;
        public List<ClickableComponent> points = new List<ClickableComponent>();
        public ClickableTextureComponent okButton;
        private List<String> hoveredNames;
        private Vector2 indoorIconVector;
        private Dictionary<string, string> npcNames;
        private int nameTooltipMode;

        public MapModMapPage(List<String> hoveredNames, Dictionary<string, string> npcNames, int nameTooltipMode)
        {
            this.nameTooltipMode = nameTooltipMode;
            this.hoveredNames = hoveredNames;
            this.npcNames = npcNames;
            this.okButton = new ClickableTextureComponent(Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11059", new object[0]), new Rectangle(this.xPositionOnScreen + width + Game1.tileSize, this.yPositionOnScreen + height - IClickableMenu.borderWidth - Game1.tileSize / 4, Game1.tileSize, Game1.tileSize), null, null, Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46, -1, -1), 1f, false);
            this.map = MapModMain.map;
            Vector2 topLeftPositionForCenteringOnScreen = Utility.getTopLeftPositionForCenteringOnScreen(this.map.Bounds.Width * Game1.pixelZoom, 180 * Game1.pixelZoom, 0, 0);
            this.mapX = (int)topLeftPositionForCenteringOnScreen.X;
            this.mapY = (int)topLeftPositionForCenteringOnScreen.Y;
            this.points.Add(
                new ClickableComponent(
                    GetRegionRect("Desert_Region"),
                    Game1.player.mailReceived.Contains("ccVault") ? Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11062", new object[0]) : "???"
                )
            );
            this.points.Add(
                new ClickableComponent(
                    GetRegionRect("Farm_Region"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11064", new object[] { Game1.player.farmName })
                )
            );
            this.points.Add(
                new ClickableComponent(
                    GetRegionRect("Backwoods_Region"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11065", new object[0])
                )
            );
            this.points.Add(
                new ClickableComponent(
                    GetRegionRect("BusStop_Region"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11066", new object[0])
                )
            );
            this.points.Add(
                new ClickableComponent(
                    GetRegionRect("WizardHouse"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11067", new object[0])
                )
            );
            this.points.Add(
                new ClickableComponent(
                    GetRegionRect("AnimalShop"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11068", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11069", new object[0])
                )
            );
            this.points.Add(
                new ClickableComponent(
                    GetRegionRect("LeahHouse"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11070", new object[0])
                )
            );
            this.points.Add(
                new ClickableComponent(
                    GetRegionRect("SamHouse"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11071", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11072", new object[0])
                )
            );
            this.points.Add(
                new ClickableComponent(
                    GetRegionRect("HaleyHouse"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11073", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11074", new object[0])
                )
            );
            this.points.Add(
                new ClickableComponent(
                    GetRegionRect("TownSquare"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11075", new object[0])
                )
            );
            this.points.Add(
                new ClickableComponent(
                    GetRegionRect("Hospital"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11076", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11077", new object[0])
                )
            );
            this.points.Add(
                new ClickableComponent(
                    GetRegionRect("SeedShop"),
                    string.Concat(new string[]
                    {
                        Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11078", new object[0]),
                        Environment.NewLine,
                        Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11079", new object[0]),
                        Environment.NewLine,
                        Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11080", new object[0])
                    })
                )
            );
            this.points.Add(
                new ClickableComponent(
                    GetRegionRect("Blacksmith"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11081", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11082", new object[0])
                )
            );
            this.points.Add(
                new ClickableComponent(
                    GetRegionRect("Saloon"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11083", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11084", new object[0])
                )
            );
            this.points.Add(
                new ClickableComponent(
                    GetRegionRect("ManorHouse"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11085", new object[0])
                )
            );
            this.points.Add(
                new ClickableComponent(
                    GetRegionRect("ArchaeologyHouse"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11086", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11087", new object[0])
                )
            );
            this.points.Add(
                new ClickableComponent(
                    GetRegionRect("ElliottHouse"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11088", new object[0])
                )
            );
            this.points.Add(
                new ClickableComponent(
                    GetRegionRect("Sewer"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11089", new object[0])
                )
            );
            this.points.Add(
                new ClickableComponent(
                    GetRegionRect("Graveyard"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11090", new object[0])
                )
            );
            this.points.Add(
                new ClickableComponent(
                    GetRegionRect("Trailer"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11091", new object[0])
                )
            );
            this.points.Add(
                new ClickableComponent(
                    GetRegionRect("JoshHouse"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11092", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11093", new object[0])
                )
            );
            this.points.Add(
                new ClickableComponent(
                    GetRegionRect("ScienceHouse"),
                    string.Concat(new string[]
                    {
                        Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11094", new object[0]),
                        Environment.NewLine,
                        Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11095", new object[0]),
                        Environment.NewLine,
                        Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11096", new object[0])
                    })
                )
            );
            this.points.Add(
                new ClickableComponent(
                    GetRegionRect("Tent"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11097", new object[0])
                )
            );
            this.points.Add(
                new ClickableComponent(
                    GetRegionRect("Mine"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11098", new object[0])
                )
            );
            this.points.Add(
                new ClickableComponent(
                    GetRegionRect("AdventureGuild"),
                    (Game1.stats.DaysPlayed >= 5u) ? (Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11099", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11100", new object[0])) : "???"
                )
            );
            this.points.Add(
                new ClickableComponent(
                    GetRegionRect("Quarry"),
                    Game1.player.mailReceived.Contains("ccCraftsRoom") ? Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11103", new object[0]) : "???"
                )
            );
            this.points.Add(
                new ClickableComponent(
                    GetRegionRect("JojaMart"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11105", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11106", new object[0])
                )
            );
            this.points.Add(
                new ClickableComponent(
                    GetRegionRect("FishShop"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11107", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11108", new object[0])
                )
            );
            this.points.Add(
                new ClickableComponent(
                    GetRegionRect("Spa"),
                    Game1.isLocationAccessible("Railroad") ? (Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11110", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11111", new object[0])) : "???"
                )
            );
            this.points.Add(
                new ClickableComponent(
                    GetRegionRect("Woods"),
                    Game1.player.mailReceived.Contains("beenToWoods") ? Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11114", new object[0]) : "???"
                )
            );
            this.points.Add(
                new ClickableComponent(
                    GetRegionRect("RuinedHouse"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11116", new object[0])
                )
            );
            this.points.Add(
                new ClickableComponent(
                    GetRegionRect("CommunityCenter"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11117", new object[0])
                )
            );
            this.points.Add(
                new ClickableComponent(
                    GetRegionRect("SewerPipe"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11118", new object[0])
                )
            );
            this.points.Add(
                new ClickableComponent(
                    GetRegionRect("Railroad_Region"),
                    Game1.isLocationAccessible("Railroad") ? Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11119", new object[0]) : "???"
                )
            );
            this.points.Add(
                new ClickableComponent(
                    GetRegionRect("LonelyStone"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11122", new object[0])
                )
            );

            // Remove vanilla map tooltips
            GameMenu menu = (GameMenu)Game1.activeClickableMenu;
            List<IClickableMenu> menuPages = (List<IClickableMenu>)typeof(GameMenu).GetField("pages", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(menu);
            MapPage mapPage = (MapPage)menuPages[menu.currentTab];
            MapModMain.modHelper.Reflection.GetField<List<ClickableComponent>>(mapPage, "points").SetValue(new List<ClickableComponent>());
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

        // Disable snapping cursor on controller
        public override bool overrideSnappyMenuCursorMovementBan()
        {
            return true;
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

        // Draw location tooltips
        public override void draw(SpriteBatch b)
        {
            string names = "";
            int x = Game1.getMouseX() + Game1.tileSize / 2;
            int y = Game1.getMouseY() + Game1.tileSize / 2;
            int offsetY = 0;
            this.performHoverAction(x - Game1.tileSize / 2, y - Game1.tileSize / 2);

            if (!this.hoverText.Equals(""))
            {
                IClickableMenu.drawHoverText(b, this.hoverText, Game1.smallFont, 0, 0, -1, null, -1, null, null, 0, -1, -1, -1, -1, 1f, null);
                int textLength = (int)Game1.smallFont.MeasureString(hoverText).X + Game1.tileSize / 2;
                foreach (KeyValuePair<string, string> customName in npcNames)
                {
                    this.hoverText = this.hoverText.Replace(customName.Key, customName.Value);
                }
                int ttWidth = Math.Max((int)Game1.smallFont.MeasureString(hoverText).X + Game1.tileSize / 2, textLength);
                int ttHeight = (int)Math.Max(60, Game1.smallFont.MeasureString(hoverText).Y + Game1.tileSize / 2);
                if (x + ttWidth > Game1.viewport.Width)
                {
                    x = Game1.viewport.Width - ttWidth;
                    y += Game1.tileSize / 4;
                }
                if (this.nameTooltipMode == 1)
                {
                    if (y + ttHeight > Game1.viewport.Height)
                    {
                        x += Game1.tileSize / 4;
                        y = Game1.viewport.Height - ttHeight;
                    }
                    offsetY = 4 - Game1.tileSize;
                }
                else if (this.nameTooltipMode == 2)
                {
                    if (y + ttHeight > Game1.viewport.Height)
                    {
                        x += Game1.tileSize / 4;
                        y = Game1.viewport.Height -ttHeight;
                    }
                    offsetY = ttHeight - 4;
                }
                else
                {
                    if (y + ttHeight > Game1.viewport.Height)
                    {
                        x += Game1.tileSize / 4;
                        y = Game1.viewport.Height - ttHeight;
                    }
                }

                if (this.hoveredNames != null && this.hoveredNames.Count > 0)
                {
                    foreach (string name in this.hoveredNames)
                    {
                        if (names == "")
                        {
                            names += name;
                            continue;
                        }

                        // Have to use special character to separate strings for Chinese
                        var separator = LocalizedContentManager.CurrentLanguageCode.Equals(LocalizedContentManager.LanguageCode.zh) ? "，" : ", ";
                        var lines = names.Split('\n');
                        if ((int)Game1.smallFont.MeasureString(lines[lines.Length - 1] + separator + name).X > (int)Game1.smallFont.MeasureString("Home of Robin, Demetrius, Sebastian & Maru").X) // Longest string
                        {
                            names += separator + Environment.NewLine;
                            names += name;
                        }
                        else
                        {
                            names += separator + name;
                        }
                    }
                }

                // Name tooltip
                DrawNPCNames(Game1.spriteBatch, names, x, y, offsetY, ttHeight, nameTooltipMode);

                // Location tooltips
                IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, y, ttWidth, ttHeight, Color.White, 1f, false);
                b.DrawString(Game1.smallFont, hoverText, new Vector2((float)(x + Game1.tileSize / 4), (float)(y + Game1.tileSize / 4 + 4)) + new Vector2(2f, 2f), Game1.textShadowColor);
                b.DrawString(Game1.smallFont, hoverText, new Vector2((float)(x + Game1.tileSize / 4), (float)(y + Game1.tileSize / 4 + 4)) + new Vector2(0f, 2f), Game1.textShadowColor);
                b.DrawString(Game1.smallFont, hoverText, new Vector2((float)(x + Game1.tileSize / 4), (float)(y + Game1.tileSize / 4 + 4)) + new Vector2(2f, 0f), Game1.textShadowColor);
                b.DrawString(Game1.smallFont, hoverText, new Vector2((float)(x + Game1.tileSize / 4), (float)(y + Game1.tileSize / 4 + 4)), Game1.textColor * 0.9f);
            }
            else
            {
                DrawNPCNames(Game1.spriteBatch, names, x, y, offsetY, this.height, nameTooltipMode);
            }
            if (names.Length > 0)
                b.Draw(Game1.mouseCursors, indoorIconVector, new Rectangle?(new Rectangle(448, 64, 32, 32)), Color.White, 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);
        }

        // Draw map to cover base rendering 
        public void DrawMap(SpriteBatch b)
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

        // Draw NPC names in bottom left corner of map page
        public void DrawNPCNames(SpriteBatch b, string names, int x, int y, int offsetY, int relocate, int nameTooltipMode)
        {
            if (!(names.Equals("")))
            {
                var lines = names.Split('\n');
                int ttHeight = (int)Math.Max(60, Game1.smallFont.MeasureString(names).Y + Game1.tileSize/2);
                int ttWidth = (int)Game1.smallFont.MeasureString(names).X + Game1.tileSize/2;

                if (nameTooltipMode == 1)
                {
                    x = Game1.getOldMouseX() + Game1.tileSize / 2;
                    if (lines.Length > 1)
                    {
                        y += offsetY - ((int)Game1.smallFont.MeasureString(names).Y) + Game1.tileSize / 2;
                    }
                    else
                    {
                        y += offsetY;
                    }
                    // If going off screen on the right, move tooltip to below location tooltip so it can stay inside the screen
                    // without the cursor covering the tooltip
                    if (x + ttWidth > Game1.viewport.Width)
                    {
                        x = Game1.viewport.Width - ttWidth;
                        if (lines.Length > 1)
                        {
                            y += relocate - 8 + ((int)Game1.smallFont.MeasureString(names).Y) + Game1.tileSize / 2;
                        }
                        else
                        {
                            y += relocate - 8 + Game1.tileSize;
                        }
                    }
                }
                else if (nameTooltipMode == 2)
                {
                    y += offsetY;
                    if (x + ttWidth > Game1.viewport.Width)
                    {
                        x = Game1.viewport.Width - ttWidth;
                    }
                    // If going off screen on the bottom, move tooltip to above location tooltip so it stays visible
                    if (y + ttHeight > Game1.viewport.Height)
                    {
                        x = Game1.getOldMouseX() + Game1.tileSize / 2;
                        if (lines.Length > 1)
                        {
                            y += -relocate + 8 - ((int)Game1.smallFont.MeasureString(names).Y) + Game1.tileSize / 2;
                        }
                        else
                        {
                            y += -relocate + 8 - Game1.tileSize;
                        }
                    }
                }
                else
                {
                    x = Game1.activeClickableMenu.xPositionOnScreen - 145;
                    y = Game1.activeClickableMenu.yPositionOnScreen + 650 - ttHeight / 2;
                }

                indoorIconVector = new Vector2(x - Game1.tileSize / 8 + 2, y - Game1.tileSize / 8 + 2);
                Vector2 vector = new Vector2(x + (float)(Game1.tileSize / 4), y + (float)(Game1.tileSize / 4 + 4));

                drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, y, ttWidth, ttHeight, Color.White, 1f, true);
                b.DrawString(Game1.smallFont, names, vector + new Vector2(2f, 2f), Game1.textShadowColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                b.DrawString(Game1.smallFont, names, vector + new Vector2(0f, 2f), Game1.textShadowColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                b.DrawString(Game1.smallFont, names, vector + new Vector2(2f, 0f), Game1.textShadowColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                b.DrawString(Game1.smallFont, names, vector, Game1.textColor * 0.9f, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            }
        }

        private Rectangle GetRegionRect(string region)
        {
            // Set origin to center
            return new Rectangle(
                (int)MapModMain.LocationToMap(region).X - regionRects[region].width/2,
                (int)MapModMain.LocationToMap(region).Y - regionRects[region].height/2,
                regionRects[region].width,
                regionRects[region].height
            );
        }
    }
}

