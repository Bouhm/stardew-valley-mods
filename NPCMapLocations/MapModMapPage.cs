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
        public const int region_desert = 1001;
        public const int region_farm = 1002;
        public const int region_backwoods = 1003;
        public const int region_busstop = 1004;
        public const int region_wizardtower = 1005;
        public const int region_marnieranch = 1006;
        public const int region_leahcottage = 1007;
        public const int region_samhouse = 1008;
        public const int region_haleyhouse = 1009;
        public const int region_townsquare = 1010;
        public const int region_harveyclinic = 1011;
        public const int region_generalstore = 1012;
        public const int region_blacksmith = 1013;
        public const int region_saloon = 1014;
        public const int region_manor = 1015;
        public const int region_museum = 1016;
        public const int region_elliottcabin = 1017;
        public const int region_sewer = 1018;
        public const int region_graveyard = 1019;
        public const int region_trailer = 1020;
        public const int region_alexhouse = 1021;
        public const int region_sciencehouse = 1022;
        public const int region_tent = 1023;
        public const int region_mines = 1024;
        public const int region_adventureguild = 1025;
        public const int region_quarry = 1026;
        public const int region_jojamart = 1027;
        public const int region_fishshop = 1028;
        public const int region_spa = 1029;
        public const int region_secretwoods = 1030;
        public const int region_ruinedhouse = 1031;
        public const int region_communitycenter = 1032;
        public const int region_sewerpipe = 1033;
        public const int region_railroad = 1034;
        private Dictionary<string, Rect> regionRects = MapModConstants.regionRects;
        private string hoverText = "";
        private Texture2D map;
        private int mapX;
        private int mapY;
        public List<ClickableComponent> points = new List<ClickableComponent>();
        public ClickableTextureComponent okButton;
        private string names;
        private Vector2 indoorIconVector;
        private Dictionary<string, string> npcNames;
        private int nameTooltipMode;
        private const int GIFT_WIDTH = 14;
        private const int QUEST_WIDTH = 8;
        private const int SPACE_WIDTH = 10;

        public MapModMapPage(string names, Dictionary<string, string> npcNames, int nameTooltipMode, GameMenu menu)
        {
            this.nameTooltipMode = nameTooltipMode;
            this.names = names;
            this.npcNames = npcNames;
            this.okButton = new ClickableTextureComponent(Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11059", new object[0]), new Rectangle(this.xPositionOnScreen + width + Game1.tileSize, this.yPositionOnScreen + height - IClickableMenu.borderWidth - Game1.tileSize / 4, Game1.tileSize, Game1.tileSize), null, null, Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46, -1, -1), 1f, false);
            this.map = MapModMain.map;
            Vector2 topLeftPositionForCenteringOnScreen = Utility.getTopLeftPositionForCenteringOnScreen(this.map.Bounds.Width * Game1.pixelZoom, 180 * Game1.pixelZoom, 0, 0);
            this.mapX = (int)topLeftPositionForCenteringOnScreen.X;
            this.mapY = (int)topLeftPositionForCenteringOnScreen.Y;
            this.points.Add(
                new ClickableComponent(
                    getRegionRect("Desert_Region"),
                    Game1.player.mailReceived.Contains("ccVault") ? Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11062", new object[0]) : "???"
                )
                {
                    myID = 1001,
                    rightNeighborID = 1003,
                    downNeighborID = 1030
                }
            );
            this.points.Add(
                new ClickableComponent(
                    getRegionRect("Farm_Region"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11064", new object[] { Game1.player.farmName })
                )
                {
                    myID = 1002,
                    leftNeighborID = 1005,
                    upNeighborID = 1003,
                    rightNeighborID = 1004,
                    downNeighborID = 1006
                }
            );
            this.points.Add(
                new ClickableComponent(
                    getRegionRect("Backwoods_Region"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11065", new object[0])
                )
                {
                    myID = 1003,
                    downNeighborID = 1002,
                    leftNeighborID = 1001,
                    rightNeighborID = 1022,
                    upNeighborID = 1029
                }
            );
            this.points.Add(
                new ClickableComponent(
                    getRegionRect("BusStop_Region"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11066", new object[0])
                )
                {
                    myID = 1004,
                    leftNeighborID = 1002,
                    upNeighborID = 1003,
                    downNeighborID = 1006,
                    rightNeighborID = 1011
                }
            );
            this.points.Add(
                new ClickableComponent(
                    getRegionRect("WizardHouse"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11067", new object[0])
                )
                {
                    myID = 1005,
                    upNeighborID = 1001,
                    downNeighborID = 1031,
                    rightNeighborID = 1006,
                    leftNeighborID = 1030
                }
            );
            this.points.Add(
                new ClickableComponent(
                    getRegionRect("AnimalShop"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11068", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11069", new object[0])
                )
                {
                    myID = 1006,
                    leftNeighborID = 1005,
                    downNeighborID = 1007,
                    upNeighborID = 1002,
                    rightNeighborID = 1008
                }
            );
            this.points.Add(
                new ClickableComponent(
                    getRegionRect("LeahHouse"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11070", new object[0])
                )
                {
                    myID = 1007,
                    upNeighborID = 1006,
                    downNeighborID = 1033,
                    leftNeighborID = 1005,
                    rightNeighborID = 1008
                }
            );
            this.points.Add(
                new ClickableComponent(
                    getRegionRect("SamHouse"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11071", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11072", new object[0])
                )
                {
                    myID = 1008,
                    leftNeighborID = 1006,
                    upNeighborID = 1010,
                    rightNeighborID = 1009
                }
            );
            this.points.Add(
                new ClickableComponent(
                    getRegionRect("HaleyHouse"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11073", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11074", new object[0])
                )
                {
                    myID = 1009,
                    leftNeighborID = 1008,
                    upNeighborID = 1010,
                    rightNeighborID = 1018
                }
            );
            this.points.Add(
                new ClickableComponent(
                    getRegionRect("TownSquare"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11075", new object[0])
                )
                {
                    myID = 1010,
                    leftNeighborID = 1008,
                    downNeighborID = 1009,
                    rightNeighborID = 1014,
                    upNeighborID = 1011
                }
            );
            this.points.Add(
                new ClickableComponent(
                    getRegionRect("Hospital"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11076", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11077", new object[0])
                )
                {
                    myID = 1011,
                    leftNeighborID = 1004,
                    rightNeighborID = 1012,
                    downNeighborID = 1010,
                    upNeighborID = 1032
                }
            );
            this.points.Add(
                new ClickableComponent(
                    getRegionRect("SeedShop"),
                    string.Concat(new string[]
                    {
                        Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11078", new object[0]),
                        Environment.NewLine,
                        Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11079", new object[0]),
                        Environment.NewLine,
                        Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11080", new object[0])
                    })
                )
                {
                    myID = 1012,
                    leftNeighborID = 1011,
                    downNeighborID = 1014,
                    rightNeighborID = 1021,
                    upNeighborID = 1032
                }
            );
            this.points.Add(
                new ClickableComponent(
                    getRegionRect("Blacksmith"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11081", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11082", new object[0])
                )
                {
                    myID = 1013,
                    upNeighborID = 1027,
                    rightNeighborID = 1016,
                    downNeighborID = 1017,
                    leftNeighborID = 1015
                }
            );
            this.points.Add(
                new ClickableComponent(
                    getRegionRect("Saloon"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11083", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11084", new object[0])
                )
                {
                    myID = 1014,
                    leftNeighborID = 1010,
                    rightNeighborID = 1020,
                    downNeighborID = 1019,
                    upNeighborID = 1012
                }
            );
            this.points.Add(
                new ClickableComponent(
                    getRegionRect("ManorHouse"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11085", new object[0])
                )
                {
                    myID = 1015,
                    leftNeighborID = 1019,
                    upNeighborID = 1020,
                    rightNeighborID = 1013,
                    downNeighborID = 1017
                }
            );
            this.points.Add(
                new ClickableComponent(
                    getRegionRect("ArchaeologyHouse"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11086", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11087", new object[0])
                )
                {
                    myID = 1016,
                    downNeighborID = 1017,
                    leftNeighborID = 1013,
                    upNeighborID = 1027,
                    rightNeighborID = 99989
                }
            );
            this.points.Add(
                new ClickableComponent(
                    getRegionRect("ElliottHouse"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11088", new object[0])
                )
                {
                    myID = 1017,
                    downNeighborID = 1028,
                    upNeighborID = 1015,
                    rightNeighborID = 99989
                }
            );
            this.points.Add(
                new ClickableComponent(
                    getRegionRect("Sewer"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11089", new object[0])
                    )
                {
                    myID = 1018,
                    downNeighborID = 1017,
                    rightNeighborID = 1019,
                    upNeighborID = 1014,
                    leftNeighborID = 1009
                }
            );
            this.points.Add(
                new ClickableComponent(
                    getRegionRect("Graveyard"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11090", new object[0])
                )
                {
                    myID = 1019,
                    leftNeighborID = 1018,
                    upNeighborID = 1014,
                    rightNeighborID = 1015,
                    downNeighborID = 1017
                }
            );
            this.points.Add(
                new ClickableComponent(
                    getRegionRect("Trailer"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11091", new object[0])
                )
                {
                    myID = 1020,
                    upNeighborID = 1021,
                    leftNeighborID = 1014,
                    downNeighborID = 1015,
                    rightNeighborID = 1027
                }
            );
            this.points.Add(
                new ClickableComponent(
                    getRegionRect("JoshHouse"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11092", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11093", new object[0])
                )
                {
                    myID = 1021,
                    rightNeighborID = 1027,
                    downNeighborID = 1020,
                    leftNeighborID = 1012,
                    upNeighborID = 1032
                }
            );
            this.points.Add(
                new ClickableComponent(
                    getRegionRect("ScienceHouse"),
                    string.Concat(new string[]
                    {
                        Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11094", new object[0]),
                        Environment.NewLine,
                        Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11095", new object[0]),
                        Environment.NewLine,
                        Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11096", new object[0])
                    })
                )
                {
                    myID = 1022,
                    downNeighborID = 1032,
                    leftNeighborID = 1003,
                    upNeighborID = 1034,
                    rightNeighborID = 1023
                }
            );
            this.points.Add(
                new ClickableComponent(
                    getRegionRect("Tent"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11097", new object[0])
                )
                {
                    myID = 1023,
                    leftNeighborID = 1034,
                    downNeighborID = 1022,
                    rightNeighborID = 1024
                }
            );
            this.points.Add(
                new ClickableComponent(
                    getRegionRect("Mine"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11098", new object[0])
                )
                {
                    myID = 1024,
                    leftNeighborID = 1023,
                    rightNeighborID = 1025,
                    downNeighborID = 1027
                }
            );
            this.points.Add(
                new ClickableComponent(
                    getRegionRect("AdventureGuild"),
                    (Game1.stats.DaysPlayed >= 5u) ? (Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11099", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11100", new object[0])) : "???"
                )
                {
                    myID = 1025,
                    leftNeighborID = 1024,
                    rightNeighborID = 1026,
                    downNeighborID = 1027
                }
            );
            this.points.Add(
                new ClickableComponent(
                    getRegionRect("Quarry"),
                    Game1.player.mailReceived.Contains("ccCraftsRoom") ? Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11103", new object[0]) : "???"
                )
                {
                    myID = 1026,
                    leftNeighborID = 1025,
                    downNeighborID = 1027
                }
            );
            this.points.Add(
                new ClickableComponent(
                    getRegionRect("JojaMart"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11105", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11106", new object[0])
                )
                {
                    myID = 1027,
                    upNeighborID = 1025,
                    leftNeighborID = 1021,
                    downNeighborID = 1013
                }
            );
            this.points.Add(
                new ClickableComponent(
                    getRegionRect("FishShop"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11107", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11108", new object[0])
                )
                {
                    myID = 1028,
                    upNeighborID = 1017,
                    rightNeighborID = 99989
                }
            );
            this.points.Add(
                new ClickableComponent(
                    getRegionRect("Spa"),
                    Game1.isLocationAccessible("Railroad") ? (Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11110", new object[0]) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11111", new object[0])) : "???"
                )
                {
                    myID = 1029,
                    rightNeighborID = 1034,
                    downNeighborID = 1003,
                    leftNeighborID = 1001
                }
            );
            this.points.Add(
                new ClickableComponent(
                    getRegionRect("Woods"),
                    Game1.player.mailReceived.Contains("beenToWoods") ? Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11114", new object[0]) : "???"
                )
                {
                    myID = 1030,
                    upNeighborID = 1001,
                    rightNeighborID = 1005
                }
            );
            this.points.Add(
                new ClickableComponent(
                    getRegionRect("RuinedHouse"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11116", new object[0])
                )
                {
                    myID = 1031,
                    rightNeighborID = 1033,
                    upNeighborID = 1005
                }
            );
            this.points.Add(
                new ClickableComponent(
                    getRegionRect("CommunityCenter"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11117", new object[0])
                )
                {
                    myID = 1032,
                    downNeighborID = 1012,
                    upNeighborID = 1022,
                    leftNeighborID = 1004
                }
            );
            this.points.Add(
                new ClickableComponent(
                    getRegionRect("SewerPipe"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11118", new object[0])
                )
                {
                    myID = 1033,
                    leftNeighborID = 1031,
                    rightNeighborID = 1017,
                    upNeighborID = 1007
                }
            );
            this.points.Add(
                new ClickableComponent(
                    getRegionRect("Railroad_Region"),
                    Game1.isLocationAccessible("Railroad") ? Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11119", new object[0]) : "???"
                )
                {
                    myID = 1034,
                    leftNeighborID = 1029,
                    rightNeighborID = 1023,
                    downNeighborID = 1022
                }
            );
            this.points.Add(
                new ClickableComponent(
                    getRegionRect("LonelyStone"),
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11122", new object[0])
                )
            );

            // Remove vanilla map tooltips
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
                int width = Math.Max((int)Game1.smallFont.MeasureString(hoverText).X + Game1.tileSize / 2, textLength);
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
                // Name tooltip
                DrawNPCNames(Game1.spriteBatch, names, x, y, offsetY, height, nameTooltipMode);

                // Location tooltips
                IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, y, width, height, Color.White, 1f, false);
                b.DrawString(Game1.smallFont, hoverText, new Vector2((float)(x + Game1.tileSize / 4), (float)(y + Game1.tileSize / 4 + 4)) + new Vector2(2f, 2f), Game1.textShadowColor);
                b.DrawString(Game1.smallFont, hoverText, new Vector2((float)(x + Game1.tileSize / 4), (float)(y + Game1.tileSize / 4 + 4)) + new Vector2(0f, 2f), Game1.textShadowColor);
                b.DrawString(Game1.smallFont, hoverText, new Vector2((float)(x + Game1.tileSize / 4), (float)(y + Game1.tileSize / 4 + 4)) + new Vector2(2f, 0f), Game1.textShadowColor);
                b.DrawString(Game1.smallFont, hoverText, new Vector2((float)(x + Game1.tileSize / 4), (float)(y + Game1.tileSize / 4 + 4)), Game1.textColor * 0.9f);
            }
            else
            {
                DrawNPCNames(Game1.spriteBatch, names, x, y, offsetY, height, nameTooltipMode);
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
                if (names.StartsWith("^"))
                {
                    names = names.Substring(1);
                }

                string[] nameStrs = names.Split(',');
                int iconWidths = 0;

                if (names.Contains("#")) 
                    iconWidths += GIFT_WIDTH;
                if (names.Contains("!"))
                    iconWidths += QUEST_WIDTH;
                if (names.Contains("_"))
                    iconWidths += SPACE_WIDTH;

                names = names.Replace("#", "").Replace("!", "").Replace("_", "").Replace(",", " ").Replace("，", "");
                var lines = names.Split('\n');
                int height = (int)Math.Max(60, Game1.smallFont.MeasureString(names).Y + Game1.tileSize/2);
                int width = (int)Game1.smallFont.MeasureString(names).X + Game1.tileSize/2 + 4 + iconWidths;

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
                    if (x + width > Game1.viewport.Width)
                    {
                        x = Game1.viewport.Width - width;
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
                    if (x + width > Game1.viewport.Width)
                    {
                        x = Game1.viewport.Width - width;
                    }
                    // If going off screen on the bottom, move tooltip to above location tooltip so it stays visible
                    if (y + height > Game1.viewport.Height)
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
                    y = Game1.activeClickableMenu.yPositionOnScreen + 650 - height / 2;
                }

                indoorIconVector = new Vector2(x - Game1.tileSize / 8 + 2, y - Game1.tileSize / 8 + 2);
                Vector2 vector = new Vector2(x + (float)(Game1.tileSize / 4), y + (float)(Game1.tileSize / 4 + 4));

                drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, y, width, height, Color.White, 1f, true);
                b.DrawString(Game1.smallFont, names, vector + new Vector2(2f, 2f), Game1.textShadowColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                b.DrawString(Game1.smallFont, names, vector + new Vector2(0f, 2f), Game1.textShadowColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                b.DrawString(Game1.smallFont, names, vector + new Vector2(2f, 0f), Game1.textShadowColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                b.DrawString(Game1.smallFont, names, vector, Game1.textColor * 0.9f, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

                drawIcons(b, nameStrs, vector);
            }
        }

        // Convert magic string into icons
        private void drawIcons(SpriteBatch b, string[] nameStrs, Vector2 vector)
        {
            // Draw icons next to names
            string namesLen = "";

            foreach (string nameStr in nameStrs)
            {
                // Replace magic string forcefully
                string name = nameStr.Replace("#", "").Replace("!", "").Replace("_", "").Replace(",", "").Replace("，","").Trim();
                namesLen += name; // Cumulatively add name lengths, commas, icons, spaces, etc.

                if (nameStr.Contains("!") && nameStr.Contains("#"))
                {
                    b.Draw(Game1.mouseCursors, new Vector2(vector.X + (int)Game1.smallFont.MeasureString(namesLen).X + 5, vector.Y + 2), new Rectangle?(new Rectangle(147, 412, 10, 11)), Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);
                    b.Draw(Game1.mouseCursors, new Vector2(vector.X + (int)Game1.smallFont.MeasureString(namesLen).X + 5, vector.Y), new Rectangle?(new Rectangle(403, 496, 5, 14)), Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);
                    namesLen += GIFT_WIDTH;
                }
                else
                {
                    if (nameStr.Contains("!"))
                    {
                        b.Draw(Game1.mouseCursors, new Vector2(vector.X + (int)Game1.smallFont.MeasureString(namesLen).X + 5, vector.Y), new Rectangle?(new Rectangle(403, 496, 5, 14)), Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);
                        namesLen += QUEST_WIDTH;
                    }
                    if (nameStr.Contains("#"))
                    {
                        b.Draw(Game1.mouseCursors, new Vector2(vector.X + (int)Game1.smallFont.MeasureString(namesLen).X + 5, vector.Y + 2), new Rectangle?(new Rectangle(147, 412, 10, 11)), Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);
                        namesLen += GIFT_WIDTH;
                    }
                }

                // Draw commas separately after name and icons... This is insanity
                // Whitespace would ideally replace magic strings to displace commas
                // but not all languages use whitespace so have to do this hacky way
                if (nameStrs.Length > 1)
                {
                    b.DrawString(Game1.smallFont, ",", new Vector2(vector.X + (int)Game1.smallFont.MeasureString(namesLen).X + 2f, vector.Y + 2f), Game1.textShadowColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                    b.DrawString(Game1.smallFont, ",", new Vector2(vector.X + (int)Game1.smallFont.MeasureString(namesLen).X, vector.Y + 2f), Game1.textShadowColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                    b.DrawString(Game1.smallFont, ",", new Vector2(vector.X + (int)Game1.smallFont.MeasureString(namesLen).X + 2f, vector.Y), Game1.textShadowColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                    b.DrawString(Game1.smallFont, ",", new Vector2(vector.X + (int)Game1.smallFont.MeasureString(namesLen).X, vector.Y), Game1.textColor * 0.9f, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                    namesLen += SPACE_WIDTH * 2; // Width of ", "
                }
            }
        }

        private Rectangle getRegionRect(string region)
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

