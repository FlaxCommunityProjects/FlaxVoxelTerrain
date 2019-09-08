using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlaxEngine;

namespace FlaxVoxel
{ 
    public class ReMeshChunk : UpdateEntry
    {
        private readonly VoxelChunk _chunk;
        private readonly Int2 _chunkPos;

        public ReMeshChunk(VoxelChunk chunk) => _chunk = chunk;
        public ReMeshChunk(Int2 chunkPos) => _chunkPos = chunkPos;

        public override void PerformUpdate(VoxelWorld world)
        {
            // TODO: Check chunk internal state (do not update chunk which is unloaded/deprecated
            if (_chunk != null)
            {
                _chunk.UpdateChunk();
                return;
            }

            if (world.Chunks.TryGetValue(_chunkPos, out var chunk))
                chunk.UpdateChunk();
        }
    }
}
