using System;
using System.Linq;
using Bouhm.Shared.Integrations.GenericModConfigMenu;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Characters;
using StardewValley.TokenizableStrings;

namespace NPCMapLocations.Framework;

/// <summary>Registers the mod configuration with Generic Mod Config Menu.</summary>
internal class GenericModConfigMenuIntegration
{
    /*********
    ** Fields
    *********/
    /// <summary>The NPC Map Locations manifest.</summary>
    private readonly IManifest Manifest;

    /// <summary>A callback to invoke when the settings are reset to default.</summary>
    private readonly Action OnReset;

    /// <summary>A callback to invoke when the settings are saved.</summary>
    private readonly Action OnSaved;

    /// <summary>The current mod settings.</summary>
    private ModConfig Config => ModEntry.Config;


    /*********
    ** Public methods
    *********/
    /// <summary>The Generic Mod Config Menu API.</summary>
    public IGenericModConfigMenuApi? ConfigMenu { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="manifest">The NPC Map Locations manifest.</param>
    /// <param name="modRegistry">An API for fetching metadata about loaded mods.</param>
    /// <param name="onReset">A callback to invoke when the settings are reset to default.</param>
    /// <param name="onSaved">A callback to invoke when the settings are saved.</param>
    public GenericModConfigMenuIntegration(IManifest manifest, IModRegistry modRegistry, Action onReset, Action onSaved)
    {
        this.Manifest = manifest;
        this.ConfigMenu = modRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        this.OnReset = onReset;
        this.OnSaved = onSaved;
    }

    /// <summary>Register the config menu if available.</summary>
    public void Register()
    {
        var menu = this.ConfigMenu;
        if (menu is null)
            return;

        menu.Register(this.Manifest, this.Reset, this.Save);

        // controls
        menu.AddSectionTitle(this.Manifest, text: I18n.Config_ControlsTitle);
        menu.AddKeybindList(
            this.Manifest,
            name: I18n.Config_MenuKey_Name,
            tooltip: I18n.Config_MenuKey_Desc,
            getValue: () => this.Config.MenuKey,
            setValue: value => this.Config.MenuKey = value
        );
        menu.AddKeybindList(
            this.Manifest,
            name: I18n.Config_MinimapKey_Name,
            tooltip: I18n.Config_MinimapKey_Desc,
            getValue: () => this.Config.MinimapToggleKey,
            setValue: value => this.Config.MinimapToggleKey = value
        );
        menu.AddKeybindList(
            this.Manifest,
            name: I18n.Config_TooltipKey_Name,
            tooltip: I18n.Config_TooltipKey_Desc,
            getValue: () => this.Config.TooltipKey,
            setValue: value => this.Config.TooltipKey = value
        );

        // minimap
        menu.AddSectionTitle(this.Manifest, text: I18n.Config_MinimapTitle);
        menu.AddBoolOption(
            this.Manifest,
            name: I18n.Config_MinimapEnabled_Name,
            tooltip: I18n.Config_MinimapEnabled_Desc,
            getValue: () => this.Config.ShowMinimap,
            setValue: value => this.Config.ShowMinimap = value
        );
        menu.AddBoolOption(
            this.Manifest,
            name: I18n.Config_LockMinimap_Name,
            tooltip: I18n.Config_LockMinimap_Desc,
            getValue: () => this.Config.LockMinimapPosition,
            setValue: value => this.Config.LockMinimapPosition = value
        );
        menu.AddNumberOption(
            this.Manifest,
            name: I18n.Config_MinimapWidth_Name,
            tooltip: I18n.Config_MinimapWidth_Desc,
            getValue: () => this.Config.MinimapWidth,
            setValue: value => this.Config.MinimapWidth = value,
            min: 45,
            max: 180,
            interval: 15,
            formatValue: value => I18n.Config_MinimapHeightOrWidth_Format(size: value)
        );
        menu.AddNumberOption(
            this.Manifest,
            name: I18n.Config_MinimapHeight_Name,
            tooltip: I18n.Config_MinimapHeight_Desc,
            getValue: () => this.Config.MinimapHeight,
            setValue: value => this.Config.MinimapHeight = value,
            min: 45,
            max: 180,
            interval: 15,
            formatValue: value => I18n.Config_MinimapHeightOrWidth_Format(size: value)
        );
        menu.AddNumberOption(
            this.Manifest,
            name: I18n.Config_MinimapOpacity_Name,
            tooltip: I18n.Config_MinimapOpacity_Desc,
            getValue: () => this.Config.MinimapOpacity,
            setValue: value => this.Config.MinimapOpacity = value,
            min: 0.05f,
            max: 1f,
            interval: 0.05f,
            formatValue: this.FormatPercent
        );

        // map display
        menu.AddSectionTitle(this.Manifest, I18n.Config_MapDisplayTitle);
        menu.AddNumberOption(
            this.Manifest,
            name: I18n.Config_NpcMarkerSize_Name,
            tooltip: I18n.Config_NpcMarkerSize_Desc,
            getValue: () => this.Config.NpcMarkerScale,
            setValue: value => this.Config.NpcMarkerScale = value,
            min: 0.1f,
            max: 4f,
            interval: 0.1f,
            formatValue: this.FormatRelativePercent
        );
        menu.AddNumberOption(
            this.Manifest,
            name: I18n.Config_CurrentPlayerMarkerSize_Name,
            tooltip: I18n.Config_CurrentPlayerMarkerSize_Desc,
            getValue: () => this.Config.CurrentPlayerMarkerScale,
            setValue: value => this.Config.CurrentPlayerMarkerScale = value,
            min: 0.1f,
            max: 4f,
            interval: 0.1f,
            formatValue: this.FormatRelativePercent
        );
        menu.AddNumberOption(
            this.Manifest,
            name: I18n.Config_OtherPlayerMarkerSize_Name,
            tooltip: I18n.Config_OtherPlayerMarkerSize_Desc,
            getValue: () => this.Config.OtherPlayerMarkerScale,
            setValue: value => this.Config.OtherPlayerMarkerScale = value,
            min: 0.1f,
            max: 4f,
            interval: 0.1f,
            formatValue: this.FormatRelativePercent
        );
        menu.AddBoolOption(
            this.Manifest,
            name: I18n.Config_ShowFarmBuildings_Name,
            tooltip: I18n.Config_ShowFarmBuildings_Name,
            getValue: () => this.Config.ShowFarmBuildings,
            setValue: value => this.Config.ShowFarmBuildings = value
        );

        // filters
        menu.AddSectionTitle(this.Manifest, I18n.Config_FiltersTitle);
        this.AddTriStateOption(
            name: I18n.Config_SpokenToFilter_Name,
            tooltip: I18n.Config_SpokenToFilter_Desc,
            getValue: () => this.Config.FilterNpcsSpokenTo,
            setValue: value => this.Config.FilterNpcsSpokenTo = value,
            formatAllowedValue: value => value switch
            {
                true => I18n.Config_SpokenToFilter_Options_TalkedTo(),
                false => I18n.Config_SpokenToFilter_Options_NotTalkedTo(),
                _ => I18n.Config_SpokenToFilter_Options_All()
            }
        );
        menu.AddBoolOption(
            this.Manifest,
            name: I18n.Config_OnlyInLocation_Name,
            tooltip: I18n.Config_OnlyInLocation_Desc,
            getValue: () => this.Config.OnlySameLocation,
            setValue: value => this.Config.OnlySameLocation = value
        );
        menu.AddNumberOption(
            this.Manifest,
            name: I18n.Config_MinHearts_Name,
            tooltip: I18n.Config_MinHearts_Desc,
            getValue: () => this.Config.HeartLevelMin,
            setValue: value => this.Config.HeartLevelMin = value,
            min: 0,
            max: ModConfig.MaxPossibleHeartLevel
        );
        menu.AddNumberOption(
            this.Manifest,
            name: I18n.Config_MaxHearts_Name,
            tooltip: I18n.Config_MaxHearts_Desc,
            getValue: () => this.Config.HeartLevelMax,
            setValue: value => this.Config.HeartLevelMax = value,
            min: 0,
            max: ModConfig.MaxPossibleHeartLevel
        );
        menu.AddBoolOption(
            this.Manifest,
            name: I18n.Config_ShowQuestsOrBirthdays_Name,
            tooltip: I18n.Config_ShowQuestsOrBirthdays_Desc,
            getValue: () => this.Config.ShowQuests,
            setValue: value => this.Config.ShowQuests = value
        );
        menu.AddBoolOption(
            this.Manifest,
            name: I18n.Config_ShowHiddenVillagers_Name,
            tooltip: I18n.Config_ShowHiddenVillagers_Desc,
            getValue: () => this.Config.ShowHiddenVillagers,
            setValue: value => this.Config.ShowHiddenVillagers = value
        );
        menu.AddBoolOption(
            this.Manifest,
            name: I18n.Config_ShowBookseller_Name,
            tooltip: I18n.Config_ShowBookseller_Desc,
            getValue: () => this.Config.ShowBookseller,
            setValue: value => this.Config.ShowBookseller = value
        );
        menu.AddBoolOption(
            this.Manifest,
            name: I18n.Config_ShowTravelingMerchant_Name,
            tooltip: I18n.Config_ShowTravelingMerchant_Desc,
            getValue: () => this.Config.ShowTravelingMerchant,
            setValue: value => this.Config.ShowTravelingMerchant = value
        );
        menu.AddBoolOption(
            this.Manifest,
            name: I18n.Config_ShowChildren_Name,
            tooltip: I18n.Config_ShowChildren_Desc,
            getValue: () => this.Config.ShowChildren,
            setValue: value => this.Config.ShowChildren = value
        );
        menu.AddBoolOption(
            this.Manifest,
            name: I18n.Config_ShowHorses_Name,
            tooltip: I18n.Config_ShowHorses_Desc,
            getValue: () => this.Config.ShowHorse,
            setValue: value => this.Config.ShowHorse = value
        );

        // Include/exclude villagers
        menu.AddSectionTitle(this.Manifest, I18n.Config_ToggleVillagersTitle);
        foreach ((string npcName, CharacterData data) in Game1.characterData.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (data is null || data.SocialTab == SocialTabBehavior.HiddenAlways || this.Config.ModNpcExclusions.Contains(npcName))
                continue;

            this.AddTriStateOption(
                name: () => TokenParser.ParseText(data.DisplayName),
                tooltip: () => I18n.Config_ToggleNpc_Desc(displayName: TokenParser.ParseText(data.DisplayName)),
                getValue: () => this.Config.NpcVisibility.TryGetValue(npcName, out bool value)
                    ? value
                    : null,
                setValue: value =>
                {
                    if (value is null)
                        this.Config.NpcVisibility.Remove(npcName);
                    else
                        this.Config.NpcVisibility[npcName] = value.Value;
                },
                formatAllowedValue: value => value switch
                {
                    true => I18n.Config_ToggleNpc_Options_AlwaysVisible(),
                    false => I18n.Config_ToggleNpc_Options_AlwaysHidden(),
                    null when this.Config.ModNpcExclusions.Contains(npcName) => I18n.Config_ToggleNpc_Options_DefaultHidden(),
                    _ => I18n.Config_ToggleNpc_Options_Default()
                }
            );
        }

        // performance
        menu.AddSectionTitle(this.Manifest, I18n.Config_PerformanceTitle);
        menu.AddNumberOption(
            this.Manifest,
            name: I18n.Config_MinimapCacheTime_Name,
            tooltip: I18n.Config_MinimapCacheTime_Desc,
            getValue: () => (int)this.Config.MiniMapCacheTicks,
            setValue: value => this.Config.MiniMapCacheTicks = (uint)value,
            min: 15,
            max: 600,
            interval: 15,
            formatValue: value => I18n.Config_CacheTime_Value(seconds: value / 60m)
        );
        menu.AddNumberOption(
            this.Manifest,
            name: I18n.Config_NpcCacheTime_Name,
            tooltip: I18n.Config_NpcCacheTime_Desc,
            getValue: () => (int)this.Config.NpcCacheTicks,
            setValue: value => this.Config.NpcCacheTicks = (uint)value,
            min: 15,
            max: 600,
            interval: 15,
            formatValue: value => I18n.Config_CacheTime_Value(seconds: value / 60m)
        );
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Add a config option with a tri-state boolean dropdown.</summary>
    /// <param name="name">The label text to show in the form.</param>
    /// <param name="tooltip">The tooltip text shown when the cursor hovers on the field, or <c>null</c> to disable the tooltip.</param>
    /// <param name="getValue">Get the current value from the mod config.</param>
    /// <param name="setValue">Set a new value in the mod config.</param>
    /// <param name="formatAllowedValue">Get the display text to show for a value.</param>

    private void AddTriStateOption(Func<string> name, Func<string> tooltip, Func<bool?> getValue, Action<bool?> setValue, Func<bool?, string> formatAllowedValue)
    {
        IGenericModConfigMenuApi menu = this.ConfigMenu!;

        menu.AddTextOption(
            this.Manifest,
            name: name,
            tooltip: tooltip,
            getValue: GetValue,
            setValue: SetValue,
            allowedValues: [string.Empty, bool.TrueString, bool.FalseString],
            formatAllowedValue: FormatValue
        );

        string GetValue()
        {
            bool? value = getValue();
            return value.HasValue
                ? value.Value.ToString()
                : string.Empty;
        }

        void SetValue(string rawValue)
        {
            bool? value = FromString(rawValue);
            setValue(value);
        }

        string FormatValue(string rawValue)
        {
            bool? value = FromString(rawValue);
            return formatAllowedValue(value);
        }

        bool? FromString(string rawValue)
        {
            return rawValue == string.Empty
                ? null
                : bool.Parse(rawValue);
        }
    }

    /// <summary>Get a formatted percentage to show in the config UI.</summary>
    /// <param name="value">The value to format.</param>
    private string FormatPercent(float value)
    {
        int percent = (int)(value * 100);
        return I18n.Config_Percentage(percent: percent);
    }

    /// <summary>Get a formatted percentage (or 'default' for 100%) to show in the config UI.</summary>
    /// <param name="value">The value to format.</param>
    private string FormatRelativePercent(float value)
    {
        return (decimal)value == 1m
            ? I18n.Config_Percentage_Default()
            : this.FormatPercent(value);
    }

    /// <summary>Reset the mod's config to its default values.</summary>
    private void Reset()
    {
        this.OnReset();
    }

    /// <summary>Save the mod's current config to the <c>config.json</c> file.</summary>
    private void Save()
    {
        this.OnSaved();
    }
}
