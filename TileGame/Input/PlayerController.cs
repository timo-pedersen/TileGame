using Microsoft.Xna.Framework;
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