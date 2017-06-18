/*
NPC Map Locations Mod by Bouhm.
Shows NPC locations real-time on the map.
*/

using StardewValley;
using StardewValley.Quests;
using StardewModdingAPI;
using StardewValley.Menus;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley.Buildings;

namespace NPCMapLocations
{
    public class MapModMain : Mod
    {
        public static ISemanticVersion current = new SemanticVersion("1.4.7");
        public static ISemanticVersion latest = new SemanticVersion("1.4.7");
        public static IModHelper modHelper;
        public static MapModConfig config;
        // public static string saveFile;
        public static int customNpcId = 0;
        public static int menuOpen = 0;
        private static Dictionary<string, Dictionary<string, int>> customNPCs;
        private static Dictionary<string, NPCMarker> npcMarkers = new Dictionary<string, NPCMarker>();
        // NPC head crops, top left corner (0, y), width = 16, height = 15 
        public static Dictionary<string, int> spriteCrop;
        private static Dictionary<string, string> startingLocations;
        private static Dictionary<string, MapVectors[]> mapVectors;
        private static Dictionary<string, string> indoorLocations;
        private static MapPageTooltips toolTips;
        private static string hoveredNPCNames;
        private static HashSet<string> birthdayNPCs;
        private static HashSet<string> questNPCs;
        private static HashSet<string> hiddenNPCs;
        private static Dictionary<string, string> npcNames = new Dictionary<string, string>();
        private bool[] showExtras = new Boolean[4];
        private bool loadComplete = false;
        private bool initialized = false;

        public override void Entry(IModHelper helper)
        {
            modHelper = helper;
            config = helper.ReadConfig<MapModConfig>();
            SaveEvents.AfterLoad += SaveEvents_AfterLoad;
            GameEvents.UpdateTick += GameEvents_UpdateTick;
            GraphicsEvents.OnPostRenderGuiEvent += GraphicsEvents_OnPostRenderGuiEvent;
            ControlEvents.KeyPressed += KeyboardInput_KeyDown;
        }

        private void SaveEvents_AfterLoad(object sender, EventArgs e)
        {
            // Task.Run(() => MapModVersionChecker.getNotification()).GetAwaiter().GetResult();
            // saveFile = Game1.player.name.Replace(" ", String.Empty) + "_" + Game1.uniqueIDForThisGame;
            spriteCrop = MapModConstants.spriteCrop;
            startingLocations = MapModConstants.startingLocations;
            mapVectors = MapModConstants.mapVectors;
            indoorLocations = MapModConstants.indoorLocations;
            customNPCs = config.customNPCs;
            loadComplete = true;
        }

        private void loadCustomMods()
        {
            var initializeCustomNPCs = 1;
            if (customNPCs != null && customNPCs.Count != 0)
            {
                initializeCustomNPCs = 0;
            }
            int id = 1;
            foreach (NPC npc in Utility.getAllCharacters())
            {
                id = loadCustomNPCs(npc, initializeCustomNPCs, id);
                loadNPCCrop(npc);
                loadCustomNames(npc);
            }
            config.customNPCs = customNPCs;
            modHelper.WriteConfig(config);
            initialized = true;
        }

        private int loadCustomNPCs(NPC npc, int initialize, int id)
        {
            if (initialize == 0)
            {
                int idx = 1;
                // Update save files for custom NPC installed or uninstalled (pseudo-persave config)
                foreach (KeyValuePair<string, Dictionary<string, int>> customNPC in customNPCs)
                {
                    // isInGame = 0;
                    if (npc.name.Equals(customNPC.Key))
                    {
                        // isInGame = 1;
                        customNpcId = idx;
                    }

                    /*
                    // Pseudo-persave config for custom NPC (since custom NPCs have to be installed to each save file)
                    // Works too unreliably; remove for now;
                    if (!customNPC.Value.ContainsKey(saveFile))
                    {
                        customNPC.Value.Add(saveFile, isInGame);
                    }
                    else
                    {
                        customNPC.Value[saveFile] = isInGame;
                    }
                    */
                    if (!customNPC.Value.ContainsKey("crop"))
                    {
                        customNPC.Value.Add("crop", 0);
                    }
                    if (!spriteCrop.ContainsKey(customNPC.Key))
                    {
                        spriteCrop.Add(customNPC.Key, customNPC.Value["crop"]);
                    }
                    idx++;
                }
            } 
            else
            {
                if (npc.Schedule != null && isCustomNPC(npc.name))
                {
                    if (!customNPCs.ContainsKey(npc.name))
                    {
                        var npcEntry = new Dictionary<string, int>();
                        npcEntry.Add("id", id);
                        npcEntry.Add("crop", 0);
                        /*
                        if (npc != null)
                        {
                            npcEntry.Add(saveFile, 1);
                        }
                        else
                        {
                            npcEntry.Add(saveFile, 0);
                        }
                        */
                        customNPCs.Add(npc.name, npcEntry);
                        spriteCrop.Add(npc.name, 0);
                        id++;
                    }
                }
            }
            return id;
        }

        private void loadCustomNames(NPC npc)
        {
            if (!npcNames.ContainsKey(npc.name))
            {
                var customName = npc.getName();
                if (string.IsNullOrEmpty(customName))
                {
                    customName = npc.name;
                }
                npcNames.Add(npc.name, customName);
            }
        }

        private void loadNPCCrop(NPC npc)
        {
            if (config.villagerCrop != null && config.villagerCrop.Count > 0)
            {
                foreach (KeyValuePair<string, int> villager in config.villagerCrop)
                {
                    if (npc.name.Equals(villager.Key))
                    {
                        spriteCrop[npc.name] = villager.Value;
                    }
                }
            }
        }

        // Open menu key
        private void KeyboardInput_KeyDown(object sender, EventArgsKeyPressed e)
        {
            if (Game1.hasLoadedGame && Game1.activeClickableMenu is GameMenu)
            {
                changeKey(e.KeyPressed.ToString(), (GameMenu)Game1.activeClickableMenu);
            }
        }

        private void changeKey(string key, GameMenu menu)
        {
            if (menu.currentTab != 3) { return; }
            if (key.Equals(config.menuKey))
            {
                Game1.activeClickableMenu = new MapModMenu(Game1.viewport.Width / 2 - (950 + IClickableMenu.borderWidth * 2) / 2, Game1.viewport.Height / 2 - (750 + IClickableMenu.borderWidth * 2) / 2, 900 + IClickableMenu.borderWidth * 2, 650 + IClickableMenu.borderWidth * 2, showExtras, customNPCs, npcNames);
                menuOpen = 1;
            }
        }

        public static Vector2 locationToMap(string location, int tileX, int tileY)
        {
            if (location == null)
            {
                return new Vector2(-5000, -5000);
            }

            Vector2 mapVector = Utility.getTopLeftPositionForCenteringOnScreen(300 * Game1.pixelZoom, 180 * Game1.pixelZoom, 0, 0);
            int mapX = (int)mapVector.X;
            int mapY = (int)mapVector.Y;
            int x = 0;
            int y = 0;

            // Get tile location of farm buildings in farm
            string[] farmBuildings = { "Coop", "Big Coop", "Deluxe Coop", "Barn", "Big Barn", "Deluxe Barn", "Slime Hutch", "Shed" };
            if (farmBuildings.Contains(location))
            {
                foreach (Building building in Game1.getFarm().buildings)
                {
                    if (building.indoors != null && building.indoors.name.Equals(location))
                    {
                        tileX = building.tileX;
                        tileY = building.tileY;
                        location = "Farm";
                    }
                }
            }
            // Sort map vectors by distance to point
            var vectors = mapVectors[location].OrderBy(vector => Math.Sqrt(Math.Pow(vector.tileX - tileX, 2) + Math.Pow(vector.tileY - tileY, 2)));

            if (vectors.Count() == 1)
            {
                x = vectors.FirstOrDefault().x;
                y = vectors.FirstOrDefault().y;
            }
            else
            {
                MapVectors lower = null;
                MapVectors upper = null;

                // Create bounding rectangle from two pre-defined points (lower & upper bound) and calculate map scale for that area
                foreach (MapVectors vector in vectors)
                {
                    if (tileX == vector.tileX && tileY == vector.tileY)
                    {
                        return new Vector2(mapX + vector.x - 16, mapY + vector.y - 15);
                    }
                    else
                    {
                        if (lower != null && upper != null)
                        {
                            break;
                        }
                        if ((lower == null || (upper != null && (vector.tileX != upper.tileX && vector.tileY != upper.tileY))) && 
                            (tileX >= vector.tileX && tileY >= vector.tileY))
                        {
                            lower = vector;
                            continue;
                        }
                        if ((upper == null || (lower != null && (vector.tileX != lower.tileX && vector.tileY != lower.tileY))) &&
                           (tileX <= vector.tileX && tileY <= vector.tileY))
                        {
                            upper = vector;
                        }
                    }
                }

                int tileXMin = Math.Min(lower.tileX, upper.tileX);
                int tileXMax = Math.Max(lower.tileX, upper.tileX);
                int tileYMin = Math.Min(lower.tileY, upper.tileY);
                int tileYMax = Math.Max(lower.tileY, upper.tileY);
                int xMin = Math.Min(lower.x, upper.x);
                int xMax = Math.Max(lower.x, upper.x);
                int yMin = Math.Min(lower.y, upper.y);
                int yMax = Math.Max(lower.y, upper.y);
                //Log.Verbose(lower.tileX + ", " + lower.tileY + ", " + upper.tileX + ", " + upper.tileY);
                x = (int)(xMin + (double)(tileX - tileXMin) / (double)(tileXMax - tileXMin) * (xMax - xMin));
                y = (int)(yMin + (double)(tileY - tileYMin) / (double)(tileYMax - tileYMin) * (yMax - yMin));
            }
            return new Vector2(mapX + x - 16, mapY + y - 15);
        }

        private void GameEvents_UpdateTick(object sender, EventArgs e)
        {
            if (!Game1.hasLoadedGame) { return; }
            if (loadComplete && !initialized)
            {
                loadCustomMods();
            }
            if (!(Game1.activeClickableMenu is GameMenu)) { return; }

            List<string> hoveredList = new List<String>();
            birthdayNPCs = new HashSet<string>();
            questNPCs = new HashSet<string>();
            hiddenNPCs = new HashSet<string>();
            hoveredNPCNames = "";

            foreach (NPC npc in Utility.getAllCharacters())
            {
                if (npc.Schedule != null || npc.isMarried() || npc.name.Equals("Sandy") || npc.name.Equals("Marlon") || npc.name.Equals("Wizard"))
                {
                    bool sameLocation = false;
                    showExtras[0] = Game1.player.mailReceived.Contains("ccVault");
                    showExtras[1] = Game1.stats.DaysPlayed >= 5u;
                    showExtras[2] = Game1.stats.DaysPlayed >= 5u;
                    showExtras[3] = Game1.year >= 2;

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

                    if ((config.immersionOption == 2 && !Game1.player.hasTalkedToFriendToday(npc.name)) ||
                        (config.immersionOption == 3 && Game1.player.hasTalkedToFriendToday(npc.name)) ||
                        (config.onlySameLocation && !sameLocation) ||
                        (config.byHeartLevel && !(Game1.player.getFriendshipHeartLevelForNPC(npc.name) >= config.heartLevelMin && Game1.player.getFriendshipHeartLevelForNPC(npc.name) <= config.heartLevelMax)))
                    {
                        hiddenNPCs.Add(npc.name);
                    }


                    if (config.showHiddenVillagers ? showNPC(npc.name, showExtras) : (!hiddenNPCs.Contains(npc.name) && showNPC(npc.name, showExtras)))
                    {
                        int x = (int)locationToMap(npc.currentLocation.name, npc.getTileX(), npc.getTileY()).X;
                        int y = (int)locationToMap(npc.currentLocation.name, npc.getTileX(), npc.getTileY()).Y;
                        int width = 32;
                        int height = 30;

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
                            if (npcNames.ContainsKey(npc.name))
                            {
                                hoveredList.Add(npcNames[npc.name]);
                            }
                        }

                        if (config.markQuests)
                        {
                            if (npc.isBirthday(Game1.currentSeason, Game1.dayOfMonth))
                            {
                                if (Game1.player.friendships.ContainsKey(npc.name) && Game1.player.friendships[npc.name][3] != 1)
                                {
                                    birthdayNPCs.Add(npc.name);
                                }
                                else
                                {
                                    birthdayNPCs.Add(npc.name);
                                }
                            }
                            foreach (Quest quest in Game1.player.questLog)
                            {
                                if (quest.accepted && quest.dailyQuest && !quest.completed)
                                {
                                    if (quest.questType == 3)
                                    {
                                        var current = (ItemDeliveryQuest)quest;
                                        if (current.target == npc.name)
                                        {
                                            questNPCs.Add(npc.name);
                                        }
                                    }
                                    else if (quest.questType == 4)
                                    {
                                        var current = (SlayMonsterQuest)quest;
                                        if (current.target == npc.name)
                                        {
                                            questNPCs.Add(npc.name);
                                        }
                                    }
                                    else if (quest.questType == 7)
                                    {
                                        var current = (FishingQuest)quest;
                                        if (current.target == npc.name)
                                        {
                                            questNPCs.Add(npc.name);
                                        }
                                    }
                                    else if (quest.questType == 10)
                                    {
                                        var current = (ResourceCollectionQuest)quest;
                                        if (current.target == npc.name)
                                        {
                                            questNPCs.Add(npc.name);
                                        }
                                    }
                                }
                            }
                        }
                        // Draw order
                        if (hiddenNPCs.Contains(npc.name))
                        {
                            npcMarkers[npc.name].layer = 4;
                            if (questNPCs.Contains(npc.name) || (birthdayNPCs.Contains(npc.name)))
                            {
                                npcMarkers[npc.name].layer = 3;
                            }
                        }
                        else
                        {
                            npcMarkers[npc.name].layer = 2;
                            if (questNPCs.Contains(npc.name) || (birthdayNPCs.Contains(npc.name)))
                            {
                                npcMarkers[npc.name].layer = 1;
                            }
                        }
                    }
                    else
                    {
                        npcMarkers.Remove(npc.name);
                    }
                }
            }

            if (hoveredList != null && hoveredList.Count > 0)
            {
                hoveredNPCNames = hoveredList[0];
                for (int i = 1; i < hoveredList.Count; i++)
                {
                    var lines = hoveredNPCNames.Split('\n');
                    if ((int)Game1.smallFont.MeasureString(lines[lines.Length - 1] + ", " + hoveredList[i]).X > (int)Game1.smallFont.MeasureString("Home of Robin, Demetrius, Sebastian & Maru").X)
                    {
                        hoveredNPCNames += ", " + Environment.NewLine;
                        hoveredNPCNames += hoveredList[i];
                    }
                    else
                    {
                        hoveredNPCNames += ", " + hoveredList[i];
                    }
                };
            }
            toolTips = new MapPageTooltips(hoveredNPCNames, npcNames, config.nameTooltipMode);
        }

        // Draw event (when Map Page is opened)
        private void GraphicsEvents_OnPostRenderGuiEvent(object sender, EventArgs e)
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
                // NPC markers and icons
                if (config.showTravelingMerchant && (Game1.dayOfMonth == 5 || Game1.dayOfMonth == 7 || Game1.dayOfMonth == 12 || Game1.dayOfMonth == 14 || Game1.dayOfMonth == 19 || Game1.dayOfMonth == 21 || Game1.dayOfMonth == 26 || Game1.dayOfMonth == 28))
                {
                    Game1.spriteBatch.Draw(Game1.mouseCursors, new Vector2(Game1.activeClickableMenu.xPositionOnScreen + 130, Game1.activeClickableMenu.yPositionOnScreen + 355), new Rectangle?(new Rectangle(191, 1410, 22, 21)), Color.White, 0f, Vector2.Zero, 1.3f, SpriteEffects.None, 1f);
                }
                var sortedMarkers = npcMarkers.ToList();
                sortedMarkers.Sort((y, x) => x.Value.layer.CompareTo(y.Value.layer));
              
                foreach (KeyValuePair<string, NPCMarker> npc in sortedMarkers)
                {
                    if (hiddenNPCs.Contains(npc.Key)) {
                        Game1.spriteBatch.Draw(npc.Value.marker, npc.Value.position, new Rectangle?(new Rectangle(0, spriteCrop[npc.Key], 16, 15)), Color.DimGray * 0.9f);
                        if (birthdayNPCs.Contains(npc.Key))
                        {
                            Game1.spriteBatch.Draw(Game1.mouseCursors, new Vector2(npc.Value.position.X + 20, npc.Value.position.Y), new Rectangle?(new Rectangle(147, 412, 10, 11)), Color.DimGray * 0.9f, 0f, Vector2.Zero, 1.8f, SpriteEffects.None, 0f);
                        }
                        if (questNPCs.Contains(npc.Key))
                        {
                            Game1.spriteBatch.Draw(Game1.mouseCursors, new Vector2(npc.Value.position.X + 22, npc.Value.position.Y - 3), new Rectangle?(new Rectangle(403, 496, 5, 14)), Color.DimGray * 0.9f, 0f, Vector2.Zero, 1.8f, SpriteEffects.None, 0f);
                        }

                    }
                    else
                    {
                        Game1.spriteBatch.Draw(npc.Value.marker, npc.Value.position, new Rectangle?(new Rectangle(0, spriteCrop[npc.Key], 16, 15)), Color.White * 0.9f);
                        if (birthdayNPCs.Contains(npc.Key))
                        {
                            Game1.spriteBatch.Draw(Game1.mouseCursors, new Vector2(npc.Value.position.X + 20, npc.Value.position.Y), new Rectangle?(new Rectangle(147, 412, 10, 11)), Color.White, 0f, Vector2.Zero, 1.8f, SpriteEffects.None, 0f);
                        }
                        if (questNPCs.Contains(npc.Key))
                        {
                            Game1.spriteBatch.Draw(Game1.mouseCursors, new Vector2(npc.Value.position.X + 22, npc.Value.position.Y - 3), new Rectangle?(new Rectangle(403, 496, 5, 14)), Color.White, 0f, Vector2.Zero, 1.8f, SpriteEffects.None, 0f);
                        }

                    }
                }
                // Location and name tooltips
                toolTips.draw(Game1.spriteBatch);

                // Cursor
                if (!Game1.options.hardwareCursor)
                {
                    Game1.spriteBatch.Draw(Game1.mouseCursors, new Vector2((float)Game1.getOldMouseX(), (float)Game1.getOldMouseY()), new Rectangle?(Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, (Game1.options.gamepadControls ? 44 : 0), 16, 16)), Color.White, 0f, Vector2.Zero, ((float)Game1.pixelZoom + Game1.dialogueButtonScale / 150f), SpriteEffects.None, 1f);
                }
            }
        }

        // Config show/hide 
        private static bool showNPC(string npc, bool[] showExtras)
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
            if (npc.Equals("Sandy")) { return config.showSandy && showExtras[0]; }
            if (npc.Equals("Marlon")) { return config.showMarlon && showExtras[1]; }
            if (npc.Equals("Wizard")) { return config.showWizard && showExtras[2]; }
            foreach (KeyValuePair<string, Dictionary<string, int>> customNPC in customNPCs)
            {
                if (customNPC.Key.Equals(npc))
                {
                    switch (customNPC.Value["id"])
                    {
                        case 1:
                            return config.showCustomNPC1;
                        case 2:
                            return config.showCustomNPC2;
                        case 3:
                            return config.showCustomNPC3;
                        case 4:
                            return config.showCustomNPC4;
                        case 5:
                            return config.showCustomNPC5;
                    }
                }
            }
            return true;
        }

        // Only checks against existing villager names
        public static bool isCustomNPC(string npc)
        {
            return (!(
                npc.Equals("Abigail") ||
                npc.Equals("Alex") ||
                npc.Equals("Caroline") ||
                npc.Equals("Clint") ||
                npc.Equals("Demetrius") ||
                npc.Equals("Elliott") ||
                npc.Equals("Emily") ||
                npc.Equals("Evelyn") ||
                npc.Equals("George") ||
                npc.Equals("Gus") ||
                npc.Equals("Haley") ||
                npc.Equals("Harvey") ||
                npc.Equals("Jas") ||
                npc.Equals("Jodi") ||
                npc.Equals("Kent") ||
                npc.Equals("Leah") ||
                npc.Equals("Lewis") ||
                npc.Equals("Linus") ||
                npc.Equals("Marnie") ||
                npc.Equals("Maru") ||
                npc.Equals("Pam") ||
                npc.Equals("Penny") ||
                npc.Equals("Pierre") ||
                npc.Equals("Robin") ||
                npc.Equals("Sam") ||
                npc.Equals("Sebastian") ||
                npc.Equals("Shane") ||
                npc.Equals("Vincent") ||
                npc.Equals("Willy") ||
                npc.Equals("Sandy") ||
                npc.Equals("Marlon") ||
                npc.Equals("Wizard"))
            );
        }
    }

    // Class for NPC markers
    public class NPCMarker
    {
        public Texture2D marker;
        public Rectangle position;
        public int layer;

        public NPCMarker(Texture2D marker, Rectangle position)
        {
            this.marker = marker;
            this.position = position;
            this.layer = 4;
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
        private Dictionary<string, string> npcNames;
        private int nameTooltipMode;

        public MapPageTooltips(string names, Dictionary<string, string> npcNames, int nameTooltipMode)
        {
            this.nameTooltipMode = nameTooltipMode;
            this.names = names;
            this.npcNames = npcNames;
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

        // Draw location tooltips
        public override void draw(SpriteBatch b)
        {
            int x = Game1.getMouseX() + Game1.tileSize / 2;
            int y = Game1.getMouseY() + Game1.tileSize / 2;
            int offsetY = 0;
            this.performHoverAction(x - Game1.tileSize / 2, y - Game1.tileSize / 2);

            if (!this.hoverText.Equals(""))
            {
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

                drawNPCNames(Game1.spriteBatch, this.names, x, y, offsetY, height, this.nameTooltipMode);
                IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, y, width, height, Color.White, 1f, false);
                b.DrawString(Game1.smallFont, hoverText, new Vector2((float)(x + Game1.tileSize / 4), (float)(y + Game1.tileSize / 4 + 4)) + new Vector2(2f, 2f), Game1.textShadowColor);
                b.DrawString(Game1.smallFont, hoverText, new Vector2((float)(x + Game1.tileSize / 4), (float)(y + Game1.tileSize / 4 + 4)) + new Vector2(0f, 2f), Game1.textShadowColor);
                b.DrawString(Game1.smallFont, hoverText, new Vector2((float)(x + Game1.tileSize / 4), (float)(y + Game1.tileSize / 4 + 4)) + new Vector2(2f, 0f), Game1.textShadowColor);
                b.DrawString(Game1.smallFont, hoverText, new Vector2((float)(x + Game1.tileSize / 4), (float)(y + Game1.tileSize / 4 + 4)), Game1.textColor * 0.9f);
            }
            else
            {
                drawNPCNames(Game1.spriteBatch, this.names, x, y, offsetY, height, this.nameTooltipMode);
            }
        }

        // Draw NPC names in bottom left corner of map page
        public void drawNPCNames(SpriteBatch b, string names, int x, int y, int offsetY, int relocate, int nameTooltipMode)
        {
            // Log.Verbose(Game1.player.currentLocation.name + ", " + Game1.player.getTileX() + ", " + Game1.player.getTileY());
            // b.Draw(this.map, new Vector2((float)this.mapX, (float)this.mapY), new Rectangle?(new Rectangle(0, 0, 300, 180)), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.86f);
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
            //Vector2 playerLoc = MapModMain.locationToMap(Game1.player.currentLocation.name, Game1.player.getTileX(), Game1.player.getTileY());
            //Game1.player.FarmerRenderer.drawMiniPortrat(b, new Vector2(playerLoc.X - 3, playerLoc.Y - 10), 0.00011f, 2f, 1, Game1.player);

            if (!(names.Equals("")))
            {
                var lines = names.Split('\n');
                int height = (int)Math.Max(60, Game1.smallFont.MeasureString(names).Y + Game1.tileSize / 2);
                int width = (int)Game1.smallFont.MeasureString(names).X + Game1.tileSize / 2;

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
                    y = Game1.activeClickableMenu.yPositionOnScreen + 625 - height / 2;
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
