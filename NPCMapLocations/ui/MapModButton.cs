using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace NPCMapLocations.Framework.Menus
{
    // Mod button for the three main modes
    internal class MapModButton : OptionsElement
    {
        public bool isActive;
        public Rectangle rect;

        public MapModButton(
            string label,
            int whichOption,
            int x,
            int y,
            int width,
            int height
        ) : base(label, x, y, 9 * Game1.pixelZoom, 9 * Game1.pixelZoom, whichOption)
        {
            this.label = ModMain.Helper.Translation.Get(label);
            this.rect = new Rectangle(x, y, width, height);

            if (ModMain.Config.ImmersionOption == whichOption - 2)
                this.greyedOut = false;
            else
                this.greyedOut = true;
        }

        public override void receiveLeftClick(int x, int y)
        {
            if (!this.isActive)
            {
                if (this.whichOption == 3 || this.whichOption == 4 || this.whichOption == 5)
                {
                    Game1.playSound("drumkit6");
                    base.receiveLeftClick(x, y);
                    this.isActive = true;
                    this.greyedOut = false;
                    ModMain.Config.ImmersionOption = this.whichOption - 2;
                }
            }
        }

        public void GreyOut()
        {
            this.isActive = false;
            this.greyedOut = true;
        }

        public override void draw(SpriteBatch b, int slotX, int slotY, IClickableMenu context = null)
        {
            base.draw(b, slotX - 32, slotY, context);
        }
    }
}
