using Microsoft.Xna.Framework;
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