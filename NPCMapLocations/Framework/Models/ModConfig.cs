﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace NPCMapLocations.Framework.Models;

/// <summary>The data model for the mod settings.</summary>
public class ModConfig
{
    /*********
    ** Accessors
    *********/
    /// <summary>The maximum heart level that can be configured for <see cref="HeartLevelMin"/> or <see cref="HeartLevelMax"/>.</summary>
    internal const int MaxPossibleHeartLevel = 14;

    /// <summary>The key binding to open the options menu when on the map view.</summary>
    public string MenuKey { get; set; } = "Tab";

    /// <summary>The key binding to cycle the tooltip position when on the map view.</summary>
    public string TooltipKey { get; set; } = "Space";

    /// <summary>The number of ticks between each minimap update.</summary>
    public uint MiniMapCacheTicks { get; set; } = 15;

    /// <summary>The number of ticks between each update to NPC markers.</summary>
    public uint NpcCacheTicks { get; set; } = 30;

    /// <summary>The tooltip position when pointing at something on the map.</summary>
    public int NameTooltipMode { get; set; } = 1;

    /// <summary>Whether to only show villagers in the same location as the player.</summary>
    public bool OnlySameLocation { get; set; } = false;

    /// <summary>Which NPCs to display.</summary>
    public VillagerVisibility ImmersionOption { get; set; } = VillagerVisibility.All;

    /// <summary>Whether to only show villagers whose hearts with the player is between <see cref="HeartLevelMin"/> and <see cref="HeartLevelMax"/>.</summary>
    public bool ByHeartLevel { get; set; } = false;

    /// <summary>The minimum heart level for <see cref="ByHeartLevel"/>.</summary>
    public int HeartLevelMin { get; set; } = 0;

    /// <summary>The maximum heart level for <see cref="ByHeartLevel"/>.</summary>
    public int HeartLevelMax { get; set; } = MaxPossibleHeartLevel;

    /// <summary>Whether to show villagers that would normally be hidden. </summary>
    public bool ShowHiddenVillagers { get; set; } = false;

    /// <summary>Whether to mark NPCs with quests or birthdays today.</summary>
    public bool ShowQuests { get; set; } = true;

    /// <summary>Whether to show the bookseller when he's in town.</summary>
    public bool ShowBookseller { get; set; } = true;

    /// <summary>Whether to show the Traveling Merchant when she's in the forest.</summary>
    public bool ShowTravelingMerchant { get; set; } = true;

    /// <summary>Whether to show the floating minimap.</summary>
    public bool ShowMinimap { get; set; } = false;

    /// <summary>Whether to lock the minimap position, so the player can't right-click to drag it.</summary>
    public bool LockMinimapPosition { get; set; } = false;

    /// <summary>Whether to show farm buildings on the map.</summary>
    public bool ShowFarmBuildings { get; set; } = true;

    /// <summary>The key binding to toggle the floating minimap.</summary>
    public string MinimapToggleKey { get; set; } = "OemPipe";

    /// <summary>The minimap's pixel X position on screen.</summary>
    public int MinimapX { get; set; } = 12;

    /// <summary>The minimap's pixel Y position on screen.</summary>
    public int MinimapY { get; set; } = 12;

    /// <summary>The minimap's pixel width on screen.</summary>
    public int MinimapWidth { get; set; } = 75;

    /// <summary>The minimap's pixel height on screen.</summary>
    public int MinimapHeight { get; set; } = 45;

    /// <summary>Location names in which the minimap should be disabled.</summary>
    public HashSet<string> MinimapExclusions { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Whether to show player children on the map.</summary>
    public bool ShowChildren { get; set; } = false;

    /// <summary>Whether to show horses on the map.</summary>
    public bool ShowHorse { get; set; } = true;

    /// <summary>NPC names to hide from the map.</summary>
    public HashSet<string> NpcExclusions { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>NPC names to hide from the map, specified by mods through the API.</summary>
    [JsonIgnore]
    public HashSet<string> ModNpcExclusions { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Custom offsets when drawing vanilla NPCs.</summary>
    public Dictionary<string, int> NpcMarkerOffsets { get; set; } = new(StringComparer.OrdinalIgnoreCase);


    /*********
    ** Public methods
    *********/
    // <summary>Normalize the model after it's deserialized.</summary>
    /// <param name="context">The deserialization context.</param>
    [OnDeserialized]
    internal void OnDeserializedMethod(StreamingContext context)
    {
        // make values case-insensitive
        this.MinimapExclusions = new HashSet<string>(this.MinimapExclusions ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        this.NpcExclusions = new HashSet<string>(this.NpcExclusions ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        this.NpcMarkerOffsets = new Dictionary<string, int>(this.NpcMarkerOffsets ?? new(), StringComparer.OrdinalIgnoreCase);
    }
}
