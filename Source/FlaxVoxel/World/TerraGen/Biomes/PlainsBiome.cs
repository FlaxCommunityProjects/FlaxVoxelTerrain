using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlaxVoxel.TerraGen
{
    public class PlainsBiome : BiomeBase
    {
        public override byte ID => (byte)Biomes.Plains;

        public override double Temperature => 0.8f;

        public override double Rainfall => 0.4f;
    }
}
