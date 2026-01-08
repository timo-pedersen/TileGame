using Microsoft.Xna.Framework;
using System;
using TileGame.World;

namespace TileGame.Util
{
    internal static class Coords
    {
        public static Point ScreenToTile(Point screenPx, Matrix cameraMatrix)
        {
            Matrix inv = Matrix.Invert(cameraMatrix);
            Vector2 worldPx = Vector2.Transform(screenPx.ToVector2(), inv);

            int tx = (int)MathF.Floor(worldPx.X / WorldConstants.TileSizePx);
            int ty = (int)MathF.Floor(worldPx.Y / WorldConstants.TileSizePx);

            return new Point(tx, ty);
        }

    }
}
