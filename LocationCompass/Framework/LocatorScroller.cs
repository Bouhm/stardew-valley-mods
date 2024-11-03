using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;

namespace LocationCompass.Framework;

// Object for keeping track of with NPC to show when
// there are multiple NPCs in a building
internal class LocatorScroller
{
    /*********
    ** Accessors
    *********/
    public string Location { get; set; }
    public HashSet<string> Characters { get; set; }
    public int Index { get; set; }
    public Rectangle LocatorRect { get; set; }


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
