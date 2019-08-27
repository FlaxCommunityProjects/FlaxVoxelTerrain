using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FlaxEngine;

namespace VoxelTerrain.Source
{
	public class Chunk : Script
    {
        public const int BLOCK_SIZE_CM = 10;
        public const int SEGMENT_SIZE = 16;


        public bool IsLoading = false;
        public bool IsLoaded = false;
        public bool IsUnloading = false;
        public bool IsQueuedForLoad = false;
        public bool IsQueuedForUnload = false;

        public Int2 ChunkPosition;
        public BoundingBox Bounds;

        public ChunkManager Manager;

        public int SegmentCount = 0;

        public void Initialize(ChunkManager manager)
        {
            Manager = manager;
            var xMin = Actor.LocalPosition.X * Chunk.BLOCK_SIZE_CM;
            var xMax = xMin + Chunk.SEGMENT_SIZE * Chunk.BLOCK_SIZE_CM;
            var yMin = 0;
            var yMax = Segments.Count * Chunk.BLOCK_SIZE_CM;
            var zMin = Actor.LocalPosition.Z * Chunk.BLOCK_SIZE_CM;
            var zMax = zMin + Chunk.SEGMENT_SIZE * Chunk.BLOCK_SIZE_CM;
            Bounds = new BoundingBox(new Vector3(xMin, yMin, zMin), new Vector3(xMax, yMax, zMax));
        }

        public override void OnDebugDraw()
        {
            var color = Color.Black;
            if (IsLoading) color = Color.Yellow;
            if (IsLoaded) color = Color.Green;
            if (IsQueuedForUnload) color = Color.LightSkyBlue;
            if (IsUnloading) color = Color.Blue;

            DebugDraw.DrawWireBox(Bounds, color);
        }

        //public MaterialBase Material;
        public List<ChunkSegment> Segments = new List<ChunkSegment>();

        public override void OnAwake()
        {
            /*for (var i = 0; i < SEGMENTS_PER_CHUNK; i++)
            {
                Segments[i] = new ChunkSegment(Actor, i * SEGMENT_SIZE, this);
                //Segments[i].Material = Material;
            }*/
        }

        public void GenerateChunk()
        {
            for (int x = 0; x < SEGMENT_SIZE; x++)
            for (int z = 0; z < SEGMENT_SIZE; z++)
            {
                SetColumn(x, z, TerrainGenerator.GetHeight(x + ChunkPosition.X * SEGMENT_SIZE, z + ChunkPosition.Y * SEGMENT_SIZE));
            }

            UpdateChunk();
        }

        public void UpdateChunk()
        {
            foreach (var t in Segments)
                t.GenerateSegment();
        }

        private void SetColumn(int x, int z, int v)
        {
            if(v < 0) return;

            SpawnSegment(v / SEGMENT_SIZE);

            for (var y = 0; y < v; y++)
            {
                var segm = Segments[y / SEGMENT_SIZE];
                segm.Data[x, y % SEGMENT_SIZE, z] = new Block(){ID = 1, Transparent = false};
            }
        }

        public override void OnFixedUpdate()
        {
            foreach (var t in Segments)
                t.Update();
        }

        public override void OnDestroy()
        {
            foreach (var t in Segments)
                t.Destroy();

            Segments.Clear();
        }

        public Block GetBlockRelative(int x, int y, int z)
        {
            if (y < 0 || y >= SegmentCount * SEGMENT_SIZE) return null;
            if (x < 0 || x >= SEGMENT_SIZE) return null;
            if (z < 0 || z >= SEGMENT_SIZE) return null;


            return Segments[y / SEGMENT_SIZE].Data[x, y % SEGMENT_SIZE, z];
        }

        private void SpawnSegment(int targetSegment)
        {
            while (SegmentCount < targetSegment + 1)
            {
                Segments.Add(new ChunkSegment(Actor, targetSegment * SEGMENT_SIZE, this));
                SegmentCount++;
            }
        }

        public void SetBlockRelative(int x, int y, int z, Block block)
        {
            if (y < 0) return;
            if (x < 0 || x >= SEGMENT_SIZE) return;
            if (z < 0 || z >= SEGMENT_SIZE) return;
            Segments[y / SEGMENT_SIZE].Data[x, y % SEGMENT_SIZE, z] = block;

            if (y >= SegmentCount * SEGMENT_SIZE) SpawnSegment(y / SEGMENT_SIZE);

            // Re-mesh self
            Manager.UpdateChunk(this);

            // TODO: UpdateSegment only neighbor segments instead of whole chunks (up to 4x performance boost at worse 2x)

            // Re-mesh affected neighbor chunks
            if (x == 0)
                Manager.UpdateChunk(ChunkPosition - Int2.UnitX);
            else if (x == Chunk.SEGMENT_SIZE - 1)
                Manager.UpdateChunk(ChunkPosition + Int2.UnitX);

            if (z == 0)
                Manager.UpdateChunk(ChunkPosition - Int2.UnitY);
            else if (z == Chunk.SEGMENT_SIZE - 1)
                Manager.UpdateChunk(ChunkPosition + Int2.UnitY);
        }
    }
}
