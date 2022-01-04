using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;

namespace JA
{
    public class Element
    {
        public Element(Color color, params int[] face)
        {
            Color = color;
            Face = face;
        }

        public int[] Face { get; }
        public Color Color { get; set; }
    }

    public class Mesh
    {
        public Mesh()
        {
            Position = Vector3.Zero;
            Orientation = Quaternion.Identity;
            Nodes = new List<Vector3>();
            Elements = new List<Element>();
        }
        public Vector3 Position { get; set; }
        public Quaternion Orientation { get; set; }
        public List<Vector3> Nodes { get; }
        public List<Element> Elements { get; }

        /// <summary>
        /// Gets the coordinates of the nodes of a face, applying the mesh transformation.
        /// </summary>
        /// <param name="index">The face index.</param>
        public Vector3[] GetNodes(int index)
        {
            var R = Matrix4x4.CreateFromQuaternion(Orientation);
            return Elements[index].Face.Select(ni => Position + Vector3.Transform(Nodes[ni],R)).ToArray();
        }
        /// <summary>
        /// Gets the normal vector of a face, applying the mesh transformation.
        /// </summary>
        /// <param name="index">The face index.</param>
        public Vector3[] GetNormals(int index)
        {
            return GetNormals(GetNodes(index));
        }

        /// <summary>
        /// Gets the normal vectors of a face at each node, applying the mesh transformation.
        /// </summary>
        /// <param name="nodes">The nodes of the face.</param>
        public Vector3[] GetNormals(Vector3[] nodes)
        {
            var normals = new Vector3[nodes.Length];
            for (int i = 0; i < nodes.Length; i++)
            {
                int j = (i + 1) % nodes.Length;
                int k = (i - 1 + nodes.Length) % nodes.Length;

                Vector3 A = nodes[i], B = nodes[j], C = nodes[k];

                normals[i] = Vector3.Normalize(
                    Vector3.Cross(A, B)
                    + Vector3.Cross(B, C)
                    + Vector3.Cross(C, A)
                    );
            }

            return normals;
        }
        /// <summary>
        /// Gets the average normal vector of a face, applying the mesh transformation.
        /// </summary>
        /// <param name="nodes">The nodes of the face.</param>
        public Vector3 GetNormal(Vector3[] nodes)
        {
            var list = GetNormals(nodes);
            Vector3 n = Vector3.Zero;
            for (int i = 0; i < list.Length; i++)
            {
                n += list[i];
            }
            return Vector3.Normalize(n);
        }

        /// <summary>
        /// Adds the face from a list of nodes.
        /// </summary>
        /// <param name="color">The face color.</param>
        /// <param name="nodes">The local face nodes.</param>
        public void AddFace(Color color,
            params Vector3[] nodes)
        {
            var elemIndex = new int[nodes.Length];
            for (int i = 0; i < nodes.Length; i++)
            {
                if (Nodes.Contains(nodes[i]))
                {
                    elemIndex[i] = Nodes.IndexOf(nodes[i]);
                }
                else
                {
                    elemIndex[i] = Nodes.Count;
                    Nodes.Add(nodes[i]);
                }
            }
            Elements.Add(new Element(color, elemIndex));
        }
        /// <summary>
        /// Adds a square panel as af face.
        /// </summary>
        /// <param name="color">The face color.</param>
        /// <param name="center">The center of the panel.</param>
        /// <param name="x_axis">The x-axis defining the direction of length.</param>
        /// <param name="length">The panel length.</param>
        /// <param name="width">The panel width.</param>
        public void AddPanel(Color color,
            Vector3 center,
            Vector3 x_axis,
            float length,
            float width)
        {
            x_axis = Vector3.Normalize(x_axis);
            Vector3 z_axis = Vector3.Normalize(center);
            Vector3 y_axis = Vector3.Cross(z_axis, x_axis);

            AddFace(color,
                center - length / 2 * x_axis - width / 2 * y_axis,
                center + length / 2 * x_axis - width / 2 * y_axis,
                center + length / 2 * x_axis + width / 2 * y_axis,
                center - length / 2 * x_axis + width / 2 * y_axis);
        }

        /// <summary>
        /// Creates a cube mesh from 6 panels.
        /// </summary>
        /// <param name="color">The face color.</param>
        /// <param name="size">The size of the cube.</param>
        public static Mesh CreateCube(Color color, float size)
        {
            var mesh = new Mesh();
            mesh.AddPanel(
                color,
                new Vector3(0, size/2, 0),
                Vector3.UnitX,
                size, size);
            mesh.AddPanel(
                color,
                new Vector3(0, -size/2, 0),
                Vector3.UnitX,
                size, size);
            mesh.AddPanel(
                color,
                new Vector3(size/2, 0, 0),
                Vector3.UnitZ,
                size, size);
            mesh.AddPanel(
                color,
                new Vector3(-size/2, 0, 0),
                Vector3.UnitZ,
                size, size);
            mesh.AddPanel(
                color,
                new Vector3(0, 0, size/2),
                Vector3.UnitX,
                size, size);
            mesh.AddPanel(
                color,
                new Vector3(0, 0, -size/2),
                Vector3.UnitX,
                size, size);
            return mesh;
        }


        /// <summary>
        /// Creates a square pyramid mesh from 5 panels.
        /// </summary>
        /// <param name="color">The face color.</param>
        /// <param name="base">The size of the base.</param>
        /// <param name="height">The height of the pyramid.</param>
        public static Mesh CreatePyramid(Color color, float @base, float height)
        {
            var mesh = new Mesh();
            mesh.AddPanel(color, Vector3.Zero, Vector3.UnitX, @base, @base);
            mesh.Nodes.Add(Vector3.UnitZ * height);
            mesh.Elements.Add(new Element(color, 4, 1, 0));
            mesh.Elements.Add(new Element(color, 4, 2, 1));
            mesh.Elements.Add(new Element(color, 4, 3, 2));
            mesh.Elements.Add(new Element(color, 4, 0, 3));

            return mesh;
        }
    }
}
