using System.Drawing;
using System.Numerics;

namespace JA
{
    public readonly struct Triangle
    {
        public Triangle(Color color, Vector3 a, Vector3 b, Vector3 c) : this()
        {
            Color = color;
            A = a;
            B = b;
            C = c;
        }

        public Vector3 A { get; }
        public Vector3 B { get; }
        public Vector3 C { get; }
        public Color Color { get; }
        public Vector3 Center { get => (A + B + C) / 3; }
        public Vector3 Normal
        {
            get => Vector3.Normalize(AreaVector);
        }
        public float Area
        {
            get => AreaVector.Length();
        }

        public Vector3 AreaVector
        {
            get => (Vector3.Cross(A, B)
             + Vector3.Cross(B, C)
             + Vector3.Cross(C, A)) / 2;
        }
    }
}
