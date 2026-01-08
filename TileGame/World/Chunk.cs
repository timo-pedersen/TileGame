using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using TileGame.Biomes;

namespace TileGame.World;

public sealed class Chunk
{
    public readonly ChunkKey Key;
    public readonly BiomeEnum BiomeType;
    public readonly byte[] Terrain = new byte[WorldConstants.ChunkSize * WorldConstants.ChunkSize];

    // Runtime cache (not serialized)
    public RenderTarget2D TextureCache;

    public Chunk(ChunkKey key, BiomeEnum biomeType)
    {
        byte fillByte = biomeType switch
        {
            BiomeEnum.None => 0,
            BiomeEnum.GrassyPlain => 1,
            BiomeEnum.Desert => 2,
            _ => 3,
        };

        Key = key;
        Array.Fill(Terrain, (byte)fillByte);
        BiomeType = biomeType;
    }
}
