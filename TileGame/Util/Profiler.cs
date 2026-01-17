using System;
using System.Collections.Generic;

public static class Profiler
{
    private static readonly Dictionary<string, (int count, double totalMs)> _stats = new();
    
    public static IDisposable Profile(string name) => new ProfileScope(name);
    
    private class ProfileScope : IDisposable
    {
        private readonly string _name;
        private readonly System.Diagnostics.Stopwatch _sw;
        
        public ProfileScope(string name) { _name = name; _sw = System.Diagnostics.Stopwatch.StartNew(); }
        public void Dispose()
        {
            _sw.Stop();
            // Track stats...
        }
    }
}

// Usage: using var _ = Profiler.Profile("ChunkBaking");