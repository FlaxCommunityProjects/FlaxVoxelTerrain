using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlaxEngine;
using FlaxEngine.Utilities;

namespace FlaxVoxel
{
    public partial class VoxelChunk
    {
        public void GenerateChunk()
        {
            // TODO: Better generator
            Random rnd = new Random();
            for (int x = 0; x < VoxelWorld.Configuration.ChunkSegmentSize; x++)
            {
                for (int z = 0; z < VoxelWorld.Configuration.ChunkSegmentSize; z++)
                {
                    var h0 = 3.0f * Mathf.Sin(Mathf.Pi * (z + WorldPosition.Y) / 12.0f - Mathf.Pi * (x + WorldPosition.X) * 0.1f) + 27;
                    for (int y = 0; y < 32; y++)
                    {
                        if (y > h0 + 1)
                        {
                            SetBlock(x, y, z, null, false, false);
                            continue;
                        }

                        if (h0 <= y)
                        {
                            SetBlock(x,y,z, new Block { IsTransparent = false, Color = new Color32(0x23, 0xdd, 0x21, 255), Id = 1 }, false, false);
                            continue;
                        }


                        var h1 = 2.0f * Mathf.Sin(Mathf.Pi * (z + WorldPosition.Y) * 0.25f - Mathf.Pi * (x + WorldPosition.X) * 0.3f) + 20;
                        if (h1 <= y)
                        {
                            SetBlock(x,y,z, new Block{IsTransparent = false, Color = new Color32(0x96, 0x4B, 0x00, 255), Id = 2}, false, false);
                            continue;
                        }

                        if (y > 1)
                        {
                            SetBlock(x,y,z, new Block{IsTransparent = false, Color = rnd.NextFloat() < 0.1f
                                ? new Color32(0x22, 0x22, 0x22, 255)
                                : new Color32(0xaa, 0xaa, 0xaa, 255), Id = 3}, false, false);

                            continue;
                        }

                        SetBlock(x,y,z, new Block{IsTransparent =  false, Color = new Color32(0x22, 0x22, 0x22, 255), Id = 4}, false, false);
                    }
                }
            }

            World.UpdateQueue.Enqueue(new ReMeshChunk(this));
            World.UpdateQueue.Enqueue(new ReMeshChunk(WorldPosition - Int2.UnitX));
            World.UpdateQueue.Enqueue(new ReMeshChunk(WorldPosition - Int2.UnitY));
            World.UpdateQueue.Enqueue(new ReMeshChunk(WorldPosition + Int2.UnitX));
            World.UpdateQueue.Enqueue(new ReMeshChunk(WorldPosition + Int2.UnitY));
        }
    }
}
