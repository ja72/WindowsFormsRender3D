using System;
using System.Drawing;
using System.Numerics;

namespace JA.Model
{
    public readonly struct Triangle
    {
        public const float DistanceTolerance = 1e-6f;
        public Triangle(Vector3 a, Vector3 b, Vector3 c) : this()
        {
            A = a;
            B = b;
            C = c;
        }

        public Vector3 A { get; }
        public Vector3 B { get; }
        public Vector3 C { get; }
        public Vector3 Center { get => (A + B + C) / 3; }
        public Vector3 Normal
        {
            get => AreaVector.Unit();
        }
        public float Area
        {
            get => AreaVector.Length();
        }

        private Vector3 AreaVector
        {
            get => (Vector3.Cross(A, B)+ Vector3.Cross(B, C)+ Vector3.Cross(C, A)) / 2;
        }

        public float DistanceTo(Vector3 point) => Vector3.Dot(Normal, point-A);

        public Vector3 Project(Vector3 point)
        {
            var n = Normal;
            var d = Vector3.Dot(n, point-A);
            return point - n*d;
        }
        public void Barycentric(Vector3 P, out (float w_A, float w_B, float w_C) coord)
        {
            Vector3 n = Vector3.Cross(A, B)+ Vector3.Cross(B, C)+ Vector3.Cross(C, A);
            float w_A = Vector3.Dot(n, Vector3.Cross(P, B)+ Vector3.Cross(B, C)+ Vector3.Cross(C, P));
            float w_B = Vector3.Dot(n, Vector3.Cross(A, P)+ Vector3.Cross(P, C)+ Vector3.Cross(C, A));
            float w_C = Vector3.Dot(n, Vector3.Cross(A, B)+ Vector3.Cross(B, P)+ Vector3.Cross(P, A));
            float sum = w_A + w_B + w_C;
            coord = (w_A/sum, w_B/sum, w_C/sum);
        }

        public bool Contains(Vector3 P)
        {
            if (Math.Abs(DistanceTo(P))<=DistanceTolerance)
            {
                Barycentric(P, out var coord);
                return coord.w_A>=0 && coord.w_A<=1
                    && coord.w_B>=0 && coord.w_B<=1
                    && coord.w_C>=0 && coord.w_C<=1;
            }
            return false;
        }
    }
}
