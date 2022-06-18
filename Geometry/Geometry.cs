using JA.Drawing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace JA.Geometry
{
    using static SingleConstants;

    public interface IRender
    {
        void Render(Graphics g, Camera camera, Pose pose);
    }

    public interface IGeometry
    {
        bool Contains(Vector3 point);
        Bounds GetBounds();
    }
    public interface ICurve : IGeometry
    {
        Vector3 GetPointAlong(float t);
        Vector3 GetTangent(float t);
    }
    public interface ISurface : IGeometry
    {
        float Area {get;}
        Vector3 Center { get; }
    }
    public interface ISolid : IGeometry
    {
        float Volume {get;}
        Vector3 Center { get; }
    }
    public interface IRayTarget : IGeometry
    {
        Vector3 GetNormal(Vector3 point);
        bool Hit(Ray ray, out float distance, bool nearest = true);
    }

    public static class Geometry
    {
        public static Vector3 FromAxis(this Axis axis, float magnitude = 1)
        {
            switch (axis)
            {
                case Axis.X: return magnitude * Vector3.UnitX;
                case Axis.Y: return magnitude * Vector3.UnitY;
                case Axis.Z: return magnitude * Vector3.UnitZ;
                default:
                    throw new NotSupportedException();
            }
        }
        public static Vector3 Sum(this Vector3[] vectors)
        {
            var sum = Vector3.Zero;
            for (int i = 0; i < vectors.Length; i++)
            {
                sum += vectors[i];
            }
            return sum;
        }
        public static Vector3 Average(this Vector3[] vectors)
            => vectors.Sum() / vectors.Length;

        public static Vector3 GetNormal(Vector3 A, Vector3 B, Vector3 C)
        {
            return Vector3.Normalize(
                Vector3.Cross(A, B)
                + Vector3.Cross(B, C)
                + Vector3.Cross(C, A)
                );
        }
        public static Matrix4x4 Outer(this Vector3 a, Vector3 b)
        {
            return new Matrix4x4(
                a.X*b.X, a.X*b.Y, a.X*b.Z, 0,
                a.Y*b.X, a.Y*b.Y, a.Y*b.Z, 0,
                a.Z*b.X, a.Z*b.Y, a.Z*b.Z, 0,
                0, 0, 0, 1);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4 CrossOp(this Vector3 a)
          => new Matrix4x4(
              0, -a.Z, a.Y, 0,
              a.Z, 0, -a.X, 0,
              -a.Y, a.X, 0, 0,
              0, 0, 0, 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4 Mmoi(this Vector3 a, float scale = 1)
        {
            //tex: ${\rm mmoi}(v) = -v\times v\times$
            float xx = scale*a.X * a.X, yy = scale*a.Y * a.Y, zz = scale*a.Z * a.Z;
            float xy = scale*a.X * a.Y, yz = scale*a.Y * a.Z, zx = scale*a.Z * a.X;
            return new Matrix4x4(
                yy + zz, -xy, -zx, 0,
                -xy, xx + zz, -yz, 0,
                -zx, -yz, xx + yy, 0,
                0, 0, 0, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Unit(this Vector3 vector)
            => Vector3.Zero == vector ? Vector3.Zero : Vector3.Normalize(vector);
        public static Quaternion Rotation(Vector3 axis, float angle)
            => Quaternion.CreateFromAxisAngle(axis, angle);
        public static Quaternion Rotation(Axis axis, float angle)
            => Quaternion.CreateFromAxisAngle(axis.FromAxis(), angle);
        public static Quaternion FromRollPitchYaw(float roll, float pitch, float yaw)
        {
            return Quaternion.CreateFromAxisAngle(Vector3.UnitZ, roll)
                * Quaternion.CreateFromAxisAngle(Vector3.UnitX, pitch)
                * Quaternion.CreateFromAxisAngle(Vector3.UnitY, yaw);
        }

        public static float GetAngleBetwenVectors(Vector3 a, Vector3 b)
        {
            float dx = Vector3.Dot(a, b)/Sqrt(a.LengthSquared()*b.LengthSquared());
            //float dy = (Vector3.Cross(a, b).Length())/(a.Length()*b.Length());
            return Acos(dx);
        }

        public static string ToString(this Quaternion q, string formatting, IFormatProvider formatProvider)
        {
            return $"{{X:{q.X.ToString(formatting, formatProvider)},Y:{q.Y.ToString(formatting, formatProvider)},Z:{q.Z.ToString(formatting, formatProvider)},W:{q.W.ToString(formatting, formatProvider)}}}";
        }
        public static Mesh FromLocal(this Mesh mesh, Pose pose)
        {
            return new Mesh(mesh.Nodes.Select((n) => pose.FromLocal(n)), mesh.Faces);
        }
        public static Mesh ToLocal(this Mesh mesh, Pose pose)
        {
            return new Mesh(mesh.Nodes.Select((n) => pose.ToLocal(n)), mesh.Faces);
        }
        public static bool IsFinite(this float value)
            => !float.IsInfinity(value) && !float.IsNaN(value);
        public static bool IsFinite(this Vector3 vector)
        {
            return !float.IsInfinity(vector.X) && !float.IsNaN(vector.X)
                && !float.IsInfinity(vector.Y) && !float.IsNaN(vector.Y)
                && !float.IsInfinity(vector.Z) && !float.IsNaN(vector.Z);
        }
    }
}
