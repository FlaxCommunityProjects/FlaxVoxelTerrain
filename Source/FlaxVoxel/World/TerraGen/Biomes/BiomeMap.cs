using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlaxEngine;
using FlaxVoxel.TerraGen.Noise;

namespace FlaxVoxel.TerraGen
{
    public class BiomeMap
    {
        public IList<BiomeCell> BiomeCells { get; private set; }

        Perlin TempNoise, RainNoise;

        public BiomeMap(int seed)
        {
            TempNoise = new Perlin(seed);
            RainNoise = new Perlin(seed);
            BiomeCells = new List<BiomeCell>();
            TempNoise.Persistance = 1.45;
            TempNoise.Frequency = 0.015;
            TempNoise.Amplitude = 5;
            TempNoise.Octaves = 2;
            TempNoise.Lacunarity = 1.3;
            RainNoise.Frequency = 0.03;
            RainNoise.Octaves = 3;
            RainNoise.Amplitude = 5;
            RainNoise.Lacunarity = 1.7;
            TempNoise.Seed = seed;
            RainNoise.Seed = seed;
        }

        public void AddCell(BiomeCell cell)
        {
            BiomeCells.Add(cell);
        }

        public byte GetBiome(Int2 location)
        {
            byte BiomeID = (ClosestCell(location) != null) ? ClosestCell(location).BiomeID : (byte)Biomes.Plains;
            return BiomeID;
        }

        public byte GenerateBiome(int seed, BiomeRepository biomes, Int2 location)
        {
            double temp = Math.Abs(TempNoise.Value2D(location.X, location.Y));
            double rainfall = Math.Abs(RainNoise.Value2D(location.X, location.Y));
            byte ID = biomes.GetBiome(temp, rainfall).ID;
            return ID;
        }

        /*
         * The closest biome cell to the specified location(uses the Chebyshev distance function).
         */
        public BiomeCell ClosestCell(Int2 location)
        {
            BiomeCell cell = null;
            var distance = double.MaxValue;
            foreach (BiomeCell C in BiomeCells)
            {
                var _distance = Distance(location, C.CellPoint);
                if (_distance < distance)
                {
                    distance = _distance;
                    cell = C;
                }
            }
            return cell;
        }

        /*
         * The distance to the closest biome cell point to the specified location(uses the Chebyshev distance function).
         */
        public double ClosestCellPoint(Int2 location)
        {
            var distance = double.MaxValue;
            foreach (BiomeCell C in BiomeCells)
            {
                var _distance = Distance(location, C.CellPoint);
                if (_distance < distance)
                {
                    distance = _distance;
                }
            }
            return distance;
        }

        public double Distance(Int2 a, Int2 b)
        {
            Int2 diff = a - b;
            return Math.Max(Math.Abs(diff.X), Math.Abs(diff.Y));
        }
    }
}
