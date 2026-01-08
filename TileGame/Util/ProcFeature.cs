using TileGame.Biomes;
using TileGame.World;

namespace TileGame.Util;

public static class ProcFeatures
{
    private const int BoulderSalt = 424242;
    private const int TreeSalt = 515151;

    public static bool HasSolidObject(Chunk chunk, int worldTileX, int worldTileY)
        => HasBoulder(chunk, worldTileX, worldTileY) || HasTree(chunk, worldTileX, worldTileY);

    public static bool HasBoulder(Chunk chunk, int worldTileX, int worldTileY)
    {
        int chance = BiomeManager.GetBiome(chunk.BiomeType).BoulderChance;
        return HasX(chunk, worldTileX, worldTileY, BoulderSalt, chance);
    }

    public static bool HasTree(Chunk chunk, int worldTileX, int worldTileY)
    {
        int chance = BiomeManager.GetBiome(chunk.BiomeType).TreeChance;
        return HasX(chunk, worldTileX, worldTileY, TreeSalt, chance);
    }

    private static bool HasX(Chunk chunk, int worldTileX, int worldTileY, int salt, int chanceOutOf1024)
    {
        if (chanceOutOf1024 <= 0) return false;
        if (chanceOutOf1024 >= 1024) return true;

        // Include biome in salt so different biomes reshuffle features even at same coords
        int biomeSalt = salt + (int)chunk.BiomeType * 1000;

        uint h = Hash.Hash32(worldTileX, worldTileY, biomeSalt);
        return (h & 1023u) < (uint)chanceOutOf1024;
    }
}
