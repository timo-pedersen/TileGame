TileGame/Biomes/BiomeEnum.cs
```
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TileGame.Biomes;

public enum BiomeEnum
{
    None = 0,
    GrassyPlain = 1,
    Desert,

    //SnowyTundra,
    //Swamp,
    //Forest,
    //Volcanic,
    //Mushroom,
    //Jungle,
    //Ocean,
    //Mountain,
    //Canyon,
    //Arctic,
    //Savanna,
    //Badlands,
    //CoralReef,
    //MysticGrove,
    //CrystalCaves,
    //HauntedWoods,
    //SkyIslands,
}

```

TileGame/Biomes/BiomeManager.cs
```
﻿using System.Collections.Generic;
using TileGame.Biomes;

public static class BiomeManager
{
    private static readonly Dictionary<BiomeEnum, IBiome> _biomes = new();

    static BiomeManager()
    {
        _biomes[BiomeEnum.None] = new None();
        _biomes[BiomeEnum.GrassyPlain] = new GrassyPlain();
        _biomes[BiomeEnum.Desert] = new Desert();
    }

    public static IBiome GetBiome(BiomeEnum biomeType)
        => _biomes.TryGetValue(biomeType, out var b) ? b : _biomes[BiomeEnum.None];
}

```

TileGame/Biomes/Desert.cs
```
﻿namespace TileGame.Biomes;

public class Desert : IBiome
{
    public BiomeEnum BiomeType => BiomeEnum.Desert;

    public int BoulderChance => 10;
    public int TreeChance => 5;
}

```

TileGame/Biomes/GrassyPlain.cs
```
﻿using System;

namespace TileGame.Biomes;

public class GrassyPlain : IBiome
{
    public BiomeEnum BiomeType => BiomeEnum.GrassyPlain;

    public int BoulderChance => 16;
    public int TreeChance => 8;
}

```

TileGame/Biomes/IBiome.cs
```
﻿namespace TileGame.Biomes;

public interface IBiome
{
    BiomeEnum BiomeType { get; }
    string Name => BiomeType.ToString();
    
    // Chance out of 1024
    int BoulderChance { get; }
    int TreeChance { get; }
}

```

TileGame/Biomes/None.cs
```
﻿using System;

namespace TileGame.Biomes;

public class None : IBiome
{
    public BiomeEnum BiomeType => BiomeEnum.None;

    public int BoulderChance => 0;
    public int TreeChance => 0;
}

```

TileGame/Collision/CollisionResolver.cs
```
﻿using Microsoft.Xna.Framework;
using System;
using TileGame.World;
using TileGame.World.Objects;

namespace TileGame.Collision;

public sealed class CollisionResolver
{
    private readonly ChunkManager _chunks;
    private readonly WorldObjectStore _objects;
    private readonly int _layerId;
    private readonly float _playerRadiusTiles;

    private readonly Func<Chunk, int, int, bool> _terrainSolid;

    public CollisionResolver(ChunkManager chunks, WorldObjectStore objects, int layerId, float playerRadiusTiles, Func<Chunk, int, int, bool> terrainSolid = null)
    {
        _objects = objects;
        _layerId = layerId;
        _playerRadiusTiles = playerRadiusTiles;
        _terrainSolid = terrainSolid;
        _chunks = chunks;
    }

    public Vector2 MoveWithCollision(Vector2 current, Vector2 desired)
    {
        Vector2 pos = current;

        // Move X
        var tryX = new Vector2(desired.X, pos.Y);
        if (!CollidesAt(tryX))
            pos = tryX;

        // Move Y
        var tryY = new Vector2(pos.X, desired.Y);
        if (!CollidesAt(tryY))
            pos = tryY;

        return pos;
    }

    private bool CollidesAt(Vector2 posTiles)
    {
        int minX = (int)MathF.Floor(posTiles.X - _playerRadiusTiles);
        int maxX = (int)MathF.Floor(posTiles.X + _playerRadiusTiles);
        int minY = (int)MathF.Floor(posTiles.Y - _playerRadiusTiles);
        int maxY = (int)MathF.Floor(posTiles.Y + _playerRadiusTiles);

        for (int ty = minY; ty <= maxY; ty++)
        {
            for (int tx = minX; tx <= maxX; tx++)
            {
                // Convert world tile -> chunk coords
                int cx = FloorDiv(tx, WorldConstants.ChunkSize);
                int cy = FloorDiv(ty, WorldConstants.ChunkSize);

                Chunk chunk = _chunks.GetChunk(cx, cy);

                bool solid =
                    (_terrainSolid?.Invoke(chunk, tx, ty) ?? false) ||
                    _objects.IsSolidTile(_layerId, tx, ty);

                if (!solid)
                    continue;

                if (CircleIntersectsTile(posTiles, _playerRadiusTiles, tx, ty))
                    return true;
            }
        }

        return false;
    }

    private static int FloorDiv(int a, int b)
    {
        int q = a / b;
        int r = a % b;
        if (r != 0 && ((r > 0) != (b > 0))) q--;
        return q;
    }

    private static bool CircleIntersectsTile(Vector2 circleCenterTiles, float radiusTiles, int tileX, int tileY)
    {
        float minX = tileX;
        float maxX = tileX + 1f;
        float minY = tileY;
        float maxY = tileY + 1f;

        float cx = MathF.Max(minX, MathF.Min(circleCenterTiles.X, maxX));
        float cy = MathF.Max(minY, MathF.Min(circleCenterTiles.Y, maxY));

        float dx = circleCenterTiles.X - cx;
        float dy = circleCenterTiles.Y - cy;

        return (dx * dx + dy * dy) < (radiusTiles * radiusTiles);
    }
}
```

TileGame/Game1.cs
```
﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using TileGame.Collision;
using TileGame.Input;
using TileGame.Rendering;
using TileGame.UI;
using TileGame.World;
using TileGame.World.Objects;

namespace TileGame;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;

    private SpriteFont _uiFont = null!;

    private Texture2D _pixel = null!;

    private int _windowedW, _windowedH;

    private readonly InputState _input = new();

    private WorldObjectStore _objects = null!;
    private WorldRenderer _worldRenderer = null!;

    private ChunkManager _chunkManager = null!;
    private ChunkBaker _baker = null!;

    private UiOverlay _ui = null!;

    private Camera2D _camera = null!;
    
    private CollisionResolver _collision = null!;
    private PlayerController _player = null!;

    private bool _hasClick;
    private Point _lastClickedTile;
    private bool _pendingClick;
    private Point _pendingClickScreen;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _windowedW = _graphics.PreferredBackBufferWidth;
        _windowedH = _graphics.PreferredBackBufferHeight;

        _chunkManager = new ChunkManager(layerId: WorldConstants.WorldLayerId);

        _objects = new WorldObjectStore();

        // Test house
        long houseId = _objects.NewId();
        _objects.Add(new HouseObject(
            id: houseId,
            layerId: WorldConstants.WorldLayerId,
            xTiles: 14,
            yTiles: 4,
            wTiles: 20,
            hTiles: 20,
            doorLocal: new Point(3, 19),
            doorWidth: 1));

        _collision = new CollisionResolver(
            _chunkManager,
            _objects,
            WorldConstants.WorldLayerId,
            playerRadiusTiles: 0.35f,
            terrainSolid: (chunk, tx, ty) => Util.ProcFeatures.HasSolidObject(chunk, tx, ty)
            );
        
        _player = new PlayerController(speedTilesPerSec: 10f, _collision);

        _camera = new Camera2D();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        //_uiFont = Content.Load<SpriteFont>("Fonts/Consolas16");
        _uiFont = Content.Load<SpriteFont>("Fonts/0xProto12");
        
        _ui = new UiOverlay(_uiFont);

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _baker = new ChunkBaker(GraphicsDevice, _spriteBatch, _pixel);
        _worldRenderer = new WorldRenderer(_chunkManager, _objects, _baker, _spriteBatch, _pixel,
            terrainSolid: (chunk, tx, ty) => Util.ProcFeatures.HasSolidObject(chunk, tx, ty));
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            Exit();

        _input.Update();

        if (_input.KeyboardState.IsKeyDown(Keys.Escape))
            Exit();

        // Toggles
        if (_input.Pressed(Keys.U))
            ToggleFullscreen();

        if (_input.Pressed(Keys.B))
            _worldRenderer.DebugShowChunkBorders = !_worldRenderer.DebugShowChunkBorders;

        if (_input.Pressed(Keys.M))
            _worldRenderer.DebugShowTileBorders = !_worldRenderer.DebugShowTileBorders;

        if (_input.Pressed(Keys.N))
            _worldRenderer.DebugShowSolidTileBorders = !_worldRenderer.DebugShowSolidTileBorders;

        // Zoom (keys)
        if (_input.Pressed(Keys.OemPlus) || _input.Pressed(Keys.Add)) _camera.Zoom = MathHelper.Clamp((_camera.Zoom == 0 ? 1f : _camera.Zoom) * 1.1f, 0.5f, 4f);
        if (_input.Pressed(Keys.OemMinus) || _input.Pressed(Keys.Subtract)) _camera.Zoom = MathHelper.Clamp((_camera.Zoom == 0 ? 1f : _camera.Zoom) / 1.1f, 0.5f, 4f);

        // Zoom (mouse wheel)
        if (_input.MouseWheelDelta != 0)
        {
            float factor = (_input.MouseWheelDelta > 0) ? 1.1f : 1f / 1.1f;
            _camera.Zoom = MathHelper.Clamp((_camera.Zoom == 0 ? 1f : _camera.Zoom) * factor, 0.1f, 4f);
        }

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _player.Update(dt, _input.KeyboardState);

        // Click to tile: resolve later in Draw when we have camera matrix
        if (_input.LeftClickPressed)
        {
            _pendingClick = true;
            _pendingClickScreen = _input.LeftClickScreenPos;
        }

        _input.CommitFrame();

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        var viewport = GraphicsDevice.Viewport;

        var playerWorldPx = _player.PlayerPosTiles * WorldConstants.TileSizePx;
        _camera.CenterWorldPx = playerWorldPx;

        Matrix cameraMatrix = _camera.GetMatrix(viewport);

        if (_pendingClick)
        {
            _pendingClick = false;
            _lastClickedTile = Util.Coords.ScreenToTile(_pendingClickScreen, cameraMatrix);
            _hasClick = true;
        }

        var (minX, maxX, minY, maxY, chunkPx) = _worldRenderer.ComputeVisibleChunkRange(viewport, _camera.Zoom, playerWorldPx);

        // Bake pass (no camera)
        _worldRenderer.EnsureBaked(minX, maxX, minY, maxY);

        // World pass
        _spriteBatch.Begin(transformMatrix: cameraMatrix, samplerState: SamplerState.PointClamp);

        _worldRenderer.DrawWorld(WorldConstants.WorldLayerId, minX, maxX, minY, maxY, chunkPx);

        DrawPlayer(playerWorldPx);

        _spriteBatch.End();

        // UI pass (screen space)
        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
        _ui.Draw(_spriteBatch, _player.PlayerPosTiles, _camera.Zoom, _hasClick, _lastClickedTile);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawPlayer(Vector2 playerWorldPx)
    {
        int playerSizePx = WorldConstants.TileSizePx;
        var playerRect = new Rectangle(
            (int)MathF.Round(playerWorldPx.X - playerSizePx * 0.5f),
            (int)MathF.Round(playerWorldPx.Y - playerSizePx * 0.5f),
            playerSizePx,
            playerSizePx);

        _spriteBatch.Draw(_pixel, playerRect, Color.White);
    }

    private void ToggleFullscreen()
    {
        if (!_graphics.IsFullScreen)
        {
            _windowedW = _graphics.PreferredBackBufferWidth;
            _windowedH = _graphics.PreferredBackBufferHeight;

            var mode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            _graphics.PreferredBackBufferWidth = mode.Width;
            _graphics.PreferredBackBufferHeight = mode.Height;

            _graphics.IsFullScreen = true;
            _graphics.ApplyChanges();
        }
        else
        {
            _graphics.IsFullScreen = false;
            _graphics.PreferredBackBufferWidth = _windowedW;
            _graphics.PreferredBackBufferHeight = _windowedH;
            _graphics.ApplyChanges();
        }
    }
}




```

TileGame/Input/InputState.cs
```
﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace TileGame.Input;

public sealed class InputState
{
    private KeyboardState _prevKb;
    private MouseState _prevMouse;
    private int _prevScroll;

    public KeyboardState KeyboardState { get; private set; }
    public MouseState MouseState { get; private set; }

    public int MouseWheelDelta { get; private set; }

    public bool LeftClickPressed { get; private set; }
    public Point LeftClickScreenPos { get; private set; }

    public void Update()
    {
        KeyboardState = Keyboard.GetState();
        MouseState = Mouse.GetState();

        // Mouse wheel
        int scroll = MouseState.ScrollWheelValue;
        MouseWheelDelta = scroll - _prevScroll;
        _prevScroll = scroll;

        // Left click edge
        bool leftPressed = MouseState.LeftButton == ButtonState.Pressed;
        bool leftWasPressed = _prevMouse.LeftButton == ButtonState.Pressed;
        LeftClickPressed = leftPressed && !leftWasPressed;
        if (LeftClickPressed)
            LeftClickScreenPos = MouseState.Position;

        _prevMouse = MouseState;
    }

    public bool Pressed(Keys key) => KeyboardState.IsKeyDown(key) && !_prevKb.IsKeyDown(key);

    public void CommitFrame()
    {
        _prevKb = KeyboardState;
    }
}
```

TileGame/Input/PlayerController.cs
```
﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TileGame.Collision;

namespace TileGame.Input;

public sealed class PlayerController
{
    public Vector2 PlayerPosTiles { get; private set; } = new(10.5f, 10.5f);

    private readonly float _speedTilesPerSec;
    private readonly CollisionResolver _collision;

    public PlayerController(float speedTilesPerSec, CollisionResolver collision)
    {
        _speedTilesPerSec = speedTilesPerSec;
        _collision = collision;
    }

    public void Update(float dt, KeyboardState kb)
    {
        Vector2 dir = Vector2.Zero;

        if (kb.IsKeyDown(Keys.W) || kb.IsKeyDown(Keys.Up)) dir.Y -= 1;
        if (kb.IsKeyDown(Keys.S) || kb.IsKeyDown(Keys.Down)) dir.Y += 1;
        if (kb.IsKeyDown(Keys.A) || kb.IsKeyDown(Keys.Left)) dir.X -= 1;
        if (kb.IsKeyDown(Keys.D) || kb.IsKeyDown(Keys.Right)) dir.X += 1;

        if (dir == Vector2.Zero)
            return;

        dir.Normalize();
        var desired = PlayerPosTiles + dir * _speedTilesPerSec * dt;
        PlayerPosTiles = _collision.MoveWithCollision(PlayerPosTiles, desired);
    }
}
```

TileGame/Program.cs
```
﻿using var game = new TileGame.Game1();
game.Run();

```

TileGame/Rendering/Camera2D.cs
```
﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TileGame.Rendering;

public sealed class Camera2D
{
    public Vector2 CenterWorldPx { get; set; }
    public float Zoom { get; set; } = 1f;

    public Matrix GetMatrix(Viewport viewport)
    {
        var screenCenter = new Vector2(viewport.Width * 0.5f, viewport.Height * 0.5f);

        return
            Matrix.CreateTranslation(new Vector3(-CenterWorldPx, 0f)) *
            Matrix.CreateScale(Zoom, Zoom, 1f) *
            Matrix.CreateTranslation(new Vector3(screenCenter, 0f));
    }
}
```

TileGame/Rendering/ChunkBaker.cs
```
﻿using Microsoft.Xna.Framework;
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
```

TileGame/Rendering/WorldRenderer.cs
```
﻿using Microsoft.Xna.Framework;
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
```

TileGame/UI/UiOverlay.cs
```
﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TileGame.UI;

public sealed class UiOverlay
{
    private readonly SpriteFont _font;

    public UiOverlay(SpriteFont font)
    {
        _font = font;
    }

    public void Draw(SpriteBatch sb, Vector2 playerPosTiles, float zoom, bool hasClick, Point clickedTile)
    {
        sb.DrawString(_font,
            $"pos: {playerPosTiles.X:0.00}, {playerPosTiles.Y:0.00}  zoom: {zoom:0.00}",
            new Vector2(10, 10),
            Color.White);

        if (hasClick)
        {
            sb.DrawString(_font,
                $"Clicked tile: {clickedTile.X}, {clickedTile.Y}",
                new Vector2(10, 30),
                Color.White);
        }
    }
}
```

TileGame/Util/Coords.cs
```
﻿using Microsoft.Xna.Framework;
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

```

TileGame/Util/Hash.cs
```
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TileGame.Util
{
    internal static class Hash
    {
        public static uint Hash32(int x, int y, int salt = 0)
        {
            unchecked
            {
                uint h = 2166136261u;
                h = (h ^ (uint)x) * 16777619u;
                h = (h ^ (uint)y) * 16777619u;
                h = (h ^ (uint)salt) * 16777619u;
                // final mix
                h ^= h >> 16;
                h *= 2246822519u;
                h ^= h >> 13;
                h *= 3266489917u;
                h ^= h >> 16;
                return h;
            }
        }


    }
}

```

TileGame/Util/ProcFeature.cs
```
﻿using TileGame.Biomes;
using TileGame.World;

namespace TileGame.Util;

public static class ProcFeatures
{
    private const int BoulderSalt = 424242;
    private const int TreeSalt = 515151;

    public static bool HasSolidObject(Chunk chunk, int worldTileX, int worldTileY)
        => HasBoulder(chunk, worldTileX, worldTileY) || HasTree(chunk, worldTileX, worldTileY);

    public static bool HasBoulder(Chunk chunk, int worldTileX, int worldTileY)
    {
        int chance = BiomeManager.GetBiome(chunk.BiomeType).BoulderChance;
        return HasX(chunk, worldTileX, worldTileY, BoulderSalt, chance);
    }

    public static bool HasTree(Chunk chunk, int worldTileX, int worldTileY)
    {
        int chance = BiomeManager.GetBiome(chunk.BiomeType).TreeChance;
        return HasX(chunk, worldTileX, worldTileY, TreeSalt, chance);
    }

    private static bool HasX(Chunk chunk, int worldTileX, int worldTileY, int salt, int chanceOutOf1024)
    {
        if (chanceOutOf1024 <= 0) return false;
        if (chanceOutOf1024 >= 1024) return true;

        // Include biome in salt so different biomes reshuffle features even at same coords
        int biomeSalt = salt + (int)chunk.BiomeType * 1000;

        uint h = Hash.Hash32(worldTileX, worldTileY, biomeSalt);
        return (h & 1023u) < (uint)chanceOutOf1024;
    }
}

```

TileGame/World/Chunk.cs
```
﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using TileGame.Biomes;

namespace TileGame.World;

public sealed class Chunk
{
    public readonly ChunkKey Key;
    public readonly BiomeEnum BiomeType;
    public readonly byte[] Terrain = new byte[WorldConstants.ChunkSize * WorldConstants.ChunkSize];

    // Runtime cache (not serialized)
    public RenderTarget2D TextureCache;

    public Chunk(ChunkKey key, BiomeEnum biomeType)
    {
        byte fillByte = biomeType switch
        {
            BiomeEnum.None => 0,
            BiomeEnum.GrassyPlain => 1,
            BiomeEnum.Desert => 2,
            _ => 3,
        };

        Key = key;
        Array.Fill(Terrain, (byte)fillByte);
        BiomeType = biomeType;
    }
}

```

TileGame/World/ChunkKey.cs
```
﻿using System;

namespace TileGame.World;

public readonly struct ChunkKey : IEquatable<ChunkKey>
{
    public readonly int Layer;
    public readonly int X;
    public readonly int Y;

    public ChunkKey(int layer, int x, int y)
    {
        Layer = layer;
        X = x;
        Y = y;
    }

    public bool Equals(ChunkKey other)
        => Layer == other.Layer && X == other.X && Y == other.Y;

    public override bool Equals(object obj)
        => obj is ChunkKey other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(Layer, X, Y);

    public override string ToString()
        => $"L{Layer} ({X},{Y})";
}

```

TileGame/World/ChunkManager.cs
```
﻿using System.Collections.Generic;
using TileGame.Biomes;
using TileGame.Rendering;
using TileGame.Util;

namespace TileGame.World;

public sealed class ChunkManager
{
    private readonly Dictionary<ChunkKey, Chunk> _chunks = new();
    private readonly int _layerId;

    // World seed: later you’ll load this from save / world settings
    private readonly int _worldSeed;

    private static readonly BiomeEnum[] _biomes = new[]
    {
        BiomeEnum.GrassyPlain,
        BiomeEnum.Desert,
        // don’t include None unless you really want it in the world
    };

    public ChunkManager(int layerId = 0, int worldSeed = 1234567)
    {
        _layerId = layerId;
        _worldSeed = worldSeed;
    }

    public Chunk GetChunk(int chunkX, int chunkY)
    {
        var key = new ChunkKey(_layerId, chunkX, chunkY);

        if (!_chunks.TryGetValue(key, out var chunk))
        {
            var biome = PickBiome(chunkX, chunkY);
            chunk = new Chunk(key, biome);
            _chunks.Add(key, chunk);
        }

        return chunk;
    }

    private BiomeEnum PickBiome(int chunkX, int chunkY)
    {
        // Hash chunk coords + layer + world seed
        int salt = _worldSeed ^ (_layerId * 1000003);
        uint h = Hash.Hash32(chunkX, chunkY, salt);

        int idx = (int)(h % (uint)_biomes.Length);
        return _biomes[idx];
    }

    public void UnbakeAll()
    {
        foreach (var c in _chunks.Values) // expose enumerable or add method on ChunkManager
            ChunkBaker.UnbakeChunkTexture(c);
    }
}

```

TileGame/World/Objects/HouseObject.cs
```
﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TileGame.World.Objects;

public sealed class HouseObject : IWorldObject
{
    public long Id { get; }
    public int LayerId { get; }
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

        // Door gap makes that border tile non-solid
        if (IsDoorTile(localX, localY))
            return false;

        return true;
    }


    public Rectangle BoundsTiles { get; }

    private readonly Point _doorLocal; // door position in local house coords
    private readonly int _doorWidth;   // tiles (1 for now)

    public HouseObject(long id, int layerId, int xTiles, int yTiles, int wTiles = 6, int hTiles = 6,
        Point? doorLocal = null, int doorWidth = 1)
    {
        Id = id;
        LayerId = layerId;
        BoundsTiles = new Rectangle(xTiles, yTiles, wTiles, hTiles);

        _doorLocal = doorLocal ?? new Point(wTiles / 2, hTiles - 1); // default: centered on bottom wall
        _doorWidth = doorWidth;
    }

    public void Draw(SpriteBatch sb, Texture2D pixel, int tileSizePx)
    {
        // Colors: keep simple
        var wallColor = Color.SaddleBrown;
        var floorColor = Color.BurlyWood * 0.8f;

        // Draw floor fill (entire rect)
        var floorPx = new Rectangle(
            BoundsTiles.X * tileSizePx,
            BoundsTiles.Y * tileSizePx,
            BoundsTiles.Width * tileSizePx,
            BoundsTiles.Height * tileSizePx);

        sb.Draw(pixel, floorPx, floorColor);

        // Draw walls as 1-tile thick border, but leave a 1-tile gap for the door.
        for (int ly = 0; ly < BoundsTiles.Height; ly++)
        {
            for (int lx = 0; lx < BoundsTiles.Width; lx++)
            {
                bool isBorder = (lx == 0 || ly == 0 || lx == BoundsTiles.Width - 1 || ly == BoundsTiles.Height - 1);
                if (!isBorder) continue;

                // Door gap on the border: default bottom wall
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
        // Door gap only makes sense if it lies on a border tile.
        // Current logic: door is a 1-tile hole starting at _doorLocal extending right.
        if (localY != _doorLocal.Y) return false;
        if (localX < _doorLocal.X || localX >= _doorLocal.X + _doorWidth) return false;

        // Ensure it’s actually on the border; if not, treat as no door
        bool onBorder = (localX == 0 || localY == 0 || localX == BoundsTiles.Width - 1 || localY == BoundsTiles.Height - 1);
        return onBorder;
    }

}

```

TileGame/World/Objects/IWorldObject.cs
```
﻿using Microsoft.Xna.Framework;
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
```

TileGame/World/Objects/WorldObjectStore.cs
```
﻿using Microsoft.Xna.Framework;
using System.Collections.Generic;
using TileGame.World;

namespace TileGame.World.Objects;

public sealed class WorldObjectStore
{
    private readonly Dictionary<long, IWorldObject> _objects = new();
    private readonly Dictionary<ChunkKey, List<long>> _chunkIndex = new();

    private long _nextId = 1;

    public long NewId() => _nextId++;

    public void Add(IWorldObject obj)
    {
        _objects.Add(obj.Id, obj);
        IndexObject(obj);
    }

    public IEnumerable<IWorldObject> QueryByChunk(int layerId, int chunkX, int chunkY)
    {
        var key = new ChunkKey(layerId, chunkX, chunkY);
        if (!_chunkIndex.TryGetValue(key, out var ids))
            yield break;

        // Note: For now, IDs are unique in the list. If you later rebuild index, dedupe if needed.
        foreach (var id in ids)
            yield return _objects[id];
    }

    private void IndexObject(IWorldObject obj)
    {
        // Determine which chunks this object overlaps based on its tile bounds.
        // Chunk size = 32
        const int cs = WorldConstants.ChunkSize;

        int minCx = FloorDiv(obj.BoundsTiles.Left, cs);
        int maxCx = FloorDiv(obj.BoundsTiles.Right - 1, cs);
        int minCy = FloorDiv(obj.BoundsTiles.Top, cs);
        int maxCy = FloorDiv(obj.BoundsTiles.Bottom - 1, cs);

        for (int cy = minCy; cy <= maxCy; cy++)
        {
            for (int cx = minCx; cx <= maxCx; cx++)
            {
                var key = new ChunkKey(obj.LayerId, cx, cy);
                if (!_chunkIndex.TryGetValue(key, out var list))
                {
                    list = new List<long>(4);
                    _chunkIndex[key] = list;
                }
                list.Add(obj.Id);
            }
        }
    }

    public bool IsSolidTile(int layerId, int worldTileX, int worldTileY)
    {
        // Query only the chunk that contains this tile.
        // Because objects are indexed into every chunk they overlap,
        // this is sufficient even when objects span chunk boundaries.
        int cx = FloorDiv(worldTileX, WorldConstants.ChunkSize);
        int cy = FloorDiv(worldTileY, WorldConstants.ChunkSize);

        var key = new ChunkKey(layerId, cx, cy);
        if (!_chunkIndex.TryGetValue(key, out var ids))
            return false;

        foreach (var id in ids)
        {
            var obj = _objects[id];
            if (obj.IsSolidTile(worldTileX, worldTileY))
                return true;
        }

        return false;
    }

    private static int FloorDiv(int a, int b)
    {
        // Handles negatives correctly too, but you’re probably non-negative anyway.
        int q = a / b;
        int r = a % b;
        if (r != 0 && ((r > 0) != (b > 0))) q--;
        return q;
    }
}

```

TileGame/World/WorldConstants.cs
```
﻿namespace TileGame.World;

public static class WorldConstants
{
    public const int WorldLayerId = 0;
    public const int TileSizePx = 32;
    public const int ChunkSize = 32;
}
```

