using Microsoft.Xna.Framework;
using System.Collections.Generic;
using TileGame.Util;
using TileGame.World;

namespace TileGame.World.Objects;

public sealed class WorldObjectStore
{
    private readonly Dictionary<long, IWorldObject> _objects = new();
    // Remove _chunkIndex - use _spatialGrid only
    private readonly Dictionary<ChunkKey, List<IWorldObject>> _spatialGrid = new();

    private readonly List<IWorldObject> _massiveObjects = new();

    private long _nextId = 1;

    public long NewId() => _nextId++;

    public void Add(IWorldObject obj)
    {
        _objects.Add(obj.Id, obj);

        // Route to appropriate indexing strategy
        switch (obj.SizeCategory)
        {
            case ObjectSizeCategory.Small:
            case ObjectSizeCategory.Medium:
            case ObjectSizeCategory.Large:
                IndexInSpatialGrid(obj);
                break;
            
            case ObjectSizeCategory.Massive:
                // Use quadtree or bake into terrain
                _massiveObjects.Add(obj);
                break;
        }
    }

    private void IndexInSpatialGrid(IWorldObject obj)
    {
        // Add to all affected chunks
        var (minChunkX, minChunkY) = Coords.TileToChunk(obj.BoundsTiles.Left, obj.BoundsTiles.Top);
        var (maxChunkX, maxChunkY) = Coords.TileToChunk(obj.BoundsTiles.Right - 1, obj.BoundsTiles.Bottom - 1);

        for (int cy = minChunkY; cy <= maxChunkY; cy++)
        {
            for (int cx = minChunkX; cx <= maxChunkX; cx++)
            {
                var key = new ChunkKey(obj.LayerId, cx, cy);
                if (!_spatialGrid.TryGetValue(key, out var list))
                {
                    list = new List<IWorldObject>();
                    _spatialGrid[key] = list;
                }
                list.Add(obj);
            }
        }
    }

    public IEnumerable<IWorldObject> QueryByChunk(int layerId, int chunkX, int chunkY)
    {
        var key = new ChunkKey(layerId, chunkX, chunkY);
        if (!_spatialGrid.TryGetValue(key, out var objects))
            yield break;

        foreach (var obj in objects)
            yield return obj;
    }

    public bool IsSolidTile(int layerId, int worldTileX, int worldTileY)
    {
        var (cx, cy) = Coords.TileToChunk(worldTileX, worldTileY);
        var key = new ChunkKey(layerId, cx, cy);
        
        if (!_spatialGrid.TryGetValue(key, out var objects))
            return false;

        foreach (var obj in objects)
        {
            if (obj.IsSolidTile(worldTileX, worldTileY))
                return true;
        }

        return false;
    }

    public void Remove(long objectId)
    {
        if (!_objects.TryGetValue(objectId, out var obj))
            return;

        _objects.Remove(objectId);

        // Remove from all affected chunks
        var (minChunkX, minChunkY) = Coords.TileToChunk(obj.BoundsTiles.Left, obj.BoundsTiles.Top);
        var (maxChunkX, maxChunkY) = Coords.TileToChunk(obj.BoundsTiles.Right - 1, obj.BoundsTiles.Bottom - 1);

        for (int cy = minChunkY; cy <= maxChunkY; cy++)
        {
            for (int cx = minChunkX; cx <= maxChunkX; cx++)
            {
                var key = new ChunkKey(obj.LayerId, cx, cy);
                if (_spatialGrid.TryGetValue(key, out var list))
                {
                    list.Remove(obj);
                    if (list.Count == 0)
                        _spatialGrid.Remove(key);
                }
            }
        }
    }
}

