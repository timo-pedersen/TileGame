using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using TileGame.World;

public class GameEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();
    
    public void Subscribe<T>(Action<T> handler) where T : IGameEvent
    {
        var type = typeof(T);
        if (!_handlers.ContainsKey(type))
            _handlers[type] = new List<Delegate>();
        _handlers[type].Add(handler);
    }
    
    public void Publish<T>(T evt) where T : IGameEvent
    {
        if (_handlers.TryGetValue(typeof(T), out var handlers))
            foreach (var h in handlers.Cast<Action<T>>())
                h(evt);
    }
}

// Example events
public record PlayerMovedEvent(Vector2 OldPos, Vector2 NewPos) : IGameEvent;
public record ChunkLoadedEvent(ChunkKey Key) : IGameEvent;