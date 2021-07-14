namespace NPCMapLocations.Framework
{
    public class SyncedNpcMarker
    {
        public string DisplayName { get; set; }
        public string LocationName { get; set; }
        public int MapX { get; set; }
        public int MapY { get; set; }
        public bool IsBirthday { get; set; }
        public Character Type { get; set; }

        public SyncedNpcMarker()
        {
            this.DisplayName = null;
            this.LocationName = null;
            this.MapX = -9999;
            this.MapY = -9999;
            this.IsBirthday = false;
            this.Type = Character.Villager;
        }
    }
}
