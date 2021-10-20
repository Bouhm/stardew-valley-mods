using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bouhm.Shared.Locations;
using Microsoft.Xna.Framework;
using NPCMapLocations.Framework.Models;
using StardewModdingAPI;
using StardewValley;

namespace NPCMapLocations.Framework
{
    /// <summary>Handles the <c>npc_map_locations_summary</c> console command.</summary>
    internal static class SummaryCommand
    {
        /********
        ** Accessors
        ********/
        /// <summary>The name of the console command/</summary>
        public static string Name { get; } = "npc_map_locations_summary";


        /********
        ** Public methods
        ********/
        /// <summary>Get the command description for the <c>help</c> command.</summary>
        public static string GetDescription()
        {
            return
                "patch summary\n"
                + "   Usage: npc_map_locations_summary\n"
                + "   Shows a summary of the current locations, context, and player position.";
        }

        /// <summary>Handle the console command.</summary>
        /// <param name="monitor">The monitor with which to write output..</param>
        /// <param name="locationUtil">Scans and maps locations in the game world.</param>
        /// <param name="customizations">Manages customized map recolors, NPCs, sprites, names, etc.</param>
        /// <param name="npcMarkers">The tracked NPC markers.</param>
        public static void Handle(IMonitor monitor, LocationUtil locationUtil, ModCustomizations customizations, Dictionary<string, NpcMarker> npcMarkers)
        {
            if (!Context.IsWorldReady)
            {
                monitor.Log("You must load a save to use this command.", LogLevel.Error);
                return;
            }

            StringBuilder output = new();
            output.AppendLine();

            // current player
            {
                // collect info
                var player = Game1.player;
                var location = player.currentLocation;
                string locationName = locationUtil.GetLocationNameFromLevel(location.NameOrUniqueName) ?? location?.NameOrUniqueName;
                LocationContext context = locationUtil.TryGetContext(locationName, mapGeneratedLevels: false);
                Vector2 mapPixel = ModEntry.LocationToMap(locationName, player.getTileX(), player.getTileY(), customizations.MapVectors, customizations.LocationExclusions);

                // collect alternate location names
                List<string> altNames = new();
                if (location.NameOrUniqueName != location.Name)
                    altNames.Add($"unique: {location.NameOrUniqueName}");
                if (location.NameOrUniqueName != locationName)
                    output.Append($"context: {locationName}");

                // build output
                output.AppendLine();
                output.AppendLine("======================");
                output.AppendLine("==  Current player  ==");
                output.AppendLine("======================");
                output.AppendLine($"Player: {player.Name} ({player.UniqueMultiplayerID})");
                output.AppendLine($"Location: {location.Name}{(altNames.Any() ? $" ({string.Join(", ", altNames)})" : "")}");
                output.AppendLine($"Tile: ({player.getTileX()}, {player.getTileY()})");
                output.AppendLine($"Excluded: {customizations.LocationExclusions.Contains(locationName)}");
                output.AppendLine($"Map pixel: {(mapPixel != ModEntry.Unknown ? $"({mapPixel.X}, {mapPixel.Y})" : "unknown")}");
                output.AppendLine();

                if (customizations.MapVectors.TryGetValue(locationName, out MapVector[] vectors) && vectors.Any())
                {
                    output.AppendLine("Configured vectors:");
                    output.Append(
                        SummaryCommand.BuildTable(
                            vectors,
                            "   ",
                            new[] { "tile", "map pixel" },
                            vector => $"{vector.TileX}, {vector.TileY}",
                            vector => $"{vector.MapX}, {vector.MapY}"
                        )
                    );
                    output.AppendLine();
                }

                if (context != null)
                {
                    output.AppendLine("Context:");
                    output.AppendLine($"   Type: {context.Type}");
                    if (context.Parent != null)
                        output.AppendLine($"   Parent: {context.Parent}");
                    output.AppendLine($"   Root: {context.Root}");
                    if (context.Children.Any())
                        output.AppendLine($"   Children: {string.Join(", ", context.Children.OrderBy(p => p))}");
                    if (context.Neighbors.Any())
                        output.AppendLine($"   Neighbors: {string.Join(", ", context.Neighbors.Keys.Distinct().OrderBy(p => p))}");
                }
                else
                    output.AppendLine("Context: unknown location!");

                output.AppendLine();
                output.AppendLine();
            }

            // excluded locations
            {
                output.AppendLine("==========================");
                output.AppendLine("==  Excluded locations  ==");
                output.AppendLine("==========================");
                output.AppendLine("These locations are completely excluded from NPC Map Locations. Players and NPCs in these locations will disappear from the map.");
                output.AppendLine("These were all added by other mods editing the `Mods/Bouhm.NPCMapLocations/Locations` asset.");
                output.AppendLine();

                if (customizations.LocationExclusions.Any())
                {
                    foreach (string name in customizations.LocationExclusions.OrderBy(p => p))
                        output.AppendLine($"   - {name}");
                }
                else
                    output.AppendLine("   (none)");
                output.AppendLine();
                output.AppendLine();
            }

            // NPC names
            {
                output.AppendLine("====================");
                output.AppendLine("==  Tracked NPCs  ==");
                output.AppendLine("====================");
                output.AppendLine("These are the the NPCs currently being tracked by the mod.");
                output.AppendLine();

                if (npcMarkers.Any())
                {
                    foreach (var typeGroup in npcMarkers.GroupBy(p => p.Value.Type).OrderBy(p => p.Key))
                    {
                        output.AppendLine($"   {typeGroup.Key}:");

                        output.Append(
                            SummaryCommand.BuildTable(
                                typeGroup,
                                "      ",
                                new[] { "name", "location", "map pixel", "notes" },

                                marker => marker.Value.DisplayName != marker.Key ? $"{marker.Key} ({marker.Value.DisplayName})" : marker.Key,
                                marker => marker.Value.LocationName,
                                marker => $"{marker.Value.MapX}, {marker.Value.MapY}",
                                marker =>
                                {
                                    List<string> notes = new();
                                    if (marker.Value.IsHidden)
                                        notes.Add("HIDDEN");
                                    if (marker.Value.IsBirthday)
                                        notes.Add("birthday");
                                    if (marker.Value.HasQuest)
                                        notes.Add("quest");
                                    if (marker.Value.CropOffset != 0)
                                        notes.Add($"crop offset: {marker.Value.CropOffset}");

                                    return string.Join(", ", notes);
                                }
                            )
                        );
                    }
                }
                else
                    output.AppendLine("   (none)");
                output.AppendLine();
                output.AppendLine();
            }

            // map vectors
            {
                output.AppendLine("===================");
                output.AppendLine("==  Map vectors  ==");
                output.AppendLine("===================");
                output.AppendLine("These map in-world tile coordinates and map pixels which represent the same position.");
                output.AppendLine("NPC Map Locations uses these map any in-game tile to its map pixel by measuring the distance between the closest map vectors.");
                output.AppendLine();

                if (customizations.MapVectors.Any())
                {
                    var records = customizations.MapVectors
                        .SelectMany(group => group.Value
                            .Select(vector => new { Location = group.Key, Vector = vector })
                        );

                    output.Append(
                        SummaryCommand.BuildTable(
                            records,
                            "",
                            new[] { "location", "tile", "map pixel" },
                            p => p.Location,
                            p => $"{p.Vector.TileX}, {p.Vector.TileY}",
                            p => $"{p.Vector.MapX}, {p.Vector.MapY}"
                        )
                    );
                }
                else
                {
                    output.AppendLine("   (none)");
                    output.AppendLine();
                }

                output.AppendLine();
            }

            // render
            monitor.Log(output.ToString(), LogLevel.Info);
        }


        /********
        ** Private methods
        ********/
        /// <summary>Get an ASCII table which represents an arbitrary set of records.</summary>
        /// <typeparam name="TRecord">The record type.</typeparam>
        /// <param name="records">The records for which to build a table.</param>
        /// <param name="linePrefix">A prefix for each line in the table, e.g. for indentation.</param>
        /// <param name="columnHeadings">The column headings to show at the top of the table.</param>
        /// <param name="getValues">Get each column's values for a row.</param>
        private static StringBuilder BuildTable<TRecord>(IEnumerable<TRecord> records, string linePrefix, string[] columnHeadings, params Func<TRecord, string>[] getValues)
        {
            // validate
            if (columnHeadings.Length != getValues.Length)
                throw new InvalidOperationException($"You must specify an equal number of {nameof(columnHeadings)} and {nameof(getValues)} values.");

            // collect table data
            int columnCount = columnHeadings.Length;
            int[] sizes = columnHeadings.Select(p => p.Length).ToArray();

            string[][] rows = records
                ?.Select(record =>
                {
                    string[] row = new string[columnCount];

                    for (int i = 0; i < columnCount; i++)
                    {
                        string value = getValues[i](record);

                        row[i] = value;
                        sizes[i] = Math.Max(sizes[i], value.Length);
                    }

                    return row;
                })
                .ToArray() ?? new string[columnCount][];

            // build table
            StringBuilder table = new();
            {
                void PrintRow(string[] values, char paddingChar = ' ')
                {
                    int last = columnCount - 1;
                    table.Append(linePrefix);
                    for (int i = 0; i <= last; i++)
                    {
                        table.Append(values[i].PadRight(sizes[i], paddingChar));
                        if (i < last)
                            table.Append(" | ");
                    }
                    table.AppendLine();
                }

                PrintRow(columnHeadings);
                PrintRow(columnHeadings.Select(p => "").ToArray(), '-');
                foreach (string[] row in rows)
                    PrintRow(row);
            }

            return table;
        }
    }
}
