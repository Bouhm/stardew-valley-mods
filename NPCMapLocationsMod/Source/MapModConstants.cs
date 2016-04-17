/*
Static class that stores constants for map mod.
Meticulously calculated locations, npc head crop, starting locations.
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
                {"Sandy", 3 },
                {"Marlon", 3 },
                {"Wizard", 1 }
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

    public static Dictionary<string, Double[]> locationVectors
    {
        get
        {
            return new Dictionary<string, Double[]>
            {
                { "SeedShop", new Double[]{695, 295, 0, 0} },
                { "Saloon", new Double[]{710, 350, 0, 0} },
                { "Beach", new Double[]{770, 514, 2.3, 2.8} },
                { "Hospital", new Double[]{671, 295, 0, 0} },
                { "HarveyRoom", new Double[]{671, 292, 0, 0} },
                { "Forest", new Double[]{120, 365, 2.652, 2.317} },
                { "ArchaeologyHouse", new Double[]{887, 410, 0, 0} },
                { "Town", new Double[]{580, 160, 3, 3} },
                { "Backwoods", new Double[]{270, 133, 8.355, 2.098} },
                { "BusStop", new Double[]{502, 172, 2.4, 4.88} },
                { "ScienceHouse", new Double[]{725, 140, 0, 0} },
                { "SebastianRoom", new Double[]{722, 140, 0, 0} },
                { "Railroad", new Double[]{549, -80, 2.86, 2.555} },
                { "JoshHouse", new Double[]{745, 315, 0, 0} },
                { "HaleyHouse", new Double[]{654, 404, 0, 0} },
                { "CommunityCenter", new Double[]{695, 199, 0, 0} },
                { "Blacksmith", new Double[]{846, 382, 0, 0} },
                { "JojaMart", new Double[]{877, 282, 0, 0} },
                { "ElliottHouse", new Double[]{820, 548, 0, 0} },
                { "AnimalShop", new Double[]{420, 388, 0, 0} },
                { "SamHouse", new Double[]{609, 400, 0, 0} },
                { "Mountain", new Double[]{692, 102, 2.7, 2.97} },
                { "ManorHouse", new Double[]{763, 390, 0, 0} },
                { "LeahHouse", new Double[]{447, 419, 0, 0} },
                { "FishShop", new Double[]{843, 605, 0, 0} },
                { "Tent", new Double[]{769, 104, 0, 0} },
                { "BathHouse_Entry", new Double[]{581, 55, 0, 0} },
                { "BathHouse_MensLocker", new Double[]{581, 55, 0, 0} },
                { "BathHouse_WomensLocker", new Double[]{581, 55, 0, 0} },
                { "BathHouse_Pool", new Double[]{581, 55, 0, 0} },
                { "Trailer", new Double[]{783, 347, 0, 0} },
                { "Farm", new Double[]{288, 249, 2.655, 1.81} },
                { "Mine", new Double[]{912, 92, 0, 0} },
                { "SandyHouse", new Double[]{111, 23, 0, 0} },
                { "FarmHouse", new Double[]{457, 259, 0, 0} },
                { "Desert", new Double[]{111, 23, 0, 0} },
                { "Sewer", new Double[]{680, 329, 0, 0} },
                { "WizardHouse", new Double[]{173, 328, 0, 0}},
                { "Barn", new Double[]{400, 280, 0, 0} },
                { "Barn2", new Double[]{400, 280, 0, 0} },
                { "Big Barn", new Double[]{400, 280, 0, 0} },
                { "Barn3", new Double[]{400, 280, 0, 0} },
                { "Coop", new Double[]{400, 280, 0, 0} },
                { "Coop2", new Double[]{400, 280, 0, 0} },
                { "Coop3", new Double[]{400, 280, 0, 0} },
                { "Greenhouse", new Double[]{390, 263, 0, 0} },
                { "FarmCave", new Double[]{390, 223, 0, 0} },
                { "SlimeHutch", new Double[]{400, 280, 0, 0} }
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
                { "Barn2", "Farm" },
                { "Big Barn", "Farm" },
                { "Barn3", "Farm"},
                { "Coop", "Farm"},
                { "Coop2", "Farm" },
                { "Coop3", "Farm"},
                { "Greenhouse", "Farm"},
                { "FarmCave", "Farm"},
                { "SlimeHutch", "Farm" },
                { "AdventureGuild", "Mountain" }
            };
        }
    }
}

