using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlaxEngine;

namespace FlaxVoxel
{
    // TODO: Despawn empty segments
    // TODO: Instead of Segments.Count use local cached variable (might be faster)
    public partial class VoxelChunk : Script
    {
        public readonly List<ChunkSegment> Segments = new List<ChunkSegment>();
        public VoxelWorld World;
        // TODO: Readonly
        public Int2 WorldPosition;

        public override void OnUpdate()
        {
            foreach (var segment in Segments)
                segment.OnUpdate();
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
            return chunkIndex < Segments.Count ? Segments[chunkIndex] : null;
        }

        /// <summary>
        /// Same as <see cref="GetChunkSegment"/> except it takes chunk segment directly instead of block position
        /// </summary>
        /// <param name="i">Chunk segment index</param>
        /// <returns>Chunk segment or null</returns>
        public ChunkSegment GetChunkSegmentIndex(int i)
        {
            if (i < 0) return null;
            return i < Segments.Count ? Segments[i] : null;
        }

        public void SetBlock(int x, int y, int z, Block block, bool chunkUpdate = true, bool updateNeighbors = true)
        {
            if(x < 0 || y < 0 || z < 0 || x >= VoxelWorld.Configuration.ChunkSegmentSize || z >= VoxelWorld.Configuration.ChunkSegmentSize) return;

            var chunkIndex = y / VoxelWorld.Configuration.ChunkSegmentSize;
            if (chunkIndex <= Segments.Count) SpawnSegments(chunkIndex+1);

            var rY = y % VoxelWorld.Configuration.ChunkSegmentSize;
            Segments[chunkIndex].SetBlock(x, rY, z, block, chunkUpdate, updateNeighbors);
        }

        public Block GetBlock(int x, int y, int z)
        {
            if (x < 0 || y < 0 || z < 0 || x >= VoxelWorld.Configuration.ChunkSegmentSize || z >= VoxelWorld.Configuration.ChunkSegmentSize || y >= VoxelWorld.Configuration.ChunkSegmentSize * Segments.Count) return null;

            var chunkIndex = y / VoxelWorld.Configuration.ChunkSegmentSize;
            var rY = y % VoxelWorld.Configuration.ChunkSegmentSize;

            return Segments[chunkIndex].GetBlock(x, rY, z);
        }

        public void UpdateChunk()
        {
            foreach (var segment in Segments)
                segment.UpdateSegment();
        }

        private void SpawnSegments(int targetChunkCount)
        {
            while (targetChunkCount > Segments.Count)
                Segments.Add(new ChunkSegment(this, Segments.Count));
        }
    }
}
