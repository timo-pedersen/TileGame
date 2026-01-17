using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TileGame.World;

/// <summary>
/// Configuration for generating a road
/// </summary>
public class RoadConfig
{
    public List<Vector2> ControlPoints { get; set; } = new();
    public List<RoadWidthSegment> WidthSegments { get; set; } = new();
    public int InterpolationSteps { get; set; } = 200; // Points per control point segment
}

public class RoadWidthSegment
{
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public int Width { get; set; }
}

public static class RoadGenerator
{
    // Road tile type - brown-gray road
    private const byte RoadTileType = 4;
    
    public static Road GenerateRoad(
        ChunkManager chunkManager, 
        RoadNetwork network, 
        int roadId, 
        string roadName, 
        RoadConfig config,
        RoadType roadType = RoadType.DirtRoad, 
        int layerId = 0)
    {
        // Generate road path from configuration
        var (roadPath, controlPoints) = GenerateRoadPath(config);
        
        var road = new Road
        {
            Id = roadId,
            Name = roadName,
            Type = roadType,
            ControlPoints = controlPoints
        };
        
        Vector2? lastPoint = null;
        
        // Apply the road to chunks and build segments
        foreach (var (tileX, tileY, width) in roadPath)
        {
            ApplyRoadTile(chunkManager, tileX, tileY, width);
            
            var currentPoint = new Vector2(tileX, tileY);
            if (lastPoint.HasValue)
            {
                // Sample every ~5 tiles to avoid too many segments
                if (Vector2.Distance(lastPoint.Value, currentPoint) >= 5)
                {
                    road.Segments.Add(new RoadSegment
                    {
                        Start = lastPoint.Value,
                        End = currentPoint,
                        Width = width
                    });
                    lastPoint = currentPoint;
                }
            }
            else
            {
                lastPoint = currentPoint;
            }
        }
        
        network.AddRoad(road);
        return road;
    }
    
    private static void ApplyRoadTile(ChunkManager chunkManager, int centerX, int centerY, int width)
    {
        // Apply road width with a circular brush
        for (int dx = -width; dx <= width; dx++)
        {
            for (int dy = -width; dy <= width; dy++)
            {
                // Circular brush pattern
                if (dx * dx + dy * dy > width * width) continue;
                
                int targetTileX = centerX + dx;
                int targetTileY = centerY + dy;
                
                // Calculate which chunk this tile belongs to
                int chunkX = (int)Math.Floor((double)targetTileX / WorldConstants.ChunkSize);
                int chunkY = (int)Math.Floor((double)targetTileY / WorldConstants.ChunkSize);
                
                var chunk = chunkManager.GetChunk(chunkX, chunkY);
                
                // Calculate local position within chunk
                int localX = targetTileX - (chunkX * WorldConstants.ChunkSize);
                int localY = targetTileY - (chunkY * WorldConstants.ChunkSize);
                
                // Handle negative wrapping
                while (localX < 0) localX += WorldConstants.ChunkSize;
                while (localY < 0) localY += WorldConstants.ChunkSize;
                while (localX >= WorldConstants.ChunkSize) localX -= WorldConstants.ChunkSize;
                while (localY >= WorldConstants.ChunkSize) localY -= WorldConstants.ChunkSize;
                
                int index = localY * WorldConstants.ChunkSize + localX;
                if (index >= 0 && index < chunk.Terrain.Length)
                {
                    chunk.Terrain[index] = RoadTileType;
                }
            }
        }
    }
    
    private static (List<(int x, int y, int width)>, List<Vector2>) GenerateRoadPath(RoadConfig config)
    {
        var path = new List<(int x, int y, int width)>();
        
        if (config.ControlPoints.Count < 4)
            throw new ArgumentException("Road configuration must have at least 4 control points for Catmull-Rom interpolation");
        
        // Convert Vector2 control points to tuples for interpolation
        var controlPoints = new List<(float x, float y)>();
        foreach (var cp in config.ControlPoints)
            controlPoints.Add((cp.X, cp.Y));
        
        // Generate smooth curve using Catmull-Rom interpolation
        for (int i = 1; i < controlPoints.Count - 2; i++)
        {
            var p0 = controlPoints[i - 1];
            var p1 = controlPoints[i];
            var p2 = controlPoints[i + 1];
            var p3 = controlPoints[i + 2];
            
            // Determine width for this segment
            int baseWidth = 4; // Default
            foreach (var widthSeg in config.WidthSegments)
            {
                if (i >= widthSeg.StartIndex && i < widthSeg.EndIndex)
                {
                    baseWidth = widthSeg.Width;
                    break;
                }
            }
            
            // Interpolate between p1 and p2
            for (int step = 0; step < config.InterpolationSteps; step++)
            {
                float t = step / (float)config.InterpolationSteps;
                var point = CatmullRom(p0, p1, p2, p3, t);
                
                // Small local variation in width (±1 tile) for natural look
                int width = baseWidth + (int)(0.5f * MathF.Sin(step * 0.05f));
                width = Math.Clamp(width, 1, 10);
                
                path.Add(((int)Math.Round(point.x), (int)Math.Round(point.y), width));
            }
        }
        
        return (path, config.ControlPoints);
    }
    
    private static (float x, float y) CatmullRom(
        (float x, float y) p0,
        (float x, float y) p1,
        (float x, float y) p2,
        (float x, float y) p3,
        float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        
        float x = 0.5f * (
            (2 * p1.x) +
            (-p0.x + p2.x) * t +
            (2 * p0.x - 5 * p1.x + 4 * p2.x - p3.x) * t2 +
            (-p0.x + 3 * p1.x - 3 * p2.x + p3.x) * t3
        );
        
        float y = 0.5f * (
            (2 * p1.y) +
            (-p0.y + p2.y) * t +
            (2 * p0.y - 5 * p1.y + 4 * p2.y - p3.y) * t2 +
            (-p0.y + 3 * p1.y - 3 * p2.y + p3.y) * t3
        );
        
        return (x, y);
    }
}