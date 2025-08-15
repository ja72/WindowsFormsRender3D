using JA.Drawing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;

namespace JA.Geometry
{

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public readonly struct Polygon :
        ISurface,
        IRayTarget,
        IEquatable<Polygon>
    {
        public Polygon(params Vector3[] nodes)
        {
            Nodes=nodes;

            if (nodes.Length>=3)
            {
                Normal = new Triangle(nodes[0], nodes[1], nodes[2]).Normal;
                IsConvex = nodes.Length==3 || CheckConvex(nodes);
            }
            else
            {
                IsConvex = true;
                Normal = Vector3.Zero;
            }
            int count = nodes.Length;            
            Center = nodes.Aggregate(Vector3.Zero, (cen, node) => cen+node/count);
            // use fan method
            Area = 0f;
            for (int i = 0; i < Nodes.Length; i++)
            {
                int j = (i+1)%Nodes.Length;
                var triangle = new Triangle(Center, Nodes[i], Nodes[j]);
                Area += triangle.Area;
            }
        }
        public Polygon FromLocal(Pose pose)
            => new Polygon(pose.FromLocal(Nodes));
        public Vector3[] Nodes { get; }
        public bool IsConvex { get; }
        public float Area { get; }
        public Vector3 Center { get; }

        public Vector3 Normal { get; }

        public Triangle[] GetTriangles(bool requieConvex = true)
        {
            var triangles = new List<Triangle>();
            if (!requieConvex || (requieConvex && IsConvex))
            {
                // use fan method 
                var P = Center;
                for (int i = 0; i < Nodes.Length; i++)
                {
                    int j = (i+1)%Nodes.Length;
                    triangles.Add(new Triangle(P, Nodes[i], Nodes[j]));
                }
            }
            else
            {
                throw new NotImplementedException();
            }
            return triangles.ToArray();
        }

        static bool CheckConvex(Vector3[] nodes)
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                int j = (i+1)%nodes.Length;
                int k = (i+2)%nodes.Length;

                var trig = new Triangle(nodes[i], nodes[j], nodes[k]);
                for (int r = 3; r < nodes.Length; r++)
                {
                    var P = nodes[(r+i)%nodes.Length];
                    if (trig.Contains(P))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public Bounds GetBounds() => Bounds.FromPointCloud(Nodes);
        public bool Contains(Vector3 point)
            => GetTriangles(false).Any((trig) => trig.Contains(point));

        public Vector3 GetNormal(Vector3 point)
        {
            if (Contains(point))
            {
                return Normal;
            }
            return Vector3.Zero;
        }
        public bool Hit(Ray ray, out float distance, bool nearest = true)
        {
            var trig = GetTriangles(false);
            for (int i = 0; i < trig.Length; i++)
            {
                if (trig[i].Hit(ray, out distance, nearest))
                {
                    return true;
                }
            }
            distance = 0;
            return false;
        }

        public override bool Equals(object? obj)
        {
            return obj is Polygon polygon && Equals(polygon);
        }
        public bool Equals(Polygon other)
        {
            return Nodes.SequenceEqual(other.Nodes);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return Nodes.Aggregate(-1474169816, (hc, n) => hc*-1521134295 + n.GetHashCode());
            }
        }
    }
}
