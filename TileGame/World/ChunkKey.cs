using System;

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
