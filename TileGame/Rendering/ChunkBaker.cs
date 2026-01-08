using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using TileGame.Biomes;
using TileGame.Util;
using TileGame.World;

namespace TileGame.Rendering;

public sealed class ChunkBaker
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _texture;

    public ChunkBaker(GraphicsDevice gd, SpriteBatch sb, Texture2D texture)
    {
        _graphicsDevice = gd;
        _spriteBatch = sb;
        _texture = texture;
    }

    public RenderTarget2D BakeChunkTexture(Chunk chunk)
    {
        int chunkPx = WorldConstants.ChunkSize * WorldConstants.TileSizePx;

        var rt = new RenderTarget2D(
            _graphicsDevice,
            chunkPx,
            chunkPx,
            false,
            SurfaceFormat.Color,
            DepthFormat.None,
            0,
            RenderTargetUsage.PreserveContents);

        _graphicsDevice.SetRenderTarget(rt);
        _graphicsDevice.Clear(Color.Transparent);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        for (int y = 0; y < WorldConstants.ChunkSize; y++)
        {
            for (int x = 0; x < WorldConstants.ChunkSize; x++)
            {
                int index = y * WorldConstants.ChunkSize + x;
                byte tile = chunk.Terrain[index];

                Color tileColor = tile switch
                {
                    0 => Color.Gray,
                    1 => Color.ForestGreen,
                    2 => Color.Yellow,
                    3 => Color.Green,
                    _ => Color.Magenta // error
                };

                Rectangle rect = new Rectangle(
                    x * WorldConstants.TileSizePx,
                    y * WorldConstants.TileSizePx,
                    WorldConstants.TileSizePx,
                    WorldConstants.TileSizePx);

                _spriteBatch.Draw(_texture, rect, tileColor);

                // Ground renderer
                bool isGround = 
                    chunk.BiomeType == BiomeEnum.GrassyPlain ||
                    chunk.BiomeType == BiomeEnum.Desert
                    ;

                if (isGround)
                {
                    int worldTileX = chunk.Key.X * WorldConstants.ChunkSize + x;
                    int worldTileY = chunk.Key.Y * WorldConstants.ChunkSize + y;

                    uint h = Hash.Hash32(worldTileX, worldTileY, salt: 12345);
                    int marks = (int)(h & 3u);
                    var grassColor = Color.Lerp(tileColor, Color.Yellow, 0.35f);
                    for (int m = 0; m < marks; m++)
                    {
                        h = Hash.Hash32((int)h, worldTileX, salt: m);

                        int ox = 2 + (int)(h & 27u);
                        int oy = 2 + (int)((h >> 5) & 27u);

                        bool line = ((h >> 10) & 1u) != 0;

                        if (!line)
                            _spriteBatch.Draw(_texture, new Rectangle(rect.X + ox, rect.Y + oy, 2, 2), grassColor);
                        else
                        {
                            bool vertical = ((h >> 11) & 1u) != 0;
                            if (vertical)
                                _spriteBatch.Draw(_texture, new Rectangle(rect.X + ox, rect.Y + oy, 1, 4), grassColor);
                            else
                                _spriteBatch.Draw(_texture, new Rectangle(rect.X + ox, rect.Y + oy, 4, 1), grassColor);
                        }
                    }

                    // Boulder (baked)
                    if (ProcFeatures.HasBoulder(chunk, worldTileX, worldTileY))
                    {
                        int tilePxX = rect.X;
                        int tilePxY = rect.Y;

                        int cx = tilePxX + WorldConstants.TileSizePx / 2;
                        int cy = tilePxY + WorldConstants.TileSizePx / 2;

                        int r = WorldConstants.TileSizePx / 3; // tune

                        DrawFilledCircle(cx, cy, r, Color.DarkGray);
                        DrawFilledCircle(cx, cy, r - 2, Color.Gray);
                    }

                    if (ProcFeatures.HasTree(chunk, worldTileX, worldTileY))
                    {
                        int tilePxX = rect.X;
                        int tilePxY = rect.Y;

                        int cx = tilePxX + WorldConstants.TileSizePx / 2;
                        int cy = tilePxY + WorldConstants.TileSizePx / 2;

                        int r = WorldConstants.TileSizePx / 3; // tune

                        DrawFilledCircle(cx, cy, r, Color.Brown);
                        DrawFilledCircle(cx, cy, r - 2, Color.SandyBrown);
                    }
                }
            }
        }

        _spriteBatch.End();

        _graphicsDevice.SetRenderTarget(null);
        return rt;
    }

    public static void UnbakeChunkTexture(Chunk chunk)
    {
        if (chunk.TextureCache != null)
        {
            chunk.TextureCache.Dispose();
            chunk.TextureCache = null;
        }
    }

    // Tile Drawing Primitives (move out) ==================================

    private void DrawFilledCircle(int cx, int cy, int radius, Color color)
    {
        // Draw horizontal spans to approximate a filled circle
        // cx,cy in pixels (chunk-local)
        for (int dy = -radius; dy <= radius; dy++)
        {
            int y = cy + dy;
            int dx = (int)MathF.Floor(MathF.Sqrt(radius * radius - dy * dy));
            int x0 = cx - dx;
            int w = dx * 2 + 1;
            _spriteBatch.Draw(_texture, new Rectangle(x0, y, w, 1), color);
        }
    }
}