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
    private Pet vanillaPet;
    private LivelyPet livelyPet;

    public override void Entry(IModHelper helper)
    {
      TimeEvents.AfterDayStarted += TimeEvents_AfterDayStarted;
      SaveEvents.BeforeSave += SaveEvents_BeforeSave;
      GameEvents.OneSecondTick += GameEvents_OneSecondTick;
    }

    private void GameEvents_OneSecondTick(object sender, EventArgs e)
    {
      if (!Context.IsWorldReady) return;
    }

    private void SaveEvents_BeforeSave(object sender, EventArgs e)
    {
      // Preserve defaults in save so game doesn't break without mod
      var characters = Helper.Reflection.GetField<NetCollection<NPC>>(Game1.getFarm(), "characters").GetValue();
      if (!characters.Contains(vanillaPet)) characters.Add(vanillaPet);
      RemovePet(livelyPet);
    }

    private void TimeEvents_AfterDayStarted(object sender, EventArgs e)
    {
      vanillaPet = GetPet(Game1.player.getPetName());
      if (!Context.IsWorldReady || vanillaPet == null) return;
      RemovePet(vanillaPet);

      if (vanillaPet is Dog dog)
        livelyPet = new LivelyDog(dog);
      else if (vanillaPet is Cat cat)
        livelyPet = new LivelyCat(cat, Monitor);

      Game1.getFarm().characters.Add(livelyPet);
    }

    private void RemovePet(Pet target)
    {
      foreach (var pet in Game1.getFarm().characters.ToList())
      {
        if (pet.GetType().IsInstanceOfType(target) && pet.Name == target.Name)
        {
          Game1.getFarm().characters.Remove(pet);
          break;
        }
      }
    }

    private Pet GetPet(string petName)
    {
      foreach (var npc in Game1.getFarm().characters.ToList())
      {
        if (npc is Pet pet && pet.Name == petName)
          return pet;
      }

      return null;
    }
  }
}
