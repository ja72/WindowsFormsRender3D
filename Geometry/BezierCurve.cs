using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
    using System.ComponentModel;

namespace JA.Geometry
{
    using JA.Drawing;
    using static SingleConstants;

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public readonly struct BezierCurve :
        ICurve,
        IRayTarget
    {
        readonly (Vector3 y_1, Vector3 y_2, Vector3 yp_1, Vector3 yp_2) data;

        public BezierCurve(Vector3 y_1, Vector3 y_2, Vector3 yp_1, Vector3 yp_2)
        {
            this.data=(y_1, y_2, yp_1, yp_2);
        }
        public BezierCurve FromLocal(Pose pose)
            => new BezierCurve(
                pose.FromLocal(Y1),
                pose.FromLocal(Y2),
                pose.FromLocalDirection(Yp1),
                pose.FromLocalDirection(Yp2));
        public Vector3 Y1 { get => data.y_1; }
        public Vector3 Y2 { get => data.y_2; }
        public Vector3 Yp1 { get => data.yp_1; }
        public Vector3 Yp2 { get => data.yp_2; }
        public Vector3 Span { get => data.y_2 - data.y_1; }

        public static BezierCurve FromFourPoints(Vector3 y_start, Vector3 y_end, Vector3 y_mid_1, Vector3 y_mid_2)
        {
            return new BezierCurve(
                y_start, y_end,
                y_mid_1 - y_start,
                y_mid_2 - y_end);
        }
        public static BezierCurve FromTheePoints(Vector3 y_start, Vector3 y_end, Vector3 y_mid)
        {
            return new BezierCurve(
                y_start, y_end,
                y_mid - y_start,
                y_mid - y_end);
        }

        public Vector3 GetPointAlong(float t)
            => Sqr(1-t)*(2*t+1)*data.y_1
            +Sqr(t)*(3-2*t)*data.y_2
            +t*Sqr(1-t)*data.yp_1
            +Sqr(t)*(1-t)*data.yp_2;
        public Vector3 GetTangent(float t)
            => 6*t*(t-1)*data.y_1
            +6*t*(1-t)*data.y_2
            +(t-1)*(3*t-1)*data.yp_1
            +t*(3*t-2)*data.yp_2;

        public Vector3[] GetNodes(int numDivisions, float t_min = 0, float t_max = 1)
        {
            Vector3[] nodes = new Vector3[numDivisions];
            for (int i = 0; i < nodes.Length; i++)
            {
                float t = t_min + (t_max-t_min) * i/(nodes.Length-1f);
                nodes[i] = GetPointAlong(t);
            }
            return nodes; 
        }

        public Bounds GetBounds()
        {
            return Bounds.FromPointCloud(GetNodes(96));
        }
        public bool Contains(Vector3 point)
        {

#if ANALYTICAL
            Vector3 S = Span;
            float s_1 = Vector3.Dot(S, Y1), s_2 = Vector3.Dot(S, Y2), sp_1 = Vector3.Dot(S, Yp1), sp_2 = Vector3.Dot(S, Yp2);
            float h = 1;
            float c_0 = s_1;
            float c_1 = h*sp_1;
            float c_2 = h*(sp_2-2*sp_1)-3*(s_1-s_2);
            float c_3 = h*(sp_1-sp_2) +2*(s_1-s_2);

            if (DoubleConstants.TryCubicRoot(c_0, c_1, c_2, c_3, out var t))
            {
                Vector3 hit = GetPointAlong((float)t);
                return Vector3.Distance(point, hit) <= tiny_sq;
            }
            return false;
#else
            var nodes = GetNodes(96);
            float ds = Span.Length()/nodes.Length;
            float distance = float.PositiveInfinity;
            int index = -1;
            for (int i = 0; i < nodes.Length; i++)
            {
                var d = Vector3.Distance(point, nodes[i]);
                if (d < distance)
                {
                    index = i;
                    distance = d;
                }
                if (d < ds)
                {
                    return true;
                }
            }
            return false;

#endif
        }
        public float GetDistanceTo(Vector3 point)
        {
            var nodes = GetNodes(96);
            float distance = float.PositiveInfinity;
            int index = -1;
            for (int i = 0; i < nodes.Length; i++)
            {
                var d = Vector3.Distance(point, nodes[i]);
                if (d < distance)
                {
                    index = i;
                    distance = d;
                }
            }
            if (index> -1)
            {
                float dt = 1/(nodes.Length-1f);
                return index*dt;                
            }
            return 0;
        }
        public Vector3 GetNormal(Vector3 point)
        {
            var t = GetDistanceTo(point);
            var p = GetPointAlong(t);
            var e = GetTangent(t);
            var k = Vector3.Cross(p, e);
            var n = Vector3.Cross(e, k);
            return Vector3.Normalize(n);
        }
        public bool Hit(Ray ray, out float distance, bool nearest = true)
        {
            const float h = 1.0f;
            var σ = Matrix4x4.Identity - Geometry.Outer(ray.Direction, ray.Direction);
            if (Matrix4x4.Invert(σ, out var σ_inv))
            {
                // note vector transform does v*A insted of A*v product, 
                // but since σ is symmetric the result is the same
                Vector3 c_0 = Y1 - Vector3.Transform(ray.Origin, σ_inv);
                Vector3 c_1 = h*Yp1;
                Vector3 c_2 = -3*(Y1-Y2)-h*(2*Yp1-Yp2);
                Vector3 c_3 = 2*(Y1-2*Y2+h*(Yp1-Yp2));

                if (DoubleConstants.TryCubicRoot(c_0.X, c_1.X, c_2.X, c_3.X, out var x)
                    && DoubleConstants.TryCubicRoot(c_0.Y, c_1.Y, c_2.Y, c_3.Y, out var y)
                    && DoubleConstants.TryCubicRoot(c_0.Z, c_1.Z, c_2.Z, c_3.Z, out var z))
                {
                    var hit = new Vector3((float)x, (float)y, (float)z);
                    distance = ray.GetDistanceTo(hit);
                    return true;
                }
            }

            distance = 0;
            return false;
        }
        public override string ToString() => $"{Y1}-{Y2}";
    }
}
