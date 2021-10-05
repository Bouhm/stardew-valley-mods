using Microsoft.Xna.Framework.Graphics;

namespace NPCMapLocations.Framework
{
    // Class for map markers
    public class NpcMarker : SyncedNpcMarker
    {
        /*********
        ** Accessors
        *********/
        public Texture2D Sprite { get; set; }
        public int CropOffset { get; set; }
        public bool HasQuest { get; set; }
        public bool IsHidden { get; set; }
        public int Layer { get; set; } = 4;
    }
}
