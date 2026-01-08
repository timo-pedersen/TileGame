using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using TileGame.World;
using TileGame.World.Objects;

namespace TileGame.Rendering;

public sealed class WorldRenderer
{
    private readonly ChunkManager _chunks;
    private readonly WorldObjectStore _objects;
    private readonly ChunkBaker _baker;
    private readonly SpriteBatch _sb;
    private readonly Texture2D _pixel;
    private readonly Func<Chunk, int, int, bool?> _terrainSolid;

    // Debug options
    public bool DebugShowChunkBorders { get; set; }
    public bool DebugShowTileBorders { get; set; }
    public bool DebugShowSolidTileBorders { get; set; }

    public WorldRenderer(ChunkManager chunks, WorldObjectStore objects, ChunkBaker baker,
        SpriteBatch sb, Texture2D pixel, Func<Chunk, int, int, bool?> terrainSolid = null)
    {
        _chunks = chunks;
        _objects = objects;
        _baker = baker;
        _sb = sb;
        _pixel = pixel;
        _terrainSolid = terrainSolid;
    }

    public (int minX, int maxX, int minY, int maxY, int chunkPx) ComputeVisibleChunkRange(Viewport viewport, float zoom, Vector2 playerWorldPx)
    {
        int chunkPx = WorldConstants.ChunkSize * WorldConstants.TileSizePx;

        var viewW = viewport.Width / zoom;
        var viewH = viewport.Height / zoom;

        int minX = (int)Math.Floor((playerWorldPx.X - viewW * 0.5f) / chunkPx) - 1;
        int maxX = (int)Math.Floor((playerWorldPx.X + viewW * 0.5f) / chunkPx) + 1;
        int minY = (int)Math.Floor((playerWorldPx.Y - viewH * 0.5f) / chunkPx) - 1;
        int maxY = (int)Math.Floor((playerWorldPx.Y + viewH * 0.5f) / chunkPx) + 1;

        return (minX, maxX, minY, maxY, chunkPx);
    }

    public void EnsureBaked(int minX, int maxX, int minY, int maxY)
    {
        for (int cy = minY; cy <= maxY; cy++)
        {
            for (int cx = minX; cx <= maxX; cx++)
            {
                Chunk chunk = _chunks.GetChunk(cx, cy);
                if (chunk.TextureCache == null)
                    chunk.TextureCache = _baker.BakeChunkTexture(chunk);
            }
        }
    }

    public void DrawWorld(int layerId, int minX, int maxX, int minY, int maxY, int chunkPx)
    {
        // Chunks
        for (int cy = minY; cy <= maxY; cy++)
        {
            for (int cx = minX; cx <= maxX; cx++)
            {
                Chunk chunk = _chunks.GetChunk(cx, cy);

                int worldPxX = cx * chunkPx;
                int worldPxY = cy * chunkPx;

                _sb.Draw(chunk.TextureCache!, new Vector2(worldPxX, worldPxY), Color.White);

                if (DebugShowChunkBorders)
                    DrawRectBorder(worldPxX, worldPxY, chunkPx, chunkPx, 2, Color.Yellow);
            }
        }

        // Objects
        for (int cy = minY; cy <= maxY; cy++)
        {
            for (int cx = minX; cx <= maxX; cx++)
            {
                foreach (var obj in _objects.QueryByChunk(layerId: layerId, chunkX: cx, chunkY: cy))
                    obj.Draw(_sb, _pixel, WorldConstants.TileSizePx);
            }
        }

        // Debug borders
        if (DebugShowTileBorders)
            DrawTileBorders(minX, maxX, minY, maxY, Color.Blue);

        if (DebugShowSolidTileBorders)
            DrawSolidTileBorders(layerId, minX, maxX, minY, maxY, Color.Red);
    }

    private void DrawTileBorders(int minChunkX, int maxChunkX, int minChunkY, int maxChunkY, Color color)
    {
        for (int cy = minChunkY; cy <= maxChunkY; cy++)
        {
            for (int cx = minChunkX; cx <= maxChunkX; cx++)
            {
                int baseX = cx * WorldConstants.ChunkSize;
                int baseY = cy * WorldConstants.ChunkSize;

                for (int ly = 0; ly < WorldConstants.ChunkSize; ly++)
                {
                    for (int lx = 0; lx < WorldConstants.ChunkSize; lx++)
                    {
                        int tx = baseX + lx;
                        int ty = baseY + ly;

                        int px = tx * WorldConstants.TileSizePx;
                        int py = ty * WorldConstants.TileSizePx;

                        DrawRectBorder(px, py, WorldConstants.TileSizePx, WorldConstants.TileSizePx, 1, color);
                    }
                }
            }
        }
    }

    private void DrawSolidTileBorders(int layerId, int minChunkX, int maxChunkX, int minChunkY, int maxChunkY, Color color)
    {
        for (int cy = minChunkY; cy <= maxChunkY; cy++)
        {
            for (int cx = minChunkX; cx <= maxChunkX; cx++)
            {
                int baseX = cx * WorldConstants.ChunkSize;
                int baseY = cy * WorldConstants.ChunkSize;
                Chunk chunk = _chunks.GetChunk(cx, cy);
                for (int ly = 0; ly < WorldConstants.ChunkSize; ly++)
                {
                    for (int lx = 0; lx < WorldConstants.ChunkSize; lx++)
                    {
                        int tx = baseX + lx;
                        int ty = baseY + ly;

                        bool solid =
                            (_terrainSolid.Invoke(chunk, tx, ty) ?? false) ||
                            _objects.IsSolidTile(layerId, tx, ty);

                        if (!solid)
                            continue;

                        int px = tx * WorldConstants.TileSizePx;
                        int py = ty * WorldConstants.TileSizePx;

                        DrawRectBorder(px, py, WorldConstants.TileSizePx, WorldConstants.TileSizePx, 1, color);
                    }
                }
            }
        }
    }

    private void DrawRectBorder(int x, int y, int w, int h, int thicknessPx, Color color)
    {
        // top
        _sb.Draw(_pixel, new Rectangle(x, y, w, thicknessPx), color);
        // bottom
        _sb.Draw(_pixel, new Rectangle(x, y + h - thicknessPx, w, thicknessPx), color);
        // left
        _sb.Draw(_pixel, new Rectangle(x, y, thicknessPx, h), color);
        // right
        _sb.Draw(_pixel, new Rectangle(x + w - thicknessPx, y, thicknessPx, h), color);
    }

}