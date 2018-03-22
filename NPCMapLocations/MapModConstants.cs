/*
Static class that stores constants for map mod.
*/

using System;
using System.Collections.Generic;

public static class MapModConstants
{
    public static Dictionary<string, int> markerCrop
    {
        get
        {
            return new Dictionary<string, int>
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
                {"Marnie", 4},
                {"Maru", 2},
                {"Pam", 5},
                {"Penny", 3},
                {"Pierre", 0},
                {"Robin", 2},
                {"Sam", 0},
                {"Sebastian", 1},
                {"Shane", 1},
                {"Vincent", 8},
                {"Willy", -1},
                {"Sandy", 2 },
                {"Marlon", 2 },
                {"Wizard", 0 }
            };
        }
    }

    // tileX and tileY (the first two values) are tile positions in the game for that location
    // x and y (the latter values) are pixel positions in the map sprite that correspond to the game location
    // the mapping is done manually due to inaccurate scaling of the game world and map. More vectors = More accuracy.
    public static Dictionary<string, MapVectors[]> mapVectors
    {
        get
        {
            return new Dictionary<string, MapVectors[]>
            {
                { "Backwoods", new MapVectors[] {
                    new MapVectors(0, 0, 400, 150),
                    new MapVectors(49, 32, 500, 238),
                }},
                // FARMS
                { "Farm", new MapVectors[] {
                    new MapVectors(0, 0, 329, 245),
                    new MapVectors(79, 64, 513, 384)
                }},
                    { "FarmHouse", new MapVectors[] {
                        new MapVectors(477, 270)
                    }},
                    { "FarmCave", new MapVectors[] {
                        new MapVectors(386, 246)
                    }},
                    { "Greenhouse", new MapVectors[] {
                        new MapVectors(380, 264)
                    }},
                { "BusStop", new MapVectors[] {
                    new MapVectors(0, 0, 521, 310),
                    new MapVectors(34, 25, 609, 312)
                }},
                    { "Tunnel", new MapVectors[] {
                        new MapVectors(430, 228)
                    }},
                { "Forest", new MapVectors[] {
                    new MapVectors(0, 0, 233, 386),
                    new MapVectors(119, 102, 553, 649)
                }},
                    { "Woods", new MapVectors[] {
                        new MapVectors(0, 0, 97, 318),
                        new MapVectors(62, 32, 189, 356)
                    }},
                    { "WizardHouse", new MapVectors[] {
                        new MapVectors(211, 398)
                    }},
                    { "WizardHouseBasement", new MapVectors[] {
                        new MapVectors(211, 398)
                    }},
                    { "AnimalShop", new MapVectors[] {
                        new MapVectors(453, 408)
                    }},
                    { "LeahHouse", new MapVectors[] {
                        new MapVectors(469, 441)
                    }},
                    { "RuinedHouse", new MapVectors[] {
                        new MapVectors(323, 622)
                    }},
                    { "SewerPipe", new MapVectors[] {
                        new MapVectors(481, 644)
                    }},
                // Town is divided by upper/lower section because the whole
                // town wouldn't fit in the map page
                { "Town", new MapVectors[] {
                    new MapVectors(0, 53, 601, 314),
                    new MapVectors(110, 97, 928, 464),
                    new MapVectors(0, 53, 601, 314),
                    new MapVectors(110, 97, 928, 464)
                }},
                    { "TownSquare", new MapVectors[] {
                        new MapVectors(698, 366)
                    }},
                    { "SeedShop", new MapVectors[] {
                        new MapVectors(711, 322)
                    }},
                    { "Saloon", new MapVectors[] {
                        new MapVectors(733, 369)
                    }},
                    { "Hospital", new MapVectors[] {
                        new MapVectors(689, 320)
                    }},
                    { "HarveyRoom", new MapVectors[] {
                        new MapVectors(689, 314)
                    }},
                    { "ArchaeologyHouse", new MapVectors[] {
                        new MapVectors(901, 433)
                    }},
                    { "AlexHouse", new MapVectors[] {
                        new MapVectors(765, 336)
                    }},
                    { "JoshHouse", new MapVectors[] {
                        new MapVectors(765, 336)
                    }},
                    { "HaleyHouse", new MapVectors[] {
                        new MapVectors(665, 429)
                    }},
                    { "CommunityCenter", new MapVectors[] {
                        new MapVectors(713, 224)
                    }},
                    { "Blacksmith", new MapVectors[] {
                        new MapVectors(865, 406)
                    }},
                    { "JojaMart", new MapVectors[] {
                        new MapVectors(897, 310)
                    }},
                    { "SamHouse", new MapVectors[] {
                        new MapVectors(629, 423)
                    }},
                    { "Trailer", new MapVectors[] {
                        new MapVectors(795, 361)
                    }},
                    { "ManorHouse", new MapVectors[] {
                        new MapVectors(781, 416)
                    }},
                    { "Graveyard", new MapVectors[] {
                        new MapVectors(700, 466)
                    }},
                    { "Sewer", new MapVectors[] {
                        new MapVectors(707, 454)
                    }},
                    { "BugLand", new MapVectors[] {
                        new MapVectors(707, 454)
                    }},
                { "Beach", new MapVectors[] {
                    new MapVectors(0, 0, 798, 595),
                    new MapVectors(14, 39, 813, 601)
                }},
                    { "ElliottHouse", new MapVectors[] {
                        new MapVectors(845, 572)
                    }},
                    { "FishShop", new MapVectors[] {
                        new MapVectors(861, 629)
                    }},
                    { "LonelyStone", new MapVectors[] {
                        new MapVectors(861, 629)
                    }},
                { "Railroad", new MapVectors[] {
                    new MapVectors(0, 0, 670, 38),
                    new MapVectors(51, 43, 697, 49)
                }},
                    { "BathHouse_Entry", new MapVectors[] {
                        new MapVectors(593, 85)
                    }},
                    { "BathHouse_MensLocker", new MapVectors[] {
                        new MapVectors(593, 85)
                    }},
                    { "BathHouse_WomensLocker", new MapVectors[] {
                        new MapVectors(593, 85)
                    }},
                    { "BathHouse_Pool", new MapVectors[] {
                        new MapVectors(593, 85)
                    }},
                    { "Spa", new MapVectors[] {
                        new MapVectors(593, 85)
                    }},
                    { "WitchWarpCave", new MapVectors[] {
                        new MapVectors(697, 28)
                    }},
                    { "WitchSwamp", new MapVectors[] {
                        new MapVectors(697, 28)
                    }},
                    { "WitchHut", new MapVectors[] {
                        new MapVectors(697, 28)
                    }},
                { "Mountain", new MapVectors[] {
                    new MapVectors(0, 0, 702, 147),
                    new MapVectors(12, 26, 741, 177),
                }},
                    { "ScienceHouse", new MapVectors[] {
                        new MapVectors(742, 162)
                    }},
                    { "SebastianRoom", new MapVectors[] {
                        new MapVectors(741, 162)
                    }},
                    { "AdventureGuild", new MapVectors[] {
                        new MapVectors(917, 130)
                    }},
                    { "Tent", new MapVectors[] {
                        new MapVectors(789, 130)
                    }},
                    { "Mine", new MapVectors[] {
                        new MapVectors(887, 109)
                    }},
                    { "Quarry", new MapVectors[] {
                        new MapVectors(887, 109)
                    }},
                { "Desert", new MapVectors[] {
                    new MapVectors(0, 0, 4, 10),
                    new MapVectors(48, 24, 254, 72),
                }},
                    { "SandyHouse", new MapVectors[] {
                        new MapVectors(12, 120)
                    }},
                    { "SkullCave", new MapVectors[] {
                        new MapVectors(19, 7)
                    }},
                    { "Club", new MapVectors[] {
                        new MapVectors(10, 117)
                    }}
            };
        }
    }

    // Resize region rectangles accord to modified map page
    public static Dictionary<string, Rect> regionRects
    {
        get
        {
            return new Dictionary<string, Rect>
            {
                { "Desert", new Rect(285, 144) },
                { "Farm", new Rect(292, 152) },
                { "Backwoods", new Rect(188, 132) },
                { "BusStop", new Rect(76, 100) },
                { "WizardHouse", new Rect(36, 76) },
                { "AnimalShop", new Rect(76, 40) },
                { "LeahHouse", new Rect(32, 24) },
                { "SamHouse", new Rect(36, 52) },
                { "HaleyHouse", new Rect(40, 36) },
                { "TownSquare", new Rect(44, 40) },
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
                { "Railroad", new Rect(16, 8) },
                { "LonelyStone", new Rect(28, 28) },
            };
        }
    }

    public static Dictionary<string, string> indoorLocations
    {
        get
        {
            return new Dictionary<string, string>
            {
                { "Town", "Town"},
                { "SeedShop", "Town"},
                { "Saloon", "Town"},
                { "Hospital", "Town" },
                { "HarveyRoom", "Town"},
                { "Mountain", "Mountain"},
                { "ArchaeologyHouse", "Mountain"},
                { "ScienceHouse", "Mountain"},
                { "SebastianRoom", "Mountain"},
                { "JoshHouse", "Town" },
                { "HaleyHouse", "Town"},
                { "CommunityCenter", "Town"},
                { "Blacksmith", "Town"},
                { "JojaMart", "Town"},
                { "Beach", "Beach"},
                { "ElliottHouse", "Beach"},
                { "AnimalShop", "Forest"},
                { "Forest", "Forest" },
                { "SamHouse", "Town"},
                { "ManorHouse", "Town"},
                { "LeahHouse", "Forest"},
                { "FishShop", "Beach"},
                { "Tent", "Mountain"},
                { "Railroad", "Railroad" },
                { "BathHouse_Entry", "Railroad"},
                { "BathHouse_MensLocker", "Railroad"},
                { "BathHouse_WomensLocker", "Railroad"},
                { "BathHouse_Pool", "Railroad"},
                { "Trailer", "Town"},
                { "Mine", "Mountain" },
                { "Desert", "Desert" },
                { "SandyHouse", "Desert"},
                { "FarmHouse", "Farm"},
                { "Farm", "Farm" },
                { "Sewer", "Town"},
                { "WizardHouse", "Forest"},
                { "Barn", "Farm"},
                { "Big Barn", "Farm" },
                { "Deluxe Barn", "Farm"},
                { "Shed", "Farm" },
                { "Coop", "Farm"},
                { "Big Coop", "Farm"},
                { "Deluxe Coop", "Farm"},
                { "Greenhouse", "Farm"},
                { "FarmCave", "Farm"},
                { "Slime Hutch", "Farm" },
                { "AdventureGuild", "Mountain" }
            };
        }
    }
}

// Class for Location Vectors
public class MapVectors
{
    public int tileX;
    public int tileY;
    public int x;
    public int y;

    public MapVectors()
    {
        this.tileX = 0;
        this.tileY = 0;
        this.x = 0;
        this.y = 0;
    }

    public MapVectors(int x, int y)
    {
        this.tileX = 0;
        this.tileY = 0;
        this.x = x;
        this.y = y;
    }

    public MapVectors(int tileX, int tileY, int x, int y)
    {
        this.tileX = tileX;
        this.tileY = tileY;
        this.x = x;
        this.y = y;
    }

    public int[] getValues()
    {
        return new int[] { this.tileX, this.tileY, this.x, this.y };
    }
}

public class Rect
{
    public int width;
    public int height;

    public Rect(int width, int height)
    {
        this.width = width;
        this.height = height;
    }
}