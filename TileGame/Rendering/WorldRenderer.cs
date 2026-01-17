using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TileGame.World;
using TileGame.World.Objects;

namespace TileGame.Rendering;

public sealed class WorldRenderer
{
    private readonly ChunkManager _chunks;
    private readonly WorldObjectStore _objects;
    private readonly ObjectModificationStore _objectMods;
    private readonly ChunkBaker _baker;
    private readonly SpriteBatch _sb;
    private readonly Texture2D _pixel;
    private readonly Func<Chunk, int, int, bool?> _terrainSolid;

    // Debug options
    public bool DebugShowChunkBorders { get; set; }
    public bool DebugShowTileBorders { get; set; }
    public bool DebugShowSolidTileBorders { get; set; }

    private readonly ConcurrentQueue<(int cx, int cy)> _bakingQueue = new();
    private readonly HashSet<ChunkKey> _baking = new();
    
    // Cache procgen objects per chunk to avoid regenerating every frame
    private readonly Dictionary<ChunkKey, List<IWorldObject>> _procGenCache = new();

    public WorldRenderer(ChunkManager chunks, WorldObjectStore objects, ObjectModificationStore objectMods, ChunkBaker baker,
        SpriteBatch sb, Texture2D pixel, Func<Chunk, int, int, bool?> terrainSolid = null)
    {
        _chunks = chunks;
        _objects = objects;
        _objectMods = objectMods;
        _baker = baker;
        _sb = sb;
        _pixel = pixel;
        _terrainSolid = terrainSolid;
    }

    public (int minX, int maxX, int minY, int maxY, int chunkPx) ComputeVisibleChunkRange(Viewport viewport, float zoom, Vector2 playerWorldPx)
    {
        int chunkPx = WorldConstants.ChunkSize * WorldConstants.TileSizePx;

        var viewW = viewport.Width / zoom;
        var viewH = viewport.Height / zoom;

        int minX = (int)Math.Floor((playerWorldPx.X - viewW * 0.5f) / chunkPx) - 1;
        int maxX = (int)Math.Floor((playerWorldPx.X + viewW * 0.5f) / chunkPx) + 1;
        int minY = (int)Math.Floor((playerWorldPx.Y - viewH * 0.5f) / chunkPx) - 1;
        int maxY = (int)Math.Floor((playerWorldPx.Y + viewH * 0.5f) / chunkPx) + 1;

        return (minX, maxX, minY, maxY, chunkPx);
    }

    public void EnsureBaked(int minX, int maxX, int minY, int maxY)
    {
        // Priority: bake visible chunks immediately, queue distant ones
        for (int cy = minY; cy <= maxY; cy++)
        {
            for (int cx = minX; cx <= maxX; cx++)
            {
                Chunk chunk = _chunks.GetChunk(cx, cy);
                var key = chunk.Key;
                
                if (chunk.TextureCache == null && !_baking.Contains(key))
                {
                    _baking.Add(key);
                    // Could use Task.Run for async baking
                    chunk.TextureCache = _baker.BakeChunkTexture(chunk);
                    _baking.Remove(key);
                }
            }
        }
    }

    public void DrawWorld(int layerId, int minX, int maxX, int minY, int maxY, int chunkPx)
    {
        // Batch all chunk textures first (single material)
        foreach (var cy in Enumerable.Range(minY, maxY - minY + 1))
        {
            foreach (var cx in Enumerable.Range(minX, maxX - minX + 1))
            {
                Chunk chunk = _chunks.GetChunk(cx, cy);

                // Rebake if texture was evicted from cache
                if (chunk.TextureCache == null)
                {
                    chunk.TextureCache = _baker.BakeChunkTexture(chunk);
                }

                int worldPxX = cx * chunkPx;
                int worldPxY = cy * chunkPx;
                _sb.Draw(chunk.TextureCache, new Vector2(worldPxX, worldPxY), Color.White);
            }
        }

        // Draw all objects (explicit objects + procgen objects)
        var objectsByType = new Dictionary<Type, List<IWorldObject>>();
        
        for (int cy = minY; cy <= maxY; cy++)
        {
            for (int cx = minX; cx <= maxX; cx++)
            {
                // Draw explicit objects from object store
                foreach (var obj in _objects.QueryByChunk(layerId, cx, cy))
                {
                    var type = obj.GetType();
                    if (!objectsByType.ContainsKey(type))
                        objectsByType[type] = new List<IWorldObject>();
                    objectsByType[type].Add(obj);
                }
                
                // Draw procedurally generated objects (trees, rocks, grass)
                DrawProcGenObjects(layerId, cx, cy, objectsByType);
            }
        }

        // Draw objects batched by type
        foreach (var batch in objectsByType.Values)
            foreach (var obj in batch)
                obj.Draw(_sb, _pixel, WorldConstants.TileSizePx);

        // Debug borders
        if (DebugShowTileBorders)
            DrawTileBorders(minX, maxX, minY, maxY, Color.Blue);

        if (DebugShowSolidTileBorders)
            DrawSolidTileBorders(layerId, minX, maxX, minY, maxY, Color.Red);
    }

    private void DrawProcGenObjects(int layerId, int chunkX, int chunkY, Dictionary<Type, List<IWorldObject>> objectsByType)
    {
        var chunk = _chunks.GetChunk(chunkX, chunkY);
        var chunkKey = new ChunkKey(layerId, chunkX, chunkY);

        // Check cache first
        if (!_procGenCache.TryGetValue(chunkKey, out var procGenObjects))
        {
            // Generate and cache procedural objects for this chunk
            var allObjects = Util.ProcFeatures.GenerateChunkObjects(chunk, chunkX, chunkY);
            procGenObjects = new List<IWorldObject>();
            
            foreach (var obj in allObjects)
            {
                // Skip if object is in an exclusion zone (only check once during caching)
                if (_objectMods.IsInExclusionZone(obj.PositionTiles))
                    continue;
                
                // Create key for this procgen object
                var localPos = obj.PositionTiles - new Vector2(chunkX * WorldConstants.ChunkSize, chunkY * WorldConstants.ChunkSize);
                var key = new ObjectProcGenKey(
                    layerId, 
                    chunkX, 
                    chunkY, 
                    (int)localPos.X, 
                    (int)localPos.Y, 
                    obj.GetType().Name);
                
                // Skip if deleted
                if (_objectMods.IsDeleted(key))
                    continue;
                
                procGenObjects.Add(obj);
            }
            
            _procGenCache[chunkKey] = procGenObjects;
        }
        
        // Add cached objects to batching dictionary
        foreach (var obj in procGenObjects)
        {
            var type = obj.GetType();
            if (!objectsByType.ContainsKey(type))
                objectsByType[type] = new List<IWorldObject>();
            objectsByType[type].Add(obj);
        }
    }
    
    private IWorldObject CreateModifiedObject(IWorldObject original, ModifiedObject modification)
    {
        // Create a copy of the object with modifications applied
        // This is a simplified version - you might need more sophisticated cloning
        // depending on your object types
        
        // For now, just return the original with updated position
        // You'll need to implement proper object modification based on state
        return original;
    }

    private void DrawTileBorders(int minChunkX, int maxChunkX, int minChunkY, int maxChunkY, Color color)
    {
        for (int cy = minChunkY; cy <= maxChunkY; cy++)
        {
            for (int cx = minChunkX; cx <= maxChunkX; cx++)
            {
                int baseX = cx * WorldConstants.ChunkSize;
                int baseY = cy * WorldConstants.ChunkSize;

                for (int ly = 0; ly < WorldConstants.ChunkSize; ly++)
                {
                    for (int lx = 0; lx < WorldConstants.ChunkSize; lx++)
                    {
                        int tx = baseX + lx;
                        int ty = baseY + ly;

                        int px = tx * WorldConstants.TileSizePx;
                        int py = ty * WorldConstants.TileSizePx;

                        DrawRectBorder(px, py, WorldConstants.TileSizePx, WorldConstants.TileSizePx, 1, color);
                    }
                }
            }
        }
    }

    private void DrawSolidTileBorders(int layerId, int minChunkX, int maxChunkX, int minChunkY, int maxChunkY, Color color)
    {
        for (int cy = minChunkY; cy <= maxChunkY; cy++)
        {
            for (int cx = minChunkX; cx <= maxChunkX; cx++)
            {
                int baseX = cx * WorldConstants.ChunkSize;
                int baseY = cy * WorldConstants.ChunkSize;
                Chunk chunk = _chunks.GetChunk(cx, cy);
                var chunkKey = new ChunkKey(layerId, cx, cy);
                
                // Get cached procgen objects for this chunk
                List<IWorldObject>? cachedObjects = null;
                _procGenCache.TryGetValue(chunkKey, out cachedObjects);
                
                for (int ly = 0; ly < WorldConstants.ChunkSize; ly++)
                {
                    for (int lx = 0; lx < WorldConstants.ChunkSize; lx++)
                    {
                        int tx = baseX + lx;
                        int ty = baseY + ly;
                        
                        bool solid = false;
                        
                        // Check cached procgen objects (much faster than checking exclusion zones)
                        if (cachedObjects != null)
                        {
                            foreach (var obj in cachedObjects)
                            {
                                if (obj.IsSolidTile(tx, ty))
                                {
                                    solid = true;
                                    break;
                                }
                            }
                        }
                        
                        // Also check explicit objects
                        if (!solid)
                        {
                            solid = _objects.IsSolidTile(layerId, tx, ty);
                        }

                        if (!solid)
                            continue;

                        int px = tx * WorldConstants.TileSizePx;
                        int py = ty * WorldConstants.TileSizePx;

                        DrawRectBorder(px, py, WorldConstants.TileSizePx, WorldConstants.TileSizePx, 1, color);
                    }
                }
            }
        }
    }

    private void DrawRectBorder(int x, int y, int w, int h, int thicknessPx, Color color)
    {
        // top
        _sb.Draw(_pixel, new Rectangle(x, y, w, thicknessPx), color);
        // bottom
        _sb.Draw(_pixel, new Rectangle(x, y + h - thicknessPx, w, thicknessPx), color);
        // left
        _sb.Draw(_pixel, new Rectangle(x, y, thicknessPx, h), color);
        // right
        _sb.Draw(_pixel, new Rectangle(x + w - thicknessPx, y, thicknessPx, h), color);
    }

}