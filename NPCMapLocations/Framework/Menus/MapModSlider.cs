using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace NPCMapLocations.Framework.Menus
{
    // Mod slider for heart level this.Config
    internal class MapModSlider : OptionsElement
    {
        private readonly int Max;
        private int Min;
        private float Value;
        public string ValueLabel;

        public MapModSlider(string label, int whichOption, int x, int y, int min, int max)
            : base(label, x, y, 48 * Game1.pixelZoom, 6 * Game1.pixelZoom, whichOption)
        {
            this.Min = min;
            this.Max = max;
            if (whichOption != 8 && whichOption != 9) this.bounds.Width = this.bounds.Width * 2;
            this.ValueLabel = ModEntry.StaticHelper.Translation.Get(label);

            this.Value = whichOption switch
            {
                8 => ModEntry.Config.HeartLevelMin,
                9 => ModEntry.Config.HeartLevelMax,
                _ => this.Value
            };
        }

        public override void leftClickHeld(int x, int y)
        {
            if (this.greyedOut)
                return;

            base.leftClickHeld(x, y);
            this.Value = x >= this.bounds.X
                ? (x <= this.bounds.Right - 10 * Game1.pixelZoom
                    ? (int)((x - this.bounds.X) / (float)(this.bounds.Width - 10 * Game1.pixelZoom) * this.Max)
                    : this.Max)
                : 0;

            switch (this.whichOption)
            {
                case 8:
                    ModEntry.Config.HeartLevelMin = (int)this.Value;
                    break;
                case 9:
                    ModEntry.Config.HeartLevelMax = (int)this.Value;
                    break;
            }

            ModEntry.StaticHelper.Data.WriteJsonFile($"config/{Constants.SaveFolderName}.json", ModEntry.Config);
        }

        public override void receiveLeftClick(int x, int y)
        {
            if (this.greyedOut) return;

            base.receiveLeftClick(x, y);
            this.leftClickHeld(x, y);
        }

        public override void draw(SpriteBatch b, int slotX, int slotY, IClickableMenu context = null)
        {
            this.label = this.ValueLabel + ": " + this.Value;
            this.greyedOut = false;
            if (this.whichOption == 8 || this.whichOption == 9) this.greyedOut = !ModEntry.Config.ByHeartLevel;

            base.draw(b, slotX, slotY, context);
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, OptionsSlider.sliderBGSource, slotX + this.bounds.X,
                slotY + this.bounds.Y, this.bounds.Width, this.bounds.Height, Color.White, Game1.pixelZoom, false);
            b.Draw(Game1.mouseCursors,
                new Vector2(
                    slotX + this.bounds.X + (this.bounds.Width - 10 * Game1.pixelZoom) *
                    (this.Value / this.Max), slotY + this.bounds.Y),
                OptionsSlider.sliderButtonRect, Color.White, 0f, Vector2.Zero, Game1.pixelZoom,
                SpriteEffects.None, 0.9f);
        }
    }
}
