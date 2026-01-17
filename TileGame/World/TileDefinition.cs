using System.Collections.Generic;
using Microsoft.Xna.Framework;

public record TileDefinition(
    byte Id,
    string Name,
    Color BaseColor,
    bool IsSolid,
    float MovementSpeed = 1.0f, // Modifier for player speed
    string TextureName = null
);

public static class TileRegistry
{
    private static readonly Dictionary<byte, TileDefinition> _tiles = new();
    
    public static void Register(TileDefinition tile) => _tiles[tile.Id] = tile;
    public static TileDefinition Get(byte id) => _tiles.TryGetValue(id, out var t) ? t : null;
}