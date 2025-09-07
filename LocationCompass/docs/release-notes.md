﻿﻿[← back to readme](README.md)

# Release notes
## 1.5.1
Released 07 September 2025 for SMAPI 4.1.2 or later. Updated by Pathoschild.

* Improved translations. Thanks to k1llm3sixy (added Russian) and trucpham04 (added Vietnamese)!

## 1.5.0
Released 25 May 2025 for SMAPI 4.1.2 or later. Updated by Pathoschild.

* Added support for controller/mouse button bindings.
* Added support for [multi-key-bindings](https://stardewvalleywiki.com/Modding:Player_Guide/Key_Bindings#Multi-key_bindings) (e.g. for split-screen support).
* Optimized performance.
* Fixed various errors related to unexpected game or mod state.
* Improved translations. Thanks to Kondiq (added Polish)!

## 1.4.2
Released 04 November 2024 for SMAPI 4.1.2 or later. Updated by Pathoschild.

* Fixed the previous update being broken on Linux/macOS.

## 1.4.1
Released 04 November 2024 for SMAPI 4.1.0 or later. Updated by Pathoschild.

* Fixed 'host does not have Location Compass installed' check on the host.
* Improved translations. Thanks to juliowh (added Portuguese), mitekano23 (added Japanese), and ranke96 (added Chinese)!

## 1.4.0
Released 21 April 2024 for SMAPI 4.0.7 or later. Updated by Pathoschild.

* Added integration with Generic Mod Config Menu.
* Updated for Stardew Valley 1.6.5.

## 1.3.13
Released 19 March 2024 for SMAPI 4.0.0 or later. Updated by Pathoschild.

* Updated for Stardew Valley 1.6.

## 1.3.12
Released 25 June 2023 for SMAPI 3.14.0 or later. Updated by Pathoschild.

* Embedded `.pdb` data into the DLL, which fixes error line numbers in Linux/macOS logs.

## 1.3.11
Released 13 June 2022 for SMAPI 3.14.0 or later. Updated by Pathoschild.

* Fixed markers not shown for NPCs in a farmhand cabin.

## 1.3.10
Released 13 May 2022 for SMAPI 3.14.0 or later. Updated by Pathoschild.

* Updated for SMAPI 3.14.0.

## 1.3.9
Released 27 December 2021 for SMAPI 3.13.0 or later. Updated by Pathoschild.

* Updated for Stardew Valley 1.5.5.

## 1.3.8
Released 26 November 2021 for SMAPI 3.12.5 or later. Updated by Pathoschild.

* Added option to keep the HUD visible while locator icons are shown.
* Fixed Caldera not handled as part of the Volcano Dungeon.

## 1.3.7
Released 20 October 2021 for SMAPI 3.12.5 or later. Updated by Pathoschild.

* Improved support for Volcano Dungeon.
* Fixed various edge cases.

## 1.3.6
Released 24 August 2021 for SMAPI 3.12.2 or later. Updated by Pathoschild.

* Fixed mod not working correctly if a location couldn't be loaded.
* Fixed error loading map info for the Volcano Dungeon. That area is now ignored.

## 1.3.5
Released 20 August 2021 for SMAPI 3.12.2 or later. Updated by Pathoschild.

* Fixed possible errors when scanning locations.

## 1.3.4
Released 06 August 2021 for SMAPI 3.12.2 or later. Updated by Pathoschild.

* Fixed crash when scanning locations in some cases.

## 1.3.3
Released 17 July 2021 for SMAPI 3.11.0 or later. Updated by Pathoschild.

* Fixed crash when a location has circular warps in some cases.
* Fixed error when a location has invalid warp targets.
* Internal refactoring.

## 1.3.2
Released 02 March 2021 for Stardew Valley 1.4 and SMAPI 3.0.0 or later.

* Increased update rate in multiplayer.
* Internal refactoring.

## 1.3.1
Released 19 January 2019 for Stardew Valley 1.4 and SMAPI 3.0.0 or later.

* Added warning if host player doesn't have Location Compass installed in multiplayer.
* Fixed compatibility with MTN2.
* Fixed other minor bugs.

## 1.3.0
Released 16 December 2018 for Stardew Valley 1.4 and SMAPI 2.9.0 or later.

* Updated for SMAPI 3.0.
* Fixed issue where NPC locations weren't always updating in multiplayer.  
  _Note that NPC movement on locators will appear choppy for farmhands in multiplayer because the updates are only synced every second for performance reasons._
* Fixed bugs.

## 1.2.0
Released 19 November 2018 for Stardew Valley 1.3.32 and SMAPI 2.8.0 or later.

* Now works for all players who have it installed in multiplayer.
* Added new config options:
  * Enable toggle mode instead of holding key to show locators.
  * Show characters in neighboring locations (toggle with `H` while showing locators).
  * Show only characters with quests/birthdays (toggle with `J` while showing locators).
  * Option to show only farmers (toggle with `K` while showing locators).
  * Option to show horses (toggle with `L` while showing locators).

## 1.0.0
Released 09 November 2018 for Stardew Valley 1.3.28 and SMAPI 2.7.0 or later.

* Initial release. NPCs only show for host players.
