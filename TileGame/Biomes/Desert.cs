namespace TileGame.Biomes;

public class Desert : IBiome
{
    public BiomeEnum BiomeType => BiomeEnum.Desert;

    public int BoulderChance => 10;
    public int TreeChance => 5;
}
