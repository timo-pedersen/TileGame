using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class TextureAtlas
{
    private readonly Texture2D _atlas;
    private readonly Dictionary<string, Rectangle> _regions = new();
    
    public Rectangle GetRegion(string name) => _regions[name];
    
    // Enables single texture for all tiles = massive performance boost
}