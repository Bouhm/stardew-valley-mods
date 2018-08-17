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
		private Dictionary<string, string> CustomNames; // For handling custom names
		private Dictionary<string, int> MarkerCropOffsets;
		private HashSet<string> NpcCustomizations;
		private string CustomNpcNames;

		public ModCustomHandler(IModHelper helper, ModConfig config, IMonitor monitor)
		{
			this.MarkerCropOffsets = ModConstants.MarkerCropOffsets;
			this.Helper = helper;
			this.Config = config;
			this.Monitor = monitor;
			this.CustomNames = new Dictionary<string, string>();
			this.NpcCustomizations = new HashSet<string>();
		}

		// Handles customizations for NPCs
		// Custom NPCs and custom names or sprites for existing NPCs
		public void UpdateCustomNpcs()
		{
			foreach (NPC npc in Utility.getAllCharacters())
			{
				if (npc == null)
				{
					continue;
				}

				if (!ModConstants.ExcludedVillagers.Contains(npc.Name) && npc.isVillager())
				{
					LoadNpcCrop(npc);
					LoadCustomNames(npc);
				}
			}

			if (this.NpcCustomizations.Count != 0)
			{
				string names = "Handled custom NPCs: ";
				foreach (string name in this.NpcCustomizations)
				{
					names += name + ", ";
				}

				this.Monitor.Log(names.Substring(0, names.Length - 2), LogLevel.Info);
			}
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

		public Dictionary<string, string> GetNpcNames()
		{
			return this.CustomNames;
		}

		public Dictionary<string, int> GetMarkerCropOffsets()
		{
			return this.MarkerCropOffsets;
		}

		// Handle any modified NPC names 
		// Specifically mods that change names in dialogue files (displayName)
		private void LoadCustomNames(NPC npc)
		{
			if (!this.CustomNames.TryGetValue(npc.Name, out string customName))
			{
				if (npc.displayName == null)
					this.CustomNames.Add(npc.Name, npc.Name);
				else
				{
					this.CustomNames.Add(npc.Name, npc.displayName);
					if (!npc.Name.Equals(npc.displayName) || this.Config.CustomCropOffsets.ContainsKey(npc.Name))
						this.NpcCustomizations.Add(npc.Name);
				}
			}
		}

		// Load user-specified NPC crops for custom sprites
		private void LoadNpcCrop(NPC npc)
		{
			if (this.Config.CustomCropOffsets != null && this.Config.CustomCropOffsets.Count > 0)
			{
				foreach (KeyValuePair<string, int> villager in this.Config.CustomCropOffsets)
				{
					if (npc.Name.Equals(villager.Key))
					{
						this.MarkerCropOffsets[npc.Name] = villager.Value;
						this.NpcCustomizations.Add(npc.Name);
					}
				}
			}

			// If custom crop offset is not specified, default to 0
			if (!this.MarkerCropOffsets.TryGetValue(npc.Name, out int crop))
			{
				this.MarkerCropOffsets[npc.Name] = 0;
      }
		}
	}
}