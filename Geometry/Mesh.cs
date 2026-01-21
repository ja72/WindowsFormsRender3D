using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Numerics;

namespace JA.Geometry
{
    using System.ComponentModel;
    using JA.Drawing;

    public class Face
    {
        public Face(params int[] nodes)
        {
            NodeIndex = nodes;
        }

        public int[] NodeIndex { get; }

        public void Flip()
        {
            Array.Copy(NodeIndex.Reverse().ToArray(), NodeIndex, NodeIndex.Length);
        }
        public override string ToString()
        {
            return $"Face({string.Join(",", NodeIndex)})";
        }
    }
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class Mesh :
        ISolid,
        IRayTarget,
        IEnumerable<Polygon>
    {
        private readonly List<Vector3> nodes;
        private readonly List<Face> faces;
        #region Factory
        public Mesh()
        {
            nodes = new List<Vector3>();
            faces = new List<Face>();
            Center = Vector3.Zero;
            Volume = 0;
        }

        public Mesh(IEnumerable<Vector3> nodes, IEnumerable<Face> faces)
        {
            this.nodes= new List<Vector3>(nodes);
            this.faces =new List<Face>(faces);
            CalculateVolumeProperties();
        }

        #endregion

        #region Properties
        public IEnumerator<Polygon> GetEnumerator()
        {
            for (int i = 0; i < faces.Count; i++)
            {
                yield return GetPolygon(i);
            }
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        public IReadOnlyList<Vector3> Nodes => nodes;
        public IReadOnlyList<Face> Faces => faces;
        public float Volume { get; private set; }
        public Vector3 Center { get; private set; }

        #endregion

        #region Inquiry
        protected void CalculateVolumeProperties()
        {
            Vector3 cg = Vector3.Zero;
            float V = 0;

            foreach (var poly in this)
            {
                foreach (var trig in poly.GetTriangles(false))
                {
                    var A = trig.A;
                    var B = trig.B;
                    var C = trig.C;

                    float dV = Vector3.Dot(A, Vector3.Cross(B, C))/6;
                    V += dV;
                    Vector3 dCG = (A+B+C)/4;
                    cg += dV*dCG;
                }
            }
            Volume = V;
            Center = cg/V;
        }

        /// <summary>
        /// Gets the coordinates of the nodes of a face, applying the mesh transformation.
        /// </summary>
        /// <param name="index">The face index.</param>
        public Vector3[] GetNodes(int index)
        {
            //var R = Matrix4x4.CreateFromQuaternion(pose.Orientation);
            return faces[index].NodeIndex.Select(ni => Nodes[ni]).ToArray();
        }
        /// <summary>
        /// Gets the normal vector of a face, applying the mesh transformation.
        /// </summary>
        /// <param name="index">The face index.</param>
        public Vector3[] GetNormals(int index)
        {
            return GetNormals(GetNodes(index));
        }

        public Polygon GetPolygon(int index) => new Polygon(GetNodes(index));

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

                normals[i] = (
                    Vector3.Cross(A, B)
                    + Vector3.Cross(B, C)
                    + Vector3.Cross(C, A)
                    ).Unit();
            }

            return normals;
        }
        /// <summary>
        /// Gets the average normal vector of a face, applying the mesh transformation.
        /// </summary>
        /// <param name="nodes">The nodes of the face.</param>
        public Vector3 AverageNormal(Vector3[] nodes)
        {
            var list = GetNormals(nodes);
            Vector3 n = Vector3.Zero;
            for (int i = 0; i < list.Length; i++)
            {
                n += list[i];
            }
            return n.Unit();
        }
        public Bounds GetBounds()
            => Bounds.FromPointCloud(Nodes.ToArray());

        public bool Contains(Vector3 point)
            => this.Any((poly) => poly.Contains(point));

        public Vector3 GetNormal(Vector3 point)
        {
            foreach (var poly in this)
            {
                if (poly.Contains(point))
                {
                    return poly.Normal;
                }
            }
            return Vector3.Zero;
        }
        public bool Hit(Ray ray, out float distance, bool nearest = true)
        {
            distance = nearest ? float.PositiveInfinity : float.NegativeInfinity;
            foreach (var poly in this)
            {
                if (poly.Hit(ray, out var temp, nearest))
                {
                    distance = nearest ? Math.Min(distance, temp) : Math.Max(distance, temp);
                }
            }
            return !float.IsInfinity(distance);
        }

        #endregion

        #region Modifications
        public int AddNode(Vector3 newNode)
        {
            int index = nodes.IndexOf(newNode);
            int count = nodes.Count;
            if (index >= 0)
            {
                return index;
            }
            else
            {
                nodes.Add(newNode);
                return count;
            }
        }
        public int[] AddNodes(params Vector3[] newNodes) => AddNodes(newNodes.AsEnumerable());
        public int[] AddNodes(IEnumerable<Vector3> newNodes)
        {
            var nodeIndex = new List<int>();
            foreach (var node in newNodes)
            {
                nodeIndex.Add(AddNode(node));
            }
            return nodeIndex.ToArray();
        }

        public int[] AddFace(params int[] nodeIndex)
        {
            return AddFace(nodeIndex.AsEnumerable());
        }
        /// <summary>
        /// Adds a face from a list of nodes. It ignores node index that are out of bounds.
        /// </summary>
        /// <param name="nodeIndexList"></param>
        /// <returns>The list of nodes it the new face.</returns>
        public int[] AddFace(IEnumerable<int> nodeIndexList)
        {
            var list = new List<int>();
            foreach (var item in nodeIndexList)
            {
                if (item>= 0 && item < nodes.Count)
                {
                    list.Add(item);
                }
            }
            var array = list.ToArray();
            var face = new Face(array);
            faces.Add(face);
            CalculateVolumeProperties();
            return array;
        }
        /// <summary>
        /// Adds the face from a list of nodes.
        /// </summary>
        /// <returns>The list of node indexes</returns>
        public int[] AddFace(params Vector3[] nodes)
        {
            var list = AddNodes(nodes);
            return AddFace(list);
        }

        public int[][] AddFaces(params int[][] nodeIndexList)
        {
            var elemIndex = new List<int[]>();
            foreach (var nodeIndex in nodeIndexList)
            {
                elemIndex.Add(AddFace(nodeIndex).ToArray());
            }
            CalculateVolumeProperties();
            return elemIndex.ToArray();
        }
        public int[][] AddFaces(IEnumerable<int[]> nodeIndexList)
        {
            var elemIndex = new List<int[]>();
            foreach (var nodeIndex in nodeIndexList)
            {
                var element = new Face(nodeIndex);
                faces.Add(element);
                elemIndex.Add(nodeIndex);
            }
            CalculateVolumeProperties();
            return elemIndex.ToArray();
        }

        /// <summary>
        /// Adds a square panel as af face.
        /// </summary>
        /// <param name="color">The face color.</param>
        /// <param name="center">The center of the panel.</param>
        /// <param name="x_axis">The x-axis defining the direction of length.</param>
        /// <param name="length">The panel length.</param>
        /// <param name="width">The panel width.</param>
        public int[] AddPanel(
            Vector3 center,
            Vector3 x_axis,
            float length,
            float width)
        {
            x_axis = x_axis.Unit();
            Vector3 z_axis = center == Vector3.Zero ? Vector3.UnitZ : center.Unit();
            Vector3 y_axis = Vector3.Cross(z_axis, x_axis);

            return AddFace(
                center - length / 2 * x_axis - width / 2 * y_axis,
                center + length / 2 * x_axis - width / 2 * y_axis,
                center + length / 2 * x_axis + width / 2 * y_axis,
                center - length / 2 * x_axis + width / 2 * y_axis);
        }

        #endregion

        public override string ToString()
        {
            return $"|Faces={faces.Count}, Center={Center}, Volume={Volume}|";
        }
    }

    public static class Meshes
    {
        /// <summary>
        /// Creates a cube mesh from 6 panels.
        /// </summary>
        /// <param name="color">The face color.</param>
        /// <param name="sizeX">The size of the cube in the x-axis.</param>
        /// <param name="sizeY">The size of the cube in the y-axis.</param>
        /// <param name="sizeZ">The size of the cube in the z-axis.</param>
        public static Mesh CreateCube(float sizeX, float sizeY, float sizeZ)
        {
            var mesh = new Mesh();
            mesh.AddPanel(
                new Vector3(0, sizeY/2, 0),
                Vector3.UnitX,
                sizeX, sizeZ);
            mesh.AddPanel(
                new Vector3(0, -sizeY/2, 0),
                Vector3.UnitX,
                sizeX, sizeZ);
            mesh.AddPanel(
                new Vector3(sizeX/2, 0, 0),
                Vector3.UnitZ,
                sizeZ, sizeY);
            mesh.AddPanel(
                new Vector3(-sizeX/2, 0, 0),
                Vector3.UnitZ,
                sizeZ, sizeY);
            mesh.AddPanel(
                new Vector3(0, 0, sizeZ/2),
                Vector3.UnitX,
                sizeX, sizeY);
            mesh.AddPanel(
                new Vector3(0, 0, -sizeZ/2),
                Vector3.UnitX,
                sizeX, sizeY);
            return mesh;
        }
        /// <summary>
        /// Creates a cube mesh from 6 panels.
        /// </summary>
        /// <param name="color">The face color.</param>
        /// <param name="size">The size of the cube in all directions.</param>
        public static Mesh CreateCube(float size)
            => CreateCube(size, size, size);

        /// <summary>
        /// Creates a square pyramid mesh from 5 panels.
        /// </summary>
        /// <param name="color">The face color.</param>
        /// <param name="side">The size of the base.</param>
        /// <param name="height">The height of the pyramid.</param>
        public static Mesh CreatePyramid(float side, float height)
        {
            var mesh = new Mesh();
            float half = side/2;

            var botIndex = mesh.AddFace(
                new Vector3(-half, -half, 0),
                new Vector3(half, -half, 0),
                new Vector3(half, half, 0),
                new Vector3(-half, half, 0));

            var apexIndex = mesh.AddNode(height*Vector3.UnitZ);

            mesh.AddFace(apexIndex, botIndex[0], botIndex[1]);
            mesh.AddFace(apexIndex, botIndex[1], botIndex[2]);
            mesh.AddFace(apexIndex, botIndex[2], botIndex[3]);
            mesh.AddFace(apexIndex, botIndex[3], botIndex[0]);

            return mesh;
        }
        /// <summary>
        /// Verbindet dieses Mesh mit einem anderen Mesh (statische Hilfsfunktion).
        /// </summary>
        public static Mesh Merge(Mesh baseMesh, Mesh addedMesh, Vector3 offset)
        {
            // --- SCHRITT 1: Bereinigung der Nodes (Punkte) ---
            // Wir erstellen eine Liste für die finalen, einzigartigen Punkte.
            List<Vector3> uniqueNodes = new List<Vector3>();

            // Diese Hilfsfunktion sucht einen Punkt in der Liste. 
            // Wenn er existiert, gibt sie den Index zurück. Wenn nicht, fügt sie ihn hinzu.
            // Wir nutzen eine kleine Toleranz (0.0001f), damit 0.999999 als 1.0 erkannt wird.
            int GetOrAddNodeIndex(Vector3 p)
            {
                for (int i = 0; i < uniqueNodes.Count; i++)
                {
                    // Abstand quadratisch prüfen (schneller als Wurzel ziehen)
                    float dx = uniqueNodes[i].X - p.X;
                    float dy = uniqueNodes[i].Y - p.Y;
                    float dz = uniqueNodes[i].Z - p.Z;

                    if ((dx * dx + dy * dy + dz * dz) < 0.0001f)
                        return i; // Punkt existiert schon! Index zurückgeben.
                }

                // Punkt ist neu -> hinzufügen
                uniqueNodes.Add(p);
                return uniqueNodes.Count - 1;
            }

            // --- SCHRITT 2: Flächen sammeln und Indizes neu berechnen ---
            // Wir speichern alle Flächen erstmal in einer temporären Liste
            List<Face> tempFaces = new List<Face>();

            // A) Flächen vom Basis-Mesh verarbeiten
            foreach (var face in baseMesh.Faces)
            {
                int[] newIndices = new int[face.NodeIndex.Length];
                for (int i = 0; i < face.NodeIndex.Length; i++)
                {
                    // Alten Punkt holen
                    Vector3 p = baseMesh.Nodes[face.NodeIndex[i]];
                    // In die neue "saubere" Liste einsortieren
                    newIndices[i] = GetOrAddNodeIndex(p);
                }
                Face f = new Face(newIndices);
                tempFaces.Add(f);
            }

            // B) Flächen vom neuen Mesh verarbeiten (mit Offset!)
            foreach (var face in addedMesh.Faces)
            {
                int[] newIndices = new int[face.NodeIndex.Length];
                for (int i = 0; i < face.NodeIndex.Length; i++)
                {
                    // Alten Punkt holen UND Offset draufrechnen
                    Vector3 p = addedMesh.Nodes[face.NodeIndex[i]] + offset;
                    // In die neue "saubere" Liste einsortieren
                    newIndices[i] = GetOrAddNodeIndex(p);
                }
                Face f = new Face(newIndices);
                tempFaces.Add(f);
            }

            // --- SCHRITT 3: Doppelte (innere) Flächen entfernen ---
            // Jetzt haben wir alle Flächen, aber Deckel und Boden sind noch da.
            // Da wir die Punkte verschmolzen haben, teilen sich Deckel und Boden 
            // jetzt EXAKT die gleichen Indizes (z.B. 4,5,6,7), evtl. in anderer Reihenfolge.

            List<Face> finalFaces = new List<Face>();

            // Liste von Flächen, die wir überspringen (löschen) wollen
            bool[] skipFace = new bool[tempFaces.Count];

            for (int i = 0; i < tempFaces.Count; i++)
            {
                if (skipFace[i]) continue; // Schon als Duplikat markiert

                for (int j = i + 1; j < tempFaces.Count; j++)
                {
                    if (skipFace[j]) continue;

                    // Prüfen ob Fläche i und Fläche j identisch sind (gleiche Punkte)
                    if (AreFacesOverlapping(tempFaces[i], tempFaces[j]))
                    {
                        // TREFFER! Das ist eine innere Wand (z.B. Würfel-Oben und Pyramide-Unten).
                        // Wir markieren BEIDE zum Löschen, damit der Körper hohl wird.
                        skipFace[i] = true;
                        skipFace[j] = true;
                        break; // Wir haben den Partner gefunden, innere Schleife abbrechen
                    }
                }
            }

            // Nur die nicht-markierten Flächen übernehmen
            for (int i = 0; i < tempFaces.Count; i++)
            {
                if (!skipFace[i]) finalFaces.Add(tempFaces[i]);
            }

            return new Mesh(uniqueNodes.ToArray(), finalFaces.ToArray());
        }

        // Hilfsfunktion: Prüft ob zwei Flächen die gleichen Punkte nutzen
        private static bool AreFacesOverlapping(Face f1, Face f2)
        {
            // Wenn Anzahl der Ecken ungleich, können sie nicht gleich sein
            if (f1.NodeIndex.Length != f2.NodeIndex.Length) return false;

            // Wir sortieren die Indizes temporär, um sie zu vergleichen.
            // (Denn [1,2,3,4] ist geometrisch das gleiche wie [4,3,2,1])
            var sorted1 = new List<int>(f1.NodeIndex);
            sorted1.Sort();

            var sorted2 = new List<int>(f2.NodeIndex);
            sorted2.Sort();

            for (int k = 0; k < sorted1.Count; k++)
            {
                if (sorted1[k] != sorted2[k]) return false; // Unterschied gefunden
            }

            return true; // Alle Indizes sind gleich!
        }
    }

}
