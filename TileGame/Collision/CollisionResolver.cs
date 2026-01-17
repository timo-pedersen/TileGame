using Microsoft.Xna.Framework;
using TileGame.World;
using TileGame.World.Objects;
using System;

namespace TileGame.Collision;

public sealed class CollisionResolver
{
    private readonly ChunkManager _chunks;
    private readonly WorldObjectStore _objects;
    private readonly ObjectModificationStore _objectMods;
    private readonly int _layerId;
    private readonly float _playerRadiusTiles;
    private readonly Func<Chunk, int, int, bool?> _terrainSolid;

    public CollisionResolver(
        ChunkManager chunks,
        WorldObjectStore objects,
        ObjectModificationStore objectMods,
        int layerId,
        float playerRadiusTiles,
        Func<Chunk, int, int, bool?> terrainSolid = null)
    {
        _chunks = chunks;
        _objects = objects;
        _objectMods = objectMods;
        _layerId = layerId;
        _playerRadiusTiles = playerRadiusTiles;
        _terrainSolid = terrainSolid;
    }

    public Vector2 ResolveMovement(Vector2 currentPosTiles, Vector2 desiredPosTiles)
    {
        // Simple AABB collision with tile-based obstacles
        Vector2 result = desiredPosTiles;

        // Check if desired position collides with solid tiles or objects
        if (IsSolidAt(result))
        {
            // Try X movement only
            Vector2 tryX = new Vector2(desiredPosTiles.X, currentPosTiles.Y);
            if (!IsSolidAt(tryX))
            {
                result = tryX;
            }
            else
            {
                // Try Y movement only
                Vector2 tryY = new Vector2(currentPosTiles.X, desiredPosTiles.Y);
                if (!IsSolidAt(tryY))
                {
                    result = tryY;
                }
                else
                {
                    // Can't move, stay in place
                    result = currentPosTiles;
                }
            }
        }

        return result;
    }

    private bool IsSolidAt(Vector2 posTiles)
    {
        // Check a circle around the player position
        int checkRadius = (int)Math.Ceiling(_playerRadiusTiles);
        
        for (int dy = -checkRadius; dy <= checkRadius; dy++)
        {
            for (int dx = -checkRadius; dx <= checkRadius; dx++)
            {
                // Only check tiles within player radius
                if (dx * dx + dy * dy > _playerRadiusTiles * _playerRadiusTiles)
                    continue;

                int tileX = (int)Math.Floor(posTiles.X) + dx;
                int tileY = (int)Math.Floor(posTiles.Y) + dy;

                if (IsTileSolid(tileX, tileY))
                    return true;
            }
        }

        return false;
    }

    private bool IsTileSolid(int tileX, int tileY)
    {
        // Check terrain solidity via custom function
        if (_terrainSolid != null)
        {
            int chunkX = (int)Math.Floor((double)tileX / WorldConstants.ChunkSize);
            int chunkY = (int)Math.Floor((double)tileY / WorldConstants.ChunkSize);
            var chunk = _chunks.GetChunk(chunkX, chunkY);
            
            // Check if there's a procgen object here (not in exclusion zone and not deleted)
            var tilePos = new Vector2(tileX, tileY);
            if (!_objectMods.IsInExclusionZone(tilePos))
            {
                var localX = tileX - (chunkX * WorldConstants.ChunkSize);
                var localY = tileY - (chunkY * WorldConstants.ChunkSize);
                while (localX < 0) localX += WorldConstants.ChunkSize;
                while (localY < 0) localY += WorldConstants.ChunkSize;
                
                var key = new ObjectProcGenKey(_layerId, chunkX, chunkY, localX, localY, "TreeObject");
                if (!_objectMods.IsDeleted(key))
                {
                    var result = _terrainSolid.Invoke(chunk, tileX, tileY);
                    if (result == true)
                        return true;
                }
                
                // Also check for rocks
                key = new ObjectProcGenKey(_layerId, chunkX, chunkY, localX, localY, "RockObject");
                if (!_objectMods.IsDeleted(key))
                {
                    var result = _terrainSolid.Invoke(chunk, tileX, tileY);
                    if (result == true)
                        return true;
                }
            }
        }

        // Check explicit objects from object store
        if (_objects.IsSolidTile(_layerId, tileX, tileY))
            return true;

        return false;
    }
}