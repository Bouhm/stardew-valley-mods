using Microsoft.Xna.Framework.Graphics;

namespace NPCMapLocations.Framework
{
    // Class for map markers
    public class NpcMarker
    {
        /*********
        ** Accessors
        *********/
        public string DisplayName { get; set; }
        public string LocationName { get; set; }
        public Texture2D Sprite { get; set; }
        public int CropOffset { get; set; }
        public int MapX { get; set; }
        public int MapY { get; set; }
        public bool IsBirthday { get; set; }
        public CharacterType Type { get; set; }
        public bool HasQuest { get; set; }
        public bool IsHidden { get; set; }
        public int Layer { get; set; }


        /*********
        ** Public methods
        *********/
        public NpcMarker()
        {
            this.DisplayName = null;
            this.LocationName = null;
            this.Sprite = null;
            this.CropOffset = 0;
            this.MapX = -9999;
            this.MapY = -9999;
            this.IsBirthday = false;
            this.HasQuest = false;
            this.IsHidden = false;
            this.Layer = 4;
            this.Type = CharacterType.Villager;
        }
    }
}
