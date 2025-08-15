using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using JA.Drawing;

namespace JA.Geometry
{
    using static SingleConstants;

    public readonly struct Hemisphere : 
        ISolid,
        IRayTarget,
        IEquatable<Hemisphere>
    {
        public Hemisphere(float radius, Vector3 center, Vector3 direction) : this()
        {
            Radius = radius;
            Center = center;
            Direction = Vector3.Normalize(direction);
        }

        public float Radius { get; }
        public Vector3 Center { get; }
        public Vector3 Direction { get; }
        public float Volume { get => 4*pi*Radius*Radius*Radius/6; }

        public Vector3 GetPointClosestTo(Vector3 target)
        {
            if (Vector3.Dot(Direction, target - Center) < 0)
            {
                Vector3 n = Vector3.Cross(Direction, target - Center);
                // only one point on the dia. circle is closest to the target
                n = Vector3.Normalize(n);
                return Center + Radius * Vector3.Cross(n, Direction);
            }
            else
            {
                // hemisphere directed towards target, so closest point
                // is on the sphere surface.
                return Center + Radius * Vector3.Normalize(target - Center);
            }
        }

        public bool Contains(Vector3 target)
        {
            if (Vector3.Dot(Direction, target - Center) < 0)
            {
                // hemisphere points away from target
                return false;
            }
            return Vector3.Distance(target, Center) <= Radius;
        }
        public Bounds GetBounds()
        {
            // Just return the sphere bounds for now
            return new Bounds(Center-(Vector3.One*Radius), Center+(Vector3.One*Radius));
        }

        public static bool Intersect(Hemisphere h_1, Hemisphere h_2)
        {
            Vector3 p_1 = h_1.GetPointClosestTo(h_2.Center);
            Vector3 p_2 = h_2.GetPointClosestTo(h_1.Center);

            float d_12 = Vector3.Distance(p_1, h_2.Center);
            float d_21 = Vector3.Distance(p_2, h_1.Center);
            return d_12 <= h_2.Radius && d_21 <= h_1.Radius;
        }

        public Vector3 GetNormal(Vector3 point)
        {
            if (Vector3.Dot(Direction, point - Center) < 0)
            {
                return -Direction;
            }
            return Vector3.Normalize(point-Center);
        }

        public bool Hit(Ray ray, out float distance, bool nearest = true)
        {
            if (Vector3.Dot(ray.Direction, Direction) < 0)
            {
                var plane = new Plane(Direction, Vector3.Dot(Center, Direction));
                if (ray.Intersect(plane, out distance))
                {
                    var point = ray.GetPointAlong(distance);
                    return Vector3.Distance(point, Center) <= Radius;
                }
                return false;
            }
            var sphere = new Sphere(Center, Radius);
            if (sphere.Hit(ray, out distance, nearest))
            {
                return true;
            }
            return false;
        }


        public override string ToString()
        {
            return $"Hemisphere(Center={Center}, Radius={Radius}, Direction={Direction})";
        }

        #region Equality
        public override bool Equals(object? obj)
        {
            return obj is Hemisphere hemisphere&&Equals(hemisphere);
        }
        public bool Equals(Hemisphere hemisphere)
        {
            return Radius==hemisphere.Radius&&
                   Center.Equals(hemisphere.Center)&&
                   Direction.Equals(hemisphere.Direction);
        }

        public override int GetHashCode()
        {
            int hashCode = -856764207;
            hashCode=hashCode*-1521134295+Radius.GetHashCode();
            hashCode=hashCode*-1521134295+Center.GetHashCode();
            hashCode=hashCode*-1521134295+Direction.GetHashCode();
            return hashCode;
        }

        #endregion
    }
}
