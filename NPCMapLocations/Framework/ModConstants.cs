using System.Collections.Generic;
using Microsoft.Xna.Framework;
using NPCMapLocations.Framework.Models;
using StardewModdingAPI;
using StardewValley.Menus;

namespace NPCMapLocations.Framework
{
    /// <summary>The constant values used by the mod.</summary>
    public static class ModConstants
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The network message IDs used by the mod.</summary>
        public static class MessageIds
        {
            /// <summary>A message from the host containing NPC internal => display names.</summary>
            public const string SyncedNames = "SyncedNames";

            /// <summary>A message from the host containing NPC marker positions to show on the map.</summary>
            public const string SyncedNpcMarkers = "SyncedNpcMarkers";
        }

        /// <summary>The page index of the map tab in the <see cref="GameMenu"/>.</summary>
        public static int MapTabIndex => Constants.TargetPlatform == GamePlatform.Android ? 4 : GameMenu.mapTab;

        /// <summary>The offset to apply when cropping NPC heads for markers. These shift the head up (negative) or down (positive).</summary>
        public static Dictionary<string, int> NpcMarkerOffsets => new()
        {
            ["Abigail"] = 3,
            ["Alex"] = 0,
            ["Birdie"] = 6,
            ["Caroline"] = 2,
            ["Clint"] = -1,
            ["Demetrius"] = -2,
            ["Dwarf"] = 1,
            ["Elliott"] = -1,
            ["Emily"] = 1,
            ["Evelyn"] = 4,
            ["George"] = 4,
            ["Gus"] = 2,
            ["Gunther"] = 3,
            ["Haley"] = 2,
            ["Harvey"] = -1,
            ["Jas"] = 7,
            ["Jodi"] = 3,
            ["Kent"] = -1,
            ["Krobus"] = 0,
            ["Leah"] = 2,
            ["Leo"] = 6,
            ["Lewis"] = 1,
            ["Linus"] = 6,
            ["Marlon"] = 2,
            ["Marnie"] = 4,
            ["Maru"] = 2,
            ["Pam"] = 5,
            ["Penny"] = 3,
            ["Pierre"] = 0,
            ["Robin"] = 2,
            ["Sam"] = 0,
            ["Sandy"] = 2,
            ["Sebastian"] = 1,
            ["Shane"] = 1,
            ["Vincent"] = 8,
            ["Willy"] = -1,
            ["Wizard"] = 0
        };

        /// <summary>NPCs with no schedules.</summary>
        public static List<string> ExcludedNpcs => new()
        {
            // "Dwarf",
            "Mister Qi",
            "Bouncer",
            "Henchman",
            "Birdie"
            // "Gunther",
            // "Krobus",
            // "Dusty"
        };

        /// <summary>Spoiler characters that are unlocked later in the game.</summary>
        public static List<string> ConditionalNpcs => new()
        {
            "Dwarf",
            "Kent",
            "Krobus",
            "Marlon",
            "Merchant",
            "Sandy",
            "Wizard",
            "Leo"
        };

        /// <summary>Custom farm markers. Also used to do a quick check for currentLocation is farm building.</summary>
        public static Dictionary<string, Rectangle> FarmBuildingRects { get; } = GetFarmBuildingRects();


        /*********
        ** Private methods
        *********/
        /// <summary>Get the map icons each each farm building type.</summary>
        private static Dictionary<string, Rectangle> GetFarmBuildingRects()
        {
            // base source rects
            var rects = new Dictionary<string, Rectangle>()
            {
                ["Shed"] = new Rectangle(0, 0, 5, 7),
                ["Coop"] = new Rectangle(5, 0, 5, 7),
                ["Barn"] = new Rectangle(10, 0, 6, 7),
                ["SlimeHutch"] = new Rectangle(16, 0, 7, 7),
                ["Greenhouse"] = new Rectangle(23, 0, 5, 7),
                ["FarmHouse"] = new Rectangle(28, 0, 5, 7),
                ["Cabin"] = new Rectangle(33, 0, 4, 7),
                ["Log Cabin"] = new Rectangle(33, 0, 4, 7),
                ["Plank Cabin"] = new Rectangle(37, 0, 4, 7),
                ["Stone Cabin"] = new Rectangle(41, 0, 4, 7)
            };

            // use same texture for upgraded buildings
            rects["Big Shed"] = rects["Shed"];
            rects["Big Coop"] = rects["Coop"];
            rects["Deluxe Coop"] = rects["Coop"];
            rects["Big Barn"] = rects["Barn"];
            rects["Deluxe Barn"] = rects["Barn"];

            return rects;
        }
    }
}
