using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TileGame.World.Objects;

public enum RockType
{
    Small,
    Medium,
    Large
}

public class RockObject : IWorldObject
{
    public long Id => 0;
    public int LayerId => 0;
    public Vector2 PositionTiles { get; set; }
    public RockType Type { get; set; }
    
    public Rectangle BoundsTiles => new Rectangle((int)PositionTiles.X, (int)PositionTiles.Y, 1, 1);
    public ObjectSizeCategory SizeCategory => ObjectSizeCategory.Point;
    
    public RockObject(Vector2 position, RockType rockType)
    {
        PositionTiles = position;
        Type = rockType;
    }
    
    public bool IsSolidTile(int worldTileX, int worldTileY)
    {
        return (int)PositionTiles.X == worldTileX && (int)PositionTiles.Y == worldTileY;
    }
    
    public void Draw(SpriteBatch sb, Texture2D pixel, int tileSizePx)
    {
        int worldPxX = (int)(PositionTiles.X * tileSizePx);
        int worldPxY = (int)(PositionTiles.Y * tileSizePx);
        
        Color rockColor = Type switch
        {
            RockType.Small => new Color(128, 128, 128),
            RockType.Medium => new Color(105, 105, 105),
            RockType.Large => new Color(112, 128, 144),
            _ => Color.Gray
        };
        
        int size = Type switch
        {
            RockType.Small => tileSizePx / 2,
            RockType.Medium => (tileSizePx * 3) / 4,
            RockType.Large => tileSizePx,
            _ => tileSizePx / 2
        };
        
        int offsetX = (tileSizePx - size) / 2;
        int offsetY = (tileSizePx - size) / 2;
        
        var rockRect = new Rectangle(
            worldPxX + offsetX,
            worldPxY + offsetY,
            size,
            size);
        
        sb.Draw(pixel, rockRect, rockColor);
        
        var shadowRect = new Rectangle(
            worldPxX + offsetX + 1,
            worldPxY + offsetY + size - 2,
            size - 1,
            2);
        sb.Draw(pixel, shadowRect, new Color(0, 0, 0, 100));
    }
}