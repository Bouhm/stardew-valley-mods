using StardewValley;

namespace NPCMapLocations
{
	public class NpcSync
	{
		public int LocX { get; set; }
		public int LocY { get; set; }
		public bool IsOutdoors { get; set; }

		public NpcSync(MapMarker npcMarker)
		{
			LocX = (int) npcMarker.MapLocation.X;
			LocY = (int) npcMarker.MapLocation.Y;
			IsOutdoors = npcMarker.IsOutdoors;
		}
	}
}
