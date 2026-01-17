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

        public static (int chunkX, int chunkY) TileToChunk(int worldTileX, int worldTileY)
        {
            int cx = FloorDiv(worldTileX, WorldConstants.ChunkSize);
            int cy = FloorDiv(worldTileY, WorldConstants.ChunkSize);
            return (cx, cy);
        }

        public static Point TileToChunk(Point worldTile)
        {
            var (cx, cy) = TileToChunk(worldTile.X, worldTile.Y);
            return new Point(cx, cy);
        }

        private static int FloorDiv(int a, int b)
        {
            int q = a / b;
            int r = a % b;
            if (r != 0 && ((r > 0) != (b > 0))) q--;
            return q;
        }
    }
}
