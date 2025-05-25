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

namespace LocationCompass;

/// <summary>The mod entry point.</summary>
public class ModEntry : Mod
{
    /*********
    ** Fields
    *********/
    private readonly int MaxProximity = 4800;
    private readonly bool DebugMode = false;

    private Texture2D Pointer = null!; // set in Entry
    private ModData Constants = null!; // set in Entry
    private ModConfig Config = null!;  // set in Entry

    /// <summary>Scans and maps locations in the game world.</summary>
    private LocationUtil LocationUtil = null!; // set in Entry

    private readonly List<Character> Characters = [];
    private readonly Dictionary<string, List<Locator>> Locators = [];
    private readonly Dictionary<string, LocatorScroller> ActiveWarpLocators = []; // Active indices of locators of doors
    private SyncedNpcLocationData? SyncedLocationData;

    /// <summary>Whether locators are visible. (This should be set via <see cref="SetShowLocators"/> to toggle the HUD if needed.)</summary>
    private bool ShowLocators;


    /*********
    ** Public methods
    *********/
    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        CommonHelper.RemoveObsoleteFiles(this, "LocationCompass.pdb");

        this.Config = helper.ReadConfig<ModConfig>();
        this.Pointer = helper.ModContent.Load<Texture2D>("assets/locator.png"); // Load pointer tex
        this.Constants = helper.Data.ReadJsonFile<ModData>("assets/constants.json") ?? new ModData();
        this.LocationUtil = new(this.Monitor);

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.GameLoop.DayStarted += this.OnDayStarted;
        helper.Events.World.LocationListChanged += this.OnLocationListChanged;
        helper.Events.Multiplayer.ModMessageReceived += this.OnModMessageReceived;
        helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
        helper.Events.Input.ButtonsChanged += this.OnButtonsChanged;
        helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        helper.Events.Display.Rendered += this.OnRendered;
    }


    /*********
    ** Private methods
    *********/
    /// <inheritdoc cref="IGameLoopEvents.GameLaunched"/>
    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        new GenericModConfigMenuIntegration(this.Config, this.ModManifest, this.Helper.ModRegistry, () => this.Helper.WriteConfig(this.Config))
            .Register();
    }

    /// <inheritdoc cref="IGameLoopEvents.DayStarted"/>
    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        this.Characters.Clear();
        this.Characters.AddRange(this.GetVillagers());

        this.UpdateLocators();
    }

    /// <inheritdoc cref="IInputEvents.ButtonsChanged"/>
    private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        // toggle locators
        if (Context.IsPlayerFree)
        {
            if (this.Config.HoldToToggle)
                this.SetShowLocators(this.Config.ToggleKeyCode.IsDown());
            else if (this.Config.ToggleKeyCode.JustPressed())
                this.SetShowLocators(!this.ShowLocators);
        }

        // toggle options
        if (this.ShowLocators && this.ActiveWarpLocators.Count > 0)
        {
            bool changed = true;

            if (this.Config.SameLocationToggleKey.JustPressed())
                this.Config.SameLocationOnly = !this.Config.SameLocationOnly;
            else if (this.Config.FarmersOnlyToggleKey.JustPressed())
                this.Config.ShowFarmersOnly = !this.Config.ShowFarmersOnly;
            else if (this.Config.QuestsOnlyToggleKey.JustPressed())
                this.Config.ShowQuestsAndBirthdaysOnly = !this.Config.ShowQuestsAndBirthdaysOnly;
            else if (this.Config.HorsesToggleKey.JustPressed())
                this.Config.ShowHorses = !this.Config.ShowHorses;
            else
                changed = false;

            if (changed)
                this.Helper.WriteConfig(this.Config);
        }
    }

    /// <inheritdoc cref="IInputEvents.ButtonPressed"/>
    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        // handle scroll click
        if (e.Button is SButton.MouseRight or SButton.ControllerA)
        {
            foreach (LocatorScroller doorLocator in this.ActiveWarpLocators.Values)
            {
                if (doorLocator.Characters.Count > 1)
                    doorLocator.ReceiveLeftClick();
            }
        }
    }

    /// <inheritdoc cref="IWorldEvents.LocationListChanged"/>
    private void OnLocationListChanged(object? sender, LocationListChangedEventArgs e)
    {
        this.LocationUtil.ScanLocationContexts();
    }

    /// <inheritdoc cref="IGameLoopEvents.SaveLoaded"/>
    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        this.ActiveWarpLocators.Clear();
        this.SyncedLocationData = new SyncedNpcLocationData();
        this.LocationUtil.ScanLocationContexts();

        // Log warning if host does not have mod installed
        if (!Context.IsMainPlayer)
        {
            bool hostHasMod = false;

            foreach (IMultiplayerPeer peer in this.Helper.Multiplayer.GetConnectedPlayers())
            {
                if (peer.IsHost)
                {
                    hostHasMod = peer.GetMod(this.ModManifest.UniqueID) != null;
                    break;
                }
            }

            if (!hostHasMod)
                this.Monitor.Log("Since the server host doesn't have Location Compass installed, NPC locations can't be synced.", LogLevel.Warn);
        }
    }

    /// <inheritdoc cref="IMultiplayerEvents.ModMessageReceived"/>
    private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
        if (e.FromModID == this.ModManifest.UniqueID && e.Type == "SyncedLocationData")
            this.SyncedLocationData = e.ReadAs<SyncedNpcLocationData>();
    }

    /// <inheritdoc cref="IGameLoopEvents.UpdateTicked"/>
    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        // Quarter-second tick
        if (e.IsMultipleOf(15))
        {
            if (this.Characters.Count > 0 && Context.IsMultiplayer)
            {
                foreach (var farmer in Game1.getOnlineFarmers())
                {
                    if (farmer != Game1.player && !this.Characters.Contains(farmer))
                        this.Characters.Add(farmer);
                }
            }

            if (Context.IsMainPlayer && Context.IsMultiplayer && this.SyncedLocationData != null)
            {
                this.GetSyncedLocationData();
                this.Helper.Multiplayer.SendMessage(this.SyncedLocationData, "SyncedLocationData", modIDs: [this.ModManifest.UniqueID]);
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
    private void OnRendered(object? sender, RenderedEventArgs e)
    {
        //if (!Context.IsWorldReady || locators == null) return;

        if (this.ShowLocators && Game1.activeClickableMenu == null)
            this.DrawLocators();

        if (!this.DebugMode)
            return;

        foreach (List<Locator> locators in this.Locators.Values)
        {
            foreach (Locator locator in locators)
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
        List<NPC> villagers = [];
        List<string> excludedNpcs =
        [
            "Dwarf",
            "Mister Qi",
            "Bouncer",
            "Henchman",
            "Gunther",
            "Krobus"
        ];

        Utility.ForEachCharacter(npc =>
        {
            if (!villagers.Contains(npc) && !excludedNpcs.Contains(npc.Name) && (npc is Horse || npc.IsVillager))
                villagers.Add(npc);

            return true;
        });

        return villagers;
    }

    private void GetSyncedLocationData()
    {
        foreach (NPC npc in this.GetVillagers())
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
        this.Locators.Clear();

        foreach (Character character in this.Characters)
        {
            if (character.currentLocation == null)
                continue;

            if (!this.Config.ShowHorses && character is Horse)
                continue;
            if (this.Config.ShowFarmersOnly && character is NPC and not Horse)
                continue;

            if (!this.SyncedLocationData.Locations.TryGetValue(character.Name, out LocationData? npcLoc) && character is NPC)
                continue;

            if (character is NPC npc && this.Config.ShowQuestsAndBirthdaysOnly)
            {
                // check if gifted for birthday
                bool isBirthday = npc.isBirthday() && Game1.player.friendshipData.GetValueOrDefault(npc.Name)?.GiftsToday == 0;

                // check for daily quests
                bool hasQuest = false;
                foreach (var quest in Game1.player.questLog)
                {
                    if (quest.accepted.Value && quest.dailyQuest.Value && !quest.completed.Value)
                    {
                        hasQuest = quest.questType.Value switch
                        {
                            3 => ((ItemDeliveryQuest)quest).target.Value == npc.Name,
                            4 => ((SlayMonsterQuest)quest).target.Value == npc.Name,
                            7 => ((FishingQuest)quest).target.Value == npc.Name,
                            10 => ((ResourceCollectionQuest)quest).target.Value == npc.Name,
                            _ => hasQuest
                        };
                    }
                }

                if (!isBirthday && !hasQuest)
                    continue;
            }

            string playerLocName = Game1.player.currentLocation.uniqueName.Value ?? Game1.player.currentLocation.Name;
            string? charLocName = character is Farmer
                ? character.currentLocation.uniqueName.Value ?? character.currentLocation.Name
                : npcLoc.LocationName;
            bool isPlayerLocOutdoors = Game1.player.currentLocation.IsOutdoors;

            LocationContext? playerLocCtx;
            LocationContext? characterLocCtx;

            // Manually handle mines
            {
                string? charMineName = this.LocationUtil.GetLocationNameFromLevel(charLocName);
                string? playerMineName = this.LocationUtil.GetLocationNameFromLevel(playerLocName);

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
            var playerPos = new Vector2(Game1.player.position.X + Game1.player.FarmerSprite.SpriteWidth / 2 * Game1.pixelZoom, Game1.player.position.Y);
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
                string? indoor = this.LocationUtil.GetBuilding(charLocName, curRecursionDepth: 1);
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
                        LocationContext? characterParentContext = this.LocationUtil.TryGetContext(characterLocCtx.Parent);
                        characterPos = characterParentContext.GetWarpPixelPosition();
                    }
                }
                else
                {
                    if (characterLocCtx.Root == playerLocCtx.Root)
                    {
                        // Point locators to the neighboring outdoor warps and
                        // doors of buildings including nested rooms
                        LocationContext? indoorContext = this.LocationUtil.TryGetContext(indoor);
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
                if (!this.ActiveWarpLocators.TryGetValue(charLocName, out LocatorScroller? warpLocator))
                {
                    warpLocator = new LocatorScroller(
                        Location: charLocName,
                        Characters: [character.Name],
                        Index: 0,
                        LocatorRect: new Rectangle((int)(characterPos.X - 32), (int)(characterPos.Y - 32), 64, 64)
                    );
                    this.ActiveWarpLocators.Add(charLocName, warpLocator);
                }
                warpLocator.Characters.Add(character.Name);
            }

            bool isOnScreen = Utility.isOnScreen(characterPos, Game1.tileSize / 4);

            double angle = this.GetPlayerToTargetAngle(playerPos, characterPos);
            int quadrant = this.GetViewportQuadrant(angle, playerPos);
            var locatorPos = this.GetLocatorPosition(angle, quadrant, playerPos, characterPos, isWarp);

            var locator = new Locator(
                Name: character.Name,
                Farmer: character as Farmer,
                Marker: character is NPC ? character.Sprite.Texture : null,
                Proximity: this.GetDistance(playerPos, characterPos),
                IsWarp: isWarp,
                IsOutdoors: isOutdoors,
                IsOnScreen: isOnScreen,
                IsHorse: isHorse,
                X: locatorPos.X,
                Y: locatorPos.Y,
                Angle: angle,
                Quadrant: quadrant
            );

            if (this.Locators.TryGetValue(charLocName, out List<Locator>? warpLocators))
            {
                if (!warpLocators.Contains(locator))
                    warpLocators.Add(locator);
            }
            else
            {
                warpLocators = [locator];
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
        if (angle < MathHelper.Pi - Math.Atan2(playerPos.Y - Game1.viewport.Y, Game1.viewport.X + Game1.viewport.Width - playerPos.X))
            return 2;
        // Right quadrant
        if (angle < MathHelper.Pi + Math.Atan2(Game1.viewport.Y + Game1.viewport.Height - playerPos.Y, Game1.viewport.X + Game1.viewport.Width - playerPos.X))
            return 3;
        // Bottom quadrant
        if (angle < MathHelper.TwoPi - Math.Atan2(Game1.viewport.Y + Game1.viewport.Height - playerPos.Y, playerPos.X - Game1.viewport.X))
            return 4;
        // Bottom half of left quadrant
        return 1;
    }

    private double GetDistance(Vector2 begin, Vector2 end)
    {
        return Math.Sqrt(Math.Pow(begin.X - end.X, 2) + Math.Pow(begin.Y - end.Y, 2));
    }

    // Get position of location relative to viewport from
    // the viewport quadrant and positions of player/npc relative to map
    private Vector2 GetLocatorPosition(double angle, int quadrant, Vector2 playerPos, Vector2 npcPos, bool isWarp = false)
    {
        float x = playerPos.X - Game1.viewport.X;
        float y = playerPos.Y - Game1.viewport.Y;

        if (isWarp)
            if (Utility.isOnScreen(new Vector2(npcPos.X, npcPos.Y), Game1.tileSize / 4))
                return new Vector2(npcPos.X - Game1.viewport.X, npcPos.Y - Game1.viewport.Y);

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
                    bool isHovering = new Rectangle((int)(locator.X - 32), (int)(locator.Y - 32), 64, 64).Contains(Game1.getMouseX(), Game1.getMouseY());

                    offsetX = 24;
                    offsetY = 15;

                    // Change opacity based on distance from player
                    double alphaLevel = isHovering ? 1f : locator.Proximity > this.MaxProximity
                        ? 0.35
                        : 0.35 + (this.MaxProximity - locator.Proximity) / this.MaxProximity * 0.65;

                    int cropY = this.Constants.MarkerCrop.GetValueOrDefault(locator.Name, 0);

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
                            new Vector2((int)Game1.tinyFont.MeasureString(distanceString).X / 2, (float)(Game1.tileSize / 4 * 0.5))
                        );
                    }
                }
            }
            // Multiple indoor locators in a location, pointing to its door
            else
            {
                Locator? locator;

                if (this.ActiveWarpLocators.TryGetValue(locPair.Key, out LocatorScroller? activeLocator))
                {
                    locator = locPair.Value.ElementAtOrDefault(activeLocator.Index) ?? locPair.Value.FirstOrDefault();
                    activeLocator.LocatorRect = new Rectangle((int)(locator.X - 32), (int)(locator.Y - 32), 64, 64);
                }
                else
                    locator = locPair.Value.FirstOrDefault();

                bool isHovering = new Rectangle((int)(locator.X - 32), (int)(locator.Y - 32), 64, 64).Contains(Game1.getMouseX(), Game1.getMouseY());

                offsetX = 24;
                offsetY = 12;

                // Adjust for offsets used to create padding around edges of screen
                if (!locator.IsOnScreen)
                {
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

                int cropY = this.Constants.MarkerCrop.GetValueOrDefault(locator.Name, 0);

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
                            new Vector2((int)(Game1.tinyFont.MeasureString(countString).X - 24) / 2, (float)(Game1.tileSize / 8) + 3)
                        );
                    }
                }

                // Draw distance text
                else if (locator.Proximity > 0)
                {
                    string distanceString = $"{Math.Round(locator.Proximity / Game1.tileSize, 0)}";
                    this.DrawText(Game1.tinyFont, distanceString, new Vector2(locator.X + offsetX - 24, locator.Y + offsetY - 4),
                        Color.Black * (float)alphaLevel,
                        new Vector2((int)Game1.tinyFont.MeasureString(distanceString).X / 2, (float)(Game1.tileSize / 4 * 0.5))
                    );
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
    private void DrawText(SpriteFont? font, string text, Vector2 pos, Color? color = null, Vector2? origin = null, float scale = 1f)
    {
        //Game1.spriteBatch.DrawString(font, text, pos + new Vector2(1, 1), Color.Black, 0f, origin ?? Vector2.Zero, scale, SpriteEffects.None, 0f);
        //Game1.spriteBatch.DrawString(font, text, pos + new Vector2(-1, 1), Color.Black, 0f, origin ?? Vector2.Zero, scale, SpriteEffects.None, 0f);
        //Game1.spriteBatch.DrawString(font, text, pos + new Vector2(1, -1), Color.Black, 0f, origin ?? Vector2.Zero, scale, SpriteEffects.None, 0f);
        //Game1.spriteBatch.DrawString(font, text, pos + new Vector2(-1, -1), Color.Black, 0f, origin ?? Vector2.Zero, scale, SpriteEffects.None, 0f);
        Game1.spriteBatch.DrawString(font ?? Game1.tinyFont, text, pos, color ?? Color.White, 0f, origin ?? Vector2.Zero, scale, SpriteEffects.None, 0f);
    }
}
