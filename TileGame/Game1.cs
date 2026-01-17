using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
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
    private ObjectModificationStore _objectMods = null!;
    private WorldRenderer _worldRenderer = null!;

    private ChunkManager _chunkManager = null!;
    private ChunkBaker _baker = null!;
    private RoadNetwork _roadNetwork = null!;

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
        _roadNetwork = new RoadNetwork();
        _objectMods = new ObjectModificationStore();

        _objects = new WorldObjectStore();

        // Test house
        long houseId = _objects.NewId();
        var house = new HouseObject(
            id: houseId,
            layerId: WorldConstants.WorldLayerId,
            xTiles: 14,
            yTiles: 4,
            wTiles: 20,
            hTiles: 20,
            doorLocal: new Point(3, 19),
            doorWidth: 1);
        _objects.Add(house);
        
        // Create exclusion zone around house (5 tiles padding for cleared area)
        _objectMods.AddExclusionZone(new RectangularExclusionZone(
            new Rectangle(14, 4, 20, 20), 
            padding: 5));

        _collision = new CollisionResolver(
            _chunkManager,
            _objects,
            _objectMods,  // Add this parameter
            WorldConstants.WorldLayerId,
            playerRadiusTiles: 0.35f,
            terrainSolid: static (chunk, tx, ty) => Util.ProcFeatures.HasSolidObject(chunk, tx, ty)
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
        _worldRenderer = new WorldRenderer(_chunkManager, _objects, _objectMods, _baker, _spriteBatch, _pixel,
            terrainSolid: (chunk, tx, ty) => Util.ProcFeatures.HasSolidObject(chunk, tx, ty));

        // Generate showcase road with configuration
        var road = GenerateOldTradeRoute();
        
        // Create exclusion zone for the road (no trees/rocks on road)
        _objectMods.AddExclusionZone(new RoadExclusionZone(road));
    }

    private Road GenerateOldTradeRoute()
    {
        // Control points for a Catmull-Rom spline
        // Note: ChunkSize is 32x32, so ~100 chunks = ~3200 tiles
        // Road goes from ~-50 chunks west, curves past the house at (14,4), to ~+50 chunks southeast
        var config = new RoadConfig
        {
            ControlPoints = new List<Vector2>
            {
                new(-1750, -150),   // Start (northwest)
                new(-1500, -75),
                new(-1200, -25),
                new(-900, 0),
                new(-600, 15),
                new(-300, 25),
                new(-100, 20),
                new(0, 10),         // Near origin/house area (house is at 14, 4)
                new(100, 5),
                new(300, 15),
                new(500, 50),
                new(700, 125),
                new(900, 250),
                new(1100, 425),
                new(1300, 650),
                new(1500, 925),
                new(1700, 1250),    // End (southeast)
                new(1850, 1500)     // Overrun for smooth ending
            },
            
            WidthSegments = new List<RoadWidthSegment>
            {
                new() { StartIndex = 0, EndIndex = 3, Width = 3 },      // Narrow start (2-3 tiles wide)
                new() { StartIndex = 3, EndIndex = 6, Width = 5 },      // Medium section (4-5 tiles wide)
                new() { StartIndex = 6, EndIndex = 9, Width = 7 },      // Wide near spawn/house (6-7 tiles wide)
                new() { StartIndex = 9, EndIndex = 12, Width = 4 },     // Medium (3-4 tiles wide)
                new() { StartIndex = 12, EndIndex = 15, Width = 6 },    // Wide section (5-6 tiles wide)
                new() { StartIndex = 15, EndIndex = 18, Width = 3 }     // Narrow ending (2-3 tiles wide)
            },
            
            InterpolationSteps = 200 // Smooth curve with 200 points per segment
        };
        
        return RoadGenerator.GenerateRoad(
            _chunkManager, 
            _roadNetwork, 
            roadId: 1, 
            roadName: "Old Trade Route", 
            config: config,
            roadType: RoadType.DirtRoad, 
            layerId: WorldConstants.WorldLayerId);
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

        // Check which road the player is on (example usage)
        var currentRoad = _roadNetwork.GetRoadAt(_player.PlayerPosTiles);
        if (currentRoad != null && _input.Pressed(Keys.R))
        {
            // Press R to see road info (you can display this in UI later)
            System.Diagnostics.Debug.WriteLine($"On road: {currentRoad.Name} (Type: {currentRoad.Type}, Segments: {currentRoad.Segments.Count})");
        }

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
        
        // Display current road info
        var currentRoad = _roadNetwork.GetRoadAt(_player.PlayerPosTiles);
        if (currentRoad != null)
        {
            string roadInfo = $"Road: {currentRoad.Name} ({currentRoad.Type})";
            _spriteBatch.DrawString(_uiFont, roadInfo, new Vector2(10, 80), Color.Yellow);
        }
        
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




