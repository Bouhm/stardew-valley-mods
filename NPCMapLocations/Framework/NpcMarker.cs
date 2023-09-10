using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NPCMapLocations.Framework
{
    /// <summary>An NPC marker on the map.</summary>
    public class NpcMarker : SyncedNpcMarker
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The NPC's overworld character sprite.</summary>
        public Texture2D Sprite { get; set; }

        /// <summary>The pixel area within the <see cref="Sprite"/> to draw, or <c>null</c> to use the first sprite in the NPC spritesheet.</summary>
        public Rectangle? SpriteSourceRect { get; set; }

        /// <summary>The zoom to apply when drawing the <see cref="Sprite"/>.</summary>
        public float? SpriteZoom { get; set; }

        /// <summary>The pixel offset to apply when cropping the NPC's head from their sprite.</summary>
        public int CropOffset { get; set; }

        /// <summary>Whether the player has an open quest for the NPC.</summary>
        public bool HasQuest { get; set; }

        /// <summary>Whether to hide the marker from the map.</summary>
        public bool IsHidden { get; set; }

        /// <summary>The reason the NPC is hidden, if applicable.</summary>
        public string ReasonHidden { get; set; }

        /// <summary>The NPC's priority when multiple markers overlap on the map, where higher values are higher priority.</summary>
        public int Layer { get; set; } = 4;


        /*********
        ** Public methods
        *********/
        /// <summary>Get the pixel area within the <see cref="Sprite"/> to draw.</summary>
        public Rectangle GetSpriteSourceRect()
        {
            if (this.SpriteSourceRect.HasValue)
                return this.SpriteSourceRect.Value;

            return this.Type == CharacterType.Horse
                ? new Rectangle(17, 104, 16, 14)
                : new Rectangle(0, this.CropOffset, 16, 15);
        }
    }
}
