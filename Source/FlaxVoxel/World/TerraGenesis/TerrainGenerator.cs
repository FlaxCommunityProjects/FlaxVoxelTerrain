using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FlaxEngine;
using LibNoise;
using LibNoise.Filter;
using LibNoise.Primitive;

namespace FlaxVoxel.TerraGenesis
{
    public class TerrainGenerator
    {
        public class Configuration
        {
            public const float SeaLevel = 140;
            public const float MountainScale = 100;
            public const float SnowTemperature = -0.6f;
            public const float TropicalTemperature = 0.2f;
            public const float DesertTemperature = 0.6f;
            public const float DesertHumidity = 0.15f;
            public const float ForestHumidity = 0.5f;
            public const float JungleHumidity = 0.85f;
        }

        private OpenSimplex TurbX;
        private OpenSimplex TurbY;
        private RidgedMultiFractal Chaos;
        private HybridMultiFractal Alt;
        private OpenSimplex Hill;
        private OpenSimplex Temp;

        private HeterogeneousMultiFractal Dry;
        private Billow Humid;
        private HeterogeneousMultiFractal Small;
        private HybridMultiFractal Rock;
        private HybridMultiFractal Cliff;

        public TerrainGenerator(long seed)
        {
            var seededConstant = new SimplexPerlin((int)seed, NoiseQuality.Fast);

            TurbX = new OpenSimplex(seed);
            TurbY = new OpenSimplex(seed);
            Chaos = new RidgedMultiFractal {OctaveCount = 7, Primitive2D = seededConstant};
            Hill = new OpenSimplex(seed);
            Alt = new HybridMultiFractal {OctaveCount = 8, Gain = 0.1f, Primitive2D = seededConstant};
            Temp = new OpenSimplex(seed);    
            Dry = new HeterogeneousMultiFractal(){Primitive2D = seededConstant};
            Small = new HeterogeneousMultiFractal {OctaveCount = 2, Primitive2D = seededConstant};
            Rock = new HybridMultiFractal() {Gain = 0.3f, Primitive2D = seededConstant};
            Cliff = new HybridMultiFractal(){Gain = 0.3f, Primitive2D = seededConstant};
            Humid = new Billow {OctaveCount = 12, Gain = 0.125f, Frequency = 1, Primitive2D = seededConstant};
        }

        // NOTE: This is how the method signature should look if generating would work
        public VoxelChunk GenerateChunk(Int2 pos, VoxelWorld world)
        {
            var chunk = world.SpawnChunk(pos);
            return GenerateChunk(chunk, world);
        }

        public VoxelChunk GenerateChunk(VoxelChunk chunk, VoxelWorld world)
        {
            var chunkPos = chunk.WorldPosition * VoxelWorld.Configuration.ChunkSegmentSize;
            for (int x = 0; x < VoxelWorld.Configuration.ChunkSegmentSize; x++)
            {
                for (int z = 0; z < VoxelWorld.Configuration.ChunkSegmentSize; z++)
                {
                    var worldPos = chunkPos + new Int2(x, z);

                    var turbX = TurbX.GetValue(worldPos.X / 48f, worldPos.Y / 48f) * 12f;
                    var turbY = TurbY.GetValue(worldPos.X / 48f, worldPos.Y / 48f) * 12f;

                    var worldPosTurb = new Vector2(worldPos.X + turbX,worldPos.Y + turbY);

                    var alt_base = GetInterpolated(worldPos, (cb) => cb.AltBase);
                    var chaos = GetInterpolated(worldPos, (cb) => cb.Chaos);
                    var temp = GetInterpolated(worldPos, (cb) => cb.Temp);
                    var humidity = GetInterpolated(worldPos, (cb) => cb.Humidity);
                    var rockiness = GetInterpolated(worldPos, (cb) => cb.Rockiness);

                    var river = 0;

                    var riverless_alt = GetInterpolated(worldPos, (cb) => cb.Alt) +
                                        Mathf.Abs(Small.GetValue(worldPosTurb.X / 150f, worldPosTurb.Y / 150f)) *
                                        Mathf.Max(chaos, 0.025f) * 64f +
                                        Mathf.Abs(Small.GetValue(worldPosTurb.X / 450f, worldPosTurb.Y / 450f)) *
                                        (1f - chaos) * (1f - humidity) * 94f;

                    var chunkBase = GetChunkBase(worldPos);

                    var is_cliffs = chunkBase.IsCliffs;
                    var near_cliffs = chunkBase.NearCliffs;

                    var alt = riverless_alt - (Mathf.Cos((1 - river) * Mathf.Pi) + 1) * 0.5f * 24f;
                    while (alt > 0)
                    {
                        alt--;
                        chunk.SetBlock(x, (int)alt, z, new Block{Color = Color32.White, IsTransparent = false, Id = 0}, false, false);
                    }
                }
            }

            world.UpdateQueue.Enqueue(new ReMeshChunk(chunk));
            world.UpdateQueue.Enqueue(new ReMeshChunk(chunkPos + new Int2( 0,-1)));
            world.UpdateQueue.Enqueue(new ReMeshChunk(chunkPos + new Int2( 0, 1)));
            world.UpdateQueue.Enqueue(new ReMeshChunk(chunkPos + new Int2(-1, 0)));
            world.UpdateQueue.Enqueue(new ReMeshChunk(chunkPos + new Int2( 1, 0)));
            return chunk;
        }

        private float GetInterpolated(Int2 worldPos, Func<ChunkBase, float> predicate)
        {
            var x = new float[4];

            var chunkPos = worldPos / VoxelWorld.Configuration.ChunkSegmentSize;

            // TODO: Cache
            for (int i = -1, xi = 0; i < 3; i++,xi++)
            {
                var y0 = predicate(GetChunkBase(new Int2(chunkPos.X + i, chunkPos.Y - 1)));
                var y1 = predicate(GetChunkBase(new Int2(chunkPos.X + i, chunkPos.Y + 0)));
                var y2 = predicate(GetChunkBase(new Int2(chunkPos.X + i, chunkPos.Y + 1)));
                var y3 = predicate(GetChunkBase(new Int2(chunkPos.X + i, chunkPos.Y + 2)));
                x[xi] = CatmullRom(y0, y1, y2, y3,
                    (worldPos.Y % VoxelWorld.Configuration.ChunkSegmentSize) /
                    (float) VoxelWorld.Configuration.ChunkSegmentSize);
            }

            return CatmullRom(x[0], x[1], x[2], x[3], (worldPos.X % VoxelWorld.Configuration.ChunkSegmentSize) /
                                                      (float) VoxelWorld.Configuration.ChunkSegmentSize);
        }

        private float CatmullRom(float a, float b, float c, float d, float x)
        {
            var x2 = x * x;
            var co0 = a * -0.5f + b * 1.5f + c * -1.5f + d * 0.5f;
            var co1 = a + b * -2.5f + c * 2.0f + d * -0.5f;
            var co2 = a * -0.5f + c * 0.5f;
            var co3 = b;

            return co0 * x2 * x + co1 * x2 + co2 * x + co3;
        }

        private ChunkBase GetChunkBase(Int2 position)
        {
            var wx = (float)position.X;
            var wz = (float)position.Y;

            var alt_base = GenAltitudeBase(wx, wz);
            var chaos = GenChaos(wx, wz);
            var humid_uniform = GenHumidityBase(wx, wz);
            var alt_pre = GenAltitude(wx, wz);
            var alt_uniform = GenAltitudeNoSeawater(wx, wz);
            var temp_uniform = GenTemperatureBase(wx, wz);

            var humidity = IrwinHallProbabilityDensityFunction(new float[] { 1, 1 }, new float[] { humid_uniform, 1.0f - alt_uniform });
            var temp = (IrwinHallProbabilityDensityFunction(new float[] { 2, 1 }, new float[] { temp_uniform, 1.0f - alt_uniform }) - 0.5f) * 2f;

            alt_base *= Configuration.MountainScale;

            var alt = Configuration.SeaLevel + alt_pre * Configuration.MountainScale;

            var cliff = Cliff.GetValue(wx / 2048f, wz / 2048f) + chaos * 0.2f;


            return new ChunkBase
            {
                Chaos =  chaos,
                AltBase = alt_base,
                Alt = alt,
                Temp = temp,
                Humidity = humidity,
                Rockiness =Mathf.Max(0,(Rock.GetValue(wx / 1024f, wz / 1024f) - 0.1f) * 0.3f),
                IsCliffs = cliff > 0.5f && alt > Configuration.SeaLevel + 5,
                NearCliffs = cliff > 0.2f
            };
        }

        private class ChunkBase
        {
            public float Chaos;
            public float AltBase;
            public float Alt;
            public float Temp;
            public float Humidity;
            public float Rockiness;
            public bool IsCliffs;
            public bool NearCliffs;
        }

        public static float IrwinHallProbabilityDensityFunction(float[] weights, float[] samples)
        {
            if(weights.Length != samples.Length) throw new Exception();

            double x = weights.Zip(samples, (w, s) => w * s).Sum();
            var n = samples.Length;

            if (x < 0 || x > n) return 0.0f;
            double d = 0;
            for (int i = 0; i <= n; i++)
                d += Math.Pow(-1, i) * Combinations(n, i) * Math.Pow(x - i, n - 1) * Math.Sign(x - i);
            d *= 0.5;
            d /= Factorial(n - 1);
            return (float)d;
        }

        private static double LogFactorial(int x)
        {
            double lf = 0;
            int i = 2;

            if (x >= 100000)
            {
                lf = 1051299.2218991187;
                i = 100001;
            }
            else if (x >= 50000)
            {
                lf = 490995.24304985348;
                i = 50001;
            }
            else if (x >= 10000)
            {
                lf = 82108.927836814153;
                i = 10001;
            }
            else if (x >= 5000)
            {
                lf = 37591.143508876841;
                i = 5001;
            }
            else if (x >= 4000)
            {
                lf = 29181.264544594731;
                i = 4001;
            }
            else if (x >= 3000)
            {
                lf = 21024.024853045572;
                i = 3001;
            }
            else if (x >= 2000)
            {
                lf = 13206.52435051381;
                i = 2001;
            }
            else if (x >= 1000)
            {
                lf = 5912.1281784881712;
                i = 1001;
            }
            else if (x >= 500)
            {
                lf = 2611.3304584601597;
                i = 501;
            }
            else if (x >= 100)
            {
                lf = 363.73937555556358;
                i = 101;
            }

            for (; i <= x; i++)
                lf += Math.Log(i);
            return lf;
        }

        internal static double LogCombin(int n, int k)
        {
            return LogFactorial(n) - LogFactorial(k) - LogFactorial(n - k);
        }

        public static double Combinations(int n, int k)
        {
            double logCombin = LogCombin(n, k);
            double combin = Math.Exp(logCombin);
            return Math.Round(combin, 0);
        }

        public static long Factorial(int n)
        {
            if (n < 0) throw new ArgumentException("Factorial not defined for negative n");
            if (n > 20) throw new ArgumentException("Answer will exceed max long");
            long fact = 1;
            for (int i = n; i > 0; i--)
                fact *= i;
            return fact;
        }

        private float LogisticCDF(float x) => (float)Math.Tanh(x / (Mathf.Sqrt(3.0f) * (2f / Mathf.Pi))) * 0.5f + 0.5f;
        private float LogIt(float x) => Mathf.Log(x) - Mathf.Log(-x + 1);

        /*private void GenChunkBase(Int2 position, VoxelWorld world)
        {
        }*/

        private float GenAltitudeBase(float x, float z) => (Alt.GetValue(x / 12_000f, z / 12_000f) - 0.1f) * 0.25f;

        private float GenChaos(float x, float z)
        {
            var hill = Mathf.Max(0,(Hill.GetValue(x / 1_500f, z / 1_500f) + Hill.GetValue(x / 400f, z / 400f) * 0.3f) * 0.3f);

            var chaos = (Chaos.GetValue(x / 3_000f, z / 3_000f) + 1f) * 0.5f;
            chaos *= Mathf.Max(0.25f, Mathf.Min(1.0f, Mathf.Abs(Chaos.GetValue(x / 6_000f, z / 6_000f))));
            return Mathf.Max(0.1f, chaos + 0.2f * hill);
        }

        private float GenAltitude(float x, float z)
        {
            var altMain = Mathf.Pow(Mathf.Abs(Alt.GetValue(x / 2_000f, z / 2_000f)), 1.35f);
            altMain += (Small.GetValue(x / 300f, z / 300f) * Mathf.Max(0.25f, altMain) * 0.3f + 1) * 0.5f;

            return GenAltitudeBase(x,z) + altMain * GenChaos(x,z);
        }

        private bool IsPureWater(float x, float z)
        {
            for (var i = x - 1; i <= x+1; i++)
                for (var j = z-1; j < z+1; j++)
                    if (GenAltitude(i, j) * Configuration.MountainScale > 0) return false;

            return true;
        }

        private float GenAltitudeNoSeawater(float x, float z)
        {
            if (IsPureWater(x, z)) return 0;
            return GenAltitude(x, z);
        }

        private float GenTemperatureBase(float x, float z)
        {
            if (IsPureWater(x, z)) return 0;
            return Temp.GetValue(x / 12_000f, z / 12_000f);
        }

        private float GenHumidityBase(float x, float z)
        {
            if (IsPureWater(x, z)) return 0;
            return (Humid.GetValue(x / 1024f, z / 1024f) + 1) * 0.5f;
        }
    }
}
