using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;

namespace LivelyPets
{
  public class ModEntry : Mod
  {
    private IModHelper Helper;
    private Pet vanillaPet;
    private LivelyPet livelyPet;

    public override void Entry(IModHelper helper)
    {
      this.Helper = helper;
      TimeEvents.AfterDayStarted += TimeEvents_AfterDayStarted;
      SaveEvents.BeforeSave += SaveEvents_BeforeSave;
    }

    private void SaveEvents_BeforeSave(object sender, EventArgs e)
    {
      // Preserve defaults in save so game doesn't break without mod
      var characters = Helper.Reflection.GetField<NetCollection<NPC>>(Game1.getFarm(), "characters").GetValue();
      if (!characters.Contains(vanillaPet)) characters.Add(vanillaPet);
      if (characters.Contains(livelyPet)) characters.Remove(livelyPet);
    }

    private void TimeEvents_AfterDayStarted(object sender, EventArgs e)
    {
      vanillaPet = (Pet) Game1.getCharacterFromName(Game1.player.getPetName());
      if (!Context.IsWorldReady || vanillaPet == null) return;
      var characters = Helper.Reflection.GetField<NetCollection<NPC>>(Game1.getFarm(), "characters").GetValue();
      if (characters.Contains(vanillaPet)) characters.Remove(vanillaPet);

      if (vanillaPet is Dog dog)
      {
        livelyPet = (LivelyPet) new LivelyDog(vanillaPet);
      }
      else if (vanillaPet is Cat cat)
      {
        livelyPet = (LivelyPet) new LivelyCat(vanillaPet);
      }
    }
  }
}
