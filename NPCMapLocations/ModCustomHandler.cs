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
        private readonly IModHelper Helper;
        private readonly IMonitor Monitor;
        private ModConfig Config;
        private Dictionary<string, string> NpcNames; // For handling custom names
        private Dictionary<string, int> MarkerCrop;
        private HashSet<string> NpcCustomizations;
        Dictionary<string, object> CustomNpcs;
        private string CustomNpcNames;

        public ModCustomHandler(IModHelper helper, ModConfig config, IMonitor monitor)
        {
            this.MarkerCrop = ModConstants.MarkerCrop;
            this.Helper = helper;
            this.Config = config;
            this.Monitor = monitor;
            this.NpcNames = new Dictionary<string, string>();
            this.CustomNpcs = config.CustomNpcs;
            this.NpcCustomizations = new HashSet<string>();
            this.CustomNpcNames = "";
        }

        // Handles customizations for NPCs
        // Custom NPCs and custom names or sprites for existing NPCs
        public void UpdateCustomNpcs()
        {
            bool areCustomNpcsInstalled = (this.CustomNpcs != null && this.CustomNpcs.Count > 0);
            foreach (NPC npc in Utility.getAllCharacters())
            {
                if (npc == null) { continue; }
                LoadCustomNpcs(npc, areCustomNpcsInstalled);
                if (!ModConstants.ExcludedVillagers.Contains(npc.Name) && npc.isVillager())
                {
                    LoadNPCCrop(npc);
                    LoadCustomNames(npc);
                }
            }

            if (this.NpcCustomizations.Count != 0)
            {
                string names = "Handled customizations for: ";
                foreach (string name in this.NpcCustomizations)
                {
                    names += name + ", ";
                }
                this.Monitor.Log(names.Substring(0, names.Length - 2), LogLevel.Info);
            }

            if (this.CustomNpcNames != "")
                this.Monitor.Log("Handled custom Npcs: " + this.CustomNpcNames.Substring(0, this.CustomNpcNames.Length-2), LogLevel.Info);
                    
            this.Helper.WriteConfig(Config);
        }

        // Load recolored map if user has recolor mods
        public string LoadMap()
        {
            // Eemie's Map Recolour
            if (this.Helper.ModRegistry.IsLoaded("minervamaga.CP.eemieMapRecolour"))
            {
                return "eemie_recolour_map";
            }
            // Starblue Valley
            else if (this.Helper.ModRegistry.IsLoaded("Lita.StarblueValley"))
            {
                return "starblue_map";
            }
            // Default
            else
            {
                return "default_map";
            }
        }

        public Dictionary<string, object> GetCustomNpcs()
        {
            return this.CustomNpcs;
        }

        public Dictionary<string, string> GetNpcNames()
        {
            return this.NpcNames;
        }

        // Handle modified or custom NPCs
        private void LoadCustomNpcs(NPC npc, bool areCustomNpcsInstalled)
        {
            if (areCustomNpcsInstalled)
            {
                foreach (KeyValuePair<string, object> customNpc in this.CustomNpcs)
                {
                    Type obj = customNpc.Value.GetType();
                    foreach (PropertyInfo prop in obj.GetProperties())
                    {
                        if (!prop.Name.Equals("crop"))
                        {
                            Dictionary<string, int> npcEntry = new Dictionary<string, int> { { "crop", 0 } };
                            this.CustomNpcs.Add(customNpc.Key, npcEntry);
                            this.CustomNpcNames += customNpc.Key + ", ";

                            if (!this.MarkerCrop.ContainsKey(customNpc.Key))
                            {
                                this.MarkerCrop.Add(customNpc.Key, (int)prop.GetValue(customNpc.Value));
                            }
                        }
                    }
                }
            }
            else
            {
                if (npc.Schedule != null && !this.MarkerCrop.ContainsKey(npc.Name))
                {
                    if (!this.CustomNpcs.TryGetValue(npc.Name, out object npcEntry))
                    {
                        npcEntry = new Dictionary<string, int>
                        {
                            { "crop", 0 }
                        };
                        this.CustomNpcs.Add(npc.Name, npcEntry);

                        if (!this.MarkerCrop.ContainsKey(npc.Name))
                            this.MarkerCrop.Add(npc.Name, 0);

                    }
                }
            }
        }

        // Handle any modified NPC names 
        // Specifically mods that change names in dialogue files (displayName)
        private void LoadCustomNames(NPC npc)
        {
            if (!this.NpcNames.TryGetValue(npc.Name, out string customName))
            {
                if (npc.displayName == null)
                    this.NpcNames.Add(npc.Name, npc.Name);
                else
                {
                    this.Monitor.Log(npc.displayName);
                    this.NpcNames.Add(npc.Name, npc.displayName);
                    if (!npc.Name.Equals(npc.displayName) || this.Config.VillagerCrop.ContainsKey(npc.Name))
                          this.NpcCustomizations.Add(npc.Name);
                }
            }
        }

        // Load user-specified NPC crops for custom sprites
        private void LoadNPCCrop(NPC npc)
        {
            if (this.Config.VillagerCrop != null && this.Config.VillagerCrop.Count > 0)
            {
                foreach (KeyValuePair<string, int> villager in this.Config.VillagerCrop)
                {
                    if (npc.Name.Equals(villager.Key))
                    {
                        this.MarkerCrop[npc.Name] = villager.Value;
                        this.NpcCustomizations.Add(npc.Name);
                    }
                }
            }
        }
    }
}
