using Microsoft.Xna.Framework;
using System;
using TileGame.World;
using TileGame.World.Objects;

namespace TileGame.Collision;

public sealed class CollisionResolver
{
    private readonly ChunkManager _chunks;
    private readonly WorldObjectStore _objects;
    private readonly int _layerId;
    private readonly float _playerRadiusTiles;

    private readonly Func<Chunk, int, int, bool> _terrainSolid;

    public CollisionResolver(ChunkManager chunks, WorldObjectStore objects, int layerId, float playerRadiusTiles, Func<Chunk, int, int, bool> terrainSolid = null)
    {
        _objects = objects;
        _layerId = layerId;
        _playerRadiusTiles = playerRadiusTiles;
        _terrainSolid = terrainSolid;
        _chunks = chunks;
    }

    public Vector2 MoveWithCollision(Vector2 current, Vector2 desired)
    {
        Vector2 pos = current;

        // Move X
        var tryX = new Vector2(desired.X, pos.Y);
        if (!CollidesAt(tryX))
            pos = tryX;

        // Move Y
        var tryY = new Vector2(pos.X, desired.Y);
        if (!CollidesAt(tryY))
            pos = tryY;

        return pos;
    }

    private bool CollidesAt(Vector2 posTiles)
    {
        int minX = (int)MathF.Floor(posTiles.X - _playerRadiusTiles);
        int maxX = (int)MathF.Floor(posTiles.X + _playerRadiusTiles);
        int minY = (int)MathF.Floor(posTiles.Y - _playerRadiusTiles);
        int maxY = (int)MathF.Floor(posTiles.Y + _playerRadiusTiles);

        for (int ty = minY; ty <= maxY; ty++)
        {
            for (int tx = minX; tx <= maxX; tx++)
            {
                // Convert world tile -> chunk coords
                int cx = FloorDiv(tx, WorldConstants.ChunkSize);
                int cy = FloorDiv(ty, WorldConstants.ChunkSize);

                Chunk chunk = _chunks.GetChunk(cx, cy);

                bool solid =
                    (_terrainSolid?.Invoke(chunk, tx, ty) ?? false) ||
                    _objects.IsSolidTile(_layerId, tx, ty);

                if (!solid)
                    continue;

                if (CircleIntersectsTile(posTiles, _playerRadiusTiles, tx, ty))
                    return true;
            }
        }

        return false;
    }

    private static int FloorDiv(int a, int b)
    {
        int q = a / b;
        int r = a % b;
        if (r != 0 && ((r > 0) != (b > 0))) q--;
        return q;
    }

    private static bool CircleIntersectsTile(Vector2 circleCenterTiles, float radiusTiles, int tileX, int tileY)
    {
        float minX = tileX;
        float maxX = tileX + 1f;
        float minY = tileY;
        float maxY = tileY + 1f;

        float cx = MathF.Max(minX, MathF.Min(circleCenterTiles.X, maxX));
        float cy = MathF.Max(minY, MathF.Min(circleCenterTiles.Y, maxY));

        float dx = circleCenterTiles.X - cx;
        float dy = circleCenterTiles.Y - cy;

        return (dx * dx + dy * dy) < (radiusTiles * radiusTiles);
    }
}