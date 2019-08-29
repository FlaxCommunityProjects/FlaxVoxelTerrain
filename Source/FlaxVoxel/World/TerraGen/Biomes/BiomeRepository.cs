using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlaxVoxel.TerraGen
{
    public class BiomeRepository
    {
        private readonly BiomeBase[] Biomes = new BiomeBase[]
        {
            new PlainsBiome(),
            new DesertBiome()
        };

        public BiomeBase GetBiome(byte biome)
        {
            return Biomes[biome];
        }

        public BiomeBase GetBiome(double temperature, double rainfall)
        {
            List<BiomeBase> temperatureResults = Biomes.Where(biome => biome != null && biome.Temperature.Equals(temperature)).ToList();

            if (temperatureResults.Count.Equals(0))
            {
                BiomeBase provider = null;
                float temperatureDifference = 100.0f;
                foreach (var biome in Biomes)
                {
                    if (biome == null) continue;
                    var Difference = Math.Abs(temperature - biome.Temperature);
                    if (provider != null && !(Difference < temperatureDifference)) continue;
                    provider = biome;
                    temperatureDifference = (float)Difference;
                }
                temperatureResults.Add(provider);
            }

            foreach (var biome in Biomes)
            {
                if (biome != null
                    && biome.Rainfall.Equals(rainfall)
                    && temperatureResults.Contains(biome))
                {
                    return biome;
                }
            }

            BiomeBase biomeProvider = null;
            float rainfallDifference = 100.0f;
            foreach (var biome in Biomes)
            {
                if (biome == null) continue;
                var difference = Math.Abs(temperature - biome.Temperature);
                if ((biomeProvider != null && !(difference < rainfallDifference))) continue;
                biomeProvider = biome;
                rainfallDifference = (float)difference;
            }
            return biomeProvider ?? new PlainsBiome();
        }
    }
}
