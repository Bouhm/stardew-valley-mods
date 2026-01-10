using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NPCMapLocations.Framework;

/// <summary>An NPC marker on the map.</summary>
public class NpcMarker : SyncedNpcMarker
{
    /*********
    ** Accessors
    *********/
    /// <summary>The NPC's overworld character sprite, if it could be loaded.</summary>
    public Texture2D? Sprite { get; set; }

    /// <summary>The pixel area within the <see cref="Sprite"/> to draw, or <c>null</c> to use the first sprite in the NPC spritesheet.</summary>
    public Rectangle? SpriteSourceRect { get; set; }

    /// <summary>The pixel area within the <see cref="Sprite"/> for the vanilla mugshot, if applicable.</summary>
    public Rectangle? VanillaMugShotSourceRect { get; set; }

    /// <summary>The zoom to apply when drawing the <see cref="Sprite"/>.</summary>
    public float? SpriteZoom { get; set; }

    /// <summary>The pixel offset to apply when cropping the NPC's head from their sprite.</summary>
    public int CropOffset { get; set; }

    /// <summary>Whether the player has an open quest for the NPC.</summary>
    public bool HasQuest { get; set; }

    /// <summary>Whether to hide the marker from the map.</summary>
    [MemberNotNullWhen(true, nameof(ReasonHidden))]
    public bool IsHidden { get; set; }

    /// <summary>The reason the NPC is hidden, if applicable.</summary>
    public string? ReasonHidden { get; set; }

    /// <summary>The NPC's priority when multiple markers overlap on the map, where higher values are higher priority.</summary>
    public int Layer { get; private set; } = 4;


    /*********
    ** Public methods
    *********/
    /// <summary>Get the pixel area within the <see cref="Sprite"/> to draw.</summary>
    public Rectangle GetSpriteSourceRect()
    {
        if (ModEntry.Config.NpcIconStyle == NpcIconStyle.Vanilla && this.VanillaMugShotSourceRect.HasValue)
            return this.VanillaMugShotSourceRect.Value;

        if (this.SpriteSourceRect.HasValue)
            return this.SpriteSourceRect.Value;

        return this.Type switch
        {
            CharacterType.Horse => new(17, 104, 16, 14),
            CharacterType.Raccoon => new(11, 17, 11, 10),
            _ => new(0, this.CropOffset, 16, 15)
        };
    }

    /// <summary>Recalculate the <see cref="Layer"/>.</summary>
    public void RecalculateDrawLayer()
    {
        // Layers 4 - 7: Outdoor NPCs in order of hidden, hidden w/ quest/birthday, standard, standard w/ quest/birthday
        // Layers 0 - 3: Indoor NPCs in order of hidden, hidden w/ quest/birthday, standard, standard w/ quest/birthday

        int layer;
        if (this.Type is CharacterType.Child or CharacterType.Horse)
            layer = 0;
        else
        {
            layer = this.IsOutdoors ? 6 : 2;
            if (this.IsHidden)
                layer -= 2;
        }

        if (this.HasQuest || this.IsBirthday)
            layer++;

        this.Layer = layer;
    }
}
