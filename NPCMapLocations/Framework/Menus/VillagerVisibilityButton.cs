using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NPCMapLocations.Framework.Models;
using StardewValley;
using StardewValley.Menus;

namespace NPCMapLocations.Framework.Menus
{
    // Mod button for the three main modes
    internal class VillagerVisibilityButton : OptionsElement
    {
        /*********
        ** Fields
        *********/
        private bool IsActive;


        /*********
        ** Accessors
        *********/
        public Rectangle Rect { get; set; }

        public VillagerVisibility Value { get; }


        /*********
        ** Public methods
        *********/
        public VillagerVisibilityButton(string label, VillagerVisibility value, int x, int y, int width, int height)
            : base(label, x, y, 9 * Game1.pixelZoom, 9 * Game1.pixelZoom, (int)value + 2)
        {
            this.Rect = new Rectangle(x, y, width, height);

            this.Value = value;
            this.greyedOut = ModEntry.Config.ImmersionOption != value;
        }

        public override void receiveLeftClick(int x, int y)
        {
            if (!this.IsActive)
            {
                Game1.playSound("drumkit6");
                base.receiveLeftClick(x, y);
                this.IsActive = true;
                this.greyedOut = false;
                ModEntry.Config.ImmersionOption = this.Value;
            }
        }

        public void GreyOut()
        {
            this.IsActive = false;
            this.greyedOut = true;
        }

        public override void draw(SpriteBatch b, int slotX, int slotY, IClickableMenu context = null)
        {
            base.draw(b, slotX - 32, slotY, context);
        }
    }
}
