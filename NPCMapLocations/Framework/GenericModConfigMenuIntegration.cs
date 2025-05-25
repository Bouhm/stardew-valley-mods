using System;
using System.Linq;
using Bouhm.Shared.Integrations.GenericModConfigMenu;
using NPCMapLocations.Framework.Models;
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

    /// <summary>The Generic Mod Config Menu integration.</summary>
    private readonly IGenericModConfigMenuApi ConfigMenu;

    /// <summary>A callback to invoke when the settings are reset to default.</summary>
    private readonly Action OnReset;

    /// <summary>A callback to invoke when the settings are saved.</summary>
    private readonly Action OnSaved;

    /// <summary>The current mod settings.</summary>
    private ModConfig Config => ModEntry.Config;


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
        menu.AddKeybind(
            this.Manifest,
            name: I18n.Config_MenuKey_Name,
            tooltip: I18n.Config_MenuKey_Desc,
            getValue: () => this.GetButton(config => config.MenuKey),
            setValue: button => this.Config.MenuKey = button.ToString()
        );
        menu.AddKeybind(
            this.Manifest,
            name: I18n.Config_MinimapKey_Name,
            tooltip: I18n.Config_MinimapKey_Desc,
            getValue: () => this.GetButton(config => config.MinimapToggleKey),
            setValue: button => this.Config.MinimapToggleKey = button.ToString()
        );
        menu.AddKeybind(
            this.Manifest,
            name: I18n.Config_TooltipKey_Name,
            tooltip: I18n.Config_TooltipKey_Desc,
            getValue: () => this.GetButton(config => config.TooltipKey),
            setValue: button => this.Config.TooltipKey = button.ToString()
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

        // settings
        menu.AddSectionTitle(this.Manifest, I18n.Config_SettingsTitle);
        menu.AddTextOption(
            this.Manifest,
            name: I18n.Config_Immersion_Name,
            tooltip: I18n.Config_Immersion_Desc,
            getValue: () => this.Config.ImmersionOption.ToString(),
            setValue: value => this.Config.ImmersionOption = Utility.TryParseEnum(value, out VillagerVisibility parsed) ? parsed : VillagerVisibility.All,
            allowedValues: Enum.GetNames<VillagerVisibility>(),
            formatAllowedValue: value => I18n.GetByKey($"config.immersion.options.{value}")
        );
        menu.AddBoolOption(
            this.Manifest,
            name: I18n.Config_OnlyInLocation_Name,
            tooltip: I18n.Config_OnlyInLocation_Desc,
            getValue: () => this.Config.OnlySameLocation,
            setValue: value => this.Config.OnlySameLocation = value
        );
        menu.AddBoolOption(
            this.Manifest,
            name: I18n.Config_OnlyHeartLevel_Name,
            tooltip: () => I18n.Config_OnlyHeartLevel_Desc(minHeartsName: I18n.Config_MinHearts_Name(), maxHeartsName: I18n.Config_MaxHearts_Name()),
            getValue: () => this.Config.ByHeartLevel,
            setValue: value => this.Config.ByHeartLevel = value
        );
        menu.AddNumberOption(
            this.Manifest,
            name: I18n.Config_MinHearts_Name,
            tooltip: () => I18n.Config_MinHearts_Desc(onlyHeartLevelName: I18n.Config_OnlyHeartLevel_Name()),
            getValue: () => this.Config.HeartLevelMin,
            setValue: value => this.Config.HeartLevelMin = value,
            min: 0,
            max: ModConfig.MaxPossibleHeartLevel
        );
        menu.AddNumberOption(
            this.Manifest,
            name: I18n.Config_MaxHearts_Name,
            tooltip: () => I18n.Config_MaxHearts_Desc(onlyHeartLevelName: I18n.Config_OnlyHeartLevel_Name()),
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
            name: I18n.Config_ShowTravelingMerchant_Name,
            tooltip: I18n.Config_ShowTravelingMerchant_Desc,
            getValue: () => this.Config.ShowTravelingMerchant,
            setValue: value => this.Config.ShowTravelingMerchant = value
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

            menu.AddTextOption(
                this.Manifest,
                name: () => TokenParser.ParseText(data.DisplayName),
                tooltip: () => I18n.Config_ToggleNpc_Desc(displayName: TokenParser.ParseText(data.DisplayName)),
                getValue: () => this.Config.NpcVisibility.TryGetValue(npcName, out bool value)
                    ? value.ToString()
                    : string.Empty,
                setValue: value =>
                {
                    if (value == string.Empty)
                        this.Config.NpcVisibility.Remove(npcName);
                    else
                        this.Config.NpcVisibility[npcName] = bool.Parse(value);
                },
                allowedValues: [true.ToString(), false.ToString(), string.Empty],
                formatAllowedValue: value =>
                {
                    if (value == string.Empty)
                    {
                        return this.Config.ModNpcExclusions.Contains(npcName)
                            ? I18n.Config_ToggleNpc_Options_DefaultHidden()
                            : I18n.Config_ToggleNpc_Options_Default();
                    }

                    return I18n.GetByKey($"config.toggle-npc.options.{value}");
                }
            );
        }
    }


    /*********
    ** Private methods
    *********/
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

    /// <summary>Get a parsed key binding from the raw mod settings.</summary>
    /// <param name="getRawValue">Get the raw value from a settings model.</param>
    private SButton GetButton(Func<ModConfig, string> getRawValue)
    {
        return Utility.TryParseEnum(getRawValue(this.Config), out SButton button)
            ? button
            : SButton.None;
    }
}
