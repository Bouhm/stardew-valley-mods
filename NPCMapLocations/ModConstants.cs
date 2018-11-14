/*
Static class that stores constants for map mod.
Do NOT modify anything here other than MapVectors
*/

using NPCMapLocations;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

public static class ModConstants
{
	// Cropping heads for NPC markers
	// Values shift the head up (negative) or down (positive)
	public static Dictionary<string, int> MarkerCropOffsets => new Dictionary<string, int>
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
		{"Leah", 2},
		{"Lewis", 1},
		{"Linus", 6},
		{"Marlon", 2},
		{"Marnie", 4},
		{"Maru", 2},
		{"Pam", 5},
		{"Penny", 3},
		{"Pierre", 0},
		{"Robin", 2},
		{"Sam", 0},
		{"Sandy", 2},
		{"Sebastian", 1},
		{"Shane", 1},
		{"Vincent", 8},
		{"Willy", -1},
		{"Wizard", 0}
	};

	// NPCs with no schedules 
	public static List<string> ExcludedVillagers => new List<string>()
	{
		"Dwarf",
		"Mister Qi",
		"Bouncer",
		"Henchman",
		"Gunther",
		"Krobus"
	};

	// tileX and TileY (the first two values) are tile positions in the game for that location
	// X and Y (the latter values) are CENTERED pixel positions in the map sprite that correspond to the game location
	// MapModMain handles all the calculations to make sure the positions are center-based.
	public static Dictionary<string, MapVector[]> MapVectors => new Dictionary<string, MapVector[]>
	{
		{
			"Backwoods", new MapVector[]
			{
				new MapVector(385, 122, 0, 0),
				new MapVector(529, 238, 50, 40),
			}
		},
		{
			"Backwoods_Region", new MapVector[]
			{
				new MapVector(460, 190)
			}
		},
		{
			"Farm", new MapVector[]
			{
				new MapVector(330, 237, 0, 0),
				new MapVector(514, 386, 80, 65)
			}
		},
		{
			"Farm_Region", new MapVector[]
			{
				new MapVector(423, 321)
			}
		},
		{
			"FarmHouse", new MapVector[]
			{
				new MapVector(478, 268)
			}
		},
		{
			"FarmCave", new MapVector[]
			{
				new MapVector(410, 244)
			}
		},
		{
			"Cellar", new MapVector[]
			{
				new MapVector(478, 268)
			}
		},
		{
			"Greenhouse", new MapVector[]
			{
				new MapVector(393, 268)
			}
		},

		{
			"BusStop", new MapVector[]
			{
				new MapVector(517, 182, 0, 0),
				new MapVector(594, 300, 35, 30)
			}
		},
		{
			"BusStop_Region", new MapVector[]
			{
				new MapVector(555, 229)
			}
		},
		{
			"Tunnel", new MapVector[]
			{
				new MapVector(447, 216)
			}
		},
		{
			"Merchant", new MapVector[]
			{
				new MapVector(320, 410),
			}
		},
		{
			"Forest", new MapVector[]
			{
				new MapVector(250, 383, 0, 0),
				new MapVector(554, 687, 120, 120)
			}
		},
        {
			"Woods", new MapVector[]
			{
				new MapVector(136, 346, 0, 0),
				new MapVector(230, 395, 60, 32)
			}
		},
		{
			"WizardHouse", new MapVector[]
			{
				new MapVector(264, 441)
			}
		},
		{
			"WizardHouseBasement", new MapVector[]
			{
				new MapVector(264, 441)
			}
		},
		{
			"AnimalShop", new MapVector[]
			{
				new MapVector(479, 418)
			}
		},
		{
			"LeahHouse", new MapVector[]
			{
				new MapVector(515, 460)
			}
		},
		{
			"RuinedHouse", new MapVector[]
			{
				new MapVector(333, 622)
			}
		},
		{
			"SewerPipe", new MapVector[]
			{
				new MapVector(489, 643)
			}
		},
		{
			"Town", new MapVector[]
			{
				// Top half of town
				new MapVector(593, 173, 0, 0),
				new MapVector(921, 275, 120, 40),

				// Bottom half of town
				new MapVector(602, 268, 0, 41),
				new MapVector(958, 513, 120, 110)
			}
		},
		{
			"TownSquare", new MapVector[]
			{
				new MapVector(686, 366)
			}
		},
		{
			"SeedShop", new MapVector[]
			{
				new MapVector(726, 317)
			}
		},
		{
			"Saloon", new MapVector[]
			{
				new MapVector(727, 371)
			}
		},
		{
			"Hospital", new MapVector[]
			{
				new MapVector(706, 319)
			}
		},
		{
			"HarveyRoom", new MapVector[]
			{
				new MapVector(706, 315)
			}
		},
		{
			"ArchaeologyHouse", new MapVector[]
			{
				new MapVector(905, 436)
			}
		},
		// Just incase CA fixes the old name to the live one. I don't know.
		{
			"AlexHouse", new MapVector[]
			{
				new MapVector(771, 340)
			}
		},
		{
			"JoshHouse", new MapVector[]
			{
				new MapVector(771, 340)
			}
		},
		{
			"HaleyHouse", new MapVector[]
			{
				new MapVector(669, 427)
			}
		},
		{
			"CommunityCenter", new MapVector[]
			{
				new MapVector(737, 220)
			}
		},
		{
			"Blacksmith", new MapVector[]
			{
				new MapVector(878, 405)
			}
		},
		{
			"JojaMart", new MapVector[]
			{
				new MapVector(885, 302)
			}
		},
		{
			"SamHouse", new MapVector[]
			{
				new MapVector(630, 418)
			}
		},
		{
			"Trailer", new MapVector[]
			{
				new MapVector(813, 365)
			}
		},
	  {
	    "Trailer_Big", new MapVector[]
	    {
	      new MapVector(810, 365)
      }
	  },
    {
			"ManorHouse", new MapVector[]
			{
				new MapVector(780, 417)
			}
		},
		{
			"Graveyard", new MapVector[]
			{
				new MapVector(738, 438)
			}
		},
		{
			"Sewer", new MapVector[]
			{
				new MapVector(701, 465)
			}
		},
		{
			"BugLand", new MapVector[]
			{
				new MapVector(701, 467)
			}
		},
		{
			"Beach", new MapVector[]
			{
				new MapVector(726, 541, 0, 0),
				new MapVector(997, 688, 104, 50)
			}
		},
	  {
	    "BeachNightMarket", new MapVector[]
	    {
	      new MapVector(726, 541, 0, 0),
	      new MapVector(997, 688, 104, 50)
	    }
	  },
    {
			"ElliottHouse", new MapVector[]
			{
				new MapVector(852, 564)
			}
		},
		{
			"FishShop", new MapVector[]
			{
				new MapVector(809, 632)
			}
		},
		{
			"LonelyStone", new MapVector[]
			{
				new MapVector(714, 636)
			}
		},
		{
			"MermaidHouse", new MapVector[]
			{
				new MapVector(875, 638)
			}
		},
		{
			"Submarine", new MapVector[]
			{
				new MapVector(738, 644)
			}
		},
		{
			"Railroad", new MapVector[]
			{
				new MapVector(589, 0, 0, 34),
				new MapVector(794, 81, 70, 62)
			}
		},
		{
			"Railroad_Region", new MapVector[]
			{
				new MapVector(696, 47),
			}
		},
		{
			"BathHouse_Entry", new MapVector[]
			{
				new MapVector(627, 56)
			}
		},
		{
			"BathHouse_MensLocker", new MapVector[]
			{
				new MapVector(627, 56)
			}
		},
		{
			"BathHouse_WomensLocker", new MapVector[]
			{
				new MapVector(627, 56)
			}
		},
		{
			"BathHouse_Pool", new MapVector[]
			{
				new MapVector(627, 56)
			}
		},
		{
			"Spa", new MapVector[]
			{
				new MapVector(627, 56)
			}
		},
		{
			"WitchWarpCave", new MapVector[]
			{
				new MapVector(749, 3)
			}
		},
		{
			"WitchSwamp", new MapVector[]
			{
				new MapVector(749, 3)
			}
		},
		{
			"WitchHut", new MapVector[]
			{
				new MapVector(749, 3)
			}
		},
		{
			"Summit", new MapVector[]
			{
				new MapVector(819, 36)
			}
		},
		{
			"Mountain", new MapVector[]
			{
				new MapVector(718, 81, 0, 0),
				new MapVector(1074, 186, 135, 41),
			}
		},
		{
			"ScienceHouse", new MapVector[]
			{
				new MapVector(751, 133)
			}
		},
		{
			"SebastianRoom", new MapVector[]
			{
				new MapVector(751, 133)
			}
		},
		{
			"AdventureGuild", new MapVector[]
			{
				new MapVector(918, 96)
			}
		},
		{
			"Tent", new MapVector[]
			{
				new MapVector(795, 95)
			}
		},
		{
			"Mine", new MapVector[]
			{
				new MapVector(861, 83)
			}
		},
		{
			"Quarry", new MapVector[]
			{
				new MapVector(1032, 139)
			}
		},
		{
			"Desert", new MapVector[]
			{
				new MapVector(64, 2, 0, 0),
				new MapVector(216, 166, 50, 60),
			}
		},
		{
			"Desert_Region", new MapVector[]
			{
				new MapVector(130, 96),
			}
		},
		{
			"SandyHouse", new MapVector[]
			{
				new MapVector(86, 146)
			}
		},
	  {
	    "Club", new MapVector[]
	    {
	      new MapVector(86, 146)
	    }
	  },
 
    {
			"SkullCave", new MapVector[]
			{
				new MapVector(88, 8)
			}
		}
	};
		
	// Custom farm markers
	// Also used to do a quick check for currentLocation is farm building
	public static Dictionary<string, Rectangle> FarmBuildingRects => new Dictionary<string, Rectangle>
	{
		{"Shed", new Rectangle(0, 0, 5, 7)},
		{"Coop", new Rectangle(5, 0, 5, 7)},
		{"Big Coop", new Rectangle(5, 0, 5, 7)},
		{"Deluxe Coop", new Rectangle(5, 0, 5, 7)},
		{"Barn", new Rectangle(10, 0, 6, 7)},
		{"Big Barn", new Rectangle(10, 0, 6, 7)},
		{"Deluxe Barn", new Rectangle(10, 0, 6, 7)},
		{"SlimeHutch", new Rectangle(16, 0, 7, 7)},
		{"Greenhouse", new Rectangle(23, 0, 5, 7)},
		{"FarmHouse", new Rectangle(28, 0, 5, 7)},
		{"Cabin", new Rectangle(33, 0, 4, 7)},
		{"Log Cabin", new Rectangle(33, 0, 4, 7)},
		{"Plank Cabin", new Rectangle(37, 0, 4, 7)},
		{"Stone Cabin", new Rectangle(41, 0, 4, 7)},
	};
}