using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TileGame.World;

public class Road
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public RoadType Type { get; set; }
    public List<Vector2> ControlPoints { get; set; } = new();
    public List<RoadSegment> Segments { get; set; } = new();
    
    /// <summary>
    /// Get nearest point on road centerline for pathfinding
    /// </summary>
    public Vector2 GetNearestPoint(Vector2 position)
    {
        if (Segments.Count == 0)
            return position;
            
        Vector2 nearest = Segments[0].Start;
        float minDist = Vector2.DistanceSquared(position, nearest);
        
        foreach (var segment in Segments)
        {
            // Check both endpoints and center
            foreach (var point in new[] { segment.Start, segment.Center, segment.End })
            {
                float dist = Vector2.DistanceSquared(position, point);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = point;
                }
            }
        }
        return nearest;
    }
    
    /// <summary>
    /// Get direction vector at nearest point (for NPC following)
    /// </summary>
    public Vector2 GetDirectionAt(Vector2 position)
    {
        if (Segments.Count == 0)
            return Vector2.Zero;
            
        RoadSegment nearestSegment = null;
        float minDist = float.MaxValue;
        
        foreach (var segment in Segments)
        {
            float dist = segment.DistanceToSegment(position);
            if (dist < minDist)
            {
                minDist = dist;
                nearestSegment = segment;
            }
        }
        
        if (nearestSegment != null)
        {
            Vector2 direction = nearestSegment.End - nearestSegment.Start;
            if (direction.LengthSquared() > 0)
                direction.Normalize();
            return direction;
        }
        
        return Vector2.Zero;
    }
}
