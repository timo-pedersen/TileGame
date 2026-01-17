using System.IO;
using System.Text.Json;

public class GameConfig
{
    public int ChunkSize { get; set; } = 32;
    public int TileSizePx { get; set; } = 32;
    public int MaxCachedChunks { get; set; } = 256;
    public float PlayerSpeed { get; set; } = 10f;
    public int RenderDistance { get; set; } = 3; // chunks
    
    public static GameConfig Load(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<GameConfig>(json);
    }
}