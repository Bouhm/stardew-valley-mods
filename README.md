# StardewValleyMods
A collection of Stardew Valley mods that improve player experience, quality of life, and add new content to the game.

## Install
1. [Install the latest version of SMAPI](https://smapi.io)
2. Download mods and unzip them into *Stardew Valley/Mods*.
3. Run the game using StardewModdingAPI (SMAPI).

### [NPC Map Locations Mod](https://www.nexusmods.com/stardewvalley/mods/239)
Shows character locations on the map. Uses a modified map page much more accurate to the game.

_[Adding custom locations](https://github.com/Bouhm/stardew-valley-mods/tree/master/NPCMapLocations)_

### [Location Compass](https://www.nexusmods.com/stardewvalley/mods/3045)
Locates characters on the screen indicating direction and distance from player's current position.

### [Pet Dogs Mod](https://www.nexusmods.com/stardewvalley/mods/570)
Replaces pet dog with a Shiba Inu, Shepherd, or Husky.
Requires [Content Patcher]("https://www.nexusmods.com/stardewvalley/mods/1915").

## Compiling
To compile a SMAPI mod for testing, use the following instructions:

1. Make sure to have the appropriate versions of the game and [SMAPI](https://smapi.io) installed.
2. Open the solution with Visual Studio or MonoDevelop.
3. Add the package [Stardew.ModBuildConfig](https://www.nuget.org/packages/Pathoschild.Stardew.ModBuildConfig) by Pathoschild to enable cross-platform compatibility.
4. Re-build the solution and run the debugger to launch the project with SMAPI.
