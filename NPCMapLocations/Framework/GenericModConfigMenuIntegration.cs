using System;
using Bouhm.Shared.Integrations.GenericModConfigMenu;
using NPCMapLocations.Framework.Models;
using StardewModdingAPI;
using StardewValley;

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

    /// <summary>The current mod settings.</summary>
    private GlobalConfig Config => ModEntry.Globals;

    /// <summary>The default mod settings.</summary>
    private readonly GlobalConfig DefaultConfig = new();


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="manifest">The NPC Map Locations manifest.</param>
    /// <param name="modRegistry">An API for fetching metadata about loaded mods.</param>
    public GenericModConfigMenuIntegration(IManifest manifest, IModRegistry modRegistry)
    {
        this.Manifest = manifest;
        this.ConfigMenu = modRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
    }

    /// <summary>Register the config menu if available.</summary>
    public void Register()
    {
        var menu = this.ConfigMenu;
        if (menu is null)
            return;

        menu.Register(this.Manifest, this.Reset, this.Save);

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

        menu.AddParagraph(
            this.Manifest,
            I18n.Config_OtherSettings
        );
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Reset the mod's config to its default values.</summary>
    private void Reset()
    {
        this.Config.MenuKey = this.DefaultConfig.MenuKey;
        this.Config.MinimapToggleKey = this.DefaultConfig.MinimapToggleKey;
        this.Config.TooltipKey = this.DefaultConfig.TooltipKey;
    }

    /// <summary>Save the mod's current config to the <c>config.json</c> file.</summary>
    private void Save()
    {
        ModEntry.StaticHelper.Data.WriteJsonFile("config/globals.json", this.Config);
    }

    /// <summary>Get a parsed key binding from the raw mod settings.</summary>
    /// <param name="getRawValue">Get the raw value from a settings model.</param>
    private SButton GetButton(Func<GlobalConfig, string> getRawValue)
    {
        return Utility.TryParseEnum(getRawValue(this.Config), out SButton button) || Utility.TryParseEnum(getRawValue(this.DefaultConfig), out button)
            ? button
            : SButton.None;
    }
}
