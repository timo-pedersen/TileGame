using Microsoft.Xna.Framework;
using System.Collections.Generic;
using TileGame.World;

namespace TileGame.World.Objects;

public sealed class WorldObjectStore
{
    private readonly Dictionary<long, IWorldObject> _objects = new();
    private readonly Dictionary<ChunkKey, List<long>> _chunkIndex = new();

    private long _nextId = 1;

    public long NewId() => _nextId++;

    public void Add(IWorldObject obj)
    {
        _objects.Add(obj.Id, obj);
        IndexObject(obj);
    }

    public IEnumerable<IWorldObject> QueryByChunk(int layerId, int chunkX, int chunkY)
    {
        var key = new ChunkKey(layerId, chunkX, chunkY);
        if (!_chunkIndex.TryGetValue(key, out var ids))
            yield break;

        // Note: For now, IDs are unique in the list. If you later rebuild index, dedupe if needed.
        foreach (var id in ids)
            yield return _objects[id];
    }

    private void IndexObject(IWorldObject obj)
    {
        // Determine which chunks this object overlaps based on its tile bounds.
        // Chunk size = 32
        const int cs = WorldConstants.ChunkSize;

        int minCx = FloorDiv(obj.BoundsTiles.Left, cs);
        int maxCx = FloorDiv(obj.BoundsTiles.Right - 1, cs);
        int minCy = FloorDiv(obj.BoundsTiles.Top, cs);
        int maxCy = FloorDiv(obj.BoundsTiles.Bottom - 1, cs);

        for (int cy = minCy; cy <= maxCy; cy++)
        {
            for (int cx = minCx; cx <= maxCx; cx++)
            {
                var key = new ChunkKey(obj.LayerId, cx, cy);
                if (!_chunkIndex.TryGetValue(key, out var list))
                {
                    list = new List<long>(4);
                    _chunkIndex[key] = list;
                }
                list.Add(obj.Id);
            }
        }
    }

    public bool IsSolidTile(int layerId, int worldTileX, int worldTileY)
    {
        // Query only the chunk that contains this tile.
        // Because objects are indexed into every chunk they overlap,
        // this is sufficient even when objects span chunk boundaries.
        int cx = FloorDiv(worldTileX, WorldConstants.ChunkSize);
        int cy = FloorDiv(worldTileY, WorldConstants.ChunkSize);

        var key = new ChunkKey(layerId, cx, cy);
        if (!_chunkIndex.TryGetValue(key, out var ids))
            return false;

        foreach (var id in ids)
        {
            var obj = _objects[id];
            if (obj.IsSolidTile(worldTileX, worldTileY))
                return true;
        }

        return false;
    }

    private static int FloorDiv(int a, int b)
    {
        // Handles negatives correctly too, but you’re probably non-negative anyway.
        int q = a / b;
        int r = a % b;
        if (r != 0 && ((r > 0) != (b > 0))) q--;
        return q;
    }
}
