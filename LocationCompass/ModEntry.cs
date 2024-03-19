using System;
using System.Collections.Generic;
using System.Linq;
using Bouhm.Shared;
using Bouhm.Shared.Locations;
using LocationCompass.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Quests;

namespace LocationCompass
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        /*********
        ** Fields
        *********/
        private readonly int MaxProximity = 4800;

        private readonly bool DebugMode = false;
        private Texture2D Pointer;
        private ModData Constants;
        private List<Character> Characters;
        private SyncedNpcLocationData SyncedLocationData;
        private Dictionary<string, List<Locator>> Locators;
        private Dictionary<string, LocatorScroller> ActiveWarpLocators; // Active indices of locators of doors
        private ModConfig Config;

        /// <summary>Whether locators are visible. (This should be set via <see cref="SetShowLocators"/> to toggle the HUD if needed.)</summary>
        private bool ShowLocators;

        /// <summary>Scans and maps locations in the game world.</summary>
        private LocationUtil LocationUtil;


        /*********
        ** Public methods
        *********/
        public override void Entry(IModHelper helper)
        {
            CommonHelper.RemoveObsoleteFiles(this, "LocationCompass.pdb");

            this.Config = helper.ReadConfig<ModConfig>();
            this.Pointer = helper.ModContent.Load<Texture2D>("assets/locator.png"); // Load pointer tex
            this.Constants = helper.Data.ReadJsonFile<ModData>("assets/constants.json") ?? new ModData();
            this.LocationUtil = new(this.Monitor);

            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.World.LocationListChanged += this.OnLocationListChanged;
            helper.Events.Multiplayer.ModMessageReceived += this.OnModMessageReceived;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.Input.ButtonReleased += this.OnButtonReleased;
            helper.Events.Display.Rendered += this.OnRendered;
        }


        /*********
        ** Private methods
        *********/
        /// <inheritdoc cref="IGameLoopEvents.DayStarted"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            this.Characters = new List<Character>();

            foreach (var npc in this.GetVillagers())
                this.Characters.Add(npc);

            this.UpdateLocators();
        }

        /// <inheritdoc cref="IInputEvents.ButtonPressed"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            // Handle toggle
            if (e.Button.ToString().Equals(this.Config.ToggleKeyCode) && Context.IsPlayerFree)
            {
                if (this.Config.HoldToToggle)
                    this.SetShowLocators(true);
                else
                    this.SetShowLocators(!this.ShowLocators);
            }

            // Configs
            if (this.ActiveWarpLocators != null)
            {
                // Handle scroll click
                if (e.Button.Equals(SButton.MouseRight) || e.Button.Equals(SButton.ControllerA))
                    foreach (var doorLocator in this.ActiveWarpLocators)
                    {
                        if (doorLocator.Value.Characters.Count > 1)
                            doorLocator.Value.ReceiveLeftClick();
                    }

                if (this.ShowLocators)
                {
                    if (e.Button.ToString() == this.Config.SameLocationToggleKey)
                        this.Config.SameLocationOnly = !this.Config.SameLocationOnly;
                    else if (e.Button.ToString() == this.Config.FarmersOnlyToggleKey)
                        this.Config.ShowFarmersOnly = !this.Config.ShowFarmersOnly;
                    else if (e.Button.ToString() == this.Config.QuestsOnlyToggleKey)
                        this.Config.ShowQuestsAndBirthdaysOnly = !this.Config.ShowQuestsAndBirthdaysOnly;
                    else if (e.Button.ToString() == this.Config.HorsesToggleKey)
                        this.Config.ShowHorses = !this.Config.ShowHorses;

                    this.Helper.Data.WriteJsonFile("config.json", this.Config);
                }
            }
        }

        /// <inheritdoc cref="IWorldEvents.LocationListChanged"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnLocationListChanged(object sender, LocationListChangedEventArgs e)
        {
            this.LocationUtil.ScanLocationContexts();
        }

        /// <inheritdoc cref="IGameLoopEvents.SaveLoaded"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            this.ActiveWarpLocators = new Dictionary<string, LocatorScroller>();
            this.SyncedLocationData = new SyncedNpcLocationData();
            this.LocationUtil.ScanLocationContexts();

            // Log warning if host does not have mod installed
            if (Context.IsMultiplayer)
            {
                bool hostHasMod = false;

                foreach (IMultiplayerPeer peer in this.Helper.Multiplayer.GetConnectedPlayers())
                {
                    if (peer.GetMod("Bouhm.LocationCompass") != null && peer.IsHost)
                    {
                        hostHasMod = true;
                        break;
                    }
                }

                if (!hostHasMod)
                    this.Monitor.Log("Since the server host does not have LocationCompass installed, NPC locations cannot be synced and updated.", LogLevel.Warn);
            }
        }

        /// <inheritdoc cref="IMultiplayerEvents.ModMessageReceived"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnModMessageReceived(object sender, ModMessageReceivedEventArgs e)
        {
            if (e.FromModID == this.ModManifest.UniqueID && e.Type == "SyncedLocationData")
                this.SyncedLocationData = e.ReadAs<SyncedNpcLocationData>();
        }

        /// <inheritdoc cref="IInputEvents.ButtonReleased"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonReleased(object sender, ButtonReleasedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            if (this.Config.HoldToToggle && e.Button.ToString().Equals(this.Config.ToggleKeyCode) && Context.IsPlayerFree)
                this.SetShowLocators(false);
        }

        /// <inheritdoc cref="IGameLoopEvents.UpdateTicked"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            // Quarter-second tick
            if (e.IsMultipleOf(15))
            {
                if (this.Characters != null && Context.IsMultiplayer)
                {
                    foreach (var farmer in Game1.getOnlineFarmers())
                    {
                        if (farmer == Game1.player) continue;
                        if (!this.Characters.Contains(farmer))
                            this.Characters.Add(farmer);
                    }
                }

                if (Context.IsMainPlayer && Context.IsMultiplayer && this.SyncedLocationData != null)
                {
                    this.GetSyncedLocationData();
                    this.Helper.Multiplayer.SendMessage(this.SyncedLocationData, "SyncedLocationData", modIDs: new[] { this.ModManifest.UniqueID });
                }
            }

            // Update tick
            if (!Game1.paused && this.ShowLocators && this.SyncedLocationData != null)
            {
                if (Context.IsMainPlayer)
                    this.GetSyncedLocationData();

                this.UpdateLocators();
            }
        }

        /// <inheritdoc cref="IDisplayEvents.Rendered"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnRendered(object sender, RenderedEventArgs e)
        {
            //if (!Context.IsWorldReady || locators == null) return;

            if (this.ShowLocators && Game1.activeClickableMenu == null)
                this.DrawLocators();

            if (!this.DebugMode)
                return;

            foreach (var locPair in this.Locators)
            {
                foreach (var locator in locPair.Value)
                {
                    if (!locator.IsOnScreen)
                        continue;

                    var npc = Game1.getCharacterFromName(locator.Name);
                    float viewportX = Game1.player.position.X + Game1.pixelZoom * Game1.player.Sprite.SpriteWidth / 2 - Game1.viewport.X;
                    float viewportY = Game1.player.position.Y - Game1.viewport.Y;
                    float npcViewportX = npc.position.X + Game1.pixelZoom * npc.Sprite.SpriteWidth / 2 - Game1.viewport.X;
                    float npcViewportY = npc.position.Y - Game1.viewport.Y;

                    // Draw NPC sprite noodle connecting center of screen to NPC for debugging
                    this.DrawLine(Game1.spriteBatch, new Vector2(viewportX, viewportY), new Vector2(npcViewportX, npcViewportY), npc.Sprite.Texture);
                }
            }
        }

        // Get only relevant villagers for map
        private List<NPC> GetVillagers()
        {
            var villagers = new List<NPC>();
            var excludedNpcs = new List<string>
            {
                "Dwarf",
                "Mister Qi",
                "Bouncer",
                "Henchman",
                "Gunther",
                "Krobus"
            };

            Utility.ForEachCharacter(npc =>
            {
                if (!villagers.Contains(npc) && !excludedNpcs.Contains(npc.Name) && (npc is Horse || npc.isVillager()))
                    villagers.Add(npc);

                return true;
            });

            return villagers;
        }

        private void GetSyncedLocationData()
        {
            foreach (var npc in this.GetVillagers())
            {
                if (npc?.currentLocation != null)
                    this.SyncedLocationData.Locations[npc.Name] = new LocationData(npc.currentLocation.uniqueName.Value ?? npc.currentLocation.Name, npc.Position.X, npc.Position.Y);
            }
        }

        /// <summary>Set whether locators are visible.</summary>
        /// <param name="enabled">Whether locators should be visible.</param>
        private void SetShowLocators(bool enabled)
        {
            this.ShowLocators = enabled;
            if (this.Config.HideHud)
                Game1.displayHUD = !this.ShowLocators;
        }

        private void UpdateLocators()
        {
            this.Locators = new Dictionary<string, List<Locator>>();

            foreach (var character in this.Characters)
            {
                if (character.currentLocation == null)
                    continue;
                if (!this.Config.ShowHorses && character is Horse || this.Config.ShowFarmersOnly && (character is NPC && !(character is Horse)))
                    continue;
                if (!this.SyncedLocationData.Locations.TryGetValue(character.Name, out var npcLoc) && character is NPC)
                    continue;
                if (character is NPC npc && this.Config.ShowQuestsAndBirthdaysOnly)
                {
                    bool isBirthday = false;
                    bool hasQuest = false;
                    // Check if gifted for birthday
                    if (npc.isBirthday())
                    {
                        isBirthday = Game1.player.friendshipData.ContainsKey(npc.Name) && Game1.player.friendshipData[npc.Name].GiftsToday == 0;
                    }

                    // Check for daily quests
                    foreach (var quest in Game1.player.questLog)
                    {
                        if (quest.accepted.Value && quest.dailyQuest.Value && !quest.completed.Value)
                            hasQuest = quest.questType.Value switch
                            {
                                3 => ((ItemDeliveryQuest)quest).target.Value == npc.Name,
                                4 => ((SlayMonsterQuest)quest).target.Value == npc.Name,
                                7 => ((FishingQuest)quest).target.Value == npc.Name,
                                10 => ((ResourceCollectionQuest)quest).target.Value == npc.Name,
                                _ => hasQuest
                            };
                    }

                    if (!isBirthday && !hasQuest)
                        continue;
                }

                string playerLocName = Game1.player.currentLocation.uniqueName.Value ?? Game1.player.currentLocation.Name;
                string charLocName = character is Farmer
                  ? character.currentLocation.uniqueName.Value ?? character.currentLocation.Name
                  : npcLoc.LocationName;
                bool isPlayerLocOutdoors = Game1.player.currentLocation.IsOutdoors;

                LocationContext playerLocCtx;
                LocationContext characterLocCtx;

                // Manually handle mines
                {
                    string charMineName = this.LocationUtil.GetLocationNameFromLevel(charLocName);
                    string playerMineName = this.LocationUtil.GetLocationNameFromLevel(playerLocName);

                    if (isPlayerLocOutdoors)
                    {
                        // If inside a generated level, show characters as inside same general mine to player outside
                        charLocName = charMineName ?? charLocName;
                    }

                    if (playerMineName != null && charMineName != null)
                    {
                        // Leave mine levels distinguished in name if player inside mine
                        playerLocCtx = this.LocationUtil.TryGetContext(playerMineName, mapGeneratedLevels: false);
                        characterLocCtx = this.LocationUtil.TryGetContext(charMineName, mapGeneratedLevels: false);
                    }
                    else
                    {
                        if (!this.LocationUtil.TryGetContext(playerLocName, out playerLocCtx))
                            continue;
                        if (!this.LocationUtil.TryGetContext(charLocName, out characterLocCtx))
                            continue;
                    }
                }

                if (this.Config.SameLocationOnly && characterLocCtx.Root != playerLocCtx.Root)
                    continue;

                var characterPos = Vector2.Zero;
                var playerPos =
                  new Vector2(Game1.player.position.X + Game1.player.FarmerSprite.SpriteWidth / 2 * Game1.pixelZoom,
                    Game1.player.position.Y);
                bool isWarp = false;
                bool isOutdoors = false;
                bool isHorse = character is Horse;

                Vector2 charPosition;
                int charSpriteHeight;

                if (character is Farmer farmer)
                {
                    charPosition = character.Position;
                    charSpriteHeight = farmer.FarmerSprite.SpriteHeight;
                }
                else
                {
                    charPosition = new Vector2(npcLoc.X, npcLoc.Y);
                    charSpriteHeight = character.Sprite.SpriteHeight;
                }

                // Player and character in same location
                if (playerLocName == charLocName)
                {
                    // Don't include locator if character is visible on screen
                    if (Utility.isOnScreen(charPosition, Game1.tileSize / 4))
                        continue;

                    characterPos = new Vector2(charPosition.X + charSpriteHeight / 2 * Game1.pixelZoom, charPosition.Y);
                }
                else
                {
                    // Indoor locations
                    // Intended behavior is for all characters in a building, including rooms within the building
                    // to show up when the player is outside. So even if an character is not in the same location
                    // ex. Maru in ScienceHouse, Sebastian in SebastianRoom, Sebastian will be placed in
                    // ScienceHouse such that the player will know Sebastian is in that building.
                    // Once the player is actually inside, Sebastian will be correctly placed in SebastianRoom.

                    // Finds the upper-most indoor location that the player is in
                    isWarp = true;
                    string indoor = this.LocationUtil.GetBuilding(charLocName, curRecursionDepth: 1);
                    if (this.Config.SameLocationOnly)
                    {
                        if (indoor == null)
                            continue;
                        if (playerLocName != characterLocCtx.Root && playerLocName != indoor)
                            continue;
                    }
                    charLocName = isPlayerLocOutdoors || characterLocCtx.Type != LocationType.Room
                        ? indoor
                        : charLocName;

                    // Neighboring outdoor warps
                    if (!isPlayerLocOutdoors)
                    {
                        if (characterLocCtx.Root != playerLocCtx.Root || characterLocCtx.Parent == null)
                            continue;

                        // Doors that lead to connected rooms to character
                        if (characterLocCtx.Parent == playerLocName)
                            characterPos = characterLocCtx.GetWarpPixelPosition();
                        else
                        {
                            LocationContext characterParentContext = this.LocationUtil.TryGetContext(characterLocCtx.Parent);
                            characterPos = characterParentContext.GetWarpPixelPosition();
                        }
                    }
                    else
                    {
                        if (characterLocCtx.Root == playerLocCtx.Root)
                        {
                            // Point locators to the neighboring outdoor warps and
                            // doors of buildings including nested rooms
                            LocationContext indoorContext = this.LocationUtil.TryGetContext(indoor);
                            characterPos = indoorContext.GetWarpPixelPosition();
                        }
                        else if (!this.Config.SameLocationOnly)
                        {
                            // Warps to other outdoor locations
                            isOutdoors = true;
                            if ((characterLocCtx.Root != null && playerLocCtx.Neighbors.TryGetValue(characterLocCtx.Root, out Vector2 warpPos)) || charLocName != null && playerLocCtx.Neighbors.TryGetValue(charLocName, out warpPos))
                            {
                                charLocName = characterLocCtx.Root;
                                characterPos = LocationContext.GetWarpPixelPosition(warpPos);
                            }
                            else
                                continue;
                        }
                    }

                    // Add character to the list of locators inside a building
                    if (!this.ActiveWarpLocators.ContainsKey(charLocName))
                    {
                        this.ActiveWarpLocators.Add(charLocName, new LocatorScroller()
                        {
                            Location = charLocName,
                            Characters = new HashSet<string>() { character.Name },
                            LocatorRect = new Rectangle((int)(characterPos.X - 32), (int)(characterPos.Y - 32),
                            64, 64)
                        });
                    }
                    else
                        this.ActiveWarpLocators[charLocName].Characters.Add(character.Name);
                }

                bool isOnScreen = Utility.isOnScreen(characterPos, Game1.tileSize / 4);

                var locator = new Locator
                {
                    Name = character.Name,
                    Farmer = character as Farmer,
                    Marker = character is NPC ? character.Sprite.Texture : null,
                    Proximity = this.GetDistance(playerPos, characterPos),
                    IsWarp = isWarp,
                    IsOutdoors = isOutdoors,
                    IsOnScreen = isOnScreen,
                    IsHorse = isHorse
                };

                double angle = this.GetPlayerToTargetAngle(playerPos, characterPos);
                int quadrant = this.GetViewportQuadrant(angle, playerPos);
                var locatorPos = this.GetLocatorPosition(angle, quadrant, playerPos, characterPos, isWarp);

                locator.X = locatorPos.X;
                locator.Y = locatorPos.Y;
                locator.Angle = angle;
                locator.Quadrant = quadrant;

                if (this.Locators.TryGetValue(charLocName, out var warpLocators))
                {
                    if (!warpLocators.Contains(locator))
                        warpLocators.Add(locator);
                }
                else
                {
                    warpLocators = new List<Locator> { locator };
                }

                this.Locators[charLocName] = warpLocators;
            }
        }

        // Get angle (in radians) to determine which quadrant the NPC is in
        // 0 rad will be at line from player position to (0, viewportHeight/2) relative to viewport
        private double GetPlayerToTargetAngle(Vector2 playerPos, Vector2 npcPos)
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
            if (angle < MathHelper.Pi - Math.Atan2(playerPos.Y - Game1.viewport.Y,
                  Game1.viewport.X + Game1.viewport.Width - playerPos.X))
                return 2;
            // Right quadrant
            if (angle < MathHelper.Pi + Math.Atan2(Game1.viewport.Y + Game1.viewport.Height - playerPos.Y,
                  Game1.viewport.X + Game1.viewport.Width - playerPos.X))
                return 3;
            // Bottom quadrant
            if (angle < MathHelper.TwoPi - Math.Atan2(Game1.viewport.Y + Game1.viewport.Height - playerPos.Y,
                  playerPos.X - Game1.viewport.X))
                return 4;
            // Bottom half of left quadrant
            return 1;
        }

        private double GetDistance(Vector2 begin, Vector2 end)
        {
            return Math.Sqrt(
              Math.Pow(begin.X - end.X, 2) + Math.Pow(begin.Y - end.Y, 2)
            );
        }

        // Get position of location relative to viewport from
        // the viewport quadrant and positions of player/npc relative to map
        private Vector2 GetLocatorPosition(double angle, int quadrant, Vector2 playerPos, Vector2 npcPos, bool isWarp = false)
        {
            float x = playerPos.X - Game1.viewport.X;
            float y = playerPos.Y - Game1.viewport.Y;

            if (isWarp)
                if (Utility.isOnScreen(new Vector2(npcPos.X, npcPos.Y), Game1.tileSize / 4))
                    return new Vector2(npcPos.X - Game1.viewport.X,
                      npcPos.Y - Game1.viewport.Y);

            // Draw triangle such that the hypotenuse is
            // the line from player to the point of intersection of
            // the viewport quadrant and the line to the NPC
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

                    y = MathHelper.Clamp(y, Game1.tileSize * 3 / 4 + 2, Game1.viewport.Height - (Game1.tileSize * 3 / 4 + 2));
                    return new Vector2(0 + Game1.tileSize * 3 / 4 + 2, y);
                case 2:
                    // Left half
                    if (angle < MathHelper.PiOver2)
                        x -= (playerPos.Y - Game1.viewport.Y) * (float)Math.Tan(MathHelper.PiOver2 - angle);
                    // Right half
                    else
                        x -= (playerPos.Y - Game1.viewport.Y) * (float)Math.Tan(MathHelper.PiOver2 - angle);

                    x = MathHelper.Clamp(x, Game1.tileSize * 3 / 4 + 2, Game1.viewport.Width - (Game1.tileSize * 3 / 4 + 2));
                    return new Vector2(x, 0 + Game1.tileSize * 3 / 4 + 2);
                case 3:
                    // Top half
                    if (angle < MathHelper.Pi)
                        y -= (Game1.viewport.X + Game1.viewport.Width - playerPos.X) * (float)Math.Tan(MathHelper.Pi - angle);
                    // Bottom half
                    else
                        y -= (Game1.viewport.X + Game1.viewport.Width - playerPos.X) * (float)Math.Tan(MathHelper.Pi - angle);

                    y = MathHelper.Clamp(y, Game1.tileSize * 3 / 4 + 2, Game1.viewport.Height - (Game1.tileSize * 3 / 4 + 2));
                    return new Vector2(Game1.viewport.Width - (Game1.tileSize * 3 / 4 + 2), y);
                case 4:
                    // Right half
                    if (angle < 3 * MathHelper.PiOver2)
                        x += (Game1.viewport.Y + Game1.viewport.Height - playerPos.Y) *
                             (float)Math.Tan(3 * MathHelper.PiOver2 - angle);
                    // Left half
                    else
                        x += (Game1.viewport.Y + Game1.viewport.Height - playerPos.Y) *
                             (float)Math.Tan(3 * MathHelper.PiOver2 - angle);

                    x = MathHelper.Clamp(x, Game1.tileSize * 3 / 4 + 2, Game1.viewport.Width - (Game1.tileSize * 3 / 4 + 2));
                    return new Vector2(x, Game1.viewport.Height - (Game1.tileSize * 3 / 4 + 2));
                default:
                    return Vector2.Zero;
            }
        }

        private void DrawLocators()
        {
            var sortedLocators = this.Locators.OrderBy(x => !x.Value.FirstOrDefault().IsOutdoors);

            // Individual locators, onscreen or offscreen
            foreach (var locPair in sortedLocators)
            {
                int offsetX;
                int offsetY;

                // Show outdoor NPCs position in the current location
                if (!locPair.Value.FirstOrDefault().IsWarp)
                {
                    foreach (var locator in locPair.Value)
                    {
                        bool isHovering = new Rectangle((int)(locator.X - 32), (int)(locator.Y - 32), 64, 64).Contains(
                          Game1.getMouseX(),
                          Game1.getMouseY());

                        offsetX = 24;
                        offsetY = 15;

                        // Change opacity based on distance from player
                        double alphaLevel = isHovering ? 1f : locator.Proximity > this.MaxProximity
                          ? 0.35
                          : 0.35 + (this.MaxProximity - locator.Proximity) / this.MaxProximity * 0.65;

                        if (!this.Constants.MarkerCrop.TryGetValue(locator.Name, out int cropY))
                            cropY = 0;

                        var npcSrcRect = locator.IsHorse ? new Rectangle(17, 104, 16, 14) : new Rectangle(0, cropY, 16, 15);

                        // Pointer texture
                        Game1.spriteBatch.Draw(
                            this.Pointer,
                            new Vector2(locator.X, locator.Y),
                            new Rectangle(0, 0, 64, 64),
                            Color.White * (float)alphaLevel,
                            (float)(locator.Angle - 3 * MathHelper.PiOver4),
                            new Vector2(32, 32),
                            1f,
                            SpriteEffects.None,
                            0.0f
                        );

                        // NPC head
                        if (locator.Marker != null)
                        {
                            Game1.spriteBatch.Draw(
                                locator.Marker,
                                new Vector2(locator.X + offsetX, locator.Y + offsetY),
                                npcSrcRect,
                                Color.White * (float)alphaLevel,
                                0f,
                                new Vector2(16, 16),
                                3f,
                                SpriteEffects.None,
                                1f
                            );
                        }
                        else
                        {
                            locator.Farmer.FarmerRenderer.drawMiniPortrat(Game1.spriteBatch, new Vector2(locator.X + offsetX - 48, locator.Y + offsetY - 48), 0f, 3f, 0, locator.Farmer);
                        }

                        // Draw distance text
                        if (locator.Proximity > 0)
                        {
                            string distanceString = $"{Math.Round(locator.Proximity / Game1.tileSize, 0)}";
                            this.DrawText(Game1.tinyFont, distanceString, new Vector2(locator.X + offsetX - 24, locator.Y + offsetY - 4),
                              Color.Black * (float)alphaLevel,
                              new Vector2((int)Game1.tinyFont.MeasureString(distanceString).X / 2,
                                (float)(Game1.tileSize / 4 * 0.5)));
                        }
                    }
                }
                // Multiple indoor locators in a location, pointing to its door
                else
                {
                    Locator locator;

                    if (this.ActiveWarpLocators != null && this.ActiveWarpLocators.TryGetValue(locPair.Key, out LocatorScroller activeLocator))
                    {
                        locator = locPair.Value.ElementAtOrDefault(activeLocator.Index) ?? locPair.Value.FirstOrDefault();
                        activeLocator.LocatorRect = new Rectangle((int)(locator.X - 32), (int)(locator.Y - 32), 64, 64);
                    }
                    else
                        locator = locPair.Value.FirstOrDefault();

                    bool isHovering = new Rectangle((int)(locator.X - 32), (int)(locator.Y - 32), 64, 64).Contains(
                      Game1.getMouseX(),
                      Game1.getMouseY());

                    offsetX = 24;
                    offsetY = 12;

                    // Adjust for offsets used to create padding around edges of screen
                    if (!locator.IsOnScreen)
                        switch (locator.Quadrant)
                        {
                            case 1:
                                offsetY += 2;
                                break;
                            case 2:
                                offsetX -= 0;
                                offsetY += 2;
                                break;
                            case 3:
                                offsetY -= 1;
                                break;
                            case 4:
                                break;
                        }

                    // Change opacity based on distance from player
                    double alphaLevel = isHovering ? 1f : (locator.IsOutdoors ? locator.Proximity > this.MaxProximity
                      ? 0.3
                      : 0.3 + (this.MaxProximity - locator.Proximity) / this.MaxProximity * 0.7 : locator.Proximity > this.MaxProximity
                        ? 0.35
                        : 0.35 + (this.MaxProximity - locator.Proximity) / this.MaxProximity * 0.65);

                    // Make locators point down at the door
                    if (locator.IsOnScreen)
                    {
                        locator.Angle = 3 * MathHelper.PiOver2;
                        locator.Proximity = 0;
                    }

                    if (!this.Constants.MarkerCrop.TryGetValue(locator.Name, out int cropY))
                        cropY = 0;

                    var compassSrcRect = locator.IsOutdoors ? new Rectangle(64, 0, 64, 64) : new Rectangle(0, 0, 64, 64); // Different locator color for neighboring outdoor locations
                    var npcSrcRect = locator.IsHorse ? new Rectangle(17, 104, 16, 14) : new Rectangle(0, cropY, 16, 15);

                    // Pointer texture
                    Game1.spriteBatch.Draw(
                        this.Pointer,
                        new Vector2(locator.X, locator.Y),
                        compassSrcRect,
                        Color.White * (float)alphaLevel,
                        (float)(locator.Angle - 3 * MathHelper.PiOver4),
                        new Vector2(32, 32),
                        1f,
                        SpriteEffects.None,
                        0.0f
                    );

                    // NPC head
                    if (locator.Marker != null)
                    {
                        Game1.spriteBatch.Draw(
                            locator.Marker,
                            new Vector2(locator.X + offsetX, locator.Y + offsetY),
                            npcSrcRect,
                            Color.White * (float)alphaLevel,
                            0f,
                            new Vector2(16, 16),
                            3f,
                            SpriteEffects.None,
                            1f
                        );
                    }
                    else
                    {
                        locator.Farmer.FarmerRenderer.drawMiniPortrat(Game1.spriteBatch, new Vector2(locator.X + offsetX - 48, locator.Y + offsetY - 48), 0f, 3f, 0, locator.Farmer);
                    }

                    if (locator.IsOnScreen || isHovering)
                    {
                        if (locPair.Value.Count > 1)
                        {
                            // Draw NPC count
                            string countString = $"{locPair.Value.Count}";
                            int headOffset = locPair.Value.Count > 9 ? 37 : 31;

                            // head icon
                            Game1.spriteBatch.Draw(
                                this.Pointer,
                                new Vector2(locator.X + offsetX - headOffset, locator.Y + offsetY),
                                new Rectangle(128, 0, 7, 9),
                                Color.White * (float)alphaLevel, 0f, Vector2.Zero,
                                1f,
                                SpriteEffects.None,
                                1f
                            );

                            this.DrawText(Game1.tinyFont, countString, new Vector2(locator.X + offsetX - 31, locator.Y + offsetY),
                              Color.Black * (float)alphaLevel,
                              new Vector2((int)(Game1.tinyFont.MeasureString(countString).X - 24) / 2,
                                (float)(Game1.tileSize / 8) + 3));
                        }
                    }

                    // Draw distance text
                    else if (locator.Proximity > 0)
                    {
                        string distanceString = $"{Math.Round(locator.Proximity / Game1.tileSize, 0)}";
                        this.DrawText(Game1.tinyFont, distanceString, new Vector2(locator.X + offsetX - 24, locator.Y + offsetY - 4),
                          Color.Black * (float)alphaLevel,
                          new Vector2((int)Game1.tinyFont.MeasureString(distanceString).X / 2,
                            (float)(Game1.tileSize / 4 * 0.5)));

                    }

                    if (isHovering)
                    {
                        if (locPair.Value.Count > 1)
                        {
                            // Change mouse cursor on hover
                            Game1.mouseCursor = -1;
                            Game1.mouseCursorTransparency = 1f;
                            Game1.spriteBatch.Draw(Game1.mouseCursors, new Vector2(Game1.getOldMouseX(), Game1.getOldMouseY()), Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 44, 16, 16), Color.White, 0f, Vector2.Zero, (Game1.pixelZoom + Game1.dialogueButtonScale / 150f), SpriteEffects.None, 1f);
                        }
                        else
                        {
                            Game1.spriteBatch.Draw(Game1.mouseCursors, new Vector2(Game1.getOldMouseX(), Game1.getOldMouseY()), new Rectangle(0, 0, 8, 10), Color.White, 0f, Vector2.Zero, (Game1.pixelZoom + Game1.dialogueButtonScale / 150f), SpriteEffects.None, 1f);
                        }
                    }
                }
            }
        }

        // Draw line relative to viewport
        private void DrawLine(SpriteBatch b, Vector2 begin, Vector2 end, Texture2D tex)
        {
            var r = new Rectangle((int)begin.X, (int)begin.Y, (int)(end - begin).Length() + 2, 2);
            var v = Vector2.Normalize(begin - end);
            float angle = (float)Math.Acos(Vector2.Dot(v, -Vector2.UnitX));
            if (begin.Y > end.Y) angle = MathHelper.TwoPi - angle;
            b.Draw(tex, r, null, Color.White, angle, Vector2.Zero, SpriteEffects.None, 0);
        }

        // Draw outlined text
        private void DrawText(SpriteFont font, string text, Vector2 pos, Color? color = null, Vector2? origin = null, float scale = 1f)
        {
            //Game1.spriteBatch.DrawString(font, text, pos + new Vector2(1, 1), Color.Black, 0f, origin ?? Vector2.Zero, scale, SpriteEffects.None, 0f);
            //Game1.spriteBatch.DrawString(font, text, pos + new Vector2(-1, 1), Color.Black, 0f, origin ?? Vector2.Zero, scale, SpriteEffects.None, 0f);
            //Game1.spriteBatch.DrawString(font, text, pos + new Vector2(1, -1), Color.Black, 0f, origin ?? Vector2.Zero, scale, SpriteEffects.None, 0f);
            //Game1.spriteBatch.DrawString(font, text, pos + new Vector2(-1, -1), Color.Black, 0f, origin ?? Vector2.Zero, scale, SpriteEffects.None, 0f);
            Game1.spriteBatch.DrawString(font ?? Game1.tinyFont, text, pos, color ?? Color.White, 0f, origin ?? Vector2.Zero,
              scale,
              SpriteEffects.None, 0f);
        }
    }
}
