using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TalesFromTheUnderbrush.src.Core.Tiles
{
    public static class TileIdGenerator
    {
        private static ulong _nextId = 1;

        public static ulong Next()
        {
            return _nextId++;
        }

        public static void Reset()
        {
            _nextId = 1;
        }
    }
}
