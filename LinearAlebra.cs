using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace JA
{
    public static class LinearAlebra
    {
        public static Vector3 Sum(this Vector3[] vectors)
        {
            var sum = Vector3.Zero;
            for (int i = 0; i < vectors.Length; i++)
            {
                sum += vectors[i];
            }
            return sum;
        }
        public static Vector3 Average(this Vector3[] vectors)
            => Sum(vectors) / vectors.Length;

        public static Vector3 GetNormal(Vector3 A, Vector3 B, Vector3 C)
        {
            return Vector3.Normalize(
                Vector3.Cross(A, B)
                + Vector3.Cross(B, C)
                + Vector3.Cross(C, A)
                );
        }
    }
}
