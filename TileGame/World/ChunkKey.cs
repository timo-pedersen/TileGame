using System;

namespace TileGame.World;

public readonly record struct ChunkKey(int LayerId, int X, int Y);
