using System.Collections.Generic;
using TileGame.Biomes;

public static class BiomeManager
{
    private static readonly Dictionary<BiomeEnum, IBiome> _biomes = new();

    static BiomeManager()
    {
        _biomes[BiomeEnum.None] = new None();
        _biomes[BiomeEnum.GrassyPlain] = new GrassyPlain();
        _biomes[BiomeEnum.Desert] = new Desert();
    }

    public static IBiome GetBiome(BiomeEnum biomeType)
        => _biomes.TryGetValue(biomeType, out var b) ? b : _biomes[BiomeEnum.None];
}
