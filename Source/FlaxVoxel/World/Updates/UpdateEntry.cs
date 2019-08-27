using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlaxEngine;
using FlaxVoxel;

namespace FlaxVoxel
{
    public sealed class UpdateEntry
    {
        public Int2? ChunkPosition = null;
        public VoxelChunk Chunk = null;
        public ChunkSegment Segment = null;
        public int SegmentIndex = -1;

        public bool IsSegmentUpdate = false;
        public bool IsChunkUpdate = false;

        private UpdateEntry() { }

        public static UpdateEntry UpdateChunk(Int2 pos) => new UpdateEntry {ChunkPosition = pos, IsChunkUpdate = true};
        public static  UpdateEntry UpdateChunk(VoxelChunk chunk) => new UpdateEntry {Chunk = chunk, IsChunkUpdate = true};
        public static UpdateEntry UpdateSegment(ChunkSegment segment) => new UpdateEntry {Segment = segment, IsSegmentUpdate = true };
        public  static  UpdateEntry UpdateSegment(Int2 chunkPos, int segmentIndex) => new UpdateEntry{ChunkPosition = chunkPos, SegmentIndex = segmentIndex, IsSegmentUpdate =  true};
    }
}
