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
using StardewValley.Menus;

namespace LivelyPets
{
  public class ModEntry : Mod
  {
    private Pet vanillaPet;
    private LivelyPet livelyPet;
    private ModData petData;
    private int commandTimer = 0;
    public static PetCommands petCommands;
    private int prevChatCount;
    private List<ChatMessage> chat;

    public override void Entry(IModHelper helper)
    {
      petCommands = Helper.Data.ReadJsonFile<PetCommands>("commands.json") ?? new PetCommands();
      TimeEvents.AfterDayStarted += TimeEvents_AfterDayStarted;
      SaveEvents.AfterLoad += SaveEvents_AfterLoad;
      SaveEvents.BeforeSave += SaveEvents_BeforeSave;
      GameEvents.HalfSecondTick += GameEvents_HalfSecondTick;
      GameEvents.OneSecondTick += GameEvents_OneSecondTick;
      GameEvents.UpdateTick += GameEvents_UpdateTick;
      PlayerEvents.Warped += PlayerEvents_Warped;
    }

    private void GameEvents_HalfSecondTick(object sender, EventArgs e)
    {
      if (!Context.IsWorldReady) return;

      if (livelyPet?.commandBehaviorTimer > 0)
        livelyPet.commandBehaviorTimer--;

      if (commandTimer > 0)
        commandTimer--;

      if (commandTimer == 0)
      {
        CheckChatForCommands();
      }
    }

    private void SaveEvents_AfterLoad(object sender, EventArgs e)
    {
      petData = Helper.Data.ReadJsonFile<ModData>($"data/{Constants.SaveFolderName}.json") ?? new ModData();
      petCommands = Helper.Data.ReadJsonFile<PetCommands>("commands.json") ?? new PetCommands();
      chat = this.Helper.Reflection.GetField<List<ChatMessage>>(Game1.chatBox, "messages", true).GetValue();
    }

    private void GameEvents_UpdateTick(object sender, EventArgs e)
    {
    }

    private void CheckChatForCommands()
    {
      if (chat?.LastOrDefault() == null) return;
      if (prevChatCount == chat.Count) return;

      prevChatCount = chat.Count;
      var lastMsg = ChatMessage.makeMessagePlaintext(chat.LastOrDefault().message);
      var farmerName = lastMsg.Substring(0, lastMsg.IndexOf(':'));
      lastMsg = lastMsg.Replace($"{farmerName}: ", ""); // Remove sender name from text
      string command = null;

      foreach (var commands in petCommands.Commands)
      {
        if (commands.Value.Any(lastMsg.Contains))
        {
          command = commands.Key;
        }
      }

      if (command == null) return;
      if (command != livelyPet.commandBehavior)
      {
        livelyPet.commandBehavior = command;
        commandTimer = 6;
      }
    }

    private void PlayerEvents_Warped(object sender, EventArgsPlayerWarped e)
    {
      if (!e.NewLocation.IsFarm)
        livelyPet?.warpToFarmer();
    }

    private void GameEvents_OneSecondTick(object sender, EventArgs e)
    {
      if (!Context.IsWorldReady || livelyPet == null) return;
      if (!livelyPet.isNearFarmer)
        livelyPet.UpdatePathToFarmer();

      Monitor.Log(livelyPet.commandBehavior);
    }

    private void SaveEvents_BeforeSave(object sender, EventArgs e)
    {
      // Preserve defaults in save so game doesn't break without mod
      if (vanillaPet == null) return;
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
        livelyPet = new LivelyDog(dog, Monitor);
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
