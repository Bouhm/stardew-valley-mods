using System;
using System.Collections.Generic;
using System.Text;

namespace NpcUtil
{
  class NpcUtil
  {
    public static bool HasQuest(Farmer player)
    {
      // Check for daily quests
      foreach (var quest in player.questLog)
      {
        if (quest.accepted.Value && quest.dailyQuest.Value && !quest.completed.Value)
        switch (quest.questType.Value)
        {
          case 3:
            npcMarker.HasQuest = ((ItemDeliveryQuest)quest).target.Value == npcMarker.Name;
            break;
          case 4:
            npcMarker.HasQuest = ((SlayMonsterQuest)quest).target.Value == npcMarker.Name;
            break;
          case 7:
            npcMarker.HasQuest = ((FishingQuest)quest).target.Value == npcMarker.Name;
            break;
          case 10:
            npcMarker.HasQuest = ((ResourceCollectionQuest)quest).target.Value == npcMarker.Name;
            break;
        }
      }
    }
  }
}
