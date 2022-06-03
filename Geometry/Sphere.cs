using JA.Drawing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace JA.Geometry
{
    using static SingleConstants;

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public readonly struct Sphere : 
        ISolid, 
        IRayTarget, 
        IEquatable<Sphere>
    {
        readonly (Vector3 center, float radius) data;

        #region Factory
        public Sphere(Vector3 center, float radius)
        {
            data = (center, radius);
        }
        public Sphere FromLocal(Pose pose) => new Sphere(pose.FromLocal(Center), Radius);
        public static Sphere FromCenterAndPoint(Vector3 center, Vector3 point)
        {
            return new Sphere(center, Vector3.Distance(point, center));
        }
        public static Sphere FromTwoDiameterPoints(Vector3 A, Vector3 B)
        {
            var center = (A+B)/2;
            return new Sphere(center, Vector3.Distance(A, center));
        }
        public static Sphere FromFourPoints(Vector3 A, Vector3 B, Vector3 C, Vector3 D)
        {
            float AA = A.LengthSquared(), BB = B.LengthSquared(), CC = C.LengthSquared(), DD = D.LengthSquared();
            Vector3 AB = A-B, BC = B-C, CD = C-D;
            Vector3 u = new Vector3(AA-BB, BB-CC, CC-DD)/2;

            // Solve: G*M = u
            var M = new Matrix4x4(
                AB.X, BC.X, CD.X, 0,
                AB.Y, BC.Y, CD.Y, 0,
                AB.Z, BC.Z, CD.Z, 0,
                0, 0, 0, 1);

            if (Matrix4x4.Invert(M, out var M_inv))
            {
                // Solution: G = u*M_inv
                Vector3 G = Vector3.Transform(u, M_inv);
                return FromCenterAndPoint(G, A);
            }
            else
            {
                // Four points are co-planar, use least sq. fit
                Vector3 G = (A+B+C+D)/4;
                float R = (Vector3.Distance(G, A) + Vector3.Distance(G, B) + Vector3.Distance(G, C) + Vector3.Distance(G, D))/4;
                return new Sphere(G, R);
            }
        } 
        #endregion

        #region Properties
        public Vector3 Center { get => data.center; }
        public float Radius { get => data.radius; }
        public float Diameter => 2*Radius;
        public float Volume { get => 4*pi*Radius*Radius*Radius/3; }
        #endregion

        #region Interactions
        public Vector3 GetNormal(Vector3 point)
        {
            return Vector3.Normalize(point-Center);
        }

        public Bounds GetBounds()
            => new Bounds(Center-(Vector3.One*Radius), Center+(Vector3.One*Radius));
        public bool Contains(Vector3 point)
        {
            return Vector3.Distance(Center, point)<=Radius;
        }
        public Vector3 GetClosestPoint(Vector3 point)
        {
            return Center + Radius * GetNormal(point);
        }
        public bool Hit(Ray ray, out float distance, bool nearest = true)
        {
            float α = Radius*Radius - Vector3.DistanceSquared(Center, ray.Origin);
            float β = Vector3.Dot(ray.Direction, Center - ray.Origin);
            float δ = α+β*β;
            if (δ>=0)
            {
                float m = Sqrt(δ);
                distance = nearest ? β - m : β + m;
                return true;
            }
            distance = ray.GetDistanceTo(Center);
            return false;
        }
        #endregion

        #region Equatable
        public override bool Equals(object obj)
        {
            return obj is Sphere sphere&&Equals(sphere);
        }

        public bool Equals(Sphere other)
        {
            return data.Equals(other.data);
        }

        public override int GetHashCode()
        {
            return 1768953197+data.GetHashCode();
        }

        public static bool operator ==(Sphere left, Sphere right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Sphere left, Sphere right)
        {
            return !(left==right);
        }


        #endregion

        public override string ToString()
        {
            return $"(cen={Center},R={Radius})";
        }

    }
}
