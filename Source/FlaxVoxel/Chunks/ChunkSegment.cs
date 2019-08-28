using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlaxEngine;

namespace FlaxVoxel
{
    public partial class ChunkSegment
    {
        public readonly VoxelChunk ParentChunk;
        private readonly Block[,,] Blocks = new Block[VoxelWorld.Configuration.ChunkSegmentSize, VoxelWorld.Configuration.ChunkSegmentSize, VoxelWorld.Configuration.ChunkSegmentSize];
        private readonly int _segmentIndex;

        private bool _isEmpty = true;
        private int _blockCount = 0;

        // TODO: Convert to bounding box
        private int _minX = int.MinValue;
        private int _minY = int.MaxValue;
        private int _minZ = int.MaxValue;
        private int _maxX = int.MinValue;
        private int _maxY = int.MinValue;
        private int _maxZ = int.MinValue;

        public bool IsEmpty => _isEmpty;

        public ChunkSegment(VoxelChunk parent, int segmentSegmentIndex)
        {
            ParentChunk = parent;
            _segmentIndex = segmentSegmentIndex;

            InitializeMesh();
        }

        public void UpdateSegment()
        {
            BuildMesh();
        }

        /// <summary>
        /// Sets the block at specified X,Y,Z
        /// </summary>
        /// <param name="x">X</param>
        /// <param name="y">Y</param>
        /// <param name="z">Z</param>
        /// <param name="block">The block</param>
        /// <param name="chunkUpdate">If true, will queue segment for update else not</param>
        /// <param name="updateNeighbors">If true will queue neighbors for update if needed else not</param>
        public void SetBlock(int x, int y, int z, Block block, bool chunkUpdate = true, bool updateNeighbors = true)
        {
            // TODO: coordinate validation checks if direct access is enabled

            // UpdateSegment block count
            if (block != null && Blocks[x, y, z] == null) _blockCount++;
            if (block == null && Blocks[x, y, z] != null) _blockCount--;

            _isEmpty = _blockCount == 0;

            Blocks[x, y, z] = block;

            if (_isEmpty) {
                _minX = _minY = _minZ = int.MaxValue;
                _maxX = _maxY = _maxZ = int.MinValue;
            }

            _minX = Math.Min(_minX, x);
            _minY = Math.Min(_minY, y);
            _minZ = Math.Min(_minZ, z);

            _maxX = Math.Max(_maxX, x);
            _maxY = Math.Max(_maxY, y);
            _maxZ = Math.Max(_maxZ, z);

            // Update self mesh
            if(chunkUpdate)
                ParentChunk.World.UpdateQueue.Add(UpdateEntry.UpdateSegment(this));

            if(!updateNeighbors) return;

            // Handle X neighbors
            if (x == 0) ParentChunk.World.UpdateQueue.Add(UpdateEntry.UpdateSegment(ParentChunk.WorldPosition - Int2.UnitX, _segmentIndex));
            if (x == VoxelWorld.Configuration.ChunkSegmentSize - 1) ParentChunk.World.UpdateQueue.Add(UpdateEntry.UpdateSegment(ParentChunk.WorldPosition + Int2.UnitX, _segmentIndex));

            // Handle Y neighbors
            if (y == 0) ParentChunk.World.UpdateQueue.Add(UpdateEntry.UpdateSegment(ParentChunk.WorldPosition, _segmentIndex - 1));
            if (y == VoxelWorld.Configuration.ChunkSegmentSize - 1) ParentChunk.World.UpdateQueue.Add(UpdateEntry.UpdateSegment(ParentChunk.WorldPosition, _segmentIndex + 1));

            // Handle Z neighbors
            if (z == 0) ParentChunk.World.UpdateQueue.Add(UpdateEntry.UpdateSegment(ParentChunk.WorldPosition - Int2.UnitY, _segmentIndex));
            if (z == VoxelWorld.Configuration.ChunkSegmentSize - 1) ParentChunk.World.UpdateQueue.Add(UpdateEntry.UpdateSegment(ParentChunk.WorldPosition + Int2.UnitY, _segmentIndex));
        }

        public Block GetBlock(int x, int y, int z)
        {
            // if (x < _minX || y < _minY || z < _minZ || x > _maxX || y > _maxY || z > _maxZ) return null;
            return Blocks[x, y, z];
        }
    }
}
