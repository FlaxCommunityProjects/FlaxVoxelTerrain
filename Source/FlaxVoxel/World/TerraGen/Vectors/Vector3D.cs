using FlaxEngine;
using System;
using System.Runtime.InteropServices;

namespace FlaxVoxel.TerraGen.Noise
{
    /// <summary>
    /// Represents the location of an object in 3D space.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct Vector3D : IEquatable<Vector3D>
    {
        /// <summary>
        /// The X component of this vector.
        /// </summary>
        [FieldOffset(0)]
        public double X;

        /// <summary>
        /// The Y component of this vector.
        /// </summary>
        [FieldOffset(8)]
        public double Y;

        /// <summary>
        /// The Z component of this vector.
        /// </summary>
        [FieldOffset(16)]
        public double Z;

        /// <summary>
        /// Creates a new vector from the specified value.
        /// </summary>
        /// <param name="value">The value for the components of the vector.</param>
        public Vector3D(double value)
        {
            X = Y = Z = value;
        }

        /// <summary>
        /// Creates a new vector from the specified values.
        /// </summary>
        /// <param name="x">The X component of the vector.</param>
        /// <param name="y">The Y component of the vector.</param>
        /// <param name="z">The Z component of the vector.</param>
        public Vector3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>
        /// Creates a new vector from copying another.
        /// </summary>
        /// <param name="v">The vector to copy.</param>
        public Vector3D(Vector3D v)
        {
            X = v.X;
            Y = v.Y;
            Z = v.Z;
        }

        /// <summary>
        /// Converts this Vector3D to a string in the format &lt;x,y,z&gt;.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("<{0},{1},{2}>", X, Y, Z);
        }

        #region Math

        /// <summary>
        /// Truncates the decimal component of each part of this Vector3D.
        /// </summary>
        public Vector3D Floor()
        {
            return new Vector3D(Math.Floor(X), Math.Floor(Y), Math.Floor(Z));
        }

        /// <summary>
        /// Rounds the decimal component of each part of this Vector3D.
        /// </summary>
        public Vector3D Round()
        {
            return new Vector3D(Math.Round(X), Math.Round(Y), Math.Round(Z));
        }

        /// <summary>
        /// Clamps the vector to within the specified value.
        /// </summary>
        /// <param name="value">Value.</param>
        public void Clamp(double value)
        {
            if (Math.Abs(X) > value)
                X = value * (X < 0 ? -1 : 1);
            if (Math.Abs(Y) > value)
                Y = value * (Y < 0 ? -1 : 1);
            if (Math.Abs(Z) > value)
                Z = value * (Z < 0 ? -1 : 1);
        }

        /// <summary>
        /// Calculates the distance between two Vector3D objects.
        /// </summary>
        public double DistanceTo(Vector3D other)
        {
            return Math.Sqrt(Square(other.X - X) +
                             Square(other.Y - Y) +
                             Square(other.Z - Z));
        }

        public Vector3D Transform(Matrix matrix)
        {
            var x = (X * matrix.M11) + (Y * matrix.M21) + (Z * matrix.M31) + matrix.M41;
            var y = (X * matrix.M12) + (Y * matrix.M22) + (Z * matrix.M32) + matrix.M42;
            var z = (X * matrix.M13) + (Y * matrix.M23) + (Z * matrix.M33) + matrix.M43;
            return new Vector3D(x, y, z);
        }

        /// <summary>
        /// Calculates the square of a num.
        /// </summary>
        private double Square(double num)
        {
            return num * num;
        }

        /// <summary>
        /// Finds the distance of this vector from Vector3D.Zero
        /// </summary>
        public double Distance
        {
            get
            {
                return DistanceTo(Zero);
            }
        }

        /// <summary>
        /// Returns the component-wise minumum of two vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <returns></returns>
        public static Vector3D Min(Vector3D value1, Vector3D value2)
        {
            return new Vector3D(
                Math.Min(value1.X, value2.X),
                Math.Min(value1.Y, value2.Y),
                Math.Min(value1.Z, value2.Z)
                );
        }

        /// <summary>
        /// Returns the component-wise maximum of two vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <returns></returns>
        public static Vector3D Max(Vector3D value1, Vector3D value2)
        {
            return new Vector3D(
                Math.Max(value1.X, value2.X),
                Math.Max(value1.Y, value2.Y),
                Math.Max(value1.Z, value2.Z)
                );
        }

        /// <summary>
        /// Calculates the dot product between two vectors.
        /// </summary>
        public static double Dot(Vector3D value1, Vector3D value2)
        {
            return value1.X * value2.X + value1.Y * value2.Y + value1.Z * value2.Z;
        }

        /// <summary>
        /// Computes the cross product of two vectors.
        /// </summary>
        /// <param name="vector1">The first vector.</param>
        /// <param name="vector2">The second vector.</param>
        /// <returns>The cross product of two vectors.</returns>
        public static Vector3D Cross(Vector3D vector1, Vector3D vector2)
        {
            Cross(ref vector1, ref vector2, out vector1);
            return vector1;
        }

        /// <summary>
        /// Computes the cross product of two vectors.
        /// </summary>
        /// <param name="vector1">The first vector.</param>
        /// <param name="vector2">The second vector.</param>
        /// <param name="result">The cross product of two vectors as an output parameter.</param>
        public static void Cross(ref Vector3D vector1, ref Vector3D vector2, out Vector3D result)
        {
            var x = vector1.Y * vector2.Z - vector2.Y * vector1.Z;
            var y = -(vector1.X * vector2.Z - vector2.X * vector1.Z);
            var z = vector1.X * vector2.Y - vector2.X * vector1.Y;
            result.X = x;
            result.Y = y;
            result.Z = z;
        }

        #endregion

        #region Operators

        public static bool operator !=(Vector3D a, Vector3D b)
        {
            return !a.Equals(b);
        }

        public static bool operator ==(Vector3D a, Vector3D b)
        {
            return a.Equals(b);
        }

        public static Vector3D operator +(Vector3D a, Vector3D b)
        {
            return new Vector3D(
                a.X + b.X,
                a.Y + b.Y,
                a.Z + b.Z);
        }

        public static Vector3D operator -(Vector3D a, Vector3D b)
        {
            return new Vector3D(
                a.X - b.X,
                a.Y - b.Y,
                a.Z - b.Z);
        }

        public static Vector3D operator -(Vector3D a)
        {
            return new Vector3D(
                -a.X,
                -a.Y,
                -a.Z);
        }

        public static Vector3D operator *(Vector3D a, Vector3D b)
        {
            return new Vector3D(
                a.X * b.X,
                a.Y * b.Y,
                a.Z * b.Z);
        }

        public static Vector3D operator /(Vector3D a, Vector3D b)
        {
            return new Vector3D(
                a.X / b.X,
                a.Y / b.Y,
                a.Z / b.Z);
        }

        public static Vector3D operator %(Vector3D a, Vector3D b)
        {
            return new Vector3D(a.X % b.X, a.Y % b.Y, a.Z % b.Z);
        }

        public static Vector3D operator +(Vector3D a, double b)
        {
            return new Vector3D(
                a.X + b,
                a.Y + b,
                a.Z + b);
        }

        public static Vector3D operator -(Vector3D a, double b)
        {
            return new Vector3D(
                a.X - b,
                a.Y - b,
                a.Z - b);
        }

        public static Vector3D operator *(Vector3D a, double b)
        {
            return new Vector3D(
                a.X * b,
                a.Y * b,
                a.Z * b);
        }

        public static Vector3D operator /(Vector3D a, double b)
        {
            return new Vector3D(
                a.X / b,
                a.Y / b,
                a.Z / b);
        }

        public static Vector3D operator %(Vector3D a, double b)
        {
            return new Vector3D(a.X % b, a.Y % b, a.Y % b);
        }

        public static Vector3D operator +(double a, Vector3D b)
        {
            return new Vector3D(
                a + b.X,
                a + b.Y,
                a + b.Z);
        }

        public static Vector3D operator -(double a, Vector3D b)
        {
            return new Vector3D(
                a - b.X,
                a - b.Y,
                a - b.Z);
        }

        public static Vector3D operator *(double a, Vector3D b)
        {
            return new Vector3D(
                a * b.X,
                a * b.Y,
                a * b.Z);
        }

        public static Vector3D operator /(double a, Vector3D b)
        {
            return new Vector3D(
                a / b.X,
                a / b.Y,
                a / b.Z);
        }

        public static Vector3D operator %(double a, Vector3D b)
        {
            return new Vector3D(a % b.X, a % b.Y, a % b.Y);
        }

        #endregion

        #region Conversion operators

        public static implicit operator Vector3D(Int3 a)
        {
            return new Vector3D(a.X, a.Y, a.Z);
        }

        public static explicit operator Vector3D(Int2 c)
        {
            return new Vector3D(c.X, 0, c.Y);
        }
        #endregion

        #region Constants

        /// <summary>
        /// A vector with its components set to 0.0.
        /// </summary>
        public static readonly Vector3D Zero = new Vector3D(0);

        /// <summary>
        /// A vector with its components set to 1.0.
        /// </summary>
        public static readonly Vector3D One = new Vector3D(1);


        /// <summary>
        /// A vector that points upward.
        /// </summary>
        public static readonly Vector3D Up = new Vector3D(0, 1, 0);

        /// <summary>
        /// A vector that points downward.
        /// </summary>
        public static readonly Vector3D Down = new Vector3D(0, -1, 0);

        /// <summary>
        /// A vector that points to the left.
        /// </summary>
        public static readonly Vector3D Left = new Vector3D(-1, 0, 0);

        /// <summary>
        /// A vector that points to the right.
        /// </summary>
        public static readonly Vector3D Right = new Vector3D(1, 0, 0);

        /// <summary>
        /// A vector that points backward.
        /// </summary>
        public static readonly Vector3D Backwards = new Vector3D(0, 0, -1);

        /// <summary>
        /// A vector that points forward.
        /// </summary>
        public static readonly Vector3D Forwards = new Vector3D(0, 0, 1);


        /// <summary>
        /// A vector that points to the east.
        /// </summary>
        public static readonly Vector3D East = new Vector3D(1, 0, 0);

        /// <summary>
        /// A vector that points to the west.
        /// </summary>
        public static readonly Vector3D West = new Vector3D(-1, 0, 0);

        /// <summary>
        /// A vector that points to the north.
        /// </summary>
        public static readonly Vector3D North = new Vector3D(0, 0, -1);

        /// <summary>
        /// A vector that points to the south.
        /// </summary>
        public static readonly Vector3D South = new Vector3D(0, 0, 1);

        #endregion

        /// <summary>
        /// Determines whether this and another vector are equal.
        /// </summary>
        /// <param name="other">The other vector.</param>
        /// <returns></returns>
        public bool Equals(Vector3D other)
        {
            return other.X.Equals(X) && other.Y.Equals(Y) && other.Z.Equals(Z);
        }

        /// <summary>
        /// Determines whether this and another object are equal.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return obj is Vector3D && Equals((Vector3D)obj);
        }

        /// <summary>
        /// Gets the hash code for this vector.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int result = X.GetHashCode();
                result = (result * 397) ^ Y.GetHashCode();
                result = (result * 397) ^ Z.GetHashCode();
                return result;
            }
        }
    }
}