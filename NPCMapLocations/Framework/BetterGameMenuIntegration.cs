#nullable enable

using System;

using Leclair.Stardew.BetterGameMenu;

using StardewModdingAPI;
using StardewModdingAPI.Utilities;

using StardewValley.Menus;

namespace NPCMapLocations.Framework;

/// <summary>Registers the MapPage provider with Better Game Menu</summary>
internal class BetterGameMenuIntegration
{
    internal delegate IClickableMenu CreateInstanceDelegate(IClickableMenu menu);

    /*********
    ** Fields
    *********/
    /// <summary>The Better Game Menu integration.</summary>
    private readonly IBetterGameMenuApi? BetterGameMenu;

    /// <summary>A method that's used to create a new instance of the NPC Map Locations menu.</summary>
    private readonly CreateInstanceDelegate CreateInstance;

    /// <summary>A simple value to hold if the menu is active or not.</summary>
    private readonly PerScreen<bool> MenuIsOpen;

    /// <summary>
    /// A simple value to store if we expect the menu to be closing. This is used
    /// by OnResize since the previous instance's OnClose is fired after OnResize
    /// completes, and we don't want to end up with a false menu closed state.
    /// </summary>
    private readonly PerScreen<bool> MenuShouldClose;

    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="monitor">An API for logging messages.</param>
    /// <param name="modRegistry">An API for fetching APIs from loaded mods.</param>
    public BetterGameMenuIntegration(CreateInstanceDelegate createInstance, IMonitor monitor, IModRegistry modRegistry)
    {
        this.CreateInstance = createInstance;
        this.MenuIsOpen = new();
        this.MenuShouldClose = new();

        try
        {
            this.BetterGameMenu = modRegistry.GetApi<IBetterGameMenuApi>("leclair.bettergamemenu");
        }
        catch (Exception ex)
        {
            monitor.Log($"Unable to get Better Game Menu's API: {ex}", LogLevel.Warn);
            this.BetterGameMenu = null;
        }

        this.Register();
    }

    /// <summary>Whether or not the current screen has an open menu.</summary>
    public bool IsMenuOpen => this.MenuIsOpen.Value;

    public void Register()
    {
        if (this.BetterGameMenu == null)
            return;

        this.BetterGameMenu.RegisterImplementation(
            nameof(VanillaTabOrders.Map),
            priority: 100,
            getPageInstance: this.OnCreate,
            getMenuInvisible: () => true,
            getWidth: width => width + 128,
            onResize: this.OnResize,
            onClose: this.OnClose
        );

    }

    private IClickableMenu? OnResize((IClickableMenu Menu, IClickableMenu OldPage) input)
    {
        this.MenuShouldClose.Value = true;
        return this.OnCreate(input.Menu);
    }

    private void OnClose(IClickableMenu menu)
    {
        // When OnResize is called, a new instance is created before the old
        // instance is closed. As such, we use MenuShouldClose as a guard to
        // keep MenuIsOpen's value correct.

        // This could be replaced with some sort of reference tracking, but
        // that seemed more potentially unstable if something happens that
        // causes BetterGameMenu to be unable to properly call OnClose.
        if (this.MenuShouldClose.Value)
            this.MenuShouldClose.Value = false;
        else
            this.MenuIsOpen.Value = false;
    }

    private IClickableMenu OnCreate(IClickableMenu menu)
    {
        this.MenuIsOpen.Value = true;
        return this.CreateInstance(menu);
    }

}
