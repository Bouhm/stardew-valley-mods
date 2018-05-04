/*
Static class that stores constants for map mod.
Do NOT modify anything here other than MapVectors
*/
using NPCMapLocations;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

public static class MapModConstants
{
    // Custom NPCs/NPC name changes directly change the name in the game... 
    // So I have to compare it against these hard-coded names
    public static string[] Villagers => new string[]
    {
        "Abigail",
        "Alex",
        "Caroline",
        "Clint",
        "Demetrius",
        "Elliott",
        "Emily",
        "Evelyn",
        "George",
        "Gus",
        "Haley",
        "Harvey",
        "Jas",
        "Jodi",
        "Kent",
        "Leah",
        "Lewis",
        "Linus",
        "Marlon",
        "Marnie",
        "Maru",
        "Pam",
        "Penny",
        "Pierre",
        "Robin",
        "Sam",
        "Sandy",
        "Sebastian",
        "Shane",
        "Vincent",
        "Willy",
        "Wizard"
    };

    // Cropping heads for NPC markers
    // Values shift the head up (negative) or down (positive)
    public static Dictionary<string, int> MarkerCrop => new Dictionary<string, int>
    {
        {"Abigail", 3},
        {"Alex", 0},
        {"Caroline", 2},
        {"Clint", -1},
        {"Demetrius", -2},
        {"Elliott", -1},
        {"Emily", 1},
        {"Evelyn", 4},
        {"George", 4},
        {"Gus", 2},
        {"Haley", 2},
        {"Harvey", -1},
        {"Jas", 7},
        {"Jodi", 3},
        {"Kent", -1},
        {"Leah", 2 },
        {"Lewis", 1},
        {"Linus", 6},
        {"Marlon", 2 },
        {"Marnie", 4},
        {"Maru", 2},
        {"Pam", 5},
        {"Penny", 3},
        {"Pierre", 0},
        {"Robin", 2},
        {"Sam", 0},
        {"Sandy", 2 },
        {"Sebastian", 1},
        {"Shane", 1},
        {"Vincent", 8},
        {"Willy", -1},
        {"Wizard", 0 }
    };

    // tileX and tileY (the first two values) are tile positions in the game for that location
    // x and y (the latter values) are CENTERED pixel positions in the map sprite that correspond to the game location
    // MapModMain handles all the calculations to make sure the positions are center-based.
    public static Dictionary<string, MapVector[]> MapVectors => new Dictionary<string, MapVector[]>
    {
        { "Backwoods", new MapVector[] {
            new MapVector(0, 0, 385, 122),
            new MapVector(50, 40, 529, 238),
        }},
            { "Backwoods_Region", new MapVector[] {
                new MapVector(460, 190)
            }},
        { "Farm", new MapVector[] {
            new MapVector(0, 0, 330, 237),
            new MapVector(80, 65, 514, 386)
        }},
            {"Farm_Region", new MapVector[] {
                new MapVector(423, 321)
            }},
            { "FarmHouse", new MapVector[] {
                new MapVector(478, 268)
            }},
            { "FarmCave", new MapVector[] {
                new MapVector(410, 244)
            }},
            { "Greenhouse", new MapVector[] {
                new MapVector(393, 268)
            }},
        { "BusStop", new MapVector[] {
            new MapVector(0, 0, 517, 182),
            new MapVector(35, 30, 594, 300)
        }},
            { "BusStop_Region", new MapVector[] {
                new MapVector(555, 229)
            }},
            { "Tunnel", new MapVector[] {
                new MapVector(447, 216)
            }},
        { "Forest", new MapVector[] {
            new MapVector(0, 0, 250, 383),
            new MapVector(120, 120, 554, 687)
        }},
            { "Woods", new MapVector[] {
                new MapVector(0, 0, 136, 346),
                new MapVector(60, 32, 230, 395)
            }},
            { "WizardHouse", new MapVector[] {
                new MapVector(264, 441)
            }},
            { "WizardHouseBasement", new MapVector[] {
                new MapVector(264, 441)
            }},
            { "AnimalShop", new MapVector[] {
                new MapVector(479, 418)
            }},
            { "LeahHouse", new MapVector[] {
                new MapVector(515, 460)
            }},
            { "RuinedHouse", new MapVector[] {
                new MapVector(333, 622)
            }},
            { "SewerPipe", new MapVector[] {
                new MapVector(489, 643)
            }},
        { "Town", new MapVector[] {
            // Top half of town
            new MapVector(0, 0, 593, 173),
            new MapVector(120, 40, 921, 275),

            // Bottom half of town
            new MapVector(0, 41, 602, 268),
            new MapVector(120, 110, 958, 513)
        }},
            { "TownSquare", new MapVector[] {
                new MapVector(686, 366)
            }},
            { "SeedShop", new MapVector[] {
                new MapVector(726, 317)
            }},
            { "Saloon", new MapVector[] {
                new MapVector(727, 371)
            }},
            { "Hospital", new MapVector[] {
                new MapVector(706, 319)
            }},
            { "HarveyRoom", new MapVector[] {
                new MapVector(706, 315)
            }},
            { "ArchaeologyHouse", new MapVector[] {
                new MapVector(905, 436)
            }},
            // Just incase CA fixes the old name to the live one. I don't know.
            { "AlexHouse", new MapVector[] {
                new MapVector(771, 340)
            }},
            { "JoshHouse", new MapVector[] {
                new MapVector(771, 340)
            }},
            { "HaleyHouse", new MapVector[] {
                new MapVector(669, 427)
            }},
            { "CommunityCenter", new MapVector[] {
                new MapVector(737, 220)
            }},
            { "Blacksmith", new MapVector[] {
                new MapVector(878, 405)
            }},
            { "JojaMart", new MapVector[] {
                new MapVector(885, 302)
            }},
            { "SamHouse", new MapVector[] {
                new MapVector(630, 418)
            }},
            { "Trailer", new MapVector[] {
                new MapVector(813, 365)
            }},
            { "ManorHouse", new MapVector[] {
                new MapVector(780, 417)
            }},
            { "Graveyard", new MapVector[] {
                new MapVector(738, 438)
            }},
            { "Sewer", new MapVector[] {
                new MapVector(701, 465)
            }},
            { "BugLand", new MapVector[] {
                new MapVector(701, 467)
            }},
        { "Beach", new MapVector[] {
            new MapVector(0, 0, 726, 541),
            new MapVector(104, 50, 997, 688)
        }},
            { "ElliottHouse", new MapVector[] {
                new MapVector(852, 564)
            }},
            { "FishShop", new MapVector[] {
                new MapVector(809, 632)
            }},
            { "LonelyStone", new MapVector[] {
                new MapVector(714, 636)
            }},
        { "Railroad", new MapVector[] {
            new MapVector(0, 34, 589, 0),
            new MapVector(70, 62, 794, 81)
        }},
            { "Railroad_Region", new MapVector[] {
                new MapVector(696, 47),
            }},
            { "BathHouse_Entry", new MapVector[] {
                new MapVector(627, 56)
            }},
            { "BathHouse_MensLocker", new MapVector[] {
                new MapVector(627, 56)
            }},
            { "BathHouse_WomensLocker", new MapVector[] {
                new MapVector(627, 56)
            }},
            { "BathHouse_Pool", new MapVector[] {
                new MapVector(627, 56)
            }},
            { "Spa", new MapVector[] {
                new MapVector(627, 56)
            }},
            { "WitchWarpCave", new MapVector[] {
                new MapVector(749, 3)
            }},
            { "WitchSwamp", new MapVector[] {
                new MapVector(749, 3)
            }},
            { "WitchHut", new MapVector[] {
                new MapVector(749, 3)
            }},
            { "Summit", new MapVector[] {
                new MapVector(819, 36)
            }},
        { "Mountain", new MapVector[] {
            new MapVector(0, 0, 718, 81),
            new MapVector(135, 41, 1074, 186),
        }},
            { "ScienceHouse", new MapVector[] {
                new MapVector(751, 133)
            }},
            { "SebastianRoom", new MapVector[] {
                new MapVector(751, 133)
            }},
            { "AdventureGuild", new MapVector[] {
                new MapVector(918, 96)
            }},
            { "Tent", new MapVector[] {
                new MapVector(795, 95)
            }},
            { "Mine", new MapVector[] {
                new MapVector(861, 83)
            }},
            { "UndergroundMine", new MapVector[] {
                new MapVector(861, 83)
            }},
            { "Quarry", new MapVector[] {
                new MapVector(1032, 139)
            }},
        { "Desert", new MapVector[] {
            new MapVector(0, 0, 53, 6),
            new MapVector(50, 60, 201, 178),
        }},
            { "Desert_Region", new MapVector[] {
                new MapVector(130, 96),
            }},
            { "SandyHouse", new MapVector[] {
                new MapVector(76, 140)
            }},
            { "SkullCave", new MapVector[] {
                new MapVector(82, 12)
            }},
            { "Club", new MapVector[] {
                new MapVector(79, 140)
            }}
    };

    // Resize location rectangles accord to modified map page
    public static Dictionary<string, Rect> LocationRects => new Dictionary<string, Rect>
    {
        { "Desert_Region", new Rect(261, 175) },
        { "Farm_Region", new Rect(188, 148) },
        { "Backwoods_Region", new Rect(148, 120) },
        { "BusStop_Region", new Rect(76, 100) },
        { "WizardHouse", new Rect(36, 76) },
        { "AnimalShop", new Rect(76, 40) },
        { "LeahHouse", new Rect(32, 24) },
        { "SamHouse", new Rect(36, 52) },
        { "HaleyHouse", new Rect(40, 36) },
        { "TownSquare", new Rect(48, 45) },
        { "Hospital", new Rect(16, 32) },
        { "SeedShop", new Rect(28, 40) },
        { "Blacksmith", new Rect(80, 36) },
        { "Saloon", new Rect(28, 40) },
        { "ManorHouse", new Rect(44, 56) },
        { "ArchaeologyHouse", new Rect(32, 28) },
        { "ElliottHouse", new Rect(28, 20) },
        { "Sewer", new Rect(24, 20) },
        { "Graveyard", new Rect(40, 32) },
        { "Trailer", new Rect(20, 12) },
        { "AlexHouse", new Rect(36, 36) },
        { "JoshHouse", new Rect(36, 36) },
        { "ScienceHouse", new Rect(48, 32) },
        { "Tent", new Rect(12, 16) },
        { "Mine", new Rect(16, 24) },
        { "AdventureGuild", new Rect(32, 36) },
        { "Quarry", new Rect(88, 76) },
        { "JojaMart", new Rect(52, 52) },
        { "FishShop", new Rect(36, 40) },
        { "Spa", new Rect(48, 36) },
        { "Woods", new Rect(196, 176) },
        { "RuinedHouse", new Rect(20, 20) },
        { "CommunityCenter", new Rect(44, 36) },
        { "SewerPipe", new Rect(24, 32) },
        { "Railroad_Region", new Rect(200, 69) },
        { "LonelyStone", new Rect(28, 28) },
    };

    // Custom farm markers
    public static Dictionary<string, Rectangle> FarmBuildingRects => new Dictionary<string, Rectangle>
    {
        { "Shed", new Rectangle(0, 0, 5, 7) },
        { "Coop", new Rectangle(5, 0, 5, 7) },
        { "Barn", new Rectangle(10, 0, 6, 7) },
        { "SlimeHutch", new Rectangle(16, 0, 7, 7) },
        { "Greenhouse", new Rectangle(23, 0, 5, 7) }
    };
}