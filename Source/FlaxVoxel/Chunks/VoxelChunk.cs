using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlaxEngine;

namespace FlaxVoxel
{
    public partial class VoxelChunk : Script
    {
        //public readonly List<ChunkSegment> _segments = new List<ChunkSegment>();
        private ChunkSegment[] _segments = new ChunkSegment[0];
        public VoxelWorld World;

        // TODO: Readonly
        public Int2 WorldPosition;

        public override void OnUpdate()
        {
            foreach (var segment in _segments)
                segment?.OnUpdate();

            // Clean empty chunks
            for (var i = 0; i < _segments.Length; i++)
            {
                var segment = _segments[i];
                if (segment == null || !segment.IsEmpty) continue;
                segment.OnDestroy();
                _segments[i] = null;
            }
        }

        /// <summary>
        /// Gets chunk segment for specified Y coordinate or null if above or below
        /// </summary>
        /// <param name="y">Y coordinate</param>
        /// <returns>Target chunk segment or null</returns>
        public ChunkSegment GetChunkSegment(int y)
        {
            if (y < 0) return null;
            var chunkIndex = y / VoxelWorld.Configuration.ChunkSegmentSize;
            return chunkIndex < _segments.Length ? _segments[chunkIndex] : null;
        }

        /// <summary>
        /// Same as <see cref="GetChunkSegment"/> except it takes chunk segment directly instead of block position
        /// </summary>
        /// <param name="i">Chunk segment index</param>
        /// <returns>Chunk segment or null</returns>
        public ChunkSegment GetChunkSegmentIndex(int i)
        {
            if (i < 0) return null;
            return i < _segments.Length ? _segments[i] : null;
        }

        public void SetBlock(int x, int y, int z, Block block, bool chunkUpdate = true, bool updateNeighbors = true)
        {
            if(x < 0 || y < 0 || z < 0 || x >= VoxelWorld.Configuration.ChunkSegmentSize || z >= VoxelWorld.Configuration.ChunkSegmentSize) return;

            var segmentIndex = y / VoxelWorld.Configuration.ChunkSegmentSize;

            // Resize array
            if (_segments.Length <= segmentIndex) SpawnSegments(segmentIndex+1);

            var rY = y % VoxelWorld.Configuration.ChunkSegmentSize;

            var segment = _segments[segmentIndex] ?? (_segments[segmentIndex] = new ChunkSegment(this, segmentIndex));

            segment.SetBlock(x, rY, z, block, chunkUpdate, updateNeighbors);
        }

        public Block GetBlock(int x, int y, int z)
        {
            if (x < 0 || y < 0 || z < 0 || x >= VoxelWorld.Configuration.ChunkSegmentSize || z >= VoxelWorld.Configuration.ChunkSegmentSize || y >= VoxelWorld.Configuration.ChunkSegmentSize * _segments.Length) return null;

            var chunkIndex = y / VoxelWorld.Configuration.ChunkSegmentSize;
            var rY = y % VoxelWorld.Configuration.ChunkSegmentSize;

            return _segments[chunkIndex]?.GetBlock(x, rY, z);
        }

        public void UpdateChunk()
        {
            foreach (var segment in _segments)
                segment?.UpdateSegment();
        }

        private void SpawnSegments(int targetChunkCount)
        {
            // TODO: Probably resize in buckets instead of direct fit
            // var segmStart = _segments.Length;
            // Resize if needed
            if(targetChunkCount > _segments.Length)
                Array.Resize(ref _segments, targetChunkCount);

            // NOTE: Spawning code moved to SetBlock to allow for sparse spawning
            /*for (var i = segmStart; i < targetChunkCount; i++)
                _segments[i] = new ChunkSegment(this, i);*/
        }
    }
}
