using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace JA.Model
{
    public readonly struct Polygon
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
        }

        public Vector3[] Nodes { get; }
        public bool IsConvex { get; }

        public Vector3 Center
        {
            get
            {
                int count = Nodes.Length;
                return Nodes.Aggregate(Vector3.Zero, (cen, node) => cen+node/count);
            }
        }

        public Vector3 Normal { get; }

        public Triangle[] GetTriangles()
        {
            var list = new List<Triangle>();
            if (IsConvex)
            {
                // use fan method 
                var P = Center;
                for (int i = 0; i < Nodes.Length; i++)
                {
                    int j = (i+1)%Nodes.Length;
                    list.Add(new Triangle(P, Nodes[i], Nodes[j]));
                }
            }
            else
            {
                throw new NotImplementedException();
            }
            return list.ToArray();
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
    }
}
