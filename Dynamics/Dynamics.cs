using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace JA.Dynamics
{

    public static class Dynamics
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3 CrossOp(this Vector3 a)
          => new Matrix3(
              0, -a.Z, a.Y,
              a.Z, 0, -a.X,
              -a.Y, a.X, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3 Mmoi(this Vector3 a, double scale = 1)
        {
            //tex: ${\rm mmoi}(v) = -v\times v\times$
            double xx = scale*a.X * a.X, yy = scale*a.Y * a.Y, zz = scale*a.Z * a.Z;
            double xy = scale*a.X * a.Y, yz = scale*a.Y * a.Z, zx = scale*a.Z * a.X;
            return new Matrix3(
                yy + zz, -xy, -zx,
                -xy, xx + zz, -yz,
                -zx, -yz, xx + yy);
        }

        public static Geometry.Mesh FromLocal(this Geometry.Mesh mesh, Pose pose)
        {
            return Geometry.Geometry.FromLocal(mesh, pose.ToFloat());
        }
        public static Geometry.Mesh ToLocal(this Geometry.Mesh mesh, Pose pose)
        {
            return Geometry.Geometry.ToLocal(mesh, pose.ToFloat());
        }

        public static Matrix3 GetMmoiAtCenter(this Geometry.ISolid solid, double mass)
        {
            if (solid is Geometry.Sphere sphere)
            {
                double R = sphere.Radius;
                double I0 = 2*mass*R*R/5;
                return Matrix3.Scalar(I0);
            }
            else if (solid is Geometry.Mesh mesh)
            {
                Vector3 cg = mesh.Center;
                double V = mesh.Volume;
                var ρ = mass/V;
                Matrix3 I0 = Matrix3.Zero;
                foreach (var poly in mesh)
                {
                    foreach (var trig in poly.GetTriangles(false))
                    {
                        var A = (Vector3)trig.A - cg;
                        var B = (Vector3)trig.B - cg;
                        var C = (Vector3)trig.C - cg;
                        Matrix3 dI = (Mmoi(A+B) + Mmoi(B+C) + Mmoi(C+A))/20;
                        I0 += dI;
                    }
                }
                return ρ*I0;
            }
            throw new NotSupportedException();
        }
        public static bool IsFinite(this double value)
            => !double.IsInfinity(value) && !double.IsNaN(value);

        public static bool IsFinite(this Vector3 vector)
        {
            return !double.IsInfinity(vector.X) && !double.IsNaN(vector.X)
                && !double.IsInfinity(vector.Y) && !double.IsNaN(vector.Y)
                && !double.IsInfinity(vector.Z) && !double.IsNaN(vector.Z);
        }

    }
}
