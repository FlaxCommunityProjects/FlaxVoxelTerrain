using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlaxEngine;

namespace FlaxVoxel.TerraGen
{
    public class DesertBiome : BiomeBase
    {
        public override byte ID => (byte)Biomes.Desert;
        public override double Temperature => 2.0f;
        public override double Rainfall => 0.0f;

        public override Block SurfaceBlock => new Block
            {Color = new Color32(238, 234, 221, 255), IsTransparent = false, Id = 3};

        public override Block FillerBlock => new Block
            { Color = new Color32(194, 178, 128, 255), IsTransparent = false, Id = 3 };

        public override int SurfaceDepth => 4;
    }
}
