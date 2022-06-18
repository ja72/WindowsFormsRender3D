using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JA
{
    using Complex = System.Numerics.Complex;

    public enum Axis
    {
        X, Y, Z,
    }

    public static class SingleConstants
    {
        public const float ulp = 1f / 8388608;
        public const float pi = (float)Math.PI;
        public const float deg = pi / 180;
        public const float tiny = 4 * ulp;
        public const float small = 16* ulp;



        #region Functions
        public static float Random(float minValue=0, float maxValue=1) => minValue + (float)( (maxValue-minValue) * DoubleConstants.rng.NextDouble());
        public static float Cos(float θ) => (float)Math.Cos(θ);
        public static float Sin(float θ) => (float)Math.Sin(θ);
        public static float Tan(float θ) => (float)Math.Tan(θ);
        public static float Asin(float t) => (float)Math.Asin(t);
        public static float Acos(float t) => (float)Math.Acos(t);
        public static float Atan(float t) => (float)Math.Atan(t);
        public static float Atan(float dy, float dx) => (float)Math.Atan2(dy, dx);
        public static float Sqr(float x) => x*x;
        public static float Sqrt(float x) => (float)Math.Sqrt(x);
        public static float Pow(float x, float y) => (float)Math.Pow(x, y);
        public static float Raise(float x, int e)
        {
            if (e == 0) return 1;
            if (e == 1) return x;
            if (e == 2) return x * x;
            if (e < 0) return 1 / Raise(x, -e);
            return Pow(x, e);
        }
        #endregion

        #region Roots


        #endregion
    }
    public static class DoubleConstants
    {
        public const double ulp = 1.0 / 2251799813685248;
        public const double pi = Math.PI;
        public const double deg = pi / 180;

        public const double tiny = 64 * ulp;
        public const double small = 2048 * ulp;

        internal static readonly Random rng = new Random();

        #region Functions
        public static double Random(double minValue=0, double maxValue=1) => minValue + (maxValue-minValue) * rng.NextDouble();
        public static double Sqr(double x) => x*x;
        public static double Sqrt(double x) => Math.Sqrt(x);

        public static double Raise(double x, int e)
        {
            if (e == 0) return 1;
            if (e == 1) return x;
            if (e == 2) return x * x;
            if (e < 0) return 1 / Raise(x, -e);
            return Math.Pow(x, e);
        }

        #endregion

        #region Roots

        public static (Complex r_1, Complex r_2) QuadraticRoot(double c_0, double c_1, double c_2)
        {
            if (c_0!=0)
            {
                double β = -c_1/(2*c_0);
                double α = c_2/c_0;
                double d = β*β-α;
                Complex d_sqrt = Complex.Sqrt(d);
                Complex r_1 = (-d_sqrt+β)/α;
                Complex r_2 = (d_sqrt+β)/α;
                return (r_1, r_2);
            }
            else if (c_2!=0)
            {
                Complex r_1 = -c_1/c_2;
                return (r_1, r_1);
            }
            else
            {
                return (Complex.Zero, Complex.Zero);
            }
        }

        public static (Complex r_1, Complex r_2, Complex r_3) CubicRoot(double c_0, double c_1, double c_2, double c_3)
        {
            if (c_3 == 0)
            {
                var qroot =  QuadraticRoot(c_0, c_1, c_2);
                return (qroot.r_1, qroot.r_2, qroot.r_1);
            }
            // Step 1. Form cubic as c_0+c_1 x+c_2 x²-x³=0
            c_0 /= -c_3;
            c_1 /= -c_3;
            c_2 /= -c_3;
            // Step 2. Form the eigenvalue problem as b_0+b_1 z - z­³ = 0
            double b_0 = c_0+c_1*c_2/3 + 2*c_2*c_2/27;
            double b_1 = c_1 + c_2*c_2/3;
            // Step 3. Solve this problem in terms of z = √(b_1/3) (u+1/u)
            double d = 27*b_0*b_0-4*b_1*b_1*b_1;
            Complex d_sqrt = Complex.Sqrt(d);
            Complex u_3 = (3*Sqrt(3)*b_0+d_sqrt)/(2* Complex.Pow(Complex.Sqrt(b_1), 3));
            Complex u = Complex.Pow(u_3, 1/3f);
            Complex r_1 = 1*u;
            Complex r_2 = (-0.5f+Sqrt(3)*Complex.ImaginaryOne/2)*u;
            Complex r_3 = (-0.5f-Sqrt(3)*Complex.ImaginaryOne/2)*u;
            return (r_1, r_2, r_3);
        }

        public static bool TryQuadraticRoot(double c_0, double c_1, double c_2, out double x, bool alternate = false)
        {
            if (c_0!=0)
            {
                double β = -c_1/(2*c_0);
                double α = c_2/c_0;
                double d = β*β-α;
                if (d>=0)
                {
                    x = alternate ? (Sqrt(d)+β)/α : (-Sqrt(d)+β)/α;
                    return true;
                }
                x = β/α;
                return false;
            }
            else if (c_2!=0)
            {
                x = alternate ? 0 : -c_1/c_2;
                return true;
            }
            else
            {
                x = 0;
                return c_1!=0;
            }
        }
        public static bool TryCubicRoot(double c_0, double c_1, double c_2, double c_3, out double x, bool alternate = false)
        {
            int sign = alternate ? -1 : 1;

            if (c_0!=0)
            {

                var a = c_1 / c_0;
                var b = c_2 / c_0;
                var c = c_3 / c_0;

                double g2 = 4 * a * c - b * b;
                double k = b * b * b + 3 * b * g2 + 24 * c * c;
                double g6 = g2 * g2 * g2;
                double d = 4 * g6 + k * k;
                if (d>=0)
                {
                    double u3 = (sign * Math.Sqrt(d) + k) / 2;
                    double u = Math.Pow(u3, 1 / 3.0);
                    x = u / (2 * c) + (b * b - 4 * a * c) / (2 * c * u) - b / (2 * c);
                    return true;
                }
                else
                {
                    double u = Math.Pow(k / 2, 1 / 3.0);
                    x = u / (2 * c) + (b * b - 4 * a * c) / (2 * c * u) - b / (2 * c);
                    return false;
                }
            }
            else if (TryQuadraticRoot(c_1, c_2, c_3, out x, alternate))
            {
                return true;
            }
            else
            {
                return false;
            }
        } 

        #endregion
    }
}
