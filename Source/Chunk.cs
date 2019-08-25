using System;
using System.Collections.Generic;
using FlaxEngine;

namespace VoxelTerrain.Source
{
	public class Chunk : Script
    {
        public const int BLOCK_SIZE_CM = 10;
        public const int MAX_HEIGHT = 64;
        public const int SEGMENT_SIZE = 16;
        private const int SEGMENTS_PER_CHUNK = MAX_HEIGHT / SEGMENT_SIZE;


        public bool IsLoading = false;
        public bool IsLoaded = false;
        public bool IsUnloading = false;
        public bool IsQueuedForLoad = false;
        public bool IsQueuedForUnload = false;

        public Int2 ChunkPosition;
        public BoundingBox Bounds;

        public ChunkManager Manager;

        public void Initialize(ChunkManager manager)
        {
            Manager = manager;
            var xMin = Actor.LocalPosition.X * Chunk.BLOCK_SIZE_CM;
            var xMax = xMin + Chunk.SEGMENT_SIZE * Chunk.BLOCK_SIZE_CM;
            var yMin = 0;
            var yMax = Chunk.MAX_HEIGHT * Chunk.BLOCK_SIZE_CM;
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
        public ChunkSegment[] Segments = new ChunkSegment[SEGMENTS_PER_CHUNK];

        public override void OnAwake()
        {
            for (var i = 0; i < SEGMENTS_PER_CHUNK; i++)
            {
                Segments[i] = new ChunkSegment(Actor, i * SEGMENT_SIZE, this);
                //Segments[i].Material = Material;
            }
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
            for (int i = 0; i < SEGMENTS_PER_CHUNK; i++)
            {
                Segments[i].GenerateSegment();
            }
        }

        private void SetColumn(int x, int z, int v)
        {
            if(v >= MAX_HEIGHT || v < 0) return;

            for (var y = 0; y < v; y++)
            {
                var segm = Segments[y / SEGMENT_SIZE];
                segm.Data[x, y % SEGMENT_SIZE, z] = new Block(){ID = 1, Transparent = false};
            }
        }

        public override void OnFixedUpdate()
        {
            for (int i = 0; i < SEGMENTS_PER_CHUNK; i++)
            {
                Segments[i].Update();
            }
        }

        public override void OnDestroy()
        {
            for (int i = 0; i < SEGMENTS_PER_CHUNK; i++)
            {
                Segments[i].Destroy();
            }
        }
    }
}
