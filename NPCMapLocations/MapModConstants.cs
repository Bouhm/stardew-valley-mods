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

    public static Dictionary<string, string> startingLocations
    {
        get
        {
            return new Dictionary<string, string>
            {
                {"Abigail", "SeedShop"},
                {"Alex", "JoshHouse"},
                {"Caroline", "SeedShop"},
                {"Clint", "Blacksmith"},
                {"Demetrius", "ScienceHouse"},
                {"Elliott", "ElliottHouse"},
                {"Emily", "HaleyHouse"},
                {"Evelyn", "JoshHouse"},
                {"George", "JoshHouse"},
                {"Gus", "Saloon"},
                {"Haley", "HaleyHouse"},
                {"Harvey", "HarveyRoom" },
                {"Jas", "AnimalShop"},
                {"Jodi", "SamHouse"},
                {"Kent", "SamHouse"},
                {"Leah", "LeahHouse"},
                {"Lewis", "ManorHouse"},
                {"Linus", "Tent"},
                {"Marnie", "AnimalShop"},
                {"Maru", "ScienceHouse"},
                {"Pam", "Trailer"},
                {"Penny", "Trailer"},
                {"Pierre", "SeedShop"},
                {"Robin", "ScienceHouse"},
                {"Sam", "SamHouse"},
                {"Sebastian", "SebastianRoom"},
                {"Shane", "AnimalShop"},
                {"Vincent", "SamHouse"},
                {"Willy", "FishShop"},
                {"Sandy", "SandyHouse" },
                {"Marlon", "AdventureGuild" },
                {"Wizard", "WizardHouse" }
            };
        }
    }

    // tileX and tileY (the first two values) are tile positions in the game for that location
    // x and y (the latter values) are pixel positions (centered) in the map sprite that correspond to the game location
    public static Dictionary<string, MapVectors[]> mapVectors
    {
        get
        {
            return new Dictionary<string, MapVectors[]>
            {
                { "Backwoods", new MapVectors[] {
                    new MapVectors(0, 0, 385, 122),
                    new MapVectors(50, 40, 529, 238),
                }},
                    { "Backwoods_Region", new MapVectors[] {
                        new MapVectors(460, 190)
                    }},
                { "Farm", new MapVectors[] {
                    new MapVectors(0, 0, 330, 242),
                    new MapVectors(80, 65, 514, 386)
                }},
                    {"Farm_Region", new MapVectors[] {
                        new MapVectors(423, 321)
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
                    new MapVectors(0, 0, 517, 182),
                    new MapVectors(35, 30, 594, 305)
                }},
                    { "BusStop_Region", new MapVectors[] {
                        new MapVectors(555, 229)
                    }},
                    { "Tunnel", new MapVectors[] {
                        new MapVectors(447, 216)
                    }},
                { "Forest", new MapVectors[] {
                    new MapVectors(0, 0, 250, 389),
                    new MapVectors(120, 120, 554, 693)
                }},
                    { "Woods", new MapVectors[] {
                        new MapVectors(0, 0, 136, 350),
                        new MapVectors(60, 32, 230, 395)
                    }},
                    { "WizardHouse", new MapVectors[] {
                        new MapVectors(255, 444)
                    }},
                    { "WizardHouseBasement", new MapVectors[] {
                        new MapVectors(263, 447)
                    }},
                    { "AnimalShop", new MapVectors[] {
                        new MapVectors(479, 422)
                    }},
                    { "LeahHouse", new MapVectors[] {
                        new MapVectors(515, 466)
                    }},
                    { "RuinedHouse", new MapVectors[] {
                        new MapVectors(333, 622)
                    }},
                    { "SewerPipe", new MapVectors[] {
                        new MapVectors(489, 643)
                    }},
                { "Town", new MapVectors[] {
                    // Top half of town
                    new MapVectors(0, 0, 594, 182),
                    new MapVectors(120, 40, 917, 273),

                    // Bottom half of town
                    new MapVectors(0, 41, 597, 277),
                    new MapVectors(120, 110, 953, 513)
                }},
                    { "TownSquare", new MapVectors[] {
                        new MapVectors(686, 366)
                    }},
                    { "SeedShop", new MapVectors[] {
                        new MapVectors(721, 318)
                    }},
                    { "Saloon", new MapVectors[] {
                        new MapVectors(727, 371)
                    }},
                    { "Hospital", new MapVectors[] {
                        new MapVectors(704, 319)
                    }},
                    { "HarveyRoom", new MapVectors[] {
                        new MapVectors(704, 315)
                    }},
                    { "ArchaeologyHouse", new MapVectors[] {
                        new MapVectors(905, 436)
                    }},
                    { "AlexHouse", new MapVectors[] {
                        new MapVectors(771, 347)
                    }},
                    { "JoshHouse", new MapVectors[] {
                        new MapVectors(771, 347)
                    }},
                    { "HaleyHouse", new MapVectors[] {
                        new MapVectors(665, 429)
                    }},
                    { "CommunityCenter", new MapVectors[] {
                        new MapVectors(737, 220)
                    }},
                    { "Blacksmith", new MapVectors[] {
                        new MapVectors(877, 409)
                    }},
                    { "JojaMart", new MapVectors[] {
                        new MapVectors(885, 302)
                    }},
                    { "SamHouse", new MapVectors[] {
                        new MapVectors(629, 423)
                    }},
                    { "Trailer", new MapVectors[] {
                        new MapVectors(813, 365)
                    }},
                    { "ManorHouse", new MapVectors[] {
                        new MapVectors(780, 421)
                    }},
                    { "Graveyard", new MapVectors[] {
                        new MapVectors(738, 438)
                    }},
                    { "Sewer", new MapVectors[] {
                        new MapVectors(701, 467)
                    }},
                    { "BugLand", new MapVectors[] {
                        new MapVectors(701, 467)
                    }},
                { "Beach", new MapVectors[] {
                    new MapVectors(0, 0, 726, 549),
                    new MapVectors(104, 50, 994, 686)
                }},
                    { "ElliottHouse", new MapVectors[] {
                        new MapVectors(852, 568)
                    }},
                    { "FishShop", new MapVectors[] {
                        new MapVectors(807, 636)
                    }},
                    { "LonelyStone", new MapVectors[] {
                        new MapVectors(714, 636)
                    }},
                { "Railroad", new MapVectors[] {
                    new MapVectors(0, 33, 589, 5),
                    new MapVectors(70, 62, 794, 86)
                }},
                    { "Railroad_Region", new MapVectors[] {
                        new MapVectors(0, 33, 589, 5),
                        new MapVectors(70, 62, 794, 86)
                    }},
                    { "BathHouse_Entry", new MapVectors[] {
                        new MapVectors(627, 56)
                    }},
                    { "BathHouse_MensLocker", new MapVectors[] {
                        new MapVectors(627, 56)
                    }},
                    { "BathHouse_WomensLocker", new MapVectors[] {
                        new MapVectors(627, 56)
                    }},
                    { "BathHouse_Pool", new MapVectors[] {
                        new MapVectors(627, 56)
                    }},
                    { "Spa", new MapVectors[] {
                        new MapVectors(627, 56)
                    }},
                    { "WitchWarpCave", new MapVectors[] {
                        new MapVectors(749, 6)
                    }},
                    { "WitchSwamp", new MapVectors[] {
                        new MapVectors(749, 6)
                    }},
                    { "WitchHut", new MapVectors[] {
                        new MapVectors(749, 6)
                    }},
                    { "Summit", new MapVectors[] {
                        new MapVectors(819, 36)
                    }},
                { "Mountain", new MapVectors[] {
                    new MapVectors(0, 0, 718, 86),
                    new MapVectors(135, 41, 1074, 186),
                }},
                    { "ScienceHouse", new MapVectors[] {
                        new MapVectors(751, 136)
                    }},
                    { "SebastianRoom", new MapVectors[] {
                        new MapVectors(751, 136)
                    }},
                    { "AdventureGuild", new MapVectors[] {
                        new MapVectors(918, 99)
                    }},
                    { "Tent", new MapVectors[] {
                        new MapVectors(793, 97)
                    }},
                    { "Mine", new MapVectors[] {
                        new MapVectors(861, 87)
                    }},
                    { "Quarry", new MapVectors[] {
                        new MapVectors(1032, 139)
                    }},
                { "Desert", new MapVectors[] {
                    new MapVectors(0, 0, 53, 6),
                    new MapVectors(50, 60, 201, 178),
                }},
                    { "Desert_Region", new MapVectors[] {
                        new MapVectors(130, 96),
                    }},
                    { "SandyHouse", new MapVectors[] {
                        new MapVectors(76, 140)
                    }},
                    { "SkullCave", new MapVectors[] {
                        new MapVectors(82, 12)
                    }},
                    { "Club", new MapVectors[] {
                        new MapVectors(79, 140)
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
                { "Railroad_Region", new Rect(16, 8) },
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
                { "Farm", "Farm" },
                    { "FarmHouse", "Farm"},
                    { "Cellar", "Farm"},
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
                { "Town", "Town"},
                    { "SeedShop", "Town"},
                    { "Saloon", "Town"},
                    { "Hospital", "Town" },
                    { "HarveyRoom", "Town"},
                    { "JoshHouse", "Town" },
                    { "HaleyHouse", "Town"},
                    { "CommunityCenter", "Town"},
                    { "Blacksmith", "Town"},
                    { "JojaMart", "Town"},
                    { "SamHouse", "Town"},
                    { "ManorHouse", "Town"},
                    { "Trailer", "Town"},
                    { "Sewer", "Town"},
                { "Forest", "Forest" },
                    { "AnimalShop", "Forest"},
                    { "LeahHouse", "Forest"},
                    { "WizardHouse", "Forest"},
                    { "WizardHouseBasement", "Forest"},
                { "Mountain", "Mountain"},
                    { "ArchaeologyHouse", "Mountain"},
                    { "ScienceHouse", "Mountain"},
                    { "SebastianRoom", "Mountain"},
                    { "AdventureGuild", "Mountain"},
                    { "Tent", "Mountain"},
                    { "Mine", "Mountain" },
                { "Railroad", "Railroad" },
                    { "BathHouse_Entry", "Railroad"},
                    { "BathHouse_MensLocker", "Railroad"},
                    { "BathHouse_WomensLocker", "Railroad"},
                    { "BathHouse_Pool", "Railroad"},
                    { "WitchWarpCave", "Railroad"},
                    { "WitchSwamp", "Railroad"},
                    { "WitchHut", "Railroad"},
                    { "Summit", "Railroad"},
                { "Beach", "Beach"},
                    { "ElliottHouse", "Beach"},
                    { "FishShop", "Beach"},
                { "Desert", "Desert" },
                    { "SandyHouse", "Desert"},
                    { "Club", "Desert"},
                    { "SkullCave", "Desert"}
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