using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlaxEngine;

namespace FlaxVoxel
{
    public class GenerateChunk : IWorkerQueueEntry
    {
        private readonly VoxelChunk _chunk;
        private readonly Int2 _pos;
        public GenerateChunk(VoxelChunk chunk)
        {
            _chunk = chunk;
        }

        public GenerateChunk(Int2 pos)
        {
            _pos = pos;
        }

        public void PerformAction(VoxelWorld world)
        {
            if (_chunk != null)
                world.Generator.GenerateChunk(_chunk, world);
            else
                world.Generator.GenerateChunk(_pos, world);
        }
    }
}
