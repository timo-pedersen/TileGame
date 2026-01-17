using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TileGame.World;

public class RoadSegment
{
    public Vector2 Start { get; set; }
    public Vector2 End { get; set; }
    public Vector2 Center => (Start + End) * 0.5f;
    public int Width { get; set; }
    
    public bool ContainsPoint(Vector2 point, float tolerance = 0.5f)
    {
        float dist = DistanceToSegment(point);
        return dist <= Width + tolerance;
    }
    
    public float DistanceToSegment(Vector2 point)
    {
        Vector2 v = End - Start;
        Vector2 w = point - Start;
        
        float c1 = Vector2.Dot(w, v);
        if (c1 <= 0) return Vector2.Distance(point, Start);
        
        float c2 = Vector2.Dot(v, v);
        if (c1 >= c2) return Vector2.Distance(point, End);
        
        float b = c1 / c2;
        Vector2 pb = Start + b * v;
        return Vector2.Distance(point, pb);
    }
}

public enum RoadType
{
    DirtRoad,
    GravelRoad,
    PavedRoad,
    Highway
}

public class RoadNetwork
{
    private readonly Dictionary<int, Road> _roads = new();
    private readonly Dictionary<Point, List<int>> _chunkToRoads = new(); // Spatial index
    
    public void AddRoad(Road road)
    {
        _roads[road.Id] = road;
        
        // Build spatial index for fast lookups
        foreach (var segment in road.Segments)
        {
            int minChunkX = (int)Math.Floor(Math.Min(segment.Start.X, segment.End.X) / WorldConstants.ChunkSize);
            int maxChunkX = (int)Math.Floor(Math.Max(segment.Start.X, segment.End.X) / WorldConstants.ChunkSize);
            int minChunkY = (int)Math.Floor(Math.Min(segment.Start.Y, segment.End.Y) / WorldConstants.ChunkSize);
            int maxChunkY = (int)Math.Floor(Math.Max(segment.Start.Y, segment.End.Y) / WorldConstants.ChunkSize);
            
            for (int cy = minChunkY; cy <= maxChunkY; cy++)
            {
                for (int cx = minChunkX; cx <= maxChunkX; cx++)
                {
                    var chunkKey = new Point(cx, cy);
                    if (!_chunkToRoads.ContainsKey(chunkKey))
                        _chunkToRoads[chunkKey] = new List<int>();
                    
                    if (!_chunkToRoads[chunkKey].Contains(road.Id))
                        _chunkToRoads[chunkKey].Add(road.Id);
                }
            }
        }
    }
    
    /// <summary>
    /// Find which road (if any) contains the given tile position
    /// </summary>
    public Road GetRoadAt(Vector2 tilePosition)
    {
        int chunkX = (int)Math.Floor(tilePosition.X / WorldConstants.ChunkSize);
        int chunkY = (int)Math.Floor(tilePosition.Y / WorldConstants.ChunkSize);
        var chunkKey = new Point(chunkX, chunkY);
        
        if (!_chunkToRoads.TryGetValue(chunkKey, out var roadIds))
            return null;
        
        // Check all roads in this chunk
        foreach (var roadId in roadIds)
        {
            var road = _roads[roadId];
            foreach (var segment in road.Segments)
            {
                if (segment.ContainsPoint(tilePosition))
                    return road;
            }
        }
        
        return null;
    }
    
    public Road GetRoad(int id) => _roads.GetValueOrDefault(id);
    
    public IEnumerable<Road> GetAllRoads() => _roads.Values;
    
    public IEnumerable<Road> GetRoadsInChunk(int chunkX, int chunkY)
    {
        var chunkKey = new Point(chunkX, chunkY);
        if (!_chunkToRoads.TryGetValue(chunkKey, out var roadIds))
            yield break;
            
        foreach (var roadId in roadIds)
        {
            if (_roads.TryGetValue(roadId, out var road))
                yield return road;
        }
    }
}