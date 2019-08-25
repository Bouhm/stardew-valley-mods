## Adding support for custom locations

This documentation is for adding support for custom locations with **NPC Map Locatons**. This includes any newly added locations or modified existing locations.

**For more information on other features and usage of the mod, check the [Nexus Mods page](https://www.nexusmods.com/stardewvalley/mods/239).**

## Contents

- [How tracking works](#how-tracking-works)
- [Creating an accurate map](#creating-an-accurate-map)
- [Loading the custom map](#loading-the-custom-map)
  - [Folder selection](#folder-selection)
- [Adding points in config](#adding-points-in-config)
  - [Testing and validating](#testing-and-validating)
- [Adding location markers](#adding-location-markers)
- [Adding location tooltips](#adding-location-tooltips)
- [See also](#see-also)
- [Get additional help](#get-additional-help)

## How tracking works

For an outdoor location in the game, the mod needs at least two points of correspondence. The points will link the tile position in the location to the pixel position of the location on the map. Two points are required to create a bounding box (top-left corner and bottom-right corner) with which we can calculate the pixel position on the map from the current tile position within the bounding box.

Ideally, if the custom location is drawn accurately in proportion onto the map, only two points would be needed, for the tile position (0, 0) and (width, height) of the location tilemap. This is often not the case and more points will have to be added in order to increase accuracy. Splitting up the location into parts will be the most effective strategy to achieve this, instead of adding arbitrary points.

## Creating an accurate map

Creating a custom map is recommended if you are looking to add/modify a lot of areas on the map or need to do a recolor of the map. If you are looking to add just a few buildings or a small area, refer to [Adding location markers](#adding-location-markers).

In order to most effectively make tracking accurate for the custom location, the recommended method for drawing on the map is by using the actual tilemap of the custom location and drawing it to scale. Using the [Map Image Export mod](https://www.nexusmods.com/stardewvalley/mods/1073), you can get a screenshot of the whole tilemap of the location. Using this as a reference, you can be resize and overlay the tilemap to accurately trace it onto the map.

Here are the steps I take to create accurate modifications:

1. Get a screenshot of the tilemap using Map Image Export.
2. Open up a graphic editing software fit for pixel art ([Paint.NET](https://www.getpaint.net/) is great for Windows).
3. Open a NPC Map Locations map, from `"NPCMapLocations\assets\maps\_default\spring_map.png"` for example.
4. Open the tilemap from Map Image Export. Scale it down until it is approximately the size it should be on the map.
5. Overlay the resized tilemap onto the map, and then redraw that portion of the map accordingly. You will be essentially "tracing" the tilemap onto the map to ensure accuracy.
6. Repeat steps 1-5 for any other custom locations.

[Here is a quick video guide on the process.](https://streamable.com/xzfnc). In this example I am creating a map for Grandpa's Grove farm by Jessebot.

## Loading the custom map

NPC Map Locations will dynamically load the correct map based on other mods that are installed. For example, the mod will load the recolored map for Eemie's Recolour if it detects the Content Pack for that mod. The custom maps (if placed correctly, following the steps below) will load if the mod that adds the custom location is installed.

### Folder selection

When the game is launched, NPC Map Locations automatically chooses one folder to load the map from. The folder names can match combinations of mods:

| format  | effect                        |
| ------- | ----------------------------- |
| `A`     | Requires mod ID `A`.          |
| `A ~ B` | Requires mod ID `A` _or_ `B`. |
| `A, B`  | Requires _both_ `A` and `B`.  |

`~` is meant for alternative IDs, so it has precedence. For example, `A ~ B, C ~ D` means
`(A or B) and (C or D)`.

If multiple folders match, the first one alphabetically which matches more mods is used (e.g.
`A, B` has priority over `A`). If none match, the `_default` folder is used.

Refer to the [the map assets](https://github.com/Bouhm/stardew-valley-mods/tree/master/NPCMapLocations/assets/maps) for some examples on how this is done.

## Adding points in config

Any custom locations that need tracking need to be included in the config. The format is as shown:

```js
LocationName: [
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

![Map](https://i.imgur.com/YgTyxKE.png)
![Two-Points](https://i.imgur.com/J8Btvdj.png)
![Four-Points](https://i.imgur.com/kEkgn1A.png)

### Testing and validating

By turning on the `DEBUG_MODE` in the config by setting the value to true, additional information about the current locaton will be displayed in the game, including the exact name of the location and the dimensions of its tilemap. It will also give the player more freedom to move around by teleporting the player to the cursor (Alt+Right Click) to test the tracking.

After making the changes to the config, make sure to use the [JSON validator](https://json.smapi.io/) (select 'None' for JSON format) to make sure the JSON does not contain any errors.

## Adding location markers

Users can also add markers for buildings that will display on the existing map instead of creating a custom map for them.

![Custom-Textures](https://i.imgur.com/j6DQRQR.png)
![Texture-Overlay](https://i.imgur.com/0ADDJsb.png)

## Adding location tooltips

Users can add location tooltips when the player hovers the custom location if they choose to. The format includes the name of the location, the pixel position representng the top-left corner, the width and height of the bouding box where the tooltip hover should trigger, and the primary and secondary text that should display in the tooltip.

![Tooltips](https://i.imgur.com/XU4ljAR.png)

## See also

- [Config that adds support for Stardew Valle Expanded locations](https://github.com/Bouhm/stardew-valley-mods/blob/master/NPCMapLocations/config/sve_config.json)
- [My other mod that inherently supports all custom locations](https://www.nexusmods.com/stardewvalley/mods/3045)

## Get additional help

You can reach me on [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/239) and leave a comment there, or you can ping me `@Bouhm` on the [Stardew Valley Discord](https://discord.gg/stardewvalley) in the `#modding` channel.
