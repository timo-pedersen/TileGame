using Microsoft.Xna.Framework;
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



