using System;
using FlaxEngine;

namespace LibNoise.Filter
{
    public class FastNoise: IModule3D
    {
        private readonly RandomField _noise;

        public long Seed
        {
            get => _noise.Seed;
            set => _noise.Seed = value;
        }

        public FastNoise(long seed)
        {
            _noise = new RandomField(seed);
        }
        public float GetValue(float x, float y, float z)
        {
            var ix = (int) x;
            var iy = (int) y;
            var iz = (int) z;

            var v000 = _noise.Sample(ix + 0, iy + 0, iz + 0);
            var v100 = _noise.Sample(ix + 1, iy + 0, iz + 0);
            var v010 = _noise.Sample(ix + 0, iy + 1, iz + 0);
            var v110 = _noise.Sample(ix + 1, iy + 1, iz + 0);
            var v001 = _noise.Sample(ix + 0, iy + 0, iz + 1);
            var v101 = _noise.Sample(ix + 1, iy + 0, iz + 1);
            var v011 = _noise.Sample(ix + 0, iy + 1, iz + 1);
            var v111 = _noise.Sample(ix + 1, iy + 1, iz + 1);

            var fx = 0.5f - Mathf.Cos((x - (float) Math.Truncate(x)) * Mathf.Pi) * 0.5f;
            var fy = 0.5f - Mathf.Cos((y - (float) Math.Truncate(y)) * Mathf.Pi) * 0.5f;
            var fz = 0.5f - Mathf.Cos((z - (float) Math.Truncate(z)) * Mathf.Pi) * 0.5f;

            var x00 = Libnoise.Lerp(v000, v100, fx);
            var x10 = Libnoise.Lerp(v010, v110, fx);
            var x01 = Libnoise.Lerp(v001, v101, fx);
            var x11 = Libnoise.Lerp(v011, v111, fx);

            var y0 = Libnoise.Lerp(x00, x10, fy);
            var y1 = Libnoise.Lerp(x01, x11, fy);

            return Libnoise.Lerp(y0, y1, fz) * 2.0f - 1.0f;
        }
    }
}
