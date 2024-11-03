using System;
using System.Reflection;
using Bouhm.Shared.Integrations.GenericModConfigMenu;
using StardewModdingAPI;
using StardewValley;

namespace LocationCompass.Framework;

/// <summary>Registers the mod configuration with Generic Mod Config Menu.</summary>
internal class GenericModConfigMenuIntegration
{
    /*********
    ** Fields
    *********/
    /// <summary>The Location Compass manifest.</summary>
    private readonly IManifest Manifest;

    /// <summary>The current mod settings.</summary>
    private readonly ModConfig Config;

    /// <summary>The default mod settings.</summary>
    private readonly ModConfig DefaultConfig = new();

    /// <summary>Save the mod's current config to the <c>config.json</c> file.</summary>
    private readonly Action Save;

    /// <summary>The Generic Mod Config Menu integration.</summary>
    private readonly IGenericModConfigMenuApi ConfigMenu;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="config">The current mod settings.</param>
    /// <param name="manifest">The Location Compass manifest.</param>
    /// <param name="modRegistry">An API for fetching metadata about loaded mods.</param>
    /// <param name="save">Save the mod's current config to the <c>config.json</c> file.</param>
    public GenericModConfigMenuIntegration(ModConfig config, IManifest manifest, IModRegistry modRegistry, Action save)
    {
        this.Manifest = manifest;
        this.Config = config;
        this.Save = save;
        this.ConfigMenu = modRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
    }

    /// <summary>Register the config menu if available.</summary>
    public void Register()
    {
        var menu = this.ConfigMenu;
        if (menu is null)
            return;

        menu.Register(this.Manifest, this.Reset, this.Save);

        // options
        menu.AddSectionTitle(this.Manifest, I18n.Config_Title_Options);
        menu.AddBoolOption(
            this.Manifest,
            name: I18n.Config_HideHud_Name,
            tooltip: I18n.Config_HideHud_Desc,
            getValue: () => this.Config.HideHud,
            setValue: value => this.Config.HideHud = value
        );
        menu.AddBoolOption(
            this.Manifest,
            name: I18n.Config_SameLocationOnly_Name,
            tooltip: I18n.Config_SameLocationOnly_Desc,
            getValue: () => this.Config.SameLocationOnly,
            setValue: value => this.Config.SameLocationOnly = value
        );
        menu.AddBoolOption(
            this.Manifest,
            name: I18n.Config_QuestsAndBirthdaysOnly_Name,
            tooltip: I18n.Config_QuestsAndBirthdaysOnly_Desc,
            getValue: () => this.Config.ShowQuestsAndBirthdaysOnly,
            setValue: value => this.Config.ShowQuestsAndBirthdaysOnly = value
        );
        menu.AddBoolOption(
            this.Manifest,
            name: I18n.Config_PlayersOnly_Name,
            tooltip: I18n.Config_PlayersOnly_Desc,
            getValue: () => this.Config.ShowFarmersOnly,
            setValue: value => this.Config.ShowFarmersOnly = value
        );
        menu.AddBoolOption(
            this.Manifest,
            name: I18n.Config_ShowHorses_Name,
            tooltip: I18n.Config_ShowHorses_Desc,
            getValue: () => this.Config.ShowHorses,
            setValue: value => this.Config.ShowHorses = value
        );

        // controls
        menu.AddSectionTitle(this.Manifest, I18n.Config_Title_Controls);
        menu.AddBoolOption(
            this.Manifest,
            name: I18n.Config_HoldToToggle_Name,
            tooltip: I18n.Config_HoldToToggle_Desc,
            getValue: () => this.Config.HoldToToggle,
            setValue: value => this.Config.HoldToToggle = value
        );
        menu.AddKeybind(
            this.Manifest,
            name: I18n.Config_ToggleKey_Name,
            tooltip: I18n.Config_ToggleKey_Desc,
            getValue: () => this.GetButton(config => config.ToggleKeyCode),
            setValue: button => this.Config.ToggleKeyCode = button.ToString()
        );
        menu.AddKeybind(
            this.Manifest,
            name: I18n.Config_SameLocationKey_Name,
            tooltip: I18n.Config_SameLocationKey_Desc,
            getValue: () => this.GetButton(config => config.SameLocationToggleKey),
            setValue: button => this.Config.SameLocationToggleKey = button.ToString()
        );
        menu.AddKeybind(
            this.Manifest,
            name: I18n.Config_QuestsAndBirthdaysKey_Name,
            tooltip: I18n.Config_QuestsAndBirthdaysKey_Desc,
            getValue: () => this.GetButton(config => config.QuestsOnlyToggleKey),
            setValue: button => this.Config.QuestsOnlyToggleKey = button.ToString()
        );
        menu.AddKeybind(
            this.Manifest,
            name: I18n.Config_PlayersKey_Name,
            tooltip: I18n.Config_PlayersKey_Desc,
            getValue: () => this.GetButton(config => config.FarmersOnlyToggleKey),
            setValue: button => this.Config.FarmersOnlyToggleKey = button.ToString()
        );
        menu.AddKeybind(
            this.Manifest,
            name: I18n.Config_HorsesKey_Name,
            tooltip: I18n.Config_HorsesKey_Desc,
            getValue: () => this.GetButton(config => config.HorsesToggleKey),
            setValue: button => this.Config.HorsesToggleKey = button.ToString()
        );
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Reset the mod's config to its default values.</summary>
    private void Reset()
    {
        foreach (PropertyInfo property in this.Config.GetType().GetProperties())
        {
            property.SetValue(
                this.Config,
                property.GetValue(this.DefaultConfig)
            );
        }
    }

    /// <summary>Get a parsed key binding from the raw mod settings.</summary>
    /// <param name="getRawValue">Get the raw value from a settings model.</param>
    private SButton GetButton(Func<ModConfig, string> getRawValue)
    {
        return Utility.TryParseEnum(getRawValue(this.Config), out SButton button) || Utility.TryParseEnum(getRawValue(this.DefaultConfig), out button)
            ? button
            : SButton.None;
    }
}
