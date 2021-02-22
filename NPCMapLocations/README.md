## Adding custom locations

This documentation is for adding support for custom locations with **NPC Map Locatons**. This includes any newly added locations or modified existing locations. These instructions are intended to be followed on a PC.

**For more information on other features and usage of the mod, check the [Nexus Mods page](https://www.nexusmods.com/stardewvalley/mods/239).**

## Contents

- [How tracking works](#how-tracking-works)
- [Creating an accurate map](#creating-an-accurate-map)
  - [Video tutorial](#video-tutorial)
  - [Seasonal maps](#seasonal-maps)
- [Loading the custom map](#loading-the-custom-map)
- [Unknown locations](#unknown-locations)
- [Adding points](#adding-points)
  - [Example](#example)
  - [Farm types](#farm-types)
  - [Single points](#single-points)
  - [Adding location tooltips](#adding-location-tooltips)
  - [Excluding locations](#excluding-locations)
  - [Testing and validating](#testing-and-validating)
- [Custom Npcs](#custom-npcs)
  - [Excluding NPCs](#excluding-npcs)
- [Get additional help](#get-additional-help)

## How tracking works

For any outdoor location in the game, the mod needs at least two pairs of points to calculate the map location. The two pairs of points are required to create a bounding box (top-left corner and bottom-right corner) with which we can calculate the pixel position on the map from the current tile position in the location. One pair is the top-left and bottom-right pixel points on the map. The second pair are the top-left and bottom-right tile positions in the location.

![Two-Points](https://i.imgur.com/J8Btvdj.png)

Ideally, the custom location is drawn accurately in proportion onto the map such that only two points would be needed, for the tile position (0, 0) and (width, height) of the location tilemap. If the custom location is not proportionally accurate, more points will have to be added in order to increase accuracy. Splitting up the location logically into sections based on the landscape will be the most effective strategy to achieve this.

![Four-Points](https://i.imgur.com/kEkgn1A.png)

## Creating an accurate map

Creating a custom map is recommended if you are looking to add/modify a lot of areas on the map or need to do a recolor of the map. Mod authors should **`"Replace"`** the map.

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
NOTE: This video guide is outdated, but the method is still the same.

### Seasonal maps

NPC Map Locations also provides seasonal maps that dynamically load with the game season. If you choose not to create additional maps for different seasons, just create one for `spring_map.png` in the respective map folder and that will be the map used for all seasons, just like in the vanilla game.

## Loading the custom map

NPC Map Locations provides its own default maps, however when in use with any mods that modify the world, the mod author should provide their owns maps using through Content Patcher.

The folder should include the following files:
- spring_map.png
- fall_map.png (if adding seasonal maps)
- summer_map.png (if adding seasonal maps)
- winter_map.png (if adding seasonal maps)
- buildings.png (use the provided and modify if desired)

If not using seasonal maps, just a "spring_map.png" map will suffice.

## Unknown locations

NPC Map Locations will try its best to figure out where custom locations are. Each location is categorized into three types: outdoor, building, and room. Buildings exist within outdoor locations and rooms exist within indoor locations. The indoor locations are located on the map based on their warps in the outdoor map, so indoor locations within can be automatically located as long as the outdoor location has tracking (explained below). Indoor locations are single points on the map and do not require tracking. This leaves just custom outdoor locations (or sometimes indoor locations with ambiguous warps) for manual tracking. These unknown locations are identified by the mod and can be found as debug messages in the SMAPI console: 

```
[NPC Map Locations] Unknown location: WizardHouseBasement
[NPC Map Locations] Unknown location: CrimsonBadlands
[NPC Map Locations] Unknown location: DesertRailway
[NPC Map Locations] Unknown location: IridiumQuarry
[NPC Map Locations] Unknown location: TreasureCave
```

## Adding points

Any custom locations that need tracking need to be included in the `content.json` of the mods that adds custom locations to the game.
The target MUST be exactly as below.

```js
{   
  "Action": "EditData",
  "Target": "Mods/Bouhm.NPCMapLocations/Locations",
  "Entries": {
    "[LocationName]": {
      "MapVectors": [
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
    }
  }
}
```

Where `[LocationName]` is the name of the location. Each field between the curly brackets represents one point that maps the tile position to the pixel position on the map.

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
{   
  "Action": "EditData",
  "Target": "Mods/Bouhm.NPCMapLocations/Locations",
  "Entries": {
    "TownEast": {
      "MapVectors": [
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
    }
  }
}
```

Tips:
- In config/globals.json, set "DEBUG_MODE" to true.
- In the SMAPI console, use `debug warp [LocationName] to quickly move to the map.
- Use control + right click to move the player around the map in debug mode.

### Farm types

Sometimes a mod will change a farm only based on the farm type. If you want to specify points for any farm, you can leave the location name as "Farm" but for specific farm types, you will need to use the following:
- "Farm_Default"
- "Farm_Riverland"
- "Farm_Forest"
- "Farm_Hills"
- "Farm_Wilderness"
- "Farm_FourCorners"
- "Farm_Beach"

### Single points

Instead of an area with tracking, if you want to display the character in a location in a single point on the map, you only need to specify the `MapX` and `MapY` like so:

```js
{   
  "Action": "EditData",
  "Target": "Mods/Bouhm.NPCMapLocations/Locations",
  "Entries": {
    "[LocationName]": {
      "MapVectors": [
        {
          MapX: 36,
          MapY: 469
        }
      ]
    },
  ...
  }
}
```

Other points are not needed because we're simply showing the character at (36, 469) on the map in that location without needing to do any calcualtions.

**Note that you do NOT need to add any indoor locations. NPC Map Locations will automatically find indoor locations on the map, given that the tracking for its outdoor location is accurate and there exists a valid warp into the indoor location.**

### Adding location tooltips

Mod authors can add map tooltips when the player hovers the custom location if they choose to. The format includes the name of the location, the pixel position representng the top-left corner, the width and height of the bouding box where the tooltip hover should trigger, and the primary and secondary text that should display in the tooltip.

![Tooltips](https://i.imgur.com/UJSgR6l.png)

For this we need the top-left corner and the width and height of the bounding box.

```js
{   
  "Action": "EditData",
  "Target": "Mods/Bouhm.NPCMapLocations/Locations",
  "Entries": {
    "TownEast": {
      ...
      "MapToolip": {
        "X": 960,
        "Y": 337,
        "Width": 100,
        "Height": 76,
        "PrimaryText": "Town East"
      }
    }
  }
}
```

For adding just this one location, our final JSON file will look like this:

```js
{   
  "Action": "EditData",
  "Target": "Mods/Bouhm.NPCMapLocations/Locations",
  "Entries": {
    "TownEast": {
      "MapVectors": [
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
      ],
      "MapToolip": {
        "X": 960,
        "Y": 337,
        "Width": 100,
        "Height": 76,
        "PrimaryText": "Town East"
      }
    }
  }
}
```
### Excluding locations
For any custom locations that should be HIDDEN from the map (ex. when a character is in that location, they should not show up on the map), they need to be added to the LocationExclusions field.

```js
{   
  "Action": "EditData",
  "Target": "Mods/Bouhm.NPCMapLocations/Locations",
  "Entries": {
    "Claire_WarpRoom": {
      "Exclude": true
    }
  }
}
```

## Custom NPCs
For custom npcs that need the cropping adjusted for their markers on the map, we use the same method but with a different target:
This time instead of the [LocationName], we use the name of the NPC for the key.
Use the field "MarkerCropOffset" with an integer value. See https://www.nexusmods.com/stardewvalley/articles/99 for details.

```js
{   
  "Action": "EditData",
  "Target": "Mods/Bouhm.NPCMapLocations/NPCs",
  "Entries": {
    "Andy": {
      "MarkerCropOffset": -1
    }
  }
}
```

### Excluding NPCs
For any NPCs that should be HIDDEN from the map, we use the field `Exclude` which can be value `true` or `false` (not including the field is effectively the same as `false`).
```js
{   
  "Action": "EditData",
  "Target": "Mods/Bouhm.NPCMapLocations/NPCs",
  "Entries": {
    "Claire_Joja": {
      "Exclude": true
    }
  }
}
```

### Testing and validating

After making the changes to the config, make sure to use the [JSON validator](https://json.smapi.io/) to make sure the JSON does not contain any errors.
If the JSON is valid, you can go into game and test the newly added custom locations on the map. If the JSON is not valid, SMAPI will throw an error and the game will crash.

## Get additional help

Visit [Stardew Valley Discord](https://discord.gg/stardewvalley) in the `#using-mods` channel.
