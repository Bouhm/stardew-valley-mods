using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NPCMapLocations
{
    // Handles all custom NPCs, custom sprites, custom names, etc.`n`n
    internal class ModCustomHandler
    {
        private Dictionary<string, string> npcNames; // For handling custom names
        private Dictionary<string, object> customNPCs;
        private ModConfig config;
        private IModHelper helper;
        private Dictionary<string, int> markerCrop;

        public ModCustomHandler(ModConfig config, IModHelper helper, Dictionary<string, int> markerCrop)
        {
            this.config = config;
            this.helper = helper;
            this.markerCrop = markerCrop;
            customNPCs = config.CustomNPCs;
            npcNames = new Dictionary<string, string>();
        }

        // Handles customizations for NPCs
        // Custom NPCs and custom names or sprites for existing NPCs
        public void UpdateCustomNPCs()
        {
            bool areCustomNPCsInstalled = (customNPCs != null && customNPCs.Count > 0);
            foreach (NPC npc in Utility.getAllCharacters())
            {
                if (npc == null) { continue; }
                LoadCustomNPCs(npc, areCustomNPCsInstalled);
                if (!MapModConstants.ExcludedVillagers.Contains(npc.Name) && npc.isVillager())
                {
                    LoadNPCCrop(npc);
                    LoadCustomNames(npc);
                }
            }
            helper.WriteJsonFile($"config/{Constants.SaveFolderName}.json", config);
        }

        public Dictionary<string, object> GetCustomNPCs()
        {
            return customNPCs;
        }

        public Dictionary<string, string> GetNPCNames()
        {
            return npcNames;
        }

        // Handle modified or custom NPCs
        private void LoadCustomNPCs(NPC npc, bool areCustomNPCsInstalled)
        {
            if (areCustomNPCsInstalled)
            {
                foreach (KeyValuePair<string, object> customNPC in customNPCs)
                {
                    Type obj = customNPC.Value.GetType();
                    foreach (PropertyInfo prop in obj.GetProperties())
                    {
                        if (!prop.Name.Equals("crop"))
                        {
                            Dictionary<string, int> npcEntry = new Dictionary<string, int> { { "crop", 0 } };
                            customNPCs.Add(customNPC.Key, npcEntry);

                            if (!markerCrop.ContainsKey(customNPC.Key))
                            {
                                markerCrop.Add(customNPC.Key, (int)prop.GetValue(customNPC.Value));
                            }
                        }
                    }
                }
            }
            else
            {
                if (npc.Schedule != null && !MapModConstants.MarkerCrop.Keys.Contains(npc.Name))
                {
                    if (!customNPCs.TryGetValue(npc.Name, out object npcEntry))
                    {
                        npcEntry = new Dictionary<string, int>
                        {
                            { "crop", 0 }
                        };
                        customNPCs.Add(npc.Name, npcEntry);

                        if (!markerCrop.ContainsKey(npc.Name))
                            markerCrop.Add(npc.Name, 0);

                    }
                }
            }
        }

        // Handle any modified NPC names 
        // Specifically mods that change names in dialogue files (displayName)
        private void LoadCustomNames(NPC npc)
        {
            if (!npcNames.TryGetValue(npc.Name, out string customName))
            {
                if (npc.displayName == null)
                    npcNames.Add(npc.Name, npc.Name);
                else
                    npcNames.Add(npc.Name, npc.displayName);
            }
        }

        // Load user-specified NPC crops for custom sprites
        private void LoadNPCCrop(NPC npc)
        {
            if (config.VillagerCrop != null && config.VillagerCrop.Count > 0)
            {
                foreach (KeyValuePair<string, int> villager in config.VillagerCrop)
                {
                    if (npc.Name.Equals(villager.Key))
                    {
                        markerCrop[npc.Name] = villager.Value;
                    }
                }
            }
        }
    }
}
