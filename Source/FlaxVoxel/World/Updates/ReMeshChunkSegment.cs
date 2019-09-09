using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlaxEngine;

namespace FlaxVoxel
{
    // TODO: Use custom XZ struct instead of XY so axes are correctly typed
    public class ReMeshChunkSegment : IWorkerQueueEntry
    {
        private readonly Int3 _segmentPos;
        private readonly ChunkSegment _segment;

        public ReMeshChunkSegment(ChunkSegment segment) => _segment = segment;
        public ReMeshChunkSegment(Int3 pos) => _segmentPos = pos;
        public ReMeshChunkSegment(Int2 pos, int y) => _segmentPos = new Int3(pos, y);
        public ReMeshChunkSegment(int x, int z, int y) => _segmentPos = new Int3(x,z, y);

        public void PerformAction(VoxelWorld world)
        {
            if (_segment != null)
            {
                _segment.UpdateSegment();
                return;
            }

            if(_segmentPos.Z < 0) return;

            if (world.Chunks.TryGetValue(new Int2(_segmentPos.X, _segmentPos.Y), out var chunk))
                chunk.GetChunkSegment(_segmentPos.Z)?.UpdateSegment();
        }
    }
}
