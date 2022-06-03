using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace JA.Geometry
{
    using static JA.SingleConstants;
    public readonly struct Bounds
    {
        readonly (Vector3 min, Vector3 max) data;

        public Bounds(Vector3 min, Vector3 max)
        {
            this.data=(min, max);
        }

        public static Bounds FromPointCloud(params Vector3[] nodes)
        {
            Vector3 min = nodes[0], max = min;
            for (int i = 1; i < nodes.Length; i++)
            {
                min = new Vector3(
                    Math.Min(min.X, nodes[i].X),
                    Math.Min(min.Y, nodes[i].Y),
                    Math.Min(min.Z, nodes[i].Z));
                max = new Vector3(
                    Math.Max(max.X, nodes[i].X),
                    Math.Max(max.Y, nodes[i].Y),
                    Math.Max(max.Z, nodes[i].Z));
            }
            return new Bounds(min, max);
        }

        public Vector3[] GetNodes()
        {
            var nodes = new Vector3[8];
            nodes[0] = new Vector3(data.min.X, data.min.Y, data.min.Z);
            nodes[1] = new Vector3(data.max.X, data.min.Y, data.min.Z);
            nodes[2] = new Vector3(data.min.X, data.max.Y, data.min.Z);
            nodes[3] = new Vector3(data.max.X, data.max.Y, data.min.Z);
            nodes[4] = new Vector3(data.min.X, data.min.Y, data.max.Z);
            nodes[5] = new Vector3(data.max.X, data.min.Y, data.max.Z);
            nodes[6] = new Vector3(data.min.X, data.max.Y, data.max.Z);
            nodes[7] = new Vector3(data.max.X, data.max.Y, data.max.Z);

            return nodes;
        }

        public Vector3 MinVector { get => data.min; }
        public Vector3 MaxVector { get => data.max; }
        public Vector3 Span { get => data.max- data.min; }
        public Vector3 Center { get => (data.max+data.min)/2; }
    }
}
