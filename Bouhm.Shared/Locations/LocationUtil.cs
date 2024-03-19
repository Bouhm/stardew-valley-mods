using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;

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

        public Dictionary<string, LocationContext> ScanLocationContexts()
        {
            foreach (var location in Game1.locations)
            {
                // Get outdoor neighbors
                if (location.IsOutdoors)
                {
                    if (!this.TryGetContext(location.Name, out var context))
                        this.LocationContexts[location.Name] = context = new LocationContext(location) { Root = location.Name, Type = LocationType.Outdoors };

                    foreach (var warp in this.GetOutgoingWarpsForScanning(location))
                    {
                        GameLocation warpLocation = this.GetStaticLocation(warp?.TargetName);
                        if (warpLocation?.IsOutdoors == true)
                        {
                            if (!context.Neighbors.ContainsKey(warp.TargetName))
                                context.Neighbors.Add(warp.TargetName, new Vector2(warp.X, warp.Y));
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
            string GetRecursively(string locationName, ISet<string> seen, int depth)
            {
                // break infinite loops
                if (!seen.Add(locationName))
                    return locationName;
                if (depth > LocationUtil.MaxRecursionDepth)
                    throw new InvalidOperationException($"Infinite recursion detected in location scan. Technical details:\n{nameof(locationName)}: {locationName}\n{nameof(depth)}: {depth}\n\n{Environment.StackTrace}");

                // handle generated levels
                {
                    string mineName = this.GetLocationNameFromLevel(locationName);
                    if (mineName != null)
                        return mineName;
                }

                // found root building
                var context = this.TryGetContext(locationName);
                if (context?.Type == LocationType.Building)
                    return locationName;
                string building = context?.Parent;
                if (building == null)
                    return null;
                if (building == context?.Root)
                    return locationName;

                // scan recursively
                return GetRecursively(building, seen, depth + 1);
            }

            return GetRecursively(startLocationName, new HashSet<string>(), curRecursionDepth);
        }

        /// <summary>Get the context metadata for a location, if known.</summary>
        /// <param name="locationName">The location name.</param>
        /// <param name="mapGeneratedLevels">Whether to automatically map mine levels to their static entrance location.</param>
        /// <returns>Returns the context if found, else <c>null</c>.</returns>
        public LocationContext TryGetContext(string locationName, bool mapGeneratedLevels = true)
        {
            if (mapGeneratedLevels)
                locationName = this.GetLocationNameFromLevel(locationName) ?? locationName;

            if (string.IsNullOrWhiteSpace(locationName))
                return null;

            return this.LocationContexts.TryGetValue(locationName, out LocationContext context)
                ? context
                : null;
        }

        /// <summary>Get the context metadata for a location, if known.</summary>
        /// <param name="locationName">The location name.</param>
        /// <param name="context">The location context, if found.</param>
        /// <returns>Returns whether the context was found.</returns>
        public bool TryGetContext(string locationName, out LocationContext context)
        {
            context = this.TryGetContext(locationName);
            return context != null;
        }

        /// <summary>Get the name of the static entry location for a generated mine or dungeon level, if applicable.</summary>
        /// <param name="locationName">The actual location name, like <c>UndergroundMine35</c>.</param>
        /// <returns>Returns <c>Mine</c>, <c>SkullCave</c>, <c>VolcanoDungeon</c>, or <c>null</c>.</returns>
        public string GetLocationNameFromLevel(string locationName)
        {
            const string minePrefix = "UndergroundMine";
            const string volcanoPrefix = "VolcanoDungeon";

            // mines or skull cavern
            if (locationName?.StartsWith(minePrefix) == true)
            {
                string rawLevel = locationName.Substring(minePrefix.Length, locationName.Length - minePrefix.Length);
                if (int.TryParse(rawLevel, out int mineLevel))
                {
                    return mineLevel > 120 && mineLevel != MineShaft.quarryMineShaft
                        ? "SkullCave"
                        : "Mine";
                }
            }

            // volcano dungeon
            if (locationName == "Caldera" || locationName?.StartsWith(volcanoPrefix) == true)
                return "VolcanoDungeon0";

            // not a generated level
            return null;
        }

        /// <summary>Get whether a location is outdoors, if known.</summary>
        /// <param name="locationName">The location name.</param>
        public bool IsOutdoors(string locationName)
        {
            return this.TryGetContext(locationName)?.Type == LocationType.Outdoors;
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
                LocationContext prevContext = prevLocationName != null
                    ? this.LocationContexts[prevLocationName]
                    : null;

                // track contexts
                if (!this.TryGetContext(curLocationName, out var context))
                    this.LocationContexts[curLocationName] = context = new LocationContext(location);

                if (prevContext != null && !warpPosition.Equals(Vector2.Zero))
                {
                    prevContext.Warp = warpPosition;
                    if (root != curLocationName)
                        prevContext.Parent = curLocationName;
                }

                // pass root location back recursively
                if (root != null)
                {
                    context.Root = root;
                    return root;
                }

                // root location found, set as root and return
                if (location.IsOutdoors)
                {
                    context.Type = LocationType.Outdoors;
                    context.Root = curLocationName;

                    if (prevLocation != null)
                    {
                        if (!context.Children.Contains(prevLocationName))
                            context.Children.Add(prevLocationName);
                    }

                    return curLocationName;
                }

                // recursively traverse warps from current location
                foreach (var warp in this.GetOutgoingWarpsForScanning(location))
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
                    context.Type = hasOutdoorWarp ? LocationType.Building : LocationType.Room;

                    // update contexts
                    if (prevContext != null)
                    {
                        prevContext.Parent = curLocationName;

                        if (!context.Children.Contains(prevLocationName))
                            context.Children.Add(prevLocationName);
                    }
                    root = ScanRecursively(warpLocation, location, root, hasOutdoorWarp, new Vector2(warp.TargetX, warp.TargetY), seen, depth + 1);
                    context.Root = root;

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
                if (string.IsNullOrWhiteSpace(name) || this.GetLocationNameFromLevel(name) != null)
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

        /// <summary>Get the outgoing warps for the purposes of location scanning in <see cref="MapRootLocations"/>.</summary>
        /// <param name="location">The location whose warps to get.</param>
        private IEnumerable<Warp> GetOutgoingWarpsForScanning(GameLocation location)
        {
            // special case: Caldera is separated from its root location by the generated Volcano
            // Dungeon levels, which will be ignored.
            if (location is Caldera)
            {
                var entrance = Game1.getLocationFromName("VolcanoDungeon0");
                var exitWarp = entrance?.warps.FirstOrDefault(p => p.TargetName == "IslandNorth");
                if (exitWarp != null)
                    return new[] { exitWarp };
            }

            // normal case
            return location.warps;
        }
    }
}
