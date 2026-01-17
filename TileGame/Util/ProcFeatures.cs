using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using TileGame.World;
using TileGame.World.Objects;

namespace TileGame.Util;

public static class ProcFeatures
{
    // Density controls
    private const float TreeDensity = 0.03f;  // ~3% chance per tile
    private const float RockDensity = 0.015f; // ~1.5% chance per tile
    
    /// <summary>
    /// Check if a tile has a solid procedurally generated object (for collision)
    /// </summary>
    public static bool? HasSolidObject(Chunk chunk, int worldTileX, int worldTileY)
    {
        // Create salt from WORLD tile coordinates (consistent with rendering)
        int salt = (worldTileX * 73856093) ^ (worldTileY * 19349663);
        
        // Use chunk key and salt to deterministically generate features
        uint hash = Hash.Hash32(chunk.Key.X, chunk.Key.Y, salt);
        
        // Trees and rocks are solid
        if (ShouldHaveTree(hash))
            return true;
        
        if (ShouldHaveRock(hash))
            return true;
        
        return null; // No solid object here
    }
    
    /// <summary>
    /// Generate all drawable procedural objects for a chunk
    /// </summary>
    public static IEnumerable<IWorldObject> GenerateChunkObjects(Chunk chunk, int chunkX, int chunkY)
    {
        var objects = new List<IWorldObject>();
        
        // Generate objects for each tile in the chunk
        for (int ly = 0; ly < WorldConstants.ChunkSize; ly++)
        {
            for (int lx = 0; lx < WorldConstants.ChunkSize; lx++)
            {
                int worldTileX = chunkX * WorldConstants.ChunkSize + lx;
                int worldTileY = chunkY * WorldConstants.ChunkSize + ly;
                
                // Create salt from WORLD tile coordinates (consistent with collision)
                int salt = (worldTileX * 73856093) ^ (worldTileY * 19349663);
                
                // Use deterministic hash based on chunk position and salt
                uint hash = Hash.Hash32(chunk.Key.X, chunk.Key.Y, salt);
                
                // Check for tree
                if (ShouldHaveTree(hash))
                {
                    objects.Add(new TreeObject(
                        position: new Vector2(worldTileX, worldTileY),
                        treeType: GetTreeType(hash)));
                }
                // Check for rock (don't place rock if tree is already here)
                else if (ShouldHaveRock(hash))
                {
                    objects.Add(new RockObject(
                        position: new Vector2(worldTileX, worldTileY),
                        rockType: GetRockType(hash)));
                }
            }
        }
        
        return objects;
    }
    
    private static bool ShouldHaveTree(uint hash)
    {
        float value = (hash & 0xFFFF) / 65535f;
        return value < TreeDensity;
    }
    
    private static bool ShouldHaveRock(uint hash)
    {
        float value = ((hash >> 16) & 0xFFFF) / 65535f;
        return value < RockDensity;
    }
    
    private static TreeType GetTreeType(uint hash)
    {
        int typeIndex = (int)((hash >> 8) % 3);
        return (TreeType)typeIndex;
    }
    
    private static RockType GetRockType(uint hash)
    {
        int typeIndex = (int)((hash >> 10) % 3);
        return (RockType)typeIndex;
    }
}