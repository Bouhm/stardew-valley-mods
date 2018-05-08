using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NPCMapLocations
{
    // Handles custom maps (recolors of the mod map), custom NPCs, custom sprites, custom names, etc.
    internal class ModCustomHandler
    {
        private Dictionary<string, string> npcNames; // For handling custom names
        private Dictionary<string, object> customNPCs;
        private Dictionary<string, int> markerCrop;
        private HashSet<string> npcCustomizations;
        private string customNPCNames;

        public ModCustomHandler(Dictionary<string, int> markerCrop)
        {
            this.markerCrop = markerCrop;
            customNPCs = ModMain.config.CustomNPCs;
            npcNames = new Dictionary<string, string>();
            npcCustomizations = new HashSet<string>();
            customNPCNames = "";
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
                if (!ModConstants.ExcludedVillagers.Contains(npc.Name) && npc.isVillager())
                {
                    LoadNPCCrop(npc);
                    LoadCustomNames(npc);
                }
            }

            if (npcCustomizations.Count != 0)
            {
                string names = "Loaded customizations for: ";
                foreach (string name in npcCustomizations)
                {
                    names += name + ", ";
                }
                ModMain.monitor.Log(names.Substring(0, names.Length - 2), LogLevel.Info);
            }

            if (customNPCNames != "")
                ModMain.monitor.Log("Loaded custom NPCs: " + customNPCNames.Substring(0, customNPCNames.Length-2), LogLevel.Info);
            ModMain.modHelper.WriteJsonFile($"config/{Constants.SaveFolderName}.json", ModMain.config);
        }

        // Load recolored map if user has recolor mods
        public string LoadMap()
        {
            // Eemie's Map Recolour
            if (ModMain.modHelper.ModRegistry.IsLoaded("minervamaga.CP.eemieMapRecolour"))
            {
                return "eemie_recolour_map";
            }
            // Starblue Valley
            else if (ModMain.modHelper.ModRegistry.IsLoaded("Lita.StarblueValley"))
            {
                return "starblue_map";
            }
            // Default
            else
            {
                return "default_map";
            }
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
                            customNPCNames += customNPC.Key + ", ";

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
                if (npc.Schedule != null && !ModConstants.MarkerCrop.Keys.Contains(npc.Name))
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
                {
                    npcNames.Add(npc.Name, npc.displayName);
                    if (!npc.Name.Equals(npc.displayName))
                        npcCustomizations.Add(npc.Name);
                }
            }
        }

        // Load user-specified NPC crops for custom sprites
        private void LoadNPCCrop(NPC npc)
        {
            if (ModMain.config.VillagerCrop != null && ModMain.config.VillagerCrop.Count > 0)
            {
                foreach (KeyValuePair<string, int> villager in ModMain.config.VillagerCrop)
                {
                    if (npc.Name.Equals(villager.Key))
                    {
                        markerCrop[npc.Name] = villager.Value;
                        npcCustomizations.Add(npc.Name);
                    }
                }
            }
        }
    }
}
