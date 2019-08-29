﻿using System;

namespace FlaxVoxel.TerraGen.Noise
{
    public class ModifyNoise : NoiseGen
    {
        public NoiseGen PrimaryNoise { get; set; }
        public NoiseGen SecondaryNoise { get; set; }
        public NoiseModifier Modifier { get; set; }

        public ModifyNoise(NoiseGen primaryNoise, NoiseGen secondaryNoise, NoiseModifier modifier = NoiseModifier.Add)
        {
            this.PrimaryNoise = primaryNoise;
            this.SecondaryNoise = secondaryNoise;
            this.Modifier = modifier;
        }

        public override double Value2D(double x, double y)
        {
            switch (Modifier)
            {
                case NoiseModifier.Add:
                    return PrimaryNoise.Value2D(x, y) + SecondaryNoise.Value2D(x, y);
                case NoiseModifier.Multiply:
                    return PrimaryNoise.Value2D(x, y) * SecondaryNoise.Value2D(x, y);
                case NoiseModifier.Power:
                    return Math.Pow(PrimaryNoise.Value2D(x, y), SecondaryNoise.Value2D(x, y));
                case NoiseModifier.Subtract:
                    return PrimaryNoise.Value2D(x, y) - SecondaryNoise.Value2D(x, y);
                default:
                    //This is unreachable.
                    return PrimaryNoise.Value2D(x, y) + SecondaryNoise.Value2D(x, y);
            }
        }

        public override double Value3D(double x, double y, double z)
        {
            switch (Modifier)
            {
                case NoiseModifier.Add:
                    return PrimaryNoise.Value3D(x, y, z) + SecondaryNoise.Value3D(x, y, z);
                case NoiseModifier.Multiply:
                    return PrimaryNoise.Value3D(x, y, z) * SecondaryNoise.Value3D(x, y, z);
                case NoiseModifier.Power:
                    return Math.Pow(PrimaryNoise.Value3D(x, y, z), SecondaryNoise.Value3D(x, y, z));
                case NoiseModifier.Subtract:
                    return PrimaryNoise.Value3D(x, y, z) - SecondaryNoise.Value3D(x, y, z);
                default:
                    //This is unreachable.
                    return PrimaryNoise.Value3D(x, y, z) + SecondaryNoise.Value3D(x, y, z);
            }
        }
    }

    public enum NoiseModifier
    {
        Add,
        Subtract,
        Multiply,
        Power
    }
}