This folder contains map tilesheet compatibility files for various mods.

## Folder selection
When the game is launched, NPC Map Locations automatically chooses one folder to load the map
tilesheets from. The folder names can match combinations of mods:

format  | effect
------- | ------
`A`     | Requires mod ID `A`.
`A ~ B` | Requires mod ID `A` _or_ `B`.
`A, B`  | Requires _both_ `A` and `B`.

`~` is meant for alternative IDs, so it has precedence. For example, `A ~ B, C ~ D` means
`(A or B) and (C or D)`.

If multiple folders match, the first one alphabetically which matches more mods is used (e.g.
`A, B` has priority over `A`). If none match, the `_default` folder is used.

## Adding recolors
You can add any folder matching the above conventions. Feel free to submit recolors to
[the GitHub repo](https://github.com/Bouhm/stardew-valley-mods) for others to use too!
