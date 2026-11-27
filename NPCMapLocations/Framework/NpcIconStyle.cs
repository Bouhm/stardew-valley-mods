using StardewValley;

namespace NPCMapLocations.Framework;

/// <summary>The icon marker style to draw.</summary>
public enum NpcIconStyle
{
    /// <summary>Get NPC icons from manual data like <see cref="ModConfig.NpcMarkerOffsets"/>.</summary>
    Default,

    /// <summary>Get NPC icons from the <see cref="NPC.getMugShotSourceRect"/> field.</summary>
    Vanilla
}
