using System;

namespace TileGame.Biomes;

public class None : IBiome
{
    public BiomeEnum BiomeType => BiomeEnum.None;

    public int BoulderChance => 0;
    public int TreeChance => 0;
}
