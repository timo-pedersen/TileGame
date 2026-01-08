namespace TileGame.Biomes;

public interface IBiome
{
    BiomeEnum BiomeType { get; }
    string Name => BiomeType.ToString();
    
    // Chance out of 1024
    int BoulderChance { get; }
    int TreeChance { get; }
}
