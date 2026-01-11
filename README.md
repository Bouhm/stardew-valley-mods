﻿This repository contains my SMAPI mods for Stardew Valley. See the individual mods for
documentation and release notes.

## Mods
* **NPC Map Locations** ([Nexus](https://www.nexusmods.com/stardewvalley/mods/239) | [source](NPCMapLocations))  
  Shows character locations on the map. Uses a modified map page much more accurate to the game.
* **Location Compass** ([Nexus](https://www.nexusmods.com/stardewvalley/mods/3045) | [source](LocationCompass))  
  Locates characters on the screen indicating direction and distance from player's current position.
* **Pet Dogs Mod** ([Nexus](https://www.nexusmods.com/stardewvalley/mods/570) | [source](PetDogs))  
  Replaces the pet dog with a Shiba Inu, Shepherd, or Husky.

## Translating the mods
<!--

    This section is auto-generated using a script, there's no need to edit it manually.
    https://github.com/Pathoschild/StardewScripts/tree/main/create-translation-summary

-->
The mods can be translated into any language supported by the game, and SMAPI will automatically
use the right translations.

Contributions are welcome! See [Modding:Translations](https://stardewvalleywiki.com/Modding:Translations)
on the wiki for help contributing translations.

(❑ = untranslated, ↻ = partly translated, ✓ = fully translated)

&nbsp;       | NPC Location Compass              | NPC Map Locations
:----------- | :-------------------------------- | :--------------------------------
Chinese      | [✓](LocationCompass/i18n/zh.json) | [↻](NPCMapLocations/i18n/zh.json)
French       | [❑](LocationCompass/i18n)         | [↻](NPCMapLocations/i18n/fr.json)
German       | [❑](LocationCompass/i18n)         | [↻](NPCMapLocations/i18n/de.json)
Hungarian    | [❑](LocationCompass/i18n)         | [↻](NPCMapLocations/i18n/hu.json)
Italian      | [✓](LocationCompass/i18n/it.json) | [↻](NPCMapLocations/i18n/it.json)
Japanese     | [✓](LocationCompass/i18n/ja.json) | [↻](NPCMapLocations/i18n/ja.json)
Korean       | [❑](LocationCompass/i18n)         | [↻](NPCMapLocations/i18n/ko.json)
[Polish]     | [✓](LocationCompass/i18n/pl.json) | [↻](NPCMapLocations/i18n/pl.json)
Portuguese   | [✓](LocationCompass/i18n/pt.json) | [↻](NPCMapLocations/i18n/pt.json)
Russian      | [✓](LocationCompass/i18n/ru.json) | [↻](NPCMapLocations/i18n/ru.json)
Spanish      | [❑](LocationCompass/i18n)         | [↻](NPCMapLocations/i18n/es.json)
[Thai]       | [❑](LocationCompass/i18n)         | [↻](NPCMapLocations/i18n/th.json)
Turkish      | [❑](LocationCompass/i18n)         | [↻](NPCMapLocations/i18n/tr.json)
[Ukrainian]  | [❑](LocationCompass/i18n)         | [↻](NPCMapLocations/i18n/uk.json)
[Vietnamese] | [✓](LocationCompass/i18n/vi.json) | [↻](NPCMapLocations/i18n/vi.json)

[Polish]: https://www.nexusmods.com/stardewvalley/mods/3616
[Thai]: https://www.nexusmods.com/stardewvalley/mods/7052
[Ukrainian]: https://www.nexusmods.com/stardewvalley/mods/8427
[Vietnamese]: https://www.nexusmods.com/stardewvalley/mods/24371

## Compiling the mods
Installing stable releases from Nexus Mods is recommended for most users. If you really want to
compile the mod yourself, read on.

These mods use the [crossplatform build config](https://smapi.io/package) so they can be built on
Linux, Mac, and Windows without changes. See [the build config documentation](https://smapi.io/package)
for troubleshooting.

### Compiling a mod for testing
To compile a mod and add it to your game's `Mods` directory:

1. Rebuild the project in [Visual Studio](https://www.visualstudio.com/vs/community/) or
   [MonoDevelop](https://www.monodevelop.com/).  
   <small>This will compile the code and package it into the mod directory.</small>
2. Launch the project with debugging.  
   <small>This will start the game through SMAPI and attach the Visual Studio debugger.</small>

### Compiling a mod for release
To package a mod for release:

1. Switch to `Release` build configuration.
2. Recompile the mod per the previous section.
3. Upload the generated `bin/Release/<mod name>-<version>.zip` file from the project folder.
