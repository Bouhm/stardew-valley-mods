﻿﻿[← back to readme](README.md)

# Release notes
## 1.3.7
Released 20 October 2021 for SMAPI 3.12.5. Updated by Pathoschild.

* Improved support for Volcano Dungeon.
* Fixed various edge cases.

## 1.3.6
Released 24 August 2021 for SMAPI 3.12.2. Updated by Pathoschild.

* Fixed mod not working correctly if a location couldn't be loaded.
* Fixed error loading map info for the Volcano Dungeon. That area is now ignored.

## 1.3.5
Released 20 August 2021 for SMAPI 3.12.2. Updated by Pathoschild.

* Fixed possible errors when scanning locations.

## 1.3.4
Released 06 August 2021 for SMAPI 3.12.2. Updated by Pathoschild.

* Fixed crash when scanning locations in some cases.

## 1.3.3
Released 17 July 2021 for SMAPI 3.11.0. Updated by Pathoschild.

* Fixed crash when a location has circular warps in some cases.
* Fixed error when a location has invalid warp targets.
* Internal refactoring.

## 1.3.2
Released 02 March 2021 for Stardew Valley 1.4 and SMAPI 3.0.

* Increased update rate in multiplayer.
* Internal refactoring.

## 1.3.1
Released 19 January 2019 for Stardew Valley 1.4 and SMAPI 3.0.

* Added warning if host player doesn't have Location Compass installed in multiplayer.
* Fixed compatibility with MTN2.
* Fixed other minor bugs.

## 1.3.0
Released 16 December 2018 for Stardew Valley 1.4 and SMAPI 2.9.

* Updated for SMAPI 3.0.
* Fixed issue where NPC locations weren't always updating in multiplayer.  
  _Note that NPC movement on locators will appear choppy for farmhands in multiplayer because the updates are only synced every second for performance reasons._
* Fixed bugs.

## 1.2.0
Released 19 November 2018 for Stardew Valley 1.3.32 and SMAPI 2.8.

* Now works for all players who have it installed in multiplayer.
* Added new config options:
  * Enable toggle mode instead of holding key to show locators.
  * Show characters in neighboring locations (toggle with `H` while showing locators).
  * Show only characters with quests/birthdays (toggle with `J` while showing locators).
  * Option to show only farmers (toggle with `K` while showing locators).
  * Option to show horses (toggle with `L` while showing locators).

## 1.0.0
Released 09 November 2018 for Stardew Valley 1.3.28 and SMAPI 2.7.

* Initial release. NPCs only show for host players.
