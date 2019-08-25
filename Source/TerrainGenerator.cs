using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlaxEngine;

namespace VoxelTerrain.Source
{
    public static class TerrainGenerator
    {
        private static readonly PerlinNoise _generator = new PerlinNoise(16,128,64);
        public static int GetHeight(int x, int z) => (int) Mathf.Max(1, Mathf.Min(64, _generator.Sample(x, z)));
    }
}
