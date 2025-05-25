using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using StardewValley;
using StardewValley.Characters;

namespace NPCMapLocations.Framework;

/// <summary>Manages customized map recolors, NPCs, sprites, names, etc.</summary>
public class ModCustomizations
{
    /*********
    ** Fields
    *********/
    /// <summary>A callback to invoke when the settings are saved.</summary>
    private readonly Action OnSaved;


    /*********
    ** Accessors
    *********/
    /// <summary>The NPC translated or customized display names, indexed by their internal name.</summary>
    public Dictionary<string, string> Names { get; set; } = [];

    /// <summary>The locations to ignore when scanning locations for players and NPCs.</summary>
    /// <remarks>This removes the location from the location graph entirely. If a player is in an excluded location, NPC Map Locations will treat them as being in an unknown location.</remarks>
    public HashSet<string> LocationExclusions { get; } = [];


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="onSaved">A callback to invoke when the settings are saved.</param>
    public ModCustomizations(Action onSaved)
    {
        this.OnSaved = onSaved;
    }


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
        foreach ((string key, JObject location) in customLocationJson)
        {
            JToken? exclude = location.GetValue("Exclude");
            if (exclude != null && (bool)exclude)
                this.LocationExclusions.Add(key);
        }
    }

    /// <summary>Load NPC customizations received from other mods through the content pipeline.</summary>
    /// <param name="customNpcJson">The raw NPC asset data.</param>
    private void LoadCustomNpcs(Dictionary<string, JObject> customNpcJson)
    {
        // load custom NPC marker offsets and exclusions
        {
            // get defaults
            var markerOffsets = this.Merge(ModConstants.NpcMarkerOffsets, ModEntry.Config.NpcMarkerOffsets);

            // get custom data
            foreach ((string npcName, JObject npc) in customNpcJson)
            {
                JToken? exclude = npc.GetValue("Exclude");
                if (exclude != null && (bool)exclude)
                {
                    ModEntry.Config.ModNpcExclusions.Add(npcName);
                    continue;
                }

                JToken? markerCropOffset = npc.GetValue("MarkerCropOffset");
                if (markerCropOffset != null)
                    markerOffsets[npcName] = (int)markerCropOffset;
                else
                {
                    NPC gameNpc = Game1.getCharacterFromName(npcName);
                    if (gameNpc != null)
                    {
                        // If custom crop offset is not specified, default to 0
                        markerOffsets.TryAdd(gameNpc.Name, 0);

                        // Children sprites are short so give them a booster seat
                        if (gameNpc is Child)
                            markerOffsets[gameNpc.Name] += 7;
                    }
                }
            }

            // Merge customizations into globals config
            ModEntry.Config.NpcMarkerOffsets = markerOffsets;
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

        this.OnSaved();
    }

    /// <summary>Merge any number of dictionaries into a new dictionary.</summary>
    /// <typeparam name="TValue">The dictionary value type.</typeparam>
    /// <param name="dictionaries">The dictionaries to merge. Later dictionaries have precedence for conflicting keys.</param>
    /// <returns>Returns a new dictionary instance.</returns>
    private Dictionary<string, TValue> Merge<TValue>(params Dictionary<string, TValue>[] dictionaries)
    {
        Dictionary<string, TValue> merged = [];

        foreach (var dictionary in dictionaries)
        {
            foreach (var pair in dictionary)
                merged[pair.Key] = pair.Value;
        }

        return merged;
    }
}
