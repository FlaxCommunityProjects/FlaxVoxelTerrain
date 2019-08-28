using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlaxEngine;

namespace FlaxVoxel
{
    // TODO: Implement actor pooling instead of New<> and Destroy<> ?
    public class VoxelWorld : Script
    {
        public class Configuration
        {
            public const int ChunkSegmentSize = 16;
            public const int WorldScale = 10;
        }

        public readonly ConcurrentDictionary<Int2, VoxelChunk> Chunks = new ConcurrentDictionary<Int2, VoxelChunk>();

        public MaterialBase Material;
        // TODO: Create concurrent & sorted by priority
        public readonly List<UpdateEntry> UpdateQueue = new List<UpdateEntry>();
        public override void OnStart()
        {
            Actor.Scale = new Vector3(Configuration.WorldScale);

            // TODO: TEMP, USE PROPER SPAWNING RUTINE
            var chunkActor = Actor.AddChild<EmptyActor>();
            var chunk = chunkActor.AddScript<VoxelChunk>();
            chunk.WorldPosition = new Int2(-1, 0);
            chunkActor.LocalPosition = new Vector3(-16, 0,0);
            chunkActor.Name = $"Chunk[{chunk.WorldPosition.X},{chunk.WorldPosition.Y}]";
            chunk.World = this;
            Chunks.TryAdd(chunk.WorldPosition, chunk);

            chunkActor = Actor.AddChild<EmptyActor>();
            chunk = chunkActor.AddScript<VoxelChunk>();
            chunk.WorldPosition = new Int2(0, 0);
            chunkActor.LocalPosition = new Vector3(0, 0,0);
            chunkActor.Name = $"Chunk[{chunk.WorldPosition.X},{chunk.WorldPosition.Y}]";
            chunk.World = this;
            Chunks.TryAdd(chunk.WorldPosition, chunk);
        }

        public override void OnUpdate()
        {
            foreach (var entry in UpdateQueue)
            {
                if (entry.IsChunkUpdate)
                {
                    if (entry.ChunkPosition.HasValue && Chunks.TryGetValue(entry.ChunkPosition.Value, out var chunk)) chunk.UpdateChunk();
                    else entry.Chunk?.UpdateChunk();
                }

                if (entry.IsSegmentUpdate)
                {
                    if (entry.SegmentIndex>=0 && entry.ChunkPosition.HasValue && Chunks.TryGetValue(entry.ChunkPosition.Value, out var chunk)) chunk.GetChunkSegment(entry.SegmentIndex)?.UpdateSegment();
                    else entry.Segment?.UpdateSegment();
                }
            }
            UpdateQueue.Clear();
        }

        /// <summary>
        /// Gets block at world coordinates [x,y,z] if target chunk is loaded
        /// </summary>
        /// <param name="x">X</param>
        /// <param name="y">Y</param>
        /// <param name="z">Z</param>
        /// <returns>Block instance or null if chunk is not loaded or block is air</returns>
        public Block GetBlock(int x, int y, int z)
        {
            var offsetX = x >= 0 ? 0 : Configuration.ChunkSegmentSize;
            var offsetZ = z >= 0 ? 0 : Configuration.ChunkSegmentSize;
            return Chunks.TryGetValue(new Int2((x - offsetX) / Configuration.ChunkSegmentSize, (z - offsetZ) / Configuration.ChunkSegmentSize),
                out var chunk) ? chunk.GetBlock(offsetX + x % Configuration.ChunkSegmentSize, y, offsetZ + z % Configuration.ChunkSegmentSize) : null;
        }

        /// <summary>
        /// Sets block at world coordinates [x,y,z] if target chunk is loaded
        /// </summary>
        /// <param name="x">X</param>
        /// <param name="y">Y</param>
        /// <param name="z">Z</param>
        /// <param name="block">Block to set (or null for empty)</param>
        public void SetBlock(int x, int y, int z, Block block, bool chunkUpdate = true, bool updateNeighbors = true)
        {
            var offsetX = x >= 0 ? 0 : Configuration.ChunkSegmentSize;
            var offsetZ = z >= 0 ? 0 : Configuration.ChunkSegmentSize;

            if (Chunks.TryGetValue(new Int2((x - offsetX) / Configuration.ChunkSegmentSize, (z - offsetZ) / Configuration.ChunkSegmentSize), out var chunk))
                chunk.SetBlock(offsetX + x % Configuration.ChunkSegmentSize, y, offsetZ + z % Configuration.ChunkSegmentSize, block, chunkUpdate, updateNeighbors);
        }
    }
}
