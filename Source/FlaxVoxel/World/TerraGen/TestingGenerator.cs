using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlaxEngine;
using FlaxVoxel.TerraGen.Noise;

namespace FlaxVoxel.TerraGen
{
    class TestingGenerator
    {
        BiomeRepository Biomes = new BiomeRepository();
        Perlin HighNoise;
        Perlin LowNoise;
        Perlin BottomNoise;
        Perlin CaveNoise;
        ClampNoise HighClamp;
        ClampNoise LowClamp;
        ClampNoise BottomClamp;
        ModifyNoise FinalNoise;
        bool EnableCaves;
        private const int GroundLevel = 50;
        private const int featurePointDistance = 400;

        public TestingGenerator()
        {
            EnableCaves = true;
        }

        public void Initialize(VoxelWorld world)
        {
            HighNoise = new Perlin(world.Seed)
            {
                Persistance = 1,
                Frequency = 0.013,
                Amplitude = 10,
                Octaves = 2,
                Lacunarity = 2
            };

            LowNoise = new Perlin(world.Seed)
            {
                Persistance = 1,
                Frequency = 0.004,
                Amplitude = 35,
                Octaves = 2,
                Lacunarity = 2.5
            };

            BottomNoise = new Perlin(world.Seed)
            {
                Persistance = 0.5,
                Frequency = 0.013,
                Amplitude = 5,
                Octaves = 2,
                Lacunarity = 1.5
            };

            CaveNoise = new Perlin(world.Seed)
            {
                Octaves = 3,
                Amplitude = 0.05,
                Persistance = 2,
                Frequency = 0.05,
                Lacunarity = 2
            };



            HighClamp = new ClampNoise(HighNoise) {MinValue = -30, MaxValue = 50};

            LowClamp = new ClampNoise(LowNoise) {MinValue = -30, MaxValue = 30};

            BottomClamp = new ClampNoise(BottomNoise) {MinValue = -20, MaxValue = 5};

            FinalNoise = new ModifyNoise(HighClamp, LowClamp, NoiseModifier.Add);
        }

        public VoxelChunk GenerateChunk(VoxelWorld world, Int2 worldPosition)
        {
            var chunkActor = world.Actor.AddChild<EmptyActor>();
            var chunk = chunkActor.AddScript<VoxelChunk>();
            chunk.WorldPosition = worldPosition;
            chunkActor.LocalPosition = new Vector3(worldPosition.X * VoxelWorld.Configuration.ChunkSegmentSize, 0, worldPosition.Y * VoxelWorld.Configuration.ChunkSegmentSize);
            chunkActor.Name = $"Chunk[{chunk.WorldPosition.X},{chunk.WorldPosition.Y}]";
            chunk.World = world;
            world.Chunks.TryAdd(chunk.WorldPosition, chunk);

            // CHUNK GENERATION:

            int seed = world.Seed;
            var worley = new CellNoise(seed);
            HighNoise.Seed = seed;
            LowNoise.Seed = seed;
            CaveNoise.Seed = seed;

            for (int x = 0; x < VoxelWorld.Configuration.ChunkSegmentSize; x++)
            {
                for (int z = 0; z < VoxelWorld.Configuration.ChunkSegmentSize; z++)
                {
                    var blockX = worldPosition.X * VoxelWorld.Configuration.ChunkSegmentSize + x;
                    var blockZ = worldPosition.Y * VoxelWorld.Configuration.ChunkSegmentSize + z;

                    const double lowClampRange = 5;
                    double lowClampMid = LowClamp.MaxValue - ((LowClamp.MaxValue + LowClamp.MinValue) / 2);
                    double lowClampValue = LowClamp.Value2D(blockX, blockZ);

                    if (lowClampValue > lowClampMid - lowClampRange && lowClampValue < lowClampMid + lowClampRange)
                    {
                        InvertNoise NewPrimary = new InvertNoise(HighClamp);
                        FinalNoise.PrimaryNoise = NewPrimary;
                    }
                    else
                    {
                        //reset it after modifying the values
                        FinalNoise = new ModifyNoise(HighClamp, LowClamp, NoiseModifier.Add);
                    }
                    FinalNoise = new ModifyNoise(FinalNoise, BottomClamp, NoiseModifier.Subtract);

                    var cellValue = worley.Value2D(blockX, blockZ);
                    var location = new Int2(blockX, blockZ);
                    if (world.BiomeDiagram.BiomeCells.Count < 1
                        || cellValue.Equals(1)
                        && world.BiomeDiagram.ClosestCellPoint(location) >= featurePointDistance)
                    {
                        byte id = world.BiomeDiagram.GenerateBiome(seed, Biomes, location);
                        var cell = new BiomeCell(id, location);
                        world.BiomeDiagram.AddCell(cell);
                    }

                    var biomeId = GetBiome(world, location);
                    var biome = Biomes.GetBiome(biomeId);

                    // TODO: Store biome info per chunk [x,z] and maybe add support for vertical biomes too??? who knows

                    var height = GetHeight(blockX, blockZ);
                    var surfaceHeight = height - biome.SurfaceDepth;

                    for (int y = 0; y <= height; y++)
                    {
                        double cave = 0;
                        if (!EnableCaves)
                            cave = double.MaxValue;
                        else
                            cave = CaveNoise.Value3D((blockX + x) / 2, y / 2, (blockZ + z) / 2);
                        double threshold = 0.05;
                        if (y < 4)
                            threshold = double.MaxValue;
                        else
                        {
                            if (y > height - 8)
                                threshold = 8;
                        }
                        if (cave < threshold)
                        {
                            if (y == 0)
                                chunk.SetBlock(x,y,z, new Block{Color = Color32.Black, Id = 4, IsTransparent = false}, false,false);
                            else
                            {
                                if (y.Equals(height) || y < height && y > surfaceHeight)
                                    chunk.SetBlock(x, y, z, biome.SurfaceBlock, false, false);
                                else
                                {
                                    if (y > surfaceHeight - biome.FillerDepth)
                                        chunk.SetBlock(x, y, z, biome.FillerBlock, false ,false);
                                    else
                                        chunk.SetBlock(x, y, z, new Block{Color = new Color32(128,128,128,255), Id = 5, IsTransparent = false}, false, false);
                                }
                            }
                        }
                    }
                }
            }

            world.UpdateQueue.Add(UpdateEntry.UpdateChunk(chunk));
            world.UpdateQueue.Add(UpdateEntry.UpdateChunk(chunk.WorldPosition - Int2.UnitX));
            world.UpdateQueue.Add(UpdateEntry.UpdateChunk(chunk.WorldPosition + Int2.UnitX));
            world.UpdateQueue.Add(UpdateEntry.UpdateChunk(chunk.WorldPosition - Int2.UnitY));
            world.UpdateQueue.Add(UpdateEntry.UpdateChunk(chunk.WorldPosition + Int2.UnitY));
            // END CHUNK GENERATION

            return chunk;
        }

        byte GetBiome(VoxelWorld world, Int2 location)
        {
            return world.BiomeDiagram.GetBiome(location);
        }

        int GetHeight(int x, int z)
        {
            var value = FinalNoise.Value2D(x, z) + GroundLevel;
            var coords = new Int2(x, z);
            if (value < 0)
                value = GroundLevel;
            return (int)value;
        }
    }
}
