using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TileGame.World;

/// <summary>
/// Tracks modifications to procedurally generated objects (deletions, moves, state changes)
/// </summary>
public class ObjectModificationStore
{
    // Exclusion zones where procgen objects should not appear
    private readonly List<ExclusionZone> _exclusionZones = new();
    
    // Specific objects that have been deleted/removed from procgen
    private readonly HashSet<ObjectProcGenKey> _deletedObjects = new();
    
    // Objects that have been modified (moved, state changed, etc.)
    private readonly Dictionary<ObjectProcGenKey, ModifiedObject> _modifiedObjects = new();
    
    public void AddExclusionZone(ExclusionZone zone)
    {
        _exclusionZones.Add(zone);
    }
    
    public void RemoveExclusionZone(ExclusionZone zone)
    {
        _exclusionZones.Remove(zone);
    }
    
    /// <summary>
    /// Check if a position is in any exclusion zone
    /// </summary>
    public bool IsInExclusionZone(Vector2 position)
    {
        foreach (var zone in _exclusionZones)
        {
            if (zone.Contains(position))
                return true;
        }
        return false;
    }
    
    /// <summary>
    /// Mark a procgen object as deleted
    /// </summary>
    public void DeleteObject(ObjectProcGenKey key)
    {
        _deletedObjects.Add(key);
        _modifiedObjects.Remove(key); // Can't be both deleted and modified
    }
    
    /// <summary>
    /// Check if a procgen object has been deleted
    /// </summary>
    public bool IsDeleted(ObjectProcGenKey key)
    {
        return _deletedObjects.Contains(key);
    }
    
    /// <summary>
    /// Modify a procgen object (moves it from procgen to explicit storage)
    /// </summary>
    public void ModifyObject(ObjectProcGenKey key, ModifiedObject obj)
    {
        _deletedObjects.Remove(key); // Remove from deleted if it was there
        _modifiedObjects[key] = obj;
    }
    
    /// <summary>
    /// Get modified version of an object, or null if not modified
    /// </summary>
    public ModifiedObject? GetModifiedObject(ObjectProcGenKey key)
    {
        return _modifiedObjects.GetValueOrDefault(key);
    }
    
    /// <summary>
    /// Get all modified objects in a chunk
    /// </summary>
    public IEnumerable<ModifiedObject> GetModifiedObjectsInChunk(int layerId, int chunkX, int chunkY)
    {
        foreach (var (key, obj) in _modifiedObjects)
        {
            if (key.LayerId == layerId && key.ChunkX == chunkX && key.ChunkY == chunkY)
                yield return obj;
        }
    }
}

/// <summary>
/// Uniquely identifies a procedurally generated object
/// </summary>
public record struct ObjectProcGenKey(int LayerId, int ChunkX, int ChunkY, int LocalX, int LocalY, string ObjectType);

/// <summary>
/// Represents a modified object with its new state
/// </summary>
public class ModifiedObject
{
    public ObjectProcGenKey OriginalKey { get; set; }
    public Vector2 Position { get; set; } // New position if moved
    public ObjectState State { get; set; } // Current state (e.g., chopped, moved, regrowing)
    public DateTime? ModificationTime { get; set; } // When was it modified
    public Dictionary<string, object> CustomData { get; set; } = new(); // For things like regrowth timers
}

public enum ObjectState
{
    Normal,
    Moved,
    ChoppedDown,
    Destroyed,
    Regrowing,
    Damaged
}

/// <summary>
/// Defines an area where procgen objects should not spawn
/// </summary>
public abstract class ExclusionZone
{
    public abstract bool Contains(Vector2 position);
}

/// <summary>
/// Rectangular exclusion zone (for houses, buildings)
/// </summary>
public class RectangularExclusionZone : ExclusionZone
{
    public Rectangle Bounds { get; set; }
    public float Padding { get; set; } // Extra tiles around the rectangle
    
    public RectangularExclusionZone(Rectangle bounds, float padding = 0)
    {
        Bounds = bounds;
        Padding = padding;
    }
    
    public override bool Contains(Vector2 position)
    {
        return position.X >= Bounds.X - Padding &&
               position.X < Bounds.Right + Padding &&
               position.Y >= Bounds.Y - Padding &&
               position.Y < Bounds.Bottom + Padding;
    }
}

/// <summary>
/// Road-based exclusion zone (checks if position is on a road)
/// </summary>
public class RoadExclusionZone : ExclusionZone
{
    private readonly Road _road;
    
    public RoadExclusionZone(Road road)
    {
        _road = road;
    }
    
    public override bool Contains(Vector2 position)
    {
        // Check if position is on any road segment
        foreach (var segment in _road.Segments)
        {
            if (segment.ContainsPoint(position, tolerance: 1.0f))
                return true;
        }
        return false;
    }
}