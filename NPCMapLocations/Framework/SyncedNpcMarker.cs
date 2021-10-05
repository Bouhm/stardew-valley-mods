namespace NPCMapLocations.Framework
{
    public class SyncedNpcMarker
    {
        /*********
        ** Accessors
        *********/
        public string DisplayName { get; set; }
        public string LocationName { get; set; }
        public int MapX { get; set; } = -9999;
        public int MapY { get; set; } = -9999;
        public bool IsBirthday { get; set; }
        public CharacterType Type { get; set; } = CharacterType.Villager;
    }
}
