namespace TileGame.World.Objects;

public enum ObjectSizeCategory
{
    Point,      // Single tile objects like trees, rocks
    Small,      // 2-4 tiles
    Medium,     // 5-16 tiles
    Large,       // 17+ tiles (like houses)
    Massive     // Spanning multiple chunks (like castles, dungeons)
}