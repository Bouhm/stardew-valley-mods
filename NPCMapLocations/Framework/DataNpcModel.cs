namespace NPCMapLocations.Framework;

/// <summary>The model for NPC overrides in <see cref="DataModel"/>.</summary>
internal class DataNpcModel
{
    /*********
    ** Accessors
    *********/
    /// <summary>A game state query which indicates whether this NPC should be visible, or <c>null</c> for always visible.</summary>
    public string? Visible { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="visible"><inheritdoc cref="Visible" path="/summary"/></param>
    public DataNpcModel(string? visible)
    {
        this.Visible = visible;
    }
}
