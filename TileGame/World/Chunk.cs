using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
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

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(Key.LayerId);
        writer.Write(Key.X);
        writer.Write(Key.Y);
        writer.Write((byte)BiomeType);
        writer.Write(Terrain);
    }

    public static Chunk Deserialize(BinaryReader reader)
    {
        var layerId = reader.ReadInt32();
        var x = reader.ReadInt32();
        var y = reader.ReadInt32();
        var biome = (BiomeEnum)reader.ReadByte();
        var key = new ChunkKey(layerId, x, y);
        var chunk = new Chunk(key, biome);
        reader.Read(chunk.Terrain, 0, chunk.Terrain.Length);
        return chunk;
    }
}
