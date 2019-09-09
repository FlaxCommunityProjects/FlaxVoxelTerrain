using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlaxEngine;

namespace LibNoise.Filter
{
    public class RandomField
    {
        public long Seed;
        public RandomField(long seed)
        {
            Seed = seed;
        }

        public uint Sample(int x, int y, int z)
        {
            var ux = (uint)x;
            var uy = (uint)y;
            var uz = (uint)z;

            unchecked
            {
                var result = (uint)Seed;
                result = (result ^ 61) ^ (result >> 16);
                result += result << 3;
                result ^= ux;
                result += result >> 4;
                result *= 0x27d4eb2d;
                result ^= result >> 15;
                result ^= uy;
                result = (result ^ 61) ^ (result >> 16);
                result += result << 3;
                result ^= result >> 4;
                result ^= uz;
                result *= 0x27d4eb2d;
                result ^= result >> 15;

                return result;
            }
        }
    }
}
