namespace NPCMapLocations.Framework
{
    /// <summary>The basic building info for a map marker.</summary>
    /// <param name="CommonName">The base name for the interior location without the unique suffix.</param>
    /// <param name="WorldMapPosition">The marker's position on the world map.</param>
    internal record BuildingMarker(string CommonName, WorldMapPosition WorldMapPosition);
}
