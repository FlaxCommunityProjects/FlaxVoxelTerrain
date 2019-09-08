using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlaxEngine;
using Object = FlaxEngine.Object;

namespace FlaxVoxel
{
    // TODO: Implement virtual mesh bounds so meshing iterates only on valid blocks
    // TODO: Implement chunk scaling, which would allow for sub-chunks (for example 1/16th scale = 1 block) and better meshing logic for them
    public partial class ChunkSegment
    {
        // Directions used by the search algorithm to determine face
        private static int SOUTH = 0;
        private static int NORTH = 1;
        private static int EAST = 2;
        private static int WEST = 3;
        private static int TOP = 4;
        private static int BOTTOM = 5;

        private class MeshData
        {
            public MeshEntry[] Entries = new[] {new MeshEntry(), new MeshEntry()};

            public MeshEntry Opaque => Entries[0];
            public MeshEntry Transparent => Entries[1];

            public class MeshEntry
            {
                public readonly List<Vector3> Vertices = new List<Vector3>();
                public readonly List<Vector3> Normals = new List<Vector3>();
                public readonly List<Color32> Colors = new List<Color32>();
                public readonly List<int> Indices = new List<int>();
            }
        }

        private Model _chunkMesh = null;
        private StaticModel _segmentActor = null;
        private MeshData _latestMeshData = null;

        /// <summary>
        /// Initializes new <see cref="StaticModel"/>
        /// </summary>
        private void InitializeMesh()
        {
            _chunkMesh = Content.CreateVirtualAsset<Model>();
            _chunkMesh.SetupLODs(2);

            // Create or reuse child model actor
            _segmentActor = ParentChunk.Actor.AddChild<StaticModel>();
            _segmentActor.Name = "Segment " + _segmentIndex;
            // Move chunk segment to proper y offset
            _segmentActor.LocalPosition = new Vector3(0, _segmentIndex * VoxelWorld.Configuration.ChunkSegmentSize, 0);

            _chunkMesh.SetupMaterialSlots(1);
            /*_chunkMesh.MaterialSlots[0].Material = ParentChunk.World.OpaqueMaterial;
            _chunkMesh.MaterialSlots[1].Material = ParentChunk.World.TransparentMaterial;*/

            _segmentActor.EntriesChanged += m =>
            {
                if (_segmentActor.Entries.Length == 0) return;
                _segmentActor.Entries[0].Material = ParentChunk.World.OpaqueMaterial;

                if (_segmentActor.Entries.Length == 1) return;
                _segmentActor.Entries[1].Material = ParentChunk.World.OpaqueMaterial;
            };

            _segmentActor.Model = _chunkMesh;
            _segmentActor.HideFlags = HideFlags.None;
            _segmentActor.StaticFlags = StaticFlags.FullyStatic;
        }

        private void DestroyMesh()
        {
            if (_segmentActor)
            {
                Object.Destroy(_segmentActor);
                _segmentActor = null;
            }

            if (_chunkMesh)
            {
                Object.Destroy(_chunkMesh);
                _chunkMesh = null;
            }

        }

        /// <summary>
        /// Called every Chunk UpdateSegment, if new mesh data are available we call mesh update
        /// </summary>
        /// <remarks>
        /// Should be done directly in async thread, but it's not working as of Flax 0.5
        /// </remarks>
        public void OnUpdate()
        {
            if (!_chunkMesh || _latestMeshData == null) return;
            var opaqueVisible = _segmentActor.Entries[0].Visible = _latestMeshData.Opaque.Indices.Count > 0;
            if (opaqueVisible)
                _chunkMesh.LODs[0].Meshes[0].UpdateMesh(_latestMeshData.Opaque.Vertices,
                    _latestMeshData.Opaque.Indices, _latestMeshData.Opaque.Normals, null, null,
                    _latestMeshData.Opaque.Colors);

            if (_segmentActor.Entries.Length > 1)
            {

                var transparentVisible =
                    _segmentActor.Entries[1].Visible = _latestMeshData.Transparent.Indices.Count > 0;

                if (transparentVisible)
                    _chunkMesh.LODs[0].Meshes[1].UpdateMesh(_latestMeshData.Transparent.Vertices,
                        _latestMeshData.Transparent.Indices, _latestMeshData.Transparent.Normals, null, null,
                        _latestMeshData.Transparent.Colors);
            }

            _latestMeshData = null;
        }

        public void OnDestroy()
        {
            DestroyMesh();
        }

        /// <summary>
        /// Build new mesh from chunk segment data
        /// </summary>
        public void BuildMesh()
        {
            var meshData = GenerateMesh();
            _latestMeshData = meshData;
        }

        /// <summary>
        /// Returns information about given voxel face which is then used in meshing algorithm
        /// </summary>
        /// <param name="x">Chunk segment relative block X coordinate.</param>
        /// <param name="y">Chunk segment relative block Y coordinate.</param>
        /// <param name="z">Chunk segment relative block Z coordinate.</param>
        /// <param name="side">Face side we want to get, is one of constants in the beginning of this file</param>
        /// <returns>Face data about the block</returns>
        /// <remarks>
        /// Currently returns only block data since face data are not yet implemented
        /// X, Y, Z are in the range of -1 to 16 (chunk +- 1 block from neighbor)
        /// </remarks>
        private Block GetVoxelFace(int x, int y, int z, int side)
        {
            var xValid = x >= 0 && x < VoxelWorld.Configuration.ChunkSegmentSize;
            var yValid = y >= 0 && y < VoxelWorld.Configuration.ChunkSegmentSize;
            var zValid = z >= 0 && z < VoxelWorld.Configuration.ChunkSegmentSize;

            // Current segment
            if (xValid && yValid && zValid)
                return Blocks[x, y, z];

            // Get world absolute Y coordinate
            var absY = VoxelWorld.Configuration.ChunkSegmentSize * _segmentIndex + y;

            // Current chunk (this is faster then performing chunk lookup)
            if (!yValid && xValid && zValid)
                return ParentChunk.GetBlock(x, absY, z);

            // Perform world-wide block lookup
            var absX = VoxelWorld.Configuration.ChunkSegmentSize * ParentChunk.WorldPosition.X + x;
            var absZ = VoxelWorld.Configuration.ChunkSegmentSize * ParentChunk.WorldPosition.Y + z;

            return ParentChunk.World.GetBlock(absX, absY, absZ);
        }

        /// <summary>
        /// Generates mesh representation of chunk segment data using Greedy algorithm
        /// </summary>
        /// <returns>Mesh representation of current chunk segment data</returns>
        private MeshData GenerateMesh()
        {
            // TODO: Maybe move to GPU for mesh generation
            // NOTE: At least 18x18x18 cube of data (segment + neighboring slices)
            // Investigate if management costs won't be higher than running on CPU

            var data = new MeshData();
            int i, j, k, l, w, h, u, v, n, side = 0;

            int[] x = new int[] { 0, 0, 0 };
            int[] q = new int[] { 0, 0, 0 };
            int[] du = new int[] { 0, 0, 0 };
            int[] dv = new int[] { 0, 0, 0 };

            Block[] mask = new Block[VoxelWorld.Configuration.ChunkSegmentSize * VoxelWorld.Configuration.ChunkSegmentSize];

            Block block, block2;

            for (bool backFace = true, b = false; b != backFace; backFace = backFace && b, b = !b)
            {
                for (var d = 0; d < 3; d++)
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

                    for (x[d] = -1; x[d] < VoxelWorld.Configuration.ChunkSegmentSize;)
                    {
                        n = 0;

                        for (x[v] = 0; x[v] < VoxelWorld.Configuration.ChunkSegmentSize; x[v]++)
                        {

                            for (x[u] = 0; x[u] < VoxelWorld.Configuration.ChunkSegmentSize; x[u]++)
                            {

                                block = GetVoxelFace(x[0], x[1], x[2], side);
                                block2 = GetVoxelFace(x[0] + q[0], x[1] + q[1], x[2] + q[2], side);


                                mask[n++] = (block != null && block2 != null && block.IsTransparent == block2.IsTransparent) ? null : backFace ? block2 : block;
                            }
                        }

                        x[d]++;
                        n = 0;

                        for (j = 0; j < VoxelWorld.Configuration.ChunkSegmentSize; j++)
                        {

                            for (i = 0; i < VoxelWorld.Configuration.ChunkSegmentSize;)
                            {

                                if (mask[n] != null)
                                {

                                    for (w = 1; i + w < VoxelWorld.Configuration.ChunkSegmentSize && mask[n + w] != null && mask[n + w].Equals(mask[n]); w++) { }

                                    bool done = false;

                                    for (h = 1; j + h < VoxelWorld.Configuration.ChunkSegmentSize; h++)
                                    {

                                        for (k = 0; k < w; k++)
                                        {

                                            if (mask[n + k + h * VoxelWorld.Configuration.ChunkSegmentSize] == null || !mask[n + k + h * VoxelWorld.Configuration.ChunkSegmentSize].Equals(mask[n])) { done = true; break; }
                                        }

                                        if (done) { break; }
                                    }

                                    // if (!mask[n].IsTransparent)
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

                                        Quad(data.Entries[mask[n].IsTransparent ? 1 : 0], new Vector3(x[0], x[1], x[2]),
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

                                        for (k = 0; k < w; ++k) { mask[n + k + l * VoxelWorld.Configuration.ChunkSegmentSize] = null; }
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

            return data;
        }
        
        /// <summary>
        /// Appends oriented quad to mesh
        /// </summary>
        /// <param name="meshEntry">Instance of current mesh data we're filling</param>
        /// <param name="bottomLeft">Bottom left position of the quad</param>
        /// <param name="topLeft">Top left position of the quad</param>
        /// <param name="topRight">Top right position of the quad</param>
        /// <param name="bottomRight">Bottom right position of the quad</param>
        /// <param name="width">Width of the quad</param>
        /// <param name="height">Height of the quad</param>
        /// <param name="voxel">Block template this quad is representing</param>
        /// <param name="backFace">Whether or not this face is backFace (flipped winding order)</param>
        /// <remarks>
        /// Quad positions should be chunk segment relative (0-15 for each axis)
        /// </remarks>
        private static void Quad(MeshData.MeshEntry meshEntry, Vector3 bottomLeft,
            Vector3 topLeft,
            Vector3 topRight,
            Vector3 bottomRight,
            int width,
            int height,
            Block voxel,
            bool backFace)
        {

            // Starting index of vertices
            var offset = (int)meshEntry.Vertices.Count;

            // Append vertices
            meshEntry.Vertices.AddRange(new[] { topLeft, topRight, bottomLeft, bottomRight });

            // Append indices
            meshEntry.Indices.AddRange(backFace ? new int[] { 2 + offset, 3 + offset, 1 + offset, 1 + offset, 0 + offset, 2 + offset } : new int[] { 2 + offset, 0 + offset, 1 + offset, 1 + offset, 3 + offset, 2 + offset });

            // Calculate normal
            var normal = Vector3.Normalize(Vector3.Cross(topRight - topLeft, bottomRight - topLeft));

            // Flip it if back facing
            if (backFace)
                normal *= -1;

            // Append normal
            meshEntry.Normals.AddRange(new[] { normal, normal, normal, normal });

            // Append color
            meshEntry.Colors.AddRange(new []{voxel.Color, voxel.Color, voxel.Color, voxel.Color});
        }
    }
}
