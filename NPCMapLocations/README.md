## Adding custom locations

This documentation is for adding support for custom locations with **NPC Map Locatons**. This includes any newly added locations or modified existing locations. These instructions are intended to be followed on a PC.

**For more information on other features and usage of the mod, check the [Nexus Mods page](https://www.nexusmods.com/stardewvalley/mods/239).**

## Contents

- [How tracking works](#how-tracking-works)
- [Creating an accurate map](#creating-an-accurate-map)
  - [Video tutorial](#video-tutorial)
  - [Seasonal maps](#seasonal-maps)
- [Loading the custom map](#loading-the-custom-map)
  - [Folder selection](#folder-selection)
- [Unknown locations](#unknown-locations)
- [Adding points](#adding-points)
  - [Example](#example)
    - [Farm types](#farm-types)
  - [Single points](#single-points)
  - [Adding location tooltips](#adding-location-tooltips)
  - [Testing and validating](#testing-and-validating)
  - [Common issues](#common-issues)
- [See also](#see-also)
- [Get additional help](#get-additional-help)
- [Excluding Custom NPCs (For Developers)](#exclude-custom-npcs)

## How tracking works

For any outdoor location in the game, the mod needs at least two pairs of points to calculate the map location. The two pairs of points are required to create a bounding box (top-left corner and bottom-right corner) with which we can calculate the pixel position on the map from the current tile position in the location. One pair is the top-left and bottom-right pixel points on the map. The second pair are the top-left and bottom-right tile positions in the location.

![Two-Points](https://i.imgur.com/J8Btvdj.png)

Ideally, the custom location is drawn accurately in proportion onto the map such that only two points would be needed, for the tile position (0, 0) and (width, height) of the location tilemap. If the custom location is not proportionally accurate, more points will have to be added in order to increase accuracy. Splitting up the location logically into sections based on the landscape will be the most effective strategy to achieve this.

![Four-Points](https://i.imgur.com/kEkgn1A.png)

## Creating an accurate map

Creating a custom map is recommended if you are looking to add/modify a lot of areas on the map or need to do a recolor of the map. If you are looking to add just a few buildings or a small area, consider creating a [Content Pack for Content Patcher](https://github.com/Pathoschild/StardewMods/blob/develop/ContentPatcher/docs/author-guide.md#editimage) instead with `"PatchMode": "Overlay"`. Do not **`"Replace"`** the map as it will cause a conflict with NPC Map Locations.

In order to draw a custom location accurately, the recommended method is to use the actual tilemap of the custom location and tracing it. Using the [Map Image Export mod](https://www.nexusmods.com/stardewvalley/mods/1073), you can get a render of the whole tilemap of the location. Using this as a reference, you can resize and overlay the tilemap to accurately trace it onto the map.

Here are the steps I take to create accurate modifications:

1. Get a screenshot of the tilemap using Map Image Export.
2. Open up a graphic editing software fit for pixel art ([Paint.NET](https://www.getpaint.net/) is great for Windows, [GIMP](https://www.gimp.org/) is another free cross-platform alternative).
3. Open a NPC Map Locations map, from `"NPCMapLocations\maps\_default\spring_map.png"` for example.
4. Open the tilemap from Map Image Export. Scale it down until it is approximately the size it should be on the map.
5. Overlay the resized tilemap onto the map, and then redraw that portion of the map accordingly. You will be essentially "tracing" the tilemap onto the map to ensure accuracy.
6. Repeat steps 1-5 for any other custom locations.

### Video tutorial

[Here is a short video guide on the process](https://streamable.com/xzfnc). In this example I am creating a map for Grandpa's Grove farm by Jessebot.

### Seasonal maps

NPC Map Locations also provides seasonal maps that dynamically load with the game season. If you choose not to create additional maps for different seasons, just create one for `spring_map.png` in the respective map folder and that will be the map used for all seasons, just like in the vanilla game.

## Loading the custom map

NPC Map Locations will dynamically load the correct map based on other mods that are installed. For example, the mod will load the recolored map for Eemie's Recolour if it detects the Content Pack for that mod. The custom maps (if placed correctly, following the steps below) will load if the mod that adds the custom location is installed.

### Folder selection

When the game is launched, NPC Map Locations automatically chooses one folder to load the map from. The folder names can match combinations of mods:

| format  | effect                        |
| ------- | ----------------------------- |
| `A`     | Requires mod ID `A`.          |
| `A ~ B` | Requires mod ID `A` _or_ `B`. |
| `A, B`  | Requires _both_ `A` and `B`.  |

Where the mod ID is the `UniqueID` for the mod's `manifest.json`.

`~` is meant for alternative IDs, so it has precedence. For example, `A ~ B, C ~ D` means
`(A or B) and (C or D)`.

If multiple folders match, the first one alphabetically which matches more mods is used (e.g.
`A, B` has priority over `A`). If none match, the `_default` folder is used.

The folder should include the following files:

- customlocations.json (only if there are custom locations)
- spring_map.png
- fall_map.png (if adding seasonal maps)
- summer_map.png (if adding seasonal maps)
- winter_map.png (if adding seasonal maps)
- buildings.png (use the provided and modify if desired)

You can use the template [with seasonal maps](https://github.com/Bouhm/stardew-valley-mods/tree/master/NPCMapLocations/maps/_template) or [without seasonal maps](https://github.com/Bouhm/stardew-valley-mods/tree/master/NPCMapLocations/maps/_template_no_seasonal) to get started.

Refer to the [maps](https://github.com/Bouhm/stardew-valley-mods/tree/master/NPCMapLocations/maps) for some examples on the naming convention.

## Unknown locations

NPC Map Locations will try its best to figure out where custom locations are. Each location is categorized into three types: outdoor, building, and room. Buildings exist within outdoor locations and rooms exist within indoor locations. The indoor locations are located on the map based on their warps in the outdoor map, so indoor locations within can be automatically located as long as the outdoor location has tracking (explained below). Indoor locations are single points on the map and do not require tracking. This leaves just custom outdoor locations (or sometimes indoor locations with ambiguous warps) for manual tracking. These unknown locations are identified by the mod and can be found as debug messages in the SMAPI console: 

```
[NPC Map Locations] Unknown location: WizardHouseBasement
[NPC Map Locations] Unknown location: CrimsonBadlands
[NPC Map Locations] Unknown location: DesertRailway
[NPC Map Locations] Unknown location: IridiumQuarry
[NPC Map Locations] Unknown location: TreasureCave
```

A video tutorial is available for how I add tracking for these SVE custom locations below.

## Adding points

Any custom locations that need tracking need to be included in `maps\customlocations.json`. The format for adding one location is as shown:

```js
"LocationName": [
  {
    MapX: 0,
    MapY: 0,
    TileX: 300,
    TileY: 300
  },
  {
    MapX: 100,
    MapY: 80,
    TileX: 500,
    TileY: 450
  }
]
```

Where `LocationName` is the name of the location. Each field between the curly brackets represents one point that maps the tile position to the pixel position on the map.

**It is important to note that when getting the pixel positions on the map, you will need to upscale the map image (Nearest Neighbor) by 4 times since that is the zoom scale used in the game.**

![Map](https://i.imgur.com/YgTyxKE.png)

By turning on the `DEBUG_MODE` in the `config\globals.json` by setting `"DEBUG_MODE": true`, additional information and helpful tools about the current locaton will be displayed in the game, including the exact name of the location, dimensions of its tilemap, and information about the selected box on the map. It will also give the player more freedom to move around by teleporting the player to the cursor (Ctrl+RightClick) to test the tracking.

### Example

Let's look at an example using the custom location "TownEast" added on by the Stardew Valley Expanded mod.

The first step would be to physically go to the location in-game with `DEBUG_MODE` on to gather information.

![location-info](https://i.imgur.com/nNSyPZi.png)

Then we can use the drag-and-drop tools to create the bounding box for tracking for that location on the map. To perform drag-and-drop, place the cursor at the corner for top-left, right-click with the mouse and hold, and drag it to the bottom-right corner and release. You must always go from top-left to bottom-right.

![map-info](https://i.imgur.com/Z42LauO.png)

From these two actions we have the following information: the LocationName "TownEast", the size of the tilemap (40 x 30), and the coordinates of the bounding box (960, 337) and (1060, 413).

For tracking, we add an entry for `"TownEast"` in `"CustomMapLocations"`. We input the two points for the top-left corner of the bounding box and the bottom-right corner of the bounding box.

```js
"CustomMapLocations": {
  "TownEast": [
    {
      MapX: 960,
      MapY: 337,
      TileX: 0,
      TileY: 0
    },
    {
      MapX: 1060,
      MapY: 413,
      TileX: 40,
      TileY: 30
    }
  ]
},
```

#### Farm types

Sometimes a mod will change a farm only based on the farm type. If you want to specify points for any farm, you can leave the location name as "Farm" but for specific farm types, you will need to use the following:
- "Farm_Default"
- "Farm_Riverland"
- "Farm_Forest"
- "Farm_Hills"
- "Farm_Wilderness"
- "Farm_FourCorners"

### Single points

Instead of an area with tracking, if you want to display the character in a location in a single point on the map, you only need to specify the `MapX` and `MapY` like so:

```js
"CustomMapLocations": {
  "PointOfInterest": [
    {
      MapX: 36,
      MapY: 469
    },
  ]
},
```

Other points are not needed because we're simply showing the character at (36, 469) on the map in that location without needing to do any calcualtions.

**Note that you do NOT need to add any indoor locations. NPC Map Locations will automatically find indoor locations on the map, given that the tracking for its outdoor location is accurate and there exists a valid warp into the indoor location.**

### Adding location tooltips

Users can add location tooltips when the player hovers the custom location if they choose to. The format includes the name of the location, the pixel position representng the top-left corner, the width and height of the bouding box where the tooltip hover should trigger, and the primary and secondary text that should display in the tooltip.

![Tooltips](https://i.imgur.com/UJSgR6l.png)

For this we need the top-left corner and the width and height of the bounding box.

```js
"CustomMapTooltips": {
  "TownEast": {
    "X": 960,
    "Y": 337,
    "Width": 100,
    "Height": 76,
    "PrimaryText": "Town East"
  }
}
```

For adding just this one location, our final JSON file will look like this:

```js
{
  "CustomMapLocations": {
    "TownEast": [
      {
        MapX: 557,
        MapY: 516,
        TileX: 0,
        TileY: 0
      },
      {
        MapX: 678,
        MapY: 580,
        TileX: 40,
        TileY: 30
      }
    ]
  },
  "CustomMapTooltips": {
    "TownEast": {
      "X": 960,
      "Y": 337,
      "Width": 100,
      "Height": 76,
      "PrimaryText": "Town East"
    }
  }
}
```

### Testing and validating

After making the changes to the config, make sure to use the [JSON validator](https://json.smapi.io/) (select 'None' for JSON format) to make sure the JSON does not contain any errors.

For the example above, we can see that this is valid json.

![json-validator](https://i.imgur.com/jofId5l.png)

If the JSON is valid, you can go into game and test the newly added custom locations on the map. If the JSON is not valid, SMAPI will throw an error and the game will crash.

### Common issues

If the character does not show up in the map in the custom locations, there are a few possible issues to troubleshoot:

- The folder containing the custom map is not named correctly
  - You can confirm this by checking the SMAPI console or the [logs](log.smapi.io) and looking for the message that indicates which map NPC Map Locations has loaded.
- The location is not properly added in `customlocations.json`. It must be added in properly by its exact location name, provided with `DEBUG_MODE` info.

## See also

- [Data file that adds support for Stardew Valley Expanded locations](https://github.com/Bouhm/stardew-valley-mods/blob/master/NPCMapLocations/maps/flashShifter.stardewValleyExpandedCP/customlocations.json)
- [My other mod that also shows characters while inherently supporting all custom locations](https://www.nexusmods.com/stardewvalley/mods/3045)

## Get additional help

You can reach me on [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/239) and leave a comment there, or you can ping me `@Bouhm` on the [Stardew Valley Discord](https://discord.gg/stardewvalley) in the `#modding` channel.

## Excluding Custom NPCs from the map (For Developers)

If you are a developer for a Custom NPC mod, you can choose to always exclude your NPC from showing up in NPC Map Locations by adding a custom field in NPCDispositions.
The easiest way to do this is by adding to NPCDispositions through [Content Patcher](https://github.com/Pathoschild/StardewMods/blob/develop/ContentPatcher/docs/author-guide.md#editdata). The field to add is `ExludeFromMap`.
This is an example of what the patch would look like:

```
{
  "Action": "EditData",
  "Target": "Data/NPCDispositions",
  "Entries": {
    "ExcludeFromMap": "true"
  }
}
```

(Shoutout to @kdau for this suggestion.)