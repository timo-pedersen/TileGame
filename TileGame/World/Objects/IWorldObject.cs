using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TileGame.World.Objects;

public interface IWorldObject
{
    long Id { get; }
    int LayerId { get; }
    Rectangle BoundsTiles { get; } // in tile coords (x,y,w,h)
    
    bool IsSolidTile(int worldTileX, int worldTileY);

    void Draw(SpriteBatch spriteBatch, Texture2D pixel, int tileSizePx);
}