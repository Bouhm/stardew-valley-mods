#nullable enable

using System;
using Bouhm.Shared.Integrations.BetterGameMenu;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley.Menus;

namespace NPCMapLocations.Framework;

/// <summary>Registers the <see cref="MapPage"/> provider with Better Game Menu.</summary>
internal class BetterGameMenuIntegration
{
    /*********
    ** Fields
    *********/
    /// <summary>Creates an instance of the NPC Map Locations map page.</summary>
    private readonly Func<IClickableMenu, IClickableMenu> CreateMapPageImpl;

    /// <summary>Whether the menu is active.</summary>
    private readonly PerScreen<bool> MenuIsOpen = new();

    /// <summary>Whether the menu should be closed.</summary>
    /// <remarks>This is used on resize since the previous instance's <see cref="OnClosed"/> is fired after <see cref="OnResized"/> completes, and we don't want to end up with a false menu closed state.</remarks>
    private readonly PerScreen<bool> MenuShouldClose = new();


    /*********
    ** Accessors
    *********/
    /// <summary>Whether the current screen has an open menu.</summary>
    public bool IsMenuOpen => this.MenuIsOpen.Value;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="createMapPage">Creates an instance of the NPC Map Locations map page.</param>
    public BetterGameMenuIntegration(Func<IClickableMenu, IClickableMenu> createMapPage)
    {
        this.CreateMapPageImpl = createMapPage;
    }

    /// <summary>Register the integration with Better Game Menu.</summary>
    /// <param name="modRegistry">An API for fetching APIs from loaded mods.</param>
    /// <param name="monitor">An API for logging messages.</param>
    /// <returns>Returns this instance for chaining.</returns>
    public BetterGameMenuIntegration Register(IModRegistry modRegistry, IMonitor monitor)
    {
        try
        {
            var api = modRegistry.GetApi<IBetterGameMenuApi>("leclair.bettergamemenu");
            api?.RegisterImplementation(
                "Map",
                priority: 100,
                getPageInstance: this.CreateMapPage,
                getMenuInvisible: () => true,
                getWidth: width => width + 128,
                onResize: this.OnResized,
                onClose: this.OnClosed
            );
        }
        catch (Exception ex)
        {
            monitor.Log($"Unable to integrate with Better Game Menu.\nTechnical details:\n{ex}", LogLevel.Warn);
        }

        return this;
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Handle the game window being resized while the map page is displayed.</summary>
    /// <param name="input">The open game menu and map page.</param>
    /// <returns>Returns the new map page to display.</returns>
    private IClickableMenu OnResized((IClickableMenu Menu, IClickableMenu OldPage) input)
    {
        this.MenuShouldClose.Value = true;

        return this.CreateMapPage(input.Menu);
    }

    /// <summary>Handle the map page being closed.</summary>
    /// <param name="menu">The map page which was closed.</param>
    private void OnClosed(IClickableMenu menu)
    {
        // When OnResized is called, a new instance is created before the old
        // instance is closed. As such, we use MenuShouldClose as a guard to
        // keep MenuIsOpen's value correct.
        if (this.MenuShouldClose.Value)
            this.MenuShouldClose.Value = false;
        else
            this.MenuIsOpen.Value = false;
    }

    /// <summary>Create an instance of the NPC Map Locations map page.</summary>
    /// <param name="menu">The game menu.</param>
    private IClickableMenu CreateMapPage(IClickableMenu menu)
    {
        this.MenuIsOpen.Value = true;
        return this.CreateMapPageImpl(menu);
    }

}
