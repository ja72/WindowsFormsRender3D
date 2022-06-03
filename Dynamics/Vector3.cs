using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace JA.Dynamics
{
    using static DoubleConstants;

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public readonly struct Vector3 :
        ICollection<double>,
        System.Collections.ICollection,
        IEquatable<Vector3>,
        IFormattable
    {
        readonly (double x, double y, double z) data;

        #region Factory
        public Vector3(double x, double y, double z)
        {
            this.data = (x, y, z);
        }
        public static readonly Vector3 Zero = new Vector3(0, 0, 0);
        public static readonly Vector3 UnitX = new Vector3(1, 0, 0);
        public static readonly Vector3 UnitY = new Vector3(0, 1, 0);
        public static readonly Vector3 UnitZ = new Vector3(0, 0, 1);

        public static Vector3 FromAxis(Axis axis)
        {
            switch (axis)
            {
                case Axis.X: return UnitX;
                case Axis.Y: return UnitY;
                case Axis.Z: return UnitZ;
                default:
                    throw new NotSupportedException($"Axis {axis} not supported.");
            }
        }
        public static implicit operator Vector3(Axis axis) => FromAxis(axis);
        public static implicit operator Vector3(System.Numerics.Vector3 vector)
            => new Vector3(vector.X, vector.Y, vector.Z);
        public static Vector3 Direction(Vector3 from, Vector3 to) => Normalize(to - from);

        #endregion

        #region Properties
        [Browsable(false)]
        public int Size { get => 3; }

        public double X => data.x;
        public double Y => data.y;
        public double Z => data.z;
        [Browsable(false)]public double Magnitude => Math.Sqrt(MagnitudeSquared);
        [Browsable(false)]public double MagnitudeSquared => data.x * data.x + data.y * data.y + data.z * data.z;
        public Vector3 ToUnit() => Normalize(this);
        #endregion

        #region Algebra

        public static Vector3 Normalize(Vector3 vector)
        {
            double m2 = vector.MagnitudeSquared;
            if (m2 > tiny_sq)
            {
                return Scale(1 / Math.Sqrt(m2), vector);
            }
            return vector;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Abs(Vector3 vector)
            => new Vector3(Math.Abs(vector.data.x), Math.Abs(vector.data.y), Math.Abs(vector.data.z));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Add(Vector3 a, Vector3 b)
            => new Vector3(a.data.x + b.data.x, a.data.y + b.data.y, a.data.z + b.data.z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Subtract(Vector3 a, Vector3 b)
            => new Vector3(a.data.x - b.data.x, a.data.y - b.data.y, a.data.z - b.data.z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Scale(double f, Vector3 a)
            => new Vector3(f * a.data.x, f * a.data.y, f * a.data.z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Negate(Vector3 a)
            => new Vector3(-a.data.x, -a.data.y, -a.data.z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Dot(Vector3 a, Vector3 b)
            => a.data.x * b.data.x + a.data.y * b.data.y + a.data.z * b.data.z;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3 Outer(Vector3 a, Vector3 b)
            => new Matrix3(
                a.X * b.X, a.X * b.Y, a.X * b.Z,
                a.Y * b.X, a.Y * b.Y, a.Y * b.Z,
                a.Z * b.X, a.Z * b.Y, a.Z * b.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Cross(Vector3 a, Vector3 b)
          => new Vector3(
              a.data.y * b.data.z - a.data.z * b.data.y,
              a.data.z * b.data.x - a.data.x * b.data.z,
              a.data.x * b.data.y - a.data.y * b.data.x);
        

        public Matrix3 CrossOp() => Dynamics.CrossOp(this);

        /// <summary>
        /// Get the angle between two vectors;
        /// </summary>
        /// <param name="a">The first vector</param>
        /// <param name="b">The second vector</param>
        /// <returns></returns>
        public static double AngleBetween(Vector3 a, Vector3 b)
        {
            //tex: $\cos \theta = \frac{ |a \cdot b| }{|a| |b|}$
            double num = Dot(a, b);
            double den = Math.Sqrt(a.MagnitudeSquared * b.MagnitudeSquared);
            return Math.Acos(num / den);
        }

        public Vector3 RotateAboutX(double angle)
        {
            double c = Math.Cos(angle), s = Math.Sin(angle);
            return new Vector3(data.x, c * data.y - s * data.z, s * data.y + c * data.z);
        }
        public Vector3 RotateAboutY(double angle)
        {
            double c = Math.Cos(angle), s = Math.Sin(angle);
            return new Vector3(c * data.x + s * data.z, data.y, -s * data.x + c * data.z);
        }
        public Vector3 RotateAboutZ(double angle)
        {
            double c = Math.Cos(angle), s = Math.Sin(angle);
            return new Vector3(c * data.x - s * data.y, s * data.x + c * data.y, data.z);
        }
        public Vector3 RotateAbout(Axis axis, double angle)
        {
            switch (axis)
            {
                case Axis.X: return RotateAboutX(angle);
                case Axis.Y: return RotateAboutY(angle);
                case Axis.Z: return RotateAboutZ(angle);
                default:
                    throw new NotSupportedException($"Invalid Axis {axis}");
            }
        }
        public Vector3 RotateAbout(Vector3 axis, double angle)
        {
            axis = Normalize(axis);
            var kxp = Cross(axis, this);
            var kxkxp = Cross(axis, kxp);
            return this + Math.Sin(angle) * kxp + (1 - Math.Cos(angle)) * kxkxp;
        }

        public static Vector3 Transform(Vector3 vector, Quaternion rotation)
        {
            return rotation.Rotate(vector);
        }

        public System.Numerics.Vector3 ToFloat() => new System.Numerics.Vector3(
            (float)data.x,
            (float)data.y,
            (float)data.z);

        #endregion

        #region Operators
        public static Vector3 operator +(Vector3 a, Vector3 b) => Add(a, b);
        public static Vector3 operator -(Vector3 a) => Negate(a);
        public static Vector3 operator -(Vector3 a, Vector3 b) => Subtract(a, b);
        public static Vector3 operator *(double a, Vector3 b) => Scale(a, b);
        public static Vector3 operator *(Vector3 a, double b) => Scale(b, a);
        public static Vector3 operator /(Vector3 a, double b) => Scale(1 / b, a);
        public static double operator *(Vector3 a, Vector3 b) => Dot(a, b);
        public static Vector3 operator ^(Vector3 a, Vector3 b) => Cross(a, b);

        #endregion

        #region Formatting
        public override string ToString() => ToString("g");
        public string ToString(string formatting) => ToString(formatting, null);
        public string ToString(string format, IFormatProvider provider)
        {
            return $"({X.ToString(format, provider)},{Y.ToString(format, provider)},{Z.ToString(format, provider)})";
        }
        #endregion

        #region Equality
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            return obj is Vector3 vector
                && Equals(vector);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Vector3 other)
        {
            other = Abs(this - other);
            return other.data.x < tiny
                && other.data.y < tiny
                && other.data.z < tiny;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return 1768953197 + data.GetHashCode();
        }

        public static bool operator ==(Vector3 left, Vector3 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Vector3 left, Vector3 right)
        {
            return !(left == right);
        }

        #endregion

        #region Collection
        [Browsable(false)]public int Count => 3;
        [Browsable(false)]public bool IsReadOnly => true;
        void ICollection<double>.Add(double item) => throw new NotSupportedException();
        void ICollection<double>.Clear() => throw new NotSupportedException();
        bool ICollection<double>.Contains(double item) => throw new NotSupportedException();
        bool ICollection<double>.Remove(double item) => throw new NotSupportedException();
        public IEnumerator<double> GetEnumerator()
        {
            yield return data.x;
            yield return data.y;
            yield return data.z;
        }
        public double[] ToArray() => new[] { data.x, data.y, data.z };
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        public void CopyTo(double[] array, int index) => CopyTo(array as Array, index);
        public void CopyTo(Array array, int index)=> Array.Copy(ToArray(), 0, array, index, Count);
        [Browsable(false)]public object SyncRoot => null;
        [Browsable(false)]public bool IsSynchronized => false;
        #endregion
    }
}

