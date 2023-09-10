using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NPCMapLocations.Framework.Models;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;

namespace NPCMapLocations.Framework
{
    /// <summary>Manages customized map recolors, NPCs, sprites, names, etc.</summary>
    public class ModCustomizations
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The in-world tile coordinates and map pixels which represent the same position, indexed by location name.</summary>
        /// <remarks>These are used to map any in-game tile coordinate to the map by measuring the distance between the two closest map vectors.</remarks>
        public Dictionary<string, MapVector[]> MapVectors { get; set; } = new();

        /// <summary>The NPC translated or customized display names, indexed by their internal name.</summary>
        public Dictionary<string, string> Names { get; set; } = new();

        /// <summary>The locations to ignore when scanning locations for players and NPCs.</summary>
        /// <remarks>This removes the location from the location graph entirely. If a player is in an excluded location, NPC Map Locations will treat them as being in an unknown location.</remarks>
        public HashSet<string> LocationExclusions { get; set; } = new();


        /*********
        ** Public methods
        *********/
        /// <summary>Load customizations received from other mods through the content pipeline.</summary>
        /// <param name="customNpcJson">The custom NPC data.</param>
        /// <param name="customLocationJson">The custom location data.</param>
        public void LoadCustomData(Dictionary<string, JObject> customNpcJson, Dictionary<string, JObject> customLocationJson)
        {
            this.LoadCustomLocations(customLocationJson);
            this.LoadCustomNpcs(customNpcJson);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Load location customizations received from other mods through the content pipeline.</summary>
        /// <param name="customLocationJson">The raw location asset data.</param>
        private void LoadCustomLocations(Dictionary<string, JObject> customLocationJson)
        {
            foreach (var locationData in customLocationJson)
            {
                JObject location = locationData.Value;

                if (location.ContainsKey("MapVectors"))
                    this.AddCustomMapLocation(locationData.Key, (JArray)location.GetValue("MapVectors"));

                if (location.ContainsKey("Exclude") && (bool)location.GetValue("Exclude"))
                    this.LocationExclusions.Add(locationData.Key);
            }
        }

        /// <summary>Load NPC customizations received from other mods through the content pipeline.</summary>
        /// <param name="customNpcJson">The raw NPC asset data.</param>
        private void LoadCustomNpcs(Dictionary<string, JObject> customNpcJson)
        {
            // load custom NPC marker offsets and exclusions
            {
                // get defaults
                var markerOffsets = this.Merge(ModConstants.NpcMarkerOffsets, ModEntry.Globals.NpcMarkerOffsets);

                // get custom data
                foreach (var npcData in customNpcJson)
                {
                    var npc = npcData.Value;

                    if (npc.ContainsKey("Exclude") && (bool)npc.GetValue("Exclude"))
                    {
                        ModEntry.Globals.ModNpcExclusions.Add(npcData.Key);
                        continue;
                    }

                    if (npc.ContainsKey("MarkerCropOffset"))
                        markerOffsets[npcData.Key] = (int)npc.GetValue("MarkerCropOffset");
                    else
                    {
                        NPC gameNpc = Game1.getCharacterFromName(npcData.Key);
                        if (gameNpc != null)
                        {
                            // If custom crop offset is not specified, default to 0
                            if (!markerOffsets.ContainsKey(gameNpc.Name))
                                markerOffsets[gameNpc.Name] = 0;

                            // Children sprites are short so give them a booster seat
                            if (gameNpc is Child)
                                markerOffsets[gameNpc.Name] += 7;
                        }
                    }
                }

                // Merge customizations into globals config
                ModEntry.Globals.NpcMarkerOffsets = markerOffsets;
            }

            foreach (var character in Utility.getAllCharacters())
            {
                // Handle any modified NPC names 
                // Specifically mods that change names in dialogue files (displayName)
                this.Names[character.Name] = character.displayName ?? character.Name;
            }

            // Handle duplicate displayName -- custom NPCs that replaces villagers
            Dictionary<string, string> dupes = this.Names
              .Where(n1 => this.Names.Any(n2 => n2.Key != n1.Key && n2.Value == n1.Value))
              .ToDictionary(n => n.Key, n => n.Value);

            // Properly replace the villager with custom NPC
            foreach (var dupe in dupes)
            {
                if (dupe.Key != dupe.Value)
                {
                    this.Names[dupe.Key] = dupe.Value;
                }
                else
                {
                    this.Names.Remove(dupe.Key);
                }
            }

            ModEntry.StaticHelper.Data.WriteJsonFile($"config/{Constants.SaveFolderName}.json", ModEntry.Config);
            ModEntry.StaticHelper.Data.WriteJsonFile("config/globals.json", ModEntry.Globals);
        }

        /// <summary>Override the map vectors for a custom location. See <see cref="MapVectors"/> for details.</summary>
        /// <param name="locationName">The name of the location for which to add vectors.</param>
        /// <param name="mapLocations">The array of custom vectors.</param>
        private void AddCustomMapLocation(string locationName, JArray mapLocations)
        {
            var rawVectors = mapLocations.ToObject<JObject[]>();
            var parsedVectors = new MapVector[rawVectors.Length];
            for (int i = 0; i < rawVectors.Length; i++)
            {
                JObject rawVector = rawVectors[i];

                // Marker doesn't need to specify corresponding Tile position
                if (rawVector.GetValue("TileX") == null || rawVector.GetValue("TileY") == null)
                {
                    parsedVectors[i] = new MapVector(
                        (int)rawVector.GetValue("MapX"),
                        (int)rawVector.GetValue("MapY")
                    );
                }
                // Region must specify corresponding Tile positions for
                // Calculations on movement within location
                else
                {
                    parsedVectors[i] = new MapVector(
                        (int)rawVector.GetValue("MapX"),
                        (int)rawVector.GetValue("MapY"),
                        (int)rawVector.GetValue("TileX"),
                        (int)rawVector.GetValue("TileY")
                    );
                }
            }

            this.MapVectors[locationName] = parsedVectors;
        }

        /// <summary>Merge any number of dictionaries into a new dictionary.</summary>
        /// <typeparam name="TValue">The dictionary value type.</typeparam>
        /// <param name="dictionaries">The dictionaries to merge. Later dictionaries have precedence for conflicting keys.</param>
        /// <returns>Returns a new dictionary instance.</returns>
        private Dictionary<string, TValue> Merge<TValue>(params Dictionary<string, TValue>[] dictionaries)
        {
            Dictionary<string, TValue> merged = new();

            foreach (var dictionary in dictionaries)
            {
                foreach (var pair in dictionary)
                    merged[pair.Key] = pair.Value;
            }

            return merged;
        }
    }
}
