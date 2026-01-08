using Microsoft.Xna.Framework;
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