using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlaxEngine;
using FlaxVoxel.TerraGenesis;

namespace FlaxVoxel
{
    // TODO: Implement actor pooling instead of New<> and Destroy<> ?
    public partial class VoxelWorld : Script
    {
        public class Configuration
        {
            public const int ChunkSegmentSize = 16;
            public const int WorldScale = 10;
        }

        public readonly ConcurrentDictionary<Int2, VoxelChunk> Chunks = new ConcurrentDictionary<Int2, VoxelChunk>();
        public readonly TerrainGenerator Generator = new TerrainGenerator(1);
        public MaterialBase OpaqueMaterial;
        public MaterialBase TransparentMaterial;

        public override void OnAwake()
        {
            InitializeQueues();
        }

        public override void OnStart()
        {
            Actor.Scale = new Vector3(Configuration.WorldScale);
            StartQueues();
        }

        public override void OnDestroy()
        {
            StopQueues();
        }

        public override void OnUpdate()
        {
            // Queue processing in sync (debug):
            /*while (UpdateQueue.TryDequeue(out var entry))
                entry.PerformAction(this);

            while (GeneratorQueue.TryDequeue(out var entry))
                entry.PerformAction(this);*/
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

            var blockOffsetX = x >= 0 ? 0 : Configuration.ChunkSegmentSize - 1;
            var blockOffsetZ = z >= 0 ? 0 : Configuration.ChunkSegmentSize - 1;

            return Chunks.TryGetValue(new Int2((x - offsetX) / Configuration.ChunkSegmentSize, (z - offsetZ) / Configuration.ChunkSegmentSize),
                out var chunk) ? chunk.GetBlock(blockOffsetX + x % Configuration.ChunkSegmentSize, y, blockOffsetZ + z % Configuration.ChunkSegmentSize) : null;
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

            var blockOffsetX = x >= 0 ? 0 : Configuration.ChunkSegmentSize - 1;
            var blockOffsetZ = z >= 0 ? 0 : Configuration.ChunkSegmentSize - 1;

            if (Chunks.TryGetValue(new Int2((x - offsetX) / Configuration.ChunkSegmentSize, (z - offsetZ) / Configuration.ChunkSegmentSize), out var chunk))
                chunk.SetBlock(blockOffsetX + x % Configuration.ChunkSegmentSize, y, blockOffsetZ + z % Configuration.ChunkSegmentSize, block, chunkUpdate, updateNeighbors);
        }
        public VoxelChunk SpawnChunk(Int2 chunkPos)
        {
            var chunkActor = Actor.AddChild<EmptyActor>();

            var chunk = chunkActor.AddScript<VoxelChunk>();
            chunk.WorldPosition = chunkPos;
            chunkActor.LocalPosition = new Vector3(chunkPos.X * VoxelWorld.Configuration.ChunkSegmentSize, 0, chunkPos.Y * VoxelWorld.Configuration.ChunkSegmentSize);
            chunkActor.Name = $"Chunk[{chunk.WorldPosition.X},{chunk.WorldPosition.Y}]";
            chunk.World = this;
            Chunks.TryAdd(chunk.WorldPosition, chunk);
            return chunk;
        }
    }
}
