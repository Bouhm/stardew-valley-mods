/*
MapPage for the mod that handles logic for tooltips
and drawing everything.
Based on regurgitated game code.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using Bouhm.Shared.Locations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace NPCMapLocations.Framework.Menus;

internal class ModMapPage : MapPage
{
    /*********
    ** Fields
    *********/
    private Dictionary<string, bool> ConditionalNpcs { get; }
    private Dictionary<string, NpcMarker> NpcMarkers { get; }
    private Dictionary<long, FarmerMarker> FarmerMarkers { get; }
    private Dictionary<string, BuildingMarker> FarmBuildings { get; }

    private readonly Texture2D BuildingMarkers;
    private readonly ModCustomizations Customizations;
    private string HoveredNames = "";
    private bool HasIndoorCharacter;
    private Vector2 IndoorIconVector;

    /// <summary>Scans and maps locations in the game world.</summary>
    private readonly LocationUtil LocationUtil;

    /// <summary>The world map region ID being shown on the map.</summary>
    private string RegionId => this.mapRegion.Id;


    /*********
    ** Public methods
    *********/
    // Map menu that uses modified map page and modified component locations for hover
    public ModMapPage(
        int x,
        int y,
        int width,
        int height,
        Dictionary<string, NpcMarker> npcMarkers,
        Dictionary<string, bool> conditionalNpcs,
        Dictionary<long, FarmerMarker> farmerMarkers,
        Dictionary<string, BuildingMarker> farmBuildings,
        Texture2D buildingMarkers,
        ModCustomizations customizations,
        LocationUtil locationUtil
    )
        : base(x, y, width, height)
    {
        this.NpcMarkers = npcMarkers;
        this.ConditionalNpcs = conditionalNpcs;
        this.FarmerMarkers = farmerMarkers;
        this.FarmBuildings = farmBuildings;
        this.BuildingMarkers = buildingMarkers;
        this.Customizations = customizations;
        this.LocationUtil = locationUtil;
    }

    public override void performHoverAction(int x, int y)
    {
        // reset baseline tooltips
        base.performHoverAction(x, y);
        this.HoveredNames = "";
        this.HasIndoorCharacter = false;

        // apply custom tooltips
        bool hasNpcMarkers = this.NpcMarkers?.Count > 0;
        bool hasFarmerMarkers = Context.IsMultiplayer && this.FarmerMarkers?.Count > 0;
        if (hasNpcMarkers || hasFarmerMarkers)
        {
            // get hovered names
            string[] hoveredNames;
            bool hasIndoorCharacters;
            {
                const int markerWidth = 32;
                const int markerHeight = 30;

                string regionId = this.RegionId;
                Point mousePos = Game1.getMousePosition();

                HashSet<string> newHoveredNames = new();
                HashSet<string> indoorLocationNames = new();

                // add markers directly under cursor
                if (hasNpcMarkers)
                {
                    foreach ((string npcName, NpcMarker npcMarker) in this.NpcMarkers)
                    {
                        if (npcMarker.IsHidden || npcMarker.WorldMapPosition.RegionId != regionId || !this.IsMapPixelUnderCursor(mousePos, npcMarker.WorldMapPosition, markerWidth, markerHeight))
                            continue;

                        newHoveredNames.Add(this.GetNpcDisplayName(npcName));

                        if (!this.LocationUtil.IsOutdoors(npcMarker.LocationName))
                            indoorLocationNames.Add(npcMarker.LocationName);
                    }
                }
                if (hasFarmerMarkers)
                {
                    foreach (FarmerMarker farmerMarker in this.FarmerMarkers.Values)
                    {
                        if (farmerMarker.WorldMapPosition.RegionId != regionId || !this.IsMapPixelUnderCursor(mousePos, farmerMarker.WorldMapPosition, markerWidth / 2, markerHeight / 2))
                            continue;

                        newHoveredNames.Add(farmerMarker.Name);

                        if (!this.LocationUtil.IsOutdoors(farmerMarker.LocationName))
                            indoorLocationNames.Add(farmerMarker.LocationName);
                    }
                }

                // add any other markers in the same indoor locations
                if (indoorLocationNames.Count > 0)
                {
                    if (hasNpcMarkers)
                    {
                        foreach ((string npcName, NpcMarker npcMarker) in this.NpcMarkers)
                        {
                            if (!npcMarker.IsHidden && indoorLocationNames.Contains(npcMarker.LocationName))
                                newHoveredNames.Add(this.GetNpcDisplayName(npcName));
                        }
                    }
                    if (hasFarmerMarkers)
                    {
                        foreach (FarmerMarker farmerMarker in this.FarmerMarkers.Values)
                        {
                            if (indoorLocationNames.Contains(farmerMarker.LocationName))
                                newHoveredNames.Add(farmerMarker.Name);
                        }
                    }
                }

                // sort names
                hasIndoorCharacters = indoorLocationNames.Count > 0;
                hoveredNames = newHoveredNames.Count > 0
                    ? newHoveredNames.Distinct().OrderBy(p => p).ToArray()
                    : Array.Empty<string>();
            }

            // render tooltip
            this.HasIndoorCharacter = hasIndoorCharacters;
            switch (hoveredNames.Length)
            {
                case 1:
                    this.HoveredNames = hoveredNames[0];
                    break;

                case > 1:
                    {
                        string separator = LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.zh
                            ? "，" // need special character to separate strings for Chinese
                            : ", ";

                        int maxLineLength = (int)Game1.smallFont.MeasureString("Home of Robin, Demetrius, Sebastian & Maru").X;

                        List<string> lines = new() { hoveredNames[0] };
                        for (int i = 1; i < hoveredNames.Length; i++)
                        {
                            string name = hoveredNames[i];

                            int lastLineLength = (int)Game1.smallFont.MeasureString(lines[^1] + separator + name).X;
                            if (lastLineLength > maxLineLength)
                            {
                                lines[^1] += separator;
                                lines.Add(name);
                            }
                            else
                                lines[^1] += separator + name;
                        }

                        this.HoveredNames = string.Join(Environment.NewLine, lines);
                    }
                    break;
            }
        }
    }

    public override void drawMiniPortraits(SpriteBatch b, float alpha = 1f)
    {
        // base.drawMiniPortraits(b); // draw our own smaller farmer markers instead

        this.DrawMarkers(b, alpha);
    }

    public override void drawTooltip(SpriteBatch b)
    {
        int x = Game1.getMouseX() + Game1.tileSize / 2;
        int y = Game1.getMouseY() + Game1.tileSize / 2;
        int width;
        int height;
        int offsetY = 0;

        this.performHoverAction(x - Game1.tileSize / 2, y - Game1.tileSize / 2);

        if (!string.IsNullOrEmpty(this.hoverText))
        {
            int textLength = (int)Game1.smallFont.MeasureString(this.hoverText).X + Game1.tileSize / 2;
            width = Math.Max((int)Game1.smallFont.MeasureString(this.hoverText).X + Game1.tileSize / 2, textLength);
            height = (int)Math.Max(60, Game1.smallFont.MeasureString(this.hoverText).Y + 5 * Game1.tileSize / 8);
            if (x + width > Game1.uiViewport.Width)
            {
                x = Game1.uiViewport.Width - width;
                y += Game1.tileSize / 4;
            }

            if (ModEntry.Globals.NameTooltipMode == 1)
            {
                if (y + height > Game1.uiViewport.Height)
                {
                    x += Game1.tileSize / 4;
                    y = Game1.uiViewport.Height - height;
                }

                offsetY = 2 - Game1.tileSize;
            }
            else if (ModEntry.Globals.NameTooltipMode == 2)
            {
                if (y + height > Game1.uiViewport.Height)
                {
                    x += Game1.tileSize / 4;
                    y = Game1.uiViewport.Height - height;
                }

                offsetY = height - 4;
            }
            else
            {
                if (y + height > Game1.uiViewport.Height)
                {
                    x += Game1.tileSize / 4;
                    y = Game1.uiViewport.Height - height;
                }
            }

            // Draw name tooltip positioned around location tooltip
            this.DrawNames(b, this.HoveredNames, x, y, offsetY, height, ModEntry.Globals.NameTooltipMode);

            // Draw location tooltip
            IClickableMenu.drawHoverText(b, this.hoverText, Game1.smallFont);
        }
        else
        {
            // Draw name tooltip only
            this.DrawNames(Game1.spriteBatch, this.HoveredNames, x, y, offsetY, this.height, ModEntry.Globals.NameTooltipMode);
        }

        // Draw indoor icon
        if (this.HasIndoorCharacter && !string.IsNullOrEmpty(this.HoveredNames))
            b.Draw(Game1.mouseCursors, this.IndoorIconVector, new Rectangle(448, 64, 32, 32), Color.White, 0f,
                Vector2.Zero, 0.75f, SpriteEffects.None, 0f);
    }


    /*********
    ** Private methods
    *********/
    // Draw event
    // Subtractions within location vectors are to set the origin to the center of the sprite
    private void DrawMarkers(SpriteBatch b, float alpha)
    {
        string regionId = this.RegionId;

        if (regionId == "Valley")
        {
            if (ModEntry.Globals.ShowFarmBuildings && this.FarmBuildings != null && this.BuildingMarkers != null)
            {
                foreach (BuildingMarker building in this.FarmBuildings.Values.OrderBy(p => p.WorldMapPosition.Y))
                {
                    if (ModConstants.FarmBuildingRects.TryGetValue(building.CommonName, out Rectangle buildingRect))
                    {
                        b.Draw(
                            this.BuildingMarkers,
                            new Vector2(
                                this.mapBounds.X + building.WorldMapPosition.X - buildingRect.Width / 2,
                                this.mapBounds.Y + building.WorldMapPosition.Y - buildingRect.Height / 2
                            ),
                            buildingRect, Color.White * alpha, 0f, Vector2.Zero, 3f, SpriteEffects.None, 1f
                        );
                    }
                }
            }
        }

        // NPCs
        // Sort by drawing order
        if (this.NpcMarkers != null)
        {
            var sortedMarkers = this.NpcMarkers
                .Where(p => p.Value.WorldMapPosition.RegionId == regionId)
                .OrderBy(p => p.Value.Layer);

            foreach (var npcMarker in sortedMarkers)
            {
                string name = npcMarker.Key;
                NpcMarker marker = npcMarker.Value;

                // Skip if no specified location or should be hidden
                if (marker.Sprite == null
                    || ModEntry.ShouldExcludeNpc(name)
                    || (!ModEntry.Globals.ShowHiddenVillagers && marker.IsHidden)
                    || (this.ConditionalNpcs.ContainsKey(name) && !this.ConditionalNpcs[name])
                   )
                {
                    continue;
                }

                // Dim marker for hidden markers
                var markerColor = (marker.IsHidden ? Color.DarkGray * 0.7f : Color.White) * alpha;

                // Draw NPC marker
                Rectangle spriteRect = marker.GetSpriteSourceRect();
                b.Draw(marker.Sprite, new Rectangle(this.mapBounds.X + marker.WorldMapPosition.X, this.mapBounds.Y + marker.WorldMapPosition.Y, 32, 30), spriteRect, markerColor);

                // Draw icons for quests/birthday
                if (ModEntry.Globals.ShowQuests)
                {
                    if (marker.IsBirthday && (Game1.player.friendshipData.ContainsKey(name) && Game1.player.friendshipData[name].GiftsToday == 0))
                    {
                        // Gift icon
                        b.Draw(Game1.mouseCursors,
                            new Vector2(this.mapBounds.X + marker.WorldMapPosition.X + 20, this.mapBounds.Y + marker.WorldMapPosition.Y),
                            new Rectangle(147, 412, 10, 11), markerColor, 0f, Vector2.Zero, 1.8f,
                            SpriteEffects.None, 0f);
                    }

                    if (marker.HasQuest)
                    {
                        // Quest icon
                        b.Draw(Game1.mouseCursors,
                            new Vector2(this.mapBounds.X + marker.WorldMapPosition.X + 22, this.mapBounds.Y + marker.WorldMapPosition.Y - 3),
                            new Rectangle(403, 496, 5, 14), markerColor, 0f, Vector2.Zero, 1.8f,
                            SpriteEffects.None, 0f);
                    }
                }
            }
        }

        // Farmers
        if (Context.IsMultiplayer)
        {
            foreach (Farmer farmer in Game1.getOnlineFarmers())
            {
                if (this.FarmerMarkers.TryGetValue(farmer.UniqueMultiplayerID, out FarmerMarker farMarker) && farMarker.WorldMapPosition.RegionId == regionId)
                {
                    if (farMarker is { DrawDelay: 0 }) // Temporary solution to handle desync of farmhand location/tile position when changing location
                    {
                        farmer.FarmerRenderer.drawMiniPortrat(b,
                            new Vector2(this.mapBounds.X + farMarker.WorldMapPosition.X - 16, this.mapBounds.Y + farMarker.WorldMapPosition.Y - 15),
                            0.00011f, 2f, 1, farmer, alpha);
                    }
                }
            }
        }
        else
        {
            WorldMapPosition playerLoc = ModEntry.GetWorldMapPosition(Game1.player.currentLocation.uniqueName.Value ?? Game1.player.currentLocation.Name, Game1.player.TilePoint.X, Game1.player.TilePoint.Y, this.Customizations.LocationExclusions);

            Game1.player.FarmerRenderer.drawMiniPortrat(b, new Vector2(this.mapBounds.X + playerLoc.X - 16, this.mapBounds.Y + playerLoc.Y - 15), 0.00011f, 2f, 1, Game1.player, alpha);
        }
    }

    // Draw NPC name tooltips map page
    private void DrawNames(SpriteBatch b, string names, int x, int y, int offsetY, int relocate, int nameTooltipMode)
    {
        if (this.HoveredNames.Equals("")) return;

        this.IndoorIconVector = new Vector2(-9999);
        string[] lines = names.Split('\n');
        int height = (int)Math.Max(60, Game1.smallFont.MeasureString(names).Y + Game1.tileSize / 2);
        int width = (int)Game1.smallFont.MeasureString(names).X + Game1.tileSize / 2;

        if (nameTooltipMode == 1)
        {
            x = Game1.getOldMouseX() + Game1.tileSize / 2;
            if (lines.Length > 1)
            {
                y += offsetY - ((int)Game1.smallFont.MeasureString(names).Y) + Game1.tileSize / 2;
            }
            else
            {
                y += offsetY;
            }

            // If going off screen on the right, move tooltip to below location tooltip so it can stay inside the screen
            // without the cursor covering the tooltip
            if (x + width > Game1.uiViewport.Width)
            {
                x = Game1.uiViewport.Width - width;
                if (lines.Length > 1)
                {
                    y += relocate - 8 + ((int)Game1.smallFont.MeasureString(names).Y) + Game1.tileSize / 2;
                }
                else
                {
                    y += relocate - 8 + Game1.tileSize;
                }
            }
        }
        else if (nameTooltipMode == 2)
        {
            y += offsetY;
            if (x + width > Game1.uiViewport.Width)
            {
                x = Game1.uiViewport.Width - width;
            }

            // If going off screen on the bottom, move tooltip to above location tooltip so it stays visible
            if (y + height > Game1.uiViewport.Height)
            {
                x = Game1.getOldMouseX() + Game1.tileSize / 2;
                if (lines.Length > 1)
                {
                    y += -relocate + 8 - ((int)Game1.smallFont.MeasureString(names).Y) + Game1.tileSize / 2;
                }
                else
                {
                    y += -relocate + 6 - Game1.tileSize;
                }
            }
        }
        else
        {
            x = Game1.activeClickableMenu.xPositionOnScreen - 145;
            y = Game1.activeClickableMenu.yPositionOnScreen + 650 - height / 2;
        }

        if (this.HasIndoorCharacter)
        {
            this.IndoorIconVector = new Vector2(x - Game1.tileSize / 8 + 2, y - Game1.tileSize / 8 + 2);
        }

        Vector2 vector = new Vector2(x + (float)(Game1.tileSize / 4), y + (float)(Game1.tileSize / 4 + 4));

        drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, y, width, height, Color.White);
        b.DrawString(Game1.smallFont, names, vector + new Vector2(2f, 2f), Game1.textShadowColor, 0f, Vector2.Zero, 1f,
            SpriteEffects.None, 0f);
        b.DrawString(Game1.smallFont, names, vector + new Vector2(0f, 2f), Game1.textShadowColor, 0f, Vector2.Zero, 1f,
            SpriteEffects.None, 0f);
        b.DrawString(Game1.smallFont, names, vector + new Vector2(2f, 0f), Game1.textShadowColor, 0f, Vector2.Zero, 1f,
            SpriteEffects.None, 0f);
        b.DrawString(Game1.smallFont, names, vector, Game1.textColor * 0.9f, 0f, Vector2.Zero, 1f, SpriteEffects.None,
            0f);
    }

    /// <summary>Get the display name to show for an NPC.</summary>
    /// <param name="npcName">The NPC's internal name.</param>
    private string GetNpcDisplayName(string npcName)
    {
        return this.Customizations.Names.GetValueOrDefault(npcName, npcName);
    }

    /// <summary>Get whether a pixel area on the world map is under the cursor, adjusted for the map offset.</summary>
    /// <param name="mousePos">The pixel position of the mouse, relative to the game window.</param>
    /// <param name="mapPos">The pixel position on the world map, relative to the top-left corner of the map.</param>
    /// <param name="markerWidth">The pixel width of the position on the world map.</param>
    /// <param name="markerHeight">The pixel height of the position on the world map.</param>
    private bool IsMapPixelUnderCursor(Point mousePos, WorldMapPosition mapPos, int markerWidth, int markerHeight)
    {
        int x = this.mapBounds.X + mapPos.X;
        int y = this.mapBounds.Y + mapPos.Y;

        return
            mousePos.X >= x
            && mousePos.X <= x + markerWidth
            && mousePos.Y >= y
            && mousePos.Y <= y + markerHeight;
    }
}
