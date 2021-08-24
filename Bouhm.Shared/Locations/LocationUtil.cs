using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace Bouhm.Shared.Locations
{
    /// <summary>Scans and maps locations in the game world.</summary>
    internal class LocationUtil
    {
        /*********
        ** Fields
        *********/
        /// <summary>The monitor with which to log errors.</summary>
        private readonly IMonitor Monitor;


        /*********
        ** Accessors
        *********/
        /// <summary>The maximum method call depth when recursively scanning locations.</summary>
        /// <remarks>This is a last resort to prevent stack overflows. Normally the mod should prevent infinite recursion automatically by tracking locations it already visited.</remarks>
        public const int MaxRecursionDepth = 500;

        public Dictionary<string, LocationContext> LocationContexts { get; } = new();


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="monitor">The monitor with which to log errors.</param>
        public LocationUtil(IMonitor monitor)
        {
            this.Monitor = monitor;
        }

        public Dictionary<string, LocationContext> GetLocationContexts()
        {
            foreach (var location in Game1.locations)
            {
                // Get outdoor neighbors
                if (location.IsOutdoors)
                {
                    if (!this.LocationContexts.ContainsKey(location.Name))
                        this.LocationContexts.Add(location.Name, new LocationContext { Root = location.Name, Type = LocationType.Outdoors });

                    foreach (var warp in location.warps)
                    {
                        GameLocation warpLocation = this.GetStaticLocation(warp?.TargetName);
                        if (warpLocation?.IsOutdoors == true)
                        {
                            if (!this.LocationContexts[location.Name].Neighbors.ContainsKey(warp.TargetName))
                                this.LocationContexts[location.Name].Neighbors.Add(warp.TargetName, new Vector2(warp.X, warp.Y));
                        }
                    }
                }
                // Get root locations from indoor locations
                else
                    this.MapRootLocations(location, curRecursionDepth: 1);
            }

            foreach (var location in Game1.getFarm().buildings)
                this.MapRootLocations(location.indoors.Value, curRecursionDepth: 1);

            return this.LocationContexts;
        }

        /// <summary>Find the uppermost indoor location for a building.</summary>
        /// <param name="startLocationName">The location to scan.</param>
        /// <param name="curRecursionDepth">The current recursion depth when called from a recursive method, or <c>1</c> if called non-recursively.</param>
        public string GetBuilding(string startLocationName, int curRecursionDepth)
        {
            string GetRecursively(string loc, ISet<string> seen, int depth)
            {
                // break infinite loops
                if (!seen.Add(loc))
                    return loc;
                if (depth > LocationUtil.MaxRecursionDepth)
                    throw new InvalidOperationException($"Infinite recursion detected in location scan. Technical details:\n{nameof(loc)}: {loc}\n{nameof(depth)}: {depth}\n\n{Environment.StackTrace}");

                // handle mines
                if (loc.Contains("UndergroundMine"))
                    return this.GetMinesLocationName(loc);

                // found root building
                if (this.LocationContexts[loc].Type == LocationType.Building)
                    return loc;
                string building = this.LocationContexts[loc].Parent;
                if (building == null)
                    return null;
                if (building == this.LocationContexts[loc].Root)
                    return loc;

                // scan recursively
                return GetRecursively(building, seen, depth + 1);
            }

            return GetRecursively(startLocationName, new HashSet<string>(), curRecursionDepth);
        }

        // Get Mines name from floor level
        public string GetMinesLocationName(string locationName)
        {
            string mine = locationName.Substring("UndergroundMine".Length, locationName.Length - "UndergroundMine".Length);
            if (int.TryParse(mine, out int mineLevel))
            {
                return mineLevel > 120
                    ? "SkullCave"
                    : "Mine";
            }

            return null;
        }

        public bool IsOutdoors(string locationName)
        {
            if (locationName == null)
                return false;

            if (this.LocationContexts.TryGetValue(locationName, out var locCtx))
                return locCtx.Type == LocationType.Outdoors;

            return false;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Recursively traverse all locations accessible through warps from a given location, and map all locations to the root (outdoor) locations they can be reached from.</summary>
        /// <param name="startLocation">The location to start searching from.</param>
        /// <param name="curRecursionDepth">The current recursion depth when called from a recursive method, or <c>1</c> if called non-recursively.</param>
        /// <remarks>This traverses in indoor-to-outdoor order because warps and doors are not complete subsets of Game1.locations, which means there will be some rooms left out unless all the locations are iterated.</remarks>
        private void MapRootLocations(GameLocation startLocation, int curRecursionDepth)
        {
            string ScanRecursively(GameLocation location, GameLocation prevLocation, string root, bool hasOutdoorWarp, Vector2 warpPosition, ISet<string> seen, int depth)
            {
                // break infinite loops
                if (location == null || !seen.Add(location.NameOrUniqueName))
                    return root;
                if (depth > LocationUtil.MaxRecursionDepth)
                    throw new InvalidOperationException($"Infinite recursion detected in location scan. Technical details:\n{nameof(location)}: {location.NameOrUniqueName}\n{nameof(root)}: {root}\n{nameof(hasOutdoorWarp)}: {hasOutdoorWarp}\n{nameof(warpPosition)}: {warpPosition}\n{nameof(depth)}: {depth}\n\n{Environment.StackTrace}");

                // get location info
                string curLocationName = location.NameOrUniqueName;
                string prevLocationName = prevLocation?.NameOrUniqueName;

                // track contexts
                if (!this.LocationContexts.ContainsKey(curLocationName))
                    this.LocationContexts.Add(curLocationName, new LocationContext());
                if (prevLocation != null && !warpPosition.Equals(Vector2.Zero))
                {
                    this.LocationContexts[prevLocationName].Warp = warpPosition;
                    if (root != curLocationName)
                        this.LocationContexts[prevLocationName].Parent = curLocationName;
                }

                // pass root location back recursively
                if (root != null)
                {
                    this.LocationContexts[curLocationName].Root = root;
                    return root;
                }

                // root location found, set as root and return
                if (location.IsOutdoors)
                {
                    this.LocationContexts[curLocationName].Type = LocationType.Outdoors;
                    this.LocationContexts[curLocationName].Root = curLocationName;

                    if (prevLocation != null)
                    {
                        if (this.LocationContexts[curLocationName].Children == null)
                            this.LocationContexts[curLocationName].Children = new List<string> { prevLocationName };
                        else if (!this.LocationContexts[curLocationName].Children.Contains(prevLocationName))
                            this.LocationContexts[curLocationName].Children.Add(prevLocationName);
                    }

                    return curLocationName;
                }

                // recursively traverse warps from current location
                foreach (var warp in location.warps)
                {
                    // avoid circular loop
                    if (curLocationName == warp.TargetName || prevLocationName == warp.TargetName)
                        continue;

                    // get target location
                    var warpLocation = this.GetStaticLocation(warp.TargetName);
                    if (warpLocation == null)
                        continue;

                    // if one of the warps is a root location, current location is an indoor building
                    if (warpLocation.IsOutdoors)
                        hasOutdoorWarp = true;

                    // if all warps are indoors, then the current location is a room
                    this.LocationContexts[curLocationName].Type = hasOutdoorWarp ? LocationType.Building : LocationType.Room;

                    // update contexts
                    if (prevLocation != null)
                    {
                        this.LocationContexts[prevLocationName].Parent = curLocationName;

                        if (this.LocationContexts[curLocationName].Children == null)
                            this.LocationContexts[curLocationName].Children = new List<string> { prevLocationName };
                        else if (!this.LocationContexts[curLocationName].Children.Contains(prevLocationName))
                            this.LocationContexts[curLocationName].Children.Add(prevLocationName);
                    }
                    root = ScanRecursively(warpLocation, location, root, hasOutdoorWarp, new Vector2(warp.TargetX, warp.TargetY), seen, depth + 1);
                    this.LocationContexts[curLocationName].Root = root;

                    return root;
                }

                return root;
            }

            ScanRecursively(startLocation, null, null, false, Vector2.Zero, new HashSet<string>(), curRecursionDepth);
        }

        /// <summary>Get a location instance from its name if it's not a procedurally generated location.</summary>
        /// <param name="name">The location name.</param>
        private GameLocation GetStaticLocation(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return null;

                if (name.StartsWith("UndergroundMine") || (name.StartsWith("VolcanoDungeon") && name != "VolcanoDungeon0"))
                    return null;

                return Game1.getLocationFromName(name);
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"Failed loading location '{name}'. See the log file for technical details.", LogLevel.Error);
                this.Monitor.Log(ex.ToString());
                return null;
            }
        }
    }
}
