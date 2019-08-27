using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoxelTerrain.Source
{
    public class Block
    {
        public int ID;
        public bool Transparent;
        public bool Equals(Block face) { return face.Transparent == Transparent && face.ID == ID; }
    }
}
