using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TileGame.World.Objects;

public interface IWorldObject
{
    long Id { get; }
    int LayerId { get; }
    Vector2 PositionTiles { get; }
    Rectangle BoundsTiles { get; }
    ObjectSizeCategory SizeCategory { get; }
    
    bool IsSolidTile(int worldTileX, int worldTileY);
    void Draw(SpriteBatch sb, Texture2D pixel, int tileSizePx);
}