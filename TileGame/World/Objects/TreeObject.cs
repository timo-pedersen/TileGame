using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TileGame.World.Objects;

public enum TreeType
{
    Oak,
    Pine,
    Birch
}

public class TreeObject : IWorldObject
{
    // Trees don't have IDs or LayerIds in the traditional sense (they're procgen)
    // But we need to implement the interface
    public long Id => 0;
    public int LayerId => 0;
    public Vector2 PositionTiles { get; set; }
    public TreeType Type { get; set; }
    
    public Rectangle BoundsTiles => new Rectangle((int)PositionTiles.X, (int)PositionTiles.Y, 1, 1);
    public ObjectSizeCategory SizeCategory => ObjectSizeCategory.Point;
    
    public TreeObject(Vector2 position, TreeType treeType)
    {
        PositionTiles = position;
        Type = treeType;
    }
    
    public bool IsSolidTile(int worldTileX, int worldTileY)
    {
        return (int)PositionTiles.X == worldTileX && (int)PositionTiles.Y == worldTileY;
    }
    
    public void Draw(SpriteBatch sb, Texture2D pixel, int tileSizePx)
    {
        int worldPxX = (int)(PositionTiles.X * tileSizePx);
        int worldPxY = (int)(PositionTiles.Y * tileSizePx);
        
        Color treeColor = Type switch
        {
            TreeType.Oak => new Color(34, 139, 34),
            TreeType.Pine => new Color(0, 100, 0),
            TreeType.Birch => new Color(60, 179, 113),
            _ => Color.Green
        };
        
        var trunkRect = new Rectangle(
            worldPxX + tileSizePx / 3,
            worldPxY + tileSizePx / 2,
            tileSizePx / 3,
            tileSizePx / 2);
        sb.Draw(pixel, trunkRect, new Color(101, 67, 33));
        
        var canopyRect = new Rectangle(
            worldPxX,
            worldPxY,
            tileSizePx,
            tileSizePx / 2 + tileSizePx / 4);
        sb.Draw(pixel, canopyRect, treeColor);
    }
}