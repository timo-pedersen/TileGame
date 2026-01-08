using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TileGame.Util
{
    internal static class Hash
    {
        public static uint Hash32(int x, int y, int salt = 0)
        {
            unchecked
            {
                uint h = 2166136261u;
                h = (h ^ (uint)x) * 16777619u;
                h = (h ^ (uint)y) * 16777619u;
                h = (h ^ (uint)salt) * 16777619u;
                // final mix
                h ^= h >> 16;
                h *= 2246822519u;
                h ^= h >> 13;
                h *= 3266489917u;
                h ^= h >> 16;
                return h;
            }
        }


    }
}
