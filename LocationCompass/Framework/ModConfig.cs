namespace LocationCompass.Framework;

/// <summary>The mod configuration model.</summary>
internal class ModConfig
{
    /*********
    ** Accessors
    *********/
    /// <summary>Whether you need to hold the button to show icons (else the button toggles them).</summary>
    public bool HoldToToggle { get; set; } = true;

    /// <summary>The key binding to show/hide the locator icons.</summary>
    public string ToggleKeyCode { get; set; } = "LeftAlt";

    /// <summary>Whether the game HUD is hidden when the locator icons are shown.</summary>
    public bool HideHud { get; set; } = true;

    /// <summary>Whether to only show locations in the current location. If `false`, characters in neighboring locations will be shown in gray.</summary>
    public bool SameLocationOnly { get; set; } = true;

    /// <summary>The key binding to toggle <see cref="SameLocationOnly"/>.</summary>
    public string SameLocationToggleKey { get; set; } = "H";

    /// <summary>Whether to only show characters with a quest or birthday today.</summary>
    public bool ShowQuestsAndBirthdaysOnly { get; set; } = false;

    /// <summary>The key binding to toggle <see cref="ShowQuestsAndBirthdaysOnly"/>.</summary>
    public string QuestsOnlyToggleKey { get; set; } = "J";

    /// <summary>Whether to only show players.</summary>
    public bool ShowFarmersOnly { get; set; } = false;

    /// <summary>The key binding to toggle <see cref="ShowFarmersOnly"/>.</summary>
    public string FarmersOnlyToggleKey { get; set; } = "K";

    /// <summary>Whether to show horses.</summary>
    public bool ShowHorses { get; set; } = false;

    /// <summary>The key binding to toggle <see cref="ShowHorses"/>.</summary>
    public string HorsesToggleKey { get; set; } = "L";
}
