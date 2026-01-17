using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TileGame.World.Objects;

public sealed class HouseObject : IWorldObject
{
    public long Id { get; }
    public int LayerId { get; }
    public Rectangle BoundsTiles { get; }
    public Vector2 PositionTiles => new Vector2(BoundsTiles.X, BoundsTiles.Y);
    public ObjectSizeCategory SizeCategory => ObjectSizeCategory.Large;
    
    private readonly Point _doorLocal;
    private readonly int _doorWidth;

    public HouseObject(long id, int layerId, int xTiles, int yTiles, int wTiles = 6, int hTiles = 6,
        Point? doorLocal = null, int doorWidth = 1)
    {
        Id = id;
        LayerId = layerId;
        BoundsTiles = new Rectangle(xTiles, yTiles, wTiles, hTiles);
        _doorLocal = doorLocal ?? new Point(wTiles / 2, hTiles - 1);
        _doorWidth = doorWidth;
    }
    
    public bool IsSolidTile(int worldTileX, int worldTileY)
    {
        if (!BoundsTiles.Contains(worldTileX, worldTileY))
            return false;

        int localX = worldTileX - BoundsTiles.X;
        int localY = worldTileY - BoundsTiles.Y;

        bool isBorder =
            localX == 0 || localY == 0 ||
            localX == BoundsTiles.Width - 1 ||
            localY == BoundsTiles.Height - 1;

        if (!isBorder)
            return false;

        if (IsDoorTile(localX, localY))
            return false;

        return true;
    }

    public void Draw(SpriteBatch sb, Texture2D pixel, int tileSizePx)
    {
        var wallColor = Color.SaddleBrown;
        var floorColor = Color.BurlyWood * 0.8f;

        var floorPx = new Rectangle(
            BoundsTiles.X * tileSizePx,
            BoundsTiles.Y * tileSizePx,
            BoundsTiles.Width * tileSizePx,
            BoundsTiles.Height * tileSizePx);

        sb.Draw(pixel, floorPx, floorColor);

        for (int ly = 0; ly < BoundsTiles.Height; ly++)
        {
            for (int lx = 0; lx < BoundsTiles.Width; lx++)
            {
                bool isBorder = (lx == 0 || ly == 0 || lx == BoundsTiles.Width - 1 || ly == BoundsTiles.Height - 1);
                if (!isBorder) continue;

                if (IsDoorTile(lx, ly))
                    continue;

                int wx = BoundsTiles.X + lx;
                int wy = BoundsTiles.Y + ly;

                var r = new Rectangle(wx * tileSizePx, wy * tileSizePx, tileSizePx, tileSizePx);
                sb.Draw(pixel, r, wallColor);
            }
        }
    }

    private bool IsDoorTile(int localX, int localY)
    {
        if (localY != _doorLocal.Y) return false;
        if (localX < _doorLocal.X || localX >= _doorLocal.X + _doorWidth) return false;
        bool onBorder = (localX == 0 || localY == 0 || localX == BoundsTiles.Width - 1 || localY == BoundsTiles.Height - 1);
        return onBorder;
    }
}
