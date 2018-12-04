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
    private ChatMessage prevMessage;
    private List<ChatMessage> messages;

    public override void Entry(IModHelper helper)
    {
      petCommands = Helper.Data.ReadJsonFile<PetCommands>("commands.json") ?? new PetCommands();
      TimeEvents.AfterDayStarted += TimeEvents_AfterDayStarted;
      SaveEvents.AfterLoad += SaveEvents_AfterLoad;
      SaveEvents.BeforeSave += SaveEvents_BeforeSave;
      GameEvents.QuarterSecondTick += GameEvents_QuarterSecondTick;
      GameEvents.UpdateTick += GameEvents_UpdateTick;
      PlayerEvents.Warped += PlayerEvents_Warped;
      GraphicsEvents.OnPostRenderEvent += GraphicsEvents_OnPostRenderEvent;
    }

    private void GraphicsEvents_OnPostRenderEvent(object sender, EventArgs e)
    {
      if (livelyPet?.pathToFarmer != null)
      {
        Vector2 pos = livelyPet.getTileLocation();
        foreach (var path in livelyPet.pathToFarmer)
        {
          var x = (int) pos.X * 64 - Game1.viewport.X;
          var y = (int) pos.Y * 64 - Game1.viewport.Y;
          switch (path)
          {
            case 0:
              Game1.spriteBatch.Draw(Game1.shadowTexture, new Rectangle(x, y, 64, 64), new Rectangle(3, 0, 1, 1),
                Color.Red);
              pos.Y -= 1;
              break;
            case 1:
              Game1.spriteBatch.Draw(Game1.shadowTexture, new Rectangle(x, y, 64, 64), new Rectangle(3, 0, 1, 1),
                Color.Red);
              pos.X += 1;
              break;
            case 2:
              Game1.spriteBatch.Draw(Game1.shadowTexture, new Rectangle(x, y, 64, 64), new Rectangle(3, 0, 1, 1),
                Color.Red);
              pos.Y += 1;
              break;
            case 3:
              Game1.spriteBatch.Draw(Game1.shadowTexture, new Rectangle(x, y, 64, 64), new Rectangle(3, 0, 1, 1),
                Color.Red);
              pos.X -= 1;
              break;
          }
        }
      }
    }

    private void GameEvents_QuarterSecondTick(object sender, EventArgs e)
    {
      if (!Context.IsWorldReady) return;

      if (livelyPet?.commandBehaviorTimer > 0)
      {
        livelyPet.commandBehaviorTimer--;
      }
      else
      {
        livelyPet.UpdatePathToFarmer();
        Monitor.Log(livelyPet.pathingIndex + "");
      }

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
      messages = this.Helper.Reflection.GetField<List<ChatMessage>>(Game1.chatBox, "messages", true).GetValue();
    }

    private void GameEvents_UpdateTick(object sender, EventArgs e)
    {
    }

    private void CheckChatForCommands()
    {
      if (messages?.LastOrDefault() == null) return;
      var idx = messages.Count < 10 ? messages.Count-1 : 0; // After 10 messages, message at idx 0 is removed for new messages
      if (messages[idx] == prevMessage) return;

      prevMessage = messages[idx];
      var lastMsg = ChatMessage.makeMessagePlaintext(messages.LastOrDefault().message);
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
        commandTimer = 4;
      }
    }

    private void PlayerEvents_Warped(object sender, EventArgsPlayerWarped e)
    {
      if (!e.NewLocation.IsFarm)
        livelyPet?.warpToFarmer();
    }

    private void SaveEvents_BeforeSave(object sender, EventArgs e)
    {
      // Preserve defaults in save so game doesn't break without mod
      if (vanillaPet == null) return;
      var characters = Helper.Reflection.GetField<NetCollection<NPC>>(Game1.getFarm(), "characters").GetValue();
      characters.Add(vanillaPet);
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

      Game1.warpFarmer("Farm", livelyPet.getTileX() - 1, livelyPet.getTileY() + 1, false);
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
