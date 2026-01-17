using System;
using System.Collections.Concurrent;

public class ObjectPool<T> where T : class, new()
{
    private readonly ConcurrentBag<T> _objects = new();
    private readonly Func<T> _factory;
    
    public ObjectPool(Func<T> factory = null) => _factory = factory ?? (() => new T());
    
    public T Get() => _objects.TryTake(out var obj) ? obj : _factory();
    public void Return(T obj) => _objects.Add(obj);
}

// Use for frequently allocated objects like Lists, vectors, etc.