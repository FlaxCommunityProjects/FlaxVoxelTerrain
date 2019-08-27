using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlaxEngine;

namespace FlaxVoxel
{
    // TODO: Implement voxel based lighting and thus block faces
    // TODO: Implement concept of block database (templates & instances)
    // TODO: Implement custom block models per template?
    public class Block: IEquatable<Block>
    {
        public int Id;
        public Color32 Color;
        public bool IsTransparent;

        public bool Equals(Block other)
        {
            /*if (ReferenceEquals(this, other)) return true;
            if (other is null) return false;*/
            return Id == other.Id && Color.Equals(other.Color) && IsTransparent == other.IsTransparent;
        }
    }
}
