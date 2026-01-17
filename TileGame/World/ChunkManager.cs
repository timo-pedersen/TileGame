using System.Collections.Generic;
using TileGame.Biomes;
using TileGame.Rendering;
using TileGame.Util;

namespace TileGame.World;

public sealed class ChunkManager
{
    private readonly Dictionary<ChunkKey, Chunk> _chunks = new();
    private readonly LinkedList<ChunkKey> _chunkLRU = new(); // Track access order
    private readonly Dictionary<ChunkKey, LinkedListNode<ChunkKey>> _lruNodes = new(); // Fast node lookup
    private const int MaxCachedChunks = 512; // Increased to handle fullscreen at min zoom
    private readonly int _layerId;

    // World seed: later you'll load this from save / world settings
    private readonly int _worldSeed;

    private static readonly BiomeEnum[] _biomes = new[]
    {
        BiomeEnum.GrassyPlain,
        BiomeEnum.Desert,
        // don't include None unless you really want it in the world
    };

    public ChunkManager(int layerId = 0, int worldSeed = 1234567)
    {
        _layerId = layerId;
        _worldSeed = worldSeed;
    }

    public Chunk GetChunk(int chunkX, int chunkY)
    {
        var key = new ChunkKey(_layerId, chunkX, chunkY);

        if (!_chunks.TryGetValue(key, out var chunk))
        {
            // Evict old chunks if cache is full
            if (_chunks.Count >= MaxCachedChunks)
            {
                var oldestKey = _chunkLRU.First!.Value;
                _chunkLRU.RemoveFirst();
                _lruNodes.Remove(oldestKey);
                
                if (_chunks.TryGetValue(oldestKey, out var oldChunk))
                {
                    ChunkBaker.UnbakeChunkTexture(oldChunk);
                    _chunks.Remove(oldestKey);
                }
            }

            var biome = PickBiome(chunkX, chunkY);
            chunk = new Chunk(key, biome);
            _chunks.Add(key, chunk);
            
            // Add new chunk to LRU (most recently used)
            var node = _chunkLRU.AddLast(key);
            _lruNodes[key] = node;
        }
        else
        {
            // Move existing chunk to end (most recently used)
            if (_lruNodes.TryGetValue(key, out var node))
            {
                _chunkLRU.Remove(node);
                var newNode = _chunkLRU.AddLast(key);
                _lruNodes[key] = newNode;
            }
        }

        return chunk;
    }

    private BiomeEnum PickBiome(int chunkX, int chunkY)
    {
        // Hash chunk coords + layer + world seed
        int salt = _worldSeed ^ (_layerId * 1000003);
        uint h = Hash.Hash32(chunkX, chunkY, salt);

        int idx = (int)(h % (uint)_biomes.Length);
        return _biomes[idx];
    }

    public void UnbakeAll()
    {
        foreach (var c in _chunks.Values) // expose enumerable or add method on ChunkManager
            ChunkBaker.UnbakeChunkTexture(c);
    }
}

// Support multiple layers (ground, structures, roofs, etc.)
public class LayeredChunkManager
{
    private readonly Dictionary<int, ChunkManager> _layers = new();

    public ChunkManager GetLayer(int layerId)
    {
        if (!_layers.TryGetValue(layerId, out var manager))
        {
            manager = new ChunkManager(layerId);
            _layers[layerId] = manager;
        }
        return manager;
    }
}
