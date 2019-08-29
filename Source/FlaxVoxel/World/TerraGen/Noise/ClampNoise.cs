namespace FlaxVoxel.TerraGen.Noise
{
    public class ClampNoise : NoiseGen
    {
        public NoiseGen Noise { get; set; }
        public double MinValue { get; set; }
        public double MaxValue { get; set; }

        public ClampNoise(NoiseGen noise)
        {
            Noise = noise;
            MinValue = 0;
            MaxValue = 1;
        }

        public override double Value2D(double x, double y)
        {
            var NoiseValue = Noise.Value2D(x, y);
            if (NoiseValue < MinValue)
                NoiseValue = MinValue;
            if (NoiseValue > MaxValue)
                NoiseValue = MaxValue;
            return NoiseValue;
        }

        public override double Value3D(double x, double y, double z)
        {
            var NoiseValue = Noise.Value3D(x, y, z);
            if (NoiseValue < MinValue)
                NoiseValue = MinValue;
            if (NoiseValue > MaxValue)
                NoiseValue = MaxValue;
            return NoiseValue;
        }
    }
}
