using System;

namespace TileGame.Biomes;

public class GrassyPlain : IBiome
{
    public BiomeEnum BiomeType => BiomeEnum.GrassyPlain;

    public int BoulderChance => 16;
    public int TreeChance => 8;
}
