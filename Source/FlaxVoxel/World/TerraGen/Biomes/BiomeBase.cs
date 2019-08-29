using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlaxEngine;

namespace FlaxVoxel.TerraGen
{
    public abstract class BiomeBase
    {
        /// <summary>
        /// The ID of the biome.
        /// </summary>
        public abstract byte ID { get; }

        public virtual int Elevation => 0;

        /// <summary>
        /// The base temperature of the biome.
        /// </summary>
        public abstract double Temperature { get; }

        /// <summary>
        /// The base rainfall of the biome.
        /// </summary>
        public abstract double Rainfall { get; }

        /// <summary>
        /// The main surface block used for the terrain of the biome.
        /// </summary>
        public virtual Block SurfaceBlock => new Block { Color = new Color32(0,255,0,255), Id = 1, IsTransparent = false};

        /// <summary>
        /// The main "filler" block found under the surface block in the terrain of the biome.
        /// </summary>
        public virtual Block FillerBlock => new Block { Color = new Color32(165, 42, 42, 255), Id = 2, IsTransparent = false };

        /// <summary>
        /// The depth of the surface block layer
        /// </summary>
        public virtual int SurfaceDepth => 1;

        /// <summary>
        /// The depth of the "filler" blocks  located below the surface block layer
        /// </summary>
        public virtual int FillerDepth => 4;
    }
}
