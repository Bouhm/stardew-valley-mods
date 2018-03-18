/*
Static class that stores constants for map mod.
*/

using System;
using System.Collections.Generic;

public static class MapModConstants
{
    public static Dictionary<string, int> spriteCrop
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
                    new MapVectors(11, 18, 407, 186),
                    new MapVectors(15, 39, 414, 240),
                    new MapVectors(17, 11, 421, 169),
                    new MapVectors(21, 21, 448, 190),
                    new MapVectors(22, 26, 439, 221),
                    new MapVectors(49, 17, 697, 157),
                    new MapVectors(49, 39, 710, 160),
                    new MapVectors(49, 32, 500, 238),
                }},
                // FARMS
                { "Farm", new MapVectors[] {
                    new MapVectors(0, 0, 330, 262),
                    new MapVectors(4, 8, 340, 272),
                    new MapVectors(3, 61, 336, 371),
                    new MapVectors(7, 28, 341, 320),
                    new MapVectors(19, 8, 365, 265),
                    new MapVectors(32, 53, 389, 349),
                    new MapVectors(34, 6, 393, 255),
                    new MapVectors(34, 8, 393, 261),
                    new MapVectors(34, 50, 393, 333),
                    new MapVectors(39, 48, 411, 328),
                    new MapVectors(40, 0, 406, 235),
                    new MapVectors(41, 7, 415, 245),
                    new MapVectors(40, 63, 418, 376),
                    new MapVectors(44, 50, 429, 333),
                    new MapVectors(45, 57, 427, 367),
                    new MapVectors(51, 20, 437, 301),
                    new MapVectors(60, 17, 470, 293),
                    new MapVectors(64, 15, 477, 289),
                    new MapVectors(64, 17, 477, 294),
                    new MapVectors(72, 58, 498, 370),
                    new MapVectors(73, 10, 510, 257),
                    new MapVectors(79, 17, 513, 314),
                    new MapVectors(79, 64, 523, 384)
                }},
                    { "FarmHouse", new MapVectors[] {
                        new MapVectors(474, 275)
                    }},
                    { "FarmCave", new MapVectors[] {
                        new MapVectors(386, 246)
                    }},
                    { "Greenhouse", new MapVectors[] {
                        new MapVectors(380, 264)
                    }},
                { "BusStop", new MapVectors[] {
                    new MapVectors(0, 22, 521, 310),
                    new MapVectors(2, 4, 517, 217),
                    new MapVectors(11, 10, 533, 238),
                    new MapVectors(33, 9, 597, 238),
                    new MapVectors(34, 25, 609, 312)
                }},
                    { "Tunnel", new MapVectors[] {
                        new MapVectors(430, 228)
                    }},
                { "Forest", new MapVectors[] {
                    new MapVectors(0, 7, 215, 360),
                    new MapVectors(5, 27, 209, 413),
                    new MapVectors(5, 75, 205, 525),
                    new MapVectors(10, 10, 241, 361),
                    new MapVectors(10, 24, 250, 398),
                    new MapVectors(14, 40, 234, 429),
                    new MapVectors(15, 57, 241, 469),
                    new MapVectors(17, 10, 265, 379),
                    new MapVectors(22, 18, 272, 412),
                    new MapVectors(22, 25, 273, 422),
                    new MapVectors(31, 99, 259, 596),
                    new MapVectors(33, 99, 266, 596),
                    new MapVectors(34, 17, 304, 410),
                    new MapVectors(34, 96, 270, 593),
                    new MapVectors(35, 25, 305, 433),
                    new MapVectors(36, 62, 290, 532),
                    new MapVectors(37, 38, 305, 433),
                    new MapVectors(40, 11, 315, 396),
                    new MapVectors(40, 78, 289, 559),
                    new MapVectors(40, 81, 289, 567),
                    new MapVectors(42, 41, 325, 461),
                    new MapVectors(43, 59, 325, 531),
                    new MapVectors(45, 6, 326, 387),
                    new MapVectors(45, 20, 330, 421),
                    new MapVectors(45, 70, 292, 459),
                    new MapVectors(46, 50, 327, 487),
                    new MapVectors(47, 91, 301, 585),
                    new MapVectors(48, 27, 341, 430),
                    new MapVectors(48, 49, 340, 480),
                    new MapVectors(49, 99, 301, 597),
                    new MapVectors(51, 40, 340, 460),
                    new MapVectors(52, 74, 301, 554),
                    new MapVectors(57, 10, 372, 401),
                    new MapVectors(61, 81, 310, 562),
                    new MapVectors(64, 41, 360, 463),
                    new MapVectors(64, 70, 321, 549),
                    new MapVectors(66, 50, 329, 462),
                    new MapVectors(66, 70, 365, 485),
                    new MapVectors(67, 0, 416, 389),
                    new MapVectors(67, 87, 320, 576),
                    new MapVectors(68, 17, 417, 418),
                    new MapVectors(69, 61, 349, 521),
                    new MapVectors(74, 31, 402, 431),
                    new MapVectors(75, 50, 377, 479),
                    new MapVectors(76, 19, 424, 424),
                    new MapVectors(79, 8, 426, 401),
                    new MapVectors(79, 49, 383, 477),
                    new MapVectors(85, 50, 396, 476),
                    new MapVectors(87, 57, 401, 488),
                    new MapVectors(87, 69, 401, 549),
                    new MapVectors(89, 49, 383, 477),
                    new MapVectors(88, 54, 402, 488),
                    new MapVectors(89, 18, 451, 421),
                    new MapVectors(89, 49, 383, 477),
                    new MapVectors(90, 16, 453, 416),
                    new MapVectors(94, 65, 413, 533),
                    new MapVectors(94, 100, 391, 613),
                    new MapVectors(96, 23, 459, 425),
                    new MapVectors(96, 39, 440, 457),
                    new MapVectors(99, 53, 422, 486),
                    new MapVectors(104, 36, 467, 452),
                    new MapVectors(101, 101, 406, 613),
                    new MapVectors(111, 99, 435, 596),
                    new MapVectors(118, 92, 441, 584),
                    new MapVectors(118, 36, 496, 456),
                    new MapVectors(119, 27, 497, 439)
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
                { "Town", new MapVectors[] {
                    new MapVectors(0, 53, 601, 314),
                    new MapVectors(0, 90, 601, 449),
                    new MapVectors(5, 8, 608, 187),
                    new MapVectors(10, 86, 629, 438),
                    new MapVectors(14, 54, 647, 313),
                    new MapVectors(16, 13, 633, 217),
                    new MapVectors(20, 43, 657, 293),
                    new MapVectors(20, 89, 665, 444),
                    new MapVectors(21, 54, 662, 313),
                    new MapVectors(22, 100, 665, 461),
                    new MapVectors(23, 29, 639, 268),
                    new MapVectors(23, 24, 639, 242),
                    new MapVectors(29, 24, 664, 242),
                    new MapVectors(29, 29, 664, 268),
                    new MapVectors(34, 97, 707, 465),
                    new MapVectors(36, 56, 689, 333),
                    new MapVectors(42, 102, 739, 488),
                    new MapVectors(43, 78, 726, 397),
                    new MapVectors(44, 57, 701, 337),
                    new MapVectors(45, 72, 737, 385),
                    new MapVectors(46, 103, 750, 485),
                    new MapVectors(48, 44, 741, 290),
                    new MapVectors(52, 20, 712, 237),
                    new MapVectors(53, 72, 783, 486),
                    new MapVectors(53, 102, 764, 382),
                    new MapVectors(53, 109, 783, 510),
                    new MapVectors(55, 107, 788, 501),
                    new MapVectors(57, 64, 765, 350),
                    new MapVectors(59, 86, 781, 429),
                    new MapVectors(67, 39, 820, 296),
                    new MapVectors(70, 54, 813, 319),
                    new MapVectors(72, 69, 797, 374),
                    new MapVectors(74, 54, 837, 324),
                    new MapVectors(74, 103, 820, 471),
                    new MapVectors(75, 94, 829, 454),
                    new MapVectors(72, 95, 838, 464),
                    new MapVectors(78, 54, 837, 319),
                    new MapVectors(78, 57, 837, 337),
                    new MapVectors(79, 67, 823, 366),
                    new MapVectors(82, 23, 809, 261),
                    new MapVectors(83, 1, 765, 213),
                    new MapVectors(84, 20, 824, 277),
                    new MapVectors(85, 103, 842, 490),
                    new MapVectors(86, 11, 822, 250),
                    new MapVectors(86, 68, 843, 369),
                    new MapVectors(88, 68, 850, 369),
                    new MapVectors(90, 3, 834, 234),
                    new MapVectors(94, 82, 865, 241),
                    new MapVectors(94, 22, 861, 276),
                    new MapVectors(95, 51, 896, 325),
                    new MapVectors(96, 18, 861, 268),
                    new MapVectors(98, 4, 861, 241),
                    new MapVectors(101, 24, 875, 273),
                    new MapVectors(101, 90, 901, 445),
                    new MapVectors(110, 57, 929, 334),
                    new MapVectors(110, 97, 928, 464)
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
                    { "Sewer", new MapVectors[] {
                        new MapVectors(707, 454)
                    }},
                    { "BugLand", new MapVectors[] {
                        new MapVectors(707, 454)
                    }},
                { "Beach", new MapVectors[] {
                    new MapVectors(11, 17, 798, 595),
                    new MapVectors(14, 39, 813, 601),
                    new MapVectors(24, 22, 833, 642),
                    new MapVectors(27, 36, 813, 642),
                    new MapVectors(38, 36, 877, 642),
                    new MapVectors(43, 24, 855, 592),
                    new MapVectors(44, 35, 889, 641),
                    new MapVectors(59, 13, 869, 581),
                    new MapVectors(62, 4, 871, 553),
                    new MapVectors(86, 26, 941, 604),
                    new MapVectors(90, 40, 953, 633),
                    new MapVectors(92, 23, 962, 597),

                }},
                    { "ElliottHouse", new MapVectors[] {
                        new MapVectors(845, 572)
                    }},
                    { "FishShop", new MapVectors[] {
                        new MapVectors(861, 629)
                    }},
                { "Railroad", new MapVectors[] {
                    new MapVectors(32, 38, 670, 38),
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
                    new MapVectors(0, 12, 702, 147),
                    new MapVectors(12, 26, 741, 177),
                    new MapVectors(18, 36, 843, 202),
                    new MapVectors(29, 8, 789, 141),
                    new MapVectors(42, 23, 831, 167),
                    new MapVectors(47, 7, 859, 129),
                    new MapVectors(54, 6, 885, 121),
                    new MapVectors(58, 35, 848, 202),
                    new MapVectors(76, 9, 917, 144),
                    new MapVectors(105, 27, 929, 169),
                    new MapVectors(105, 27, 961, 173),
                    new MapVectors(130, 36, 981, 130),
                    new MapVectors(130, 36, 1043, 180)
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
                { "Desert", new MapVectors[] {
                    new MapVectors(2, 9, 4, 10),
                    new MapVectors(48, 24, 254, 72),
                    new MapVectors(2, 30, 4, 94),
                    new MapVectors(48, 54, 254, 125)
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

