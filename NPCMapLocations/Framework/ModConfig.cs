using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace NPCMapLocations.Framework;

/// <summary>The data model for the mod settings.</summary>
public class ModConfig
{
    /*********
    ** Accessors
    *********/
    /****
    ** Constants
    ****/
    /// <summary>The maximum heart level that can be configured for <see cref="HeartLevelMin"/> or <see cref="HeartLevelMax"/>.</summary>
    internal const int MaxPossibleHeartLevel = 14;

    /****
    ** Controls
    ****/
    /// <summary>The key binding to open the options menu when on the map view.</summary>
    public KeybindList MenuKey { get; set; } = new(SButton.Tab);

    /// <summary>The key binding to toggle the floating minimap.</summary>
    public KeybindList MinimapToggleKey { get; set; } = new(SButton.OemPipe);

    /// <summary>The key binding to cycle the tooltip position when on the map view.</summary>
    public KeybindList TooltipKey { get; set; } = new(SButton.Space);

    /****
    ** Minimap
    ****/
    /// <summary>Whether to show the floating minimap.</summary>
    public bool ShowMinimap { get; set; } = false;

    /// <summary>Whether to lock the minimap position, so the player can't right-click to drag it.</summary>
    public bool LockMinimapPosition { get; set; } = false;

    /// <summary>The minimap's pixel X position on screen.</summary>
    public int MinimapX { get; set; } = 12;

    /// <summary>The minimap's pixel Y position on screen.</summary>
    public int MinimapY { get; set; } = 12;

    /// <summary>The minimap's pixel width on screen.</summary>
    public int MinimapWidth { get; set; } = 75;

    /// <summary>The minimap's pixel height on screen.</summary>
    public int MinimapHeight { get; set; } = 45;

    /// <summary>The default transparency for the minimap, as a value between 0 (invisible) and 1 (opaque).</summary>
    public float MinimapOpacity { get; set; } = 1f;

    /// <summary>Location names in which the minimap should be disabled.</summary>
    public HashSet<string> MinimapExclusions { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /****
    ** Map display
    ****/
    /// <summary>The icon marker style to draw.</summary>
    public NpcIconStyle NpcIconStyle = NpcIconStyle.Default;

    /// <summary>The tooltip position when pointing at something on the map.</summary>
    public int NameTooltipMode { get; set; } = 1;

    /// <summary>A multiplier to apply to NPC marker sizes on the map, where 1 is the default size.</summary>
    public float NpcMarkerScale { get; set; } = 1f;

    /// <summary>A multiplier to apply to the current player's marker size on the map, where 1 is the default size.</summary>
    public float CurrentPlayerMarkerScale { get; set; } = 1f;

    /// <summary>A multiplier to apply to other players' marker sizes on the map, where 1 is the default size.</summary>
    public float OtherPlayerMarkerScale { get; set; } = 1f;

    /****
    ** Filters
    ****/
    /// <summary>An NPC filter based on whether the player has spoken to each NPC (<c>true</c> for only villagers spoken to, <c>false</c> for only those not spoken to, or <c>null</c> for all NPCs).</summary>
    public bool? FilterNpcsSpokenTo { get; set; }

    /// <summary>Whether to only show villagers in the same location as the player.</summary>
    public bool OnlySameLocation { get; set; } = false;

    /// <summary>The minimum heart level for which to show NPCs.</summary>
    public int HeartLevelMin { get; set; } = 0;

    /// <summary>The maximum heart level for which to show NPCs.</summary>
    public int HeartLevelMax { get; set; } = MaxPossibleHeartLevel;

    /// <summary>Whether to mark NPCs with quests or birthdays today.</summary>
    public bool ShowQuests { get; set; } = true;

    /// <summary>Whether to show villagers that would normally be hidden. </summary>
    public bool ShowHiddenVillagers { get; set; } = false;

    /// <summary>Whether to show the bookseller when he's in town.</summary>
    public bool ShowBookseller { get; set; } = true;

    /// <summary>Whether to show the Traveling Merchant when she's in the forest.</summary>
    public bool ShowTravelingMerchant { get; set; } = true;

    /// <summary>Whether to show horses on the map.</summary>
    public bool ShowHorse { get; set; } = true;

    /// <summary>Whether to show player children on the map.</summary>
    public bool ShowChildren { get; set; } = false;

    /// <summary>Whether to show farm buildings on the map.</summary>
    public bool ShowFarmBuildings { get; set; } = true;

    /****
    ** Advanced
    ****/
    /// <summary>Override the visibility for specific NPCs.</summary>
    public Dictionary<string, bool> NpcVisibility { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Custom offsets when drawing vanilla NPCs.</summary>
    public Dictionary<string, int> NpcMarkerOffsets { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The number of ticks between each minimap update.</summary>
    public uint MiniMapCacheTicks { get; set; } = 15;

    /// <summary>The number of ticks between each update to NPC markers.</summary>
    public uint NpcCacheTicks { get; set; } = 30;

    /****
    ** Internal
    ****/
    /// <summary>NPC names to hide from the map, specified by mods through the API.</summary>
    [JsonIgnore]
    public HashSet<string> ModNpcExclusions { get; } = new(StringComparer.OrdinalIgnoreCase);


    /*********
    ** Public methods
    *********/
    // <summary>Normalize the model after it's deserialized.</summary>
    /// <param name="context">The deserialization context.</param>
    [OnDeserialized]
    internal void OnDeserialized(StreamingContext context)
    {
        // make values case-insensitive and non-nullable
        this.MenuKey ??= new KeybindList();
        this.MinimapToggleKey ??= new KeybindList();
        this.TooltipKey ??= new KeybindList();
        this.MinimapExclusions = new HashSet<string>(this.MinimapExclusions ?? [], StringComparer.OrdinalIgnoreCase);
        this.NpcVisibility = new Dictionary<string, bool>(this.NpcVisibility ?? [], StringComparer.OrdinalIgnoreCase);
        this.NpcMarkerOffsets = new Dictionary<string, int>(this.NpcMarkerOffsets ?? [], StringComparer.OrdinalIgnoreCase);
    }
}
