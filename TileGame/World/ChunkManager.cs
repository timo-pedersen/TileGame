using System.Collections.Generic;
using TileGame.Biomes;
using TileGame.Rendering;
using TileGame.Util;

namespace TileGame.World;

public sealed class ChunkManager
{
    private readonly Dictionary<ChunkKey, Chunk> _chunks = new();
    private readonly int _layerId;

    // World seed: later you’ll load this from save / world settings
    private readonly int _worldSeed;

    private static readonly BiomeEnum[] _biomes = new[]
    {
        BiomeEnum.GrassyPlain,
        BiomeEnum.Desert,
        // don’t include None unless you really want it in the world
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
            var biome = PickBiome(chunkX, chunkY);
            chunk = new Chunk(key, biome);
            _chunks.Add(key, chunk);
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
