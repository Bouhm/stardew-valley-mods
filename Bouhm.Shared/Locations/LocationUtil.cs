using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;

namespace Bouhm.Shared.Locations
{
    // Library for methods the map out all the locations in SDV
    // and other helpful functions
    internal class LocationUtil
    {
        public static Dictionary<string, LocationContext> LocationContexts { get; set; }

        public static Dictionary<string, LocationContext> GetLocationContexts()
        {
            LocationContexts = new Dictionary<string, LocationContext>();
            foreach (var location in Game1.locations)
            {
                // Get outdoor neighbors
                if (location.IsOutdoors)
                {
                    if (!LocationContexts.ContainsKey(location.Name))
                    {
                        LocationContexts.Add(location.Name, new LocationContext() { Root = location.Name, Type = LocationType.Outdoors });
                    }

                    foreach (var warp in location.warps)
                    {
                        if (warp == null || Game1.getLocationFromName(warp.TargetName) == null) continue;
                        var warpLocation = Game1.getLocationFromName(warp.TargetName);

                        if (warpLocation.IsOutdoors)
                        {
                            if (!LocationContexts[location.Name].Neighbors.ContainsKey(warp.TargetName))
                                LocationContexts[location.Name].Neighbors.Add(warp.TargetName, new Vector2(warp.X, warp.Y));
                        }
                    }
                }
                // Get root locations from indoor locations
                else
                {
                    MapRootLocations(location, null, null, false, Vector2.Zero);
                }
            }

            foreach (var location in Game1.getFarm().buildings)
            {
                MapRootLocations(location.indoors.Value, null, null, false, Vector2.Zero);
            }

            return LocationContexts;
        }

        // Recursively traverse warps of locations and map locations to root locations (outdoor locations)
        // Traverse in reverse (indoor to outdoor) because warps and doors are not complete subsets of Game1.locations 
        // Which means there will be some rooms left out unless all the locations are iterated
        public static string MapRootLocations(GameLocation location, GameLocation prevLocation, string root, bool hasOutdoorWarp, Vector2 warpPosition)
        {
            // There can be multiple warps to the same location
            if (location == prevLocation)
                return root;

            string curLocationName = location.uniqueName.Value ?? location.Name;
            string prevLocationName = prevLocation?.uniqueName.Value ?? prevLocation?.Name;

            if (!LocationContexts.ContainsKey(curLocationName))
                LocationContexts.Add(curLocationName, new LocationContext());

            if (prevLocation != null && !warpPosition.Equals(Vector2.Zero))
            {
                LocationContexts[prevLocationName].Warp = warpPosition;

                if (root != curLocationName)
                    LocationContexts[prevLocationName].Parent = curLocationName;
            }

            // Pass root location back recursively
            if (root != null)
            {
                LocationContexts[curLocationName].Root = root;
                return root;
            }

            // Root location found, set as root and return
            if (location.IsOutdoors)
            {
                LocationContexts[curLocationName].Type = LocationType.Outdoors;
                LocationContexts[curLocationName].Root = curLocationName;

                if (prevLocation != null)
                {
                    if (LocationContexts[curLocationName].Children == null)
                        LocationContexts[curLocationName].Children = new List<string> { prevLocationName };
                    else if (!LocationContexts[curLocationName].Children.Contains(prevLocationName))
                        LocationContexts[curLocationName].Children.Add(prevLocationName);
                }

                return curLocationName;
            }

            // Iterate warps of current location and traverse recursively
            foreach (var warp in location.warps)
            {
                // Avoid circular loop
                if (curLocationName == warp.TargetName || prevLocationName == warp.TargetName) continue;

                var warpLocation = Game1.getLocationFromName(warp.TargetName);
                if (warpLocation == null)
                    continue;

                // If one of the warps is a root location, current location is an indoor building 
                if (warpLocation.IsOutdoors)
                    hasOutdoorWarp = true;

                // If all warps are indoors, then the current location is a room
                LocationContexts[curLocationName].Type = hasOutdoorWarp ? LocationType.Building : LocationType.Room;

                if (prevLocation != null)
                {
                    LocationContexts[prevLocationName].Parent = curLocationName;

                    if (LocationContexts[curLocationName].Children == null)
                        LocationContexts[curLocationName].Children = new List<string> { prevLocationName };
                    else if (!LocationContexts[curLocationName].Children.Contains(prevLocationName))
                        LocationContexts[curLocationName].Children.Add(prevLocationName);
                }

                root = MapRootLocations(warpLocation, location, root, hasOutdoorWarp,
                  new Vector2(warp.TargetX, warp.TargetY));
                LocationContexts[curLocationName].Root = root;

                return root;
            }

            return root;
        }

        // Finds the upper-most indoor location (building)
        public static string GetBuilding(string loc)
        {
            static string GetRecursively(string loc, ISet<string> seen)
            {
                if (!seen.Add(loc))
                    return loc; // break infinite loop

                if (loc.Contains("UndergroundMine"))
                    return GetMinesLocationName(loc);
                if (LocationContexts[loc].Type == LocationType.Building)
                    return loc;

                string building = LocationContexts[loc].Parent;
                if (building == null)
                    return null;
                if (building == LocationContexts[loc].Root)
                    return loc;

                return GetRecursively(building, seen);
            }

            return GetRecursively(loc, new HashSet<string>());
        }

        // Get Mines name from floor level
        public static string GetMinesLocationName(string locationName)
        {
            string mine = locationName.Substring("UndergroundMine".Length, locationName.Length - "UndergroundMine".Length);
            if (int.TryParse(mine, out int mineLevel))
            {
                // Skull cave
                if (mineLevel > 120)
                    return "SkullCave";
                // Mines
                return "Mine";
            }

            return null;
        }

        public static bool IsOutdoors(string locationName)
        {
            if (locationName == null) return false;

            if (LocationContexts.TryGetValue(locationName, out var locCtx))
            {
                return locCtx.Type == LocationType.Outdoors;
            }

            return false;
        }
    }
}
