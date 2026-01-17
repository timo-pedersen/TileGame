using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TileGame.World;

namespace TileGame.Rendering;

public sealed class ChunkBaker
{
    private readonly GraphicsDevice _gd;
    private readonly SpriteBatch _sb;
    private readonly Texture2D _pixel;

    public ChunkBaker(GraphicsDevice gd, SpriteBatch sb, Texture2D pixel)
    {
        _gd = gd;
        _sb = sb;
        _pixel = pixel;
    }

    public RenderTarget2D BakeChunkTexture(Chunk chunk)
    {
        int chunkPx = WorldConstants.ChunkSize * WorldConstants.TileSizePx;
        var rt = new RenderTarget2D(_gd, chunkPx, chunkPx);

        _gd.SetRenderTarget(rt);
        _gd.Clear(Color.Transparent);

        _sb.Begin(samplerState: SamplerState.PointClamp);

        for (int y = 0; y < WorldConstants.ChunkSize; y++)
        {
            for (int x = 0; x < WorldConstants.ChunkSize; x++)
            {
                int idx = y * WorldConstants.ChunkSize + x;
                byte terrainType = chunk.Terrain[idx];

                Color color = terrainType switch
                {
                    0 => new Color(40, 40, 45),      // None/empty - dark gray
                    1 => new Color(60, 120, 40),     // GrassyPlain - green
                    2 => new Color(210, 180, 90),    // Desert - sandy yellow
                    3 => new Color(100, 100, 100),   // Default - gray
                    4 => new Color(90, 75, 60),      // Road - brown-gray
                    _ => Color.Magenta               // Unknown - magenta for debugging
                };

                int px = x * WorldConstants.TileSizePx;
                int py = y * WorldConstants.TileSizePx;

                _sb.Draw(_pixel, new Rectangle(px, py, WorldConstants.TileSizePx, WorldConstants.TileSizePx), color);
            }
        }

        _sb.End();
        _gd.SetRenderTarget(null);

        return rt;
    }

    public static void UnbakeChunkTexture(Chunk chunk)
    {
        chunk.TextureCache?.Dispose();
        chunk.TextureCache = null;
    }
}

