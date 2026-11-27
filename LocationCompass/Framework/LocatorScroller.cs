using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;

namespace LocationCompass.Framework;

// Object for keeping track of with NPC to show when
// there are multiple NPCs in a building
internal record LocatorScroller(string Location, HashSet<string> Characters, int Index, Rectangle LocatorRect)
{
    /*********
    ** Accessors
    *********/
    public Rectangle LocatorRect { get; set; } = LocatorRect;
    public int Index { get; set; } = Index;


    /*********
    ** Public methods
    *********/
    public void ReceiveLeftClick()
    {
        if (this.LocatorRect.Contains(Game1.getMouseX(), Game1.getMouseY()))
        {
            this.Index++;
            Game1.playSound("drumkit6");

            if (this.Index > this.Characters.Count - 1)
                this.Index = 0;
        }
    }
}
