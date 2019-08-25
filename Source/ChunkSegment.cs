using System;
using System.Collections.Generic;
using FlaxEngine;

namespace VoxelTerrain.Source
{
	public class ChunkSegment
	{
        [HideInEditor] public Block[,,] Data = new Block[Chunk.SEGMENT_SIZE, Chunk.SEGMENT_SIZE, Chunk.SEGMENT_SIZE];

        private Model _tempModel;

        //public MaterialBase Material;

        private bool _updateMesh = false;

        private List<Vector3> _vertices = new List<Vector3>();
        private List<Vector3> _normals = new List<Vector3>();
        private List<int> _indices = new List<int>();

        private readonly int yOffset = 0;

        public Chunk Parent;

        public ChunkSegment(Actor actor, int yo, Chunk parent)
        {
            Parent = parent;
            yOffset = yo;
            var model = _tempModel ?? Content.CreateVirtualAsset<Model>();
            _tempModel = model;
            model.SetupLODs(1);

            // Create or reuse child model actor
            var childModel = actor.AddChild<StaticModel>();
            childModel.Model = model;
           // childModel.Entries[0].Material = Material;

           // Debug.Log("Hi");
        }

        public void Update()
        {
            if (_tempModel && _updateMesh)
            {
                _updateMesh = false;
                if(_indices.Count > 0)
                    _tempModel.LODs[0].Meshes[0].UpdateMesh(_vertices.ToArray(), _indices.ToArray(), _normals.ToArray());
            }
        }

        public void Destroy()
        {
            FlaxEngine.Object.Destroy(ref _tempModel);
        }

        public void GenerateSegment()
        {
            _updateMesh = false;
            _vertices.Clear();
            _indices.Clear();
            _normals.Clear();
            
            Greedy();

            _updateMesh = true;
        }

        Block GetVoxelFace(int x, int y, int z, int side)
        {
            if (x >= 0 && y >= 0 && z >= 0 && x < Chunk.SEGMENT_SIZE && y < Chunk.SEGMENT_SIZE &&
                z < Chunk.SEGMENT_SIZE)
                return Data[x, y, z];

            // Go to lower segment
            if (y < 0)
                return yOffset == 0 ? null : Parent.Segments[(yOffset + y) / Chunk.SEGMENT_SIZE].Data[x, Chunk.SEGMENT_SIZE + y, z];

            // Go to upper segment
            if (y >= Chunk.SEGMENT_SIZE)
                return (yOffset + y) / Chunk.SEGMENT_SIZE >= Parent.Segments.Length
                    ? null
                    : Parent.Segments[(yOffset + y) / Chunk.SEGMENT_SIZE].Data[x, y - Chunk.SEGMENT_SIZE, z];

            if (x < 0 && Parent.Manager.Chunks.TryGetValue(Parent.ChunkPosition - Int2.UnitX, out var neighbor))
                return neighbor.Segments[(yOffset + y) / Chunk.SEGMENT_SIZE].Data[Chunk.SEGMENT_SIZE + x, y, z];

            if (x >= Chunk.SEGMENT_SIZE &&
                Parent.Manager.Chunks.TryGetValue(Parent.ChunkPosition + Int2.UnitX, out neighbor))
                return neighbor.Segments[(yOffset + y) / Chunk.SEGMENT_SIZE].Data[x - Chunk.SEGMENT_SIZE, y, z];

            if (z < 0 && Parent.Manager.Chunks.TryGetValue(Parent.ChunkPosition - Int2.UnitY, out neighbor))
                return neighbor.Segments[(yOffset + y) / Chunk.SEGMENT_SIZE].Data[x, y, Chunk.SEGMENT_SIZE + z];

            if (z >= Chunk.SEGMENT_SIZE &&
                Parent.Manager.Chunks.TryGetValue(Parent.ChunkPosition + Int2.UnitY, out neighbor))
                return neighbor.Segments[(yOffset + y) / Chunk.SEGMENT_SIZE].Data[x, y, z - Chunk.SEGMENT_SIZE];

            return null;
        }
        private static int SOUTH = 0;
        private static int NORTH = 1;
        private static int EAST = 2;
        private static int WEST = 3;
        private static int TOP = 4;
        private static int BOTTOM = 5;

        private void Quad(Vector3 bottomLeft,
            Vector3 topLeft,
            Vector3 topRight,
            Vector3 bottomRight,
            int width,
            int height,
            Block voxel,
            bool backFace)
        {
            bottomLeft.Y += yOffset;
            bottomRight.Y += yOffset;
            topLeft.Y += yOffset;
            topRight.Y += yOffset;

            int offset = (int)_vertices.Count;

            _vertices.AddRange(new []{topLeft, topRight, bottomLeft, bottomRight});
            _indices.AddRange(backFace ? new int[] { 2 + offset, 3 + offset, 1 + offset, 1 + offset, 0 + offset, 2 + offset } : new int[] { 2 + offset, 0 + offset, 1 + offset, 1 + offset, 3 + offset, 2 + offset });
            var normal = Vector3.Normalize(Vector3.Cross(topRight - topLeft, bottomRight - topLeft));
            if (backFace)
                normal *= -1;

            _normals.AddRange(new []{normal, normal, normal, normal});
        }

        private void Greedy()
        {


            int i, j, k, l, w, h, u, v, n, side = 0;

            int[] x = new int[] { 0, 0, 0 };
            int[] q = new int[] { 0, 0, 0 };
            int[] du = new int[] { 0, 0, 0 };
            int[] dv = new int[] { 0, 0, 0 };

            Block[] mask = new Block[Chunk.SEGMENT_SIZE * Chunk.SEGMENT_SIZE];

            Block block, block2;

            for (bool backFace = true, b = false; b != backFace; backFace = backFace && b, b = !b)
            {
                for (int d = 0; d < 3; d++)
                {

                    u = (d + 1) % 3;
                    v = (d + 2) % 3;

                    x[0] = 0;
                    x[1] = 0;
                    x[2] = 0;

                    q[0] = 0;
                    q[1] = 0;
                    q[2] = 0;
                    q[d] = 1;

                    if (d == 0) { side = backFace ? WEST : EAST; }
                    else if (d == 1) { side = backFace ? BOTTOM : TOP; }
                    else if (d == 2) { side = backFace ? SOUTH : NORTH; }

                    for (x[d] = -1; x[d] < Chunk.SEGMENT_SIZE;)
                    {
                        n = 0;

                        for (x[v] = 0; x[v] < Chunk.SEGMENT_SIZE; x[v]++)
                        {

                            for (x[u] = 0; x[u] < Chunk.SEGMENT_SIZE; x[u]++)
                            {

                                block = /*(x[d] >= 0) ?*/ GetVoxelFace(x[0], x[1], x[2], side)/* : null*/;
                                block2 = /*(x[d] < Chunk.SEGMENT_SIZE - 1) ? */GetVoxelFace(x[0] + q[0], x[1] + q[1], x[2] + q[2], side)/* : null*/;


                                mask[n++] = ((block != null && block2 != null && block.Equals(block2)))
                                            ? null
                                            : backFace ? block2 : block;
                            }
                        }

                        x[d]++;
                        n = 0;

                        for (j = 0; j < Chunk.SEGMENT_SIZE; j++)
                        {

                            for (i = 0; i < Chunk.SEGMENT_SIZE;)
                            {

                                if (mask[n] != null)
                                {

                                    for (w = 1; i + w < Chunk.SEGMENT_SIZE && mask[n + w] != null && mask[n + w].Equals(mask[n]); w++) { }

                                    bool done = false;

                                    for (h = 1; j + h < Chunk.SEGMENT_SIZE; h++)
                                    {

                                        for (k = 0; k < w; k++)
                                        {

                                            if (mask[n + k + h * Chunk.SEGMENT_SIZE] == null || !mask[n + k + h * Chunk.SEGMENT_SIZE].Equals(mask[n])) { done = true; break; }
                                        }

                                        if (done) { break; }
                                    }

                                    if (!mask[n].Transparent)
                                    {
                                        x[u] = i;
                                        x[v] = j;

                                        du[0] = 0;
                                        du[1] = 0;
                                        du[2] = 0;
                                        du[u] = w;

                                        dv[0] = 0;
                                        dv[1] = 0;
                                        dv[2] = 0;
                                        dv[v] = h;

                                        Quad(new Vector3(x[0], x[1], x[2]),
                                             new Vector3(x[0] + du[0], x[1] + du[1], x[2] + du[2]),
                                             new Vector3(x[0] + du[0] + dv[0], x[1] + du[1] + dv[1], x[2] + du[2] + dv[2]),
                                             new Vector3(x[0] + dv[0], x[1] + dv[1], x[2] + dv[2]),
                                             w,
                                             h,
                                             mask[n],
                                             backFace);
                                    }

                                    for (l = 0; l < h; ++l)
                                    {

                                        for (k = 0; k < w; ++k) { mask[n + k + l * Chunk.SEGMENT_SIZE] = null; }
                                    }

                                    i += w;
                                    n += w;

                                }
                                else
                                {

                                    i++;
                                    n++;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
