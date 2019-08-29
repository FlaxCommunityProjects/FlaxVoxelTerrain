using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlaxEngine;

namespace FlaxVoxel.TerraGen
{
    public class BiomeCell
    {
        public byte BiomeID;
        public Int2 CellPoint;

        public BiomeCell(byte biomeID, Int2 cellPoint)
        {
            this.BiomeID = biomeID;
            this.CellPoint = cellPoint;
        }
    }
}
