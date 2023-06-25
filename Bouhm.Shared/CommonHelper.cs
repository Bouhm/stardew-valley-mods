using StardewModdingAPI;
using System.IO;
using System;

namespace Bouhm.Shared
{
    /// <summary>Provides common utility methods for interacting with the game code shared by my various mods.</summary>
    internal static class CommonHelper
    {
        /*********
        ** Public methods
        *********/
        /****
        ** File handling
        ****/
        /// <summary>Remove one or more obsolete files from the mod folder, if they exist.</summary>
        /// <param name="mod">The mod for which to delete files.</param>
        /// <param name="relativePaths">The relative file path within the mod's folder.</param>
        public static void RemoveObsoleteFiles(IMod mod, params string[] relativePaths)
        {
            string basePath = mod.Helper.DirectoryPath;

            foreach (string relativePath in relativePaths)
            {
                string fullPath = Path.Combine(basePath, relativePath);
                if (File.Exists(fullPath))
                {
                    try
                    {
                        File.Delete(fullPath);
                        mod.Monitor.Log($"Removed obsolete file '{relativePath}'.");
                    }
                    catch (Exception ex)
                    {
                        mod.Monitor.Log($"Failed deleting obsolete file '{relativePath}':\n{ex}");
                    }
                }
            }
        }
    }
}
