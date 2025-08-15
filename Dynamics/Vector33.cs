using System;
using System.ComponentModel;

namespace JA.Dynamics
{
    public enum Coordinates
    {
        Axis,
        Ray
    }
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public readonly struct Vector33 : IEquatable<Vector33>
    {
        readonly (Vector3 tra, Vector3 rot) data;

        public Vector33(Vector3 translational, Vector3 rotational)
        {
            this.data=(translational, rotational);
        }
        public static readonly Vector33 Zero = new Vector33(Vector3.Zero, Vector3.Zero);

        public static Vector33 TwistAt(Vector3 value, Vector3 position, double pitch = 0)
            => new Vector33(Vector3.Cross(position, value) + pitch*value, value);
        public static Vector33 TwistAt(Vector3 value, Vector3 position, Vector3 moment)
            => new Vector33(Vector3.Cross(position, value) + moment, value);
        public static Vector33 PureTwist(Vector3 value) => new Vector33(value, Vector3.Zero);
        public static Vector33 WrenchAt(Vector3 value, Vector3 position, double pitch = 0)
            => new Vector33(value, Vector3.Cross(position, value) + pitch*value);
        public static Vector33 WrenchAt(Vector3 value, Vector3 position, Vector3 moment)
            => new Vector33(value, Vector3.Cross(position, value) + moment);
        public static Vector33 PureWrench(Vector3 value) => new Vector33(Vector3.Zero, value);

        #region Properties
        public Vector3 Translational { get => data.tra; }
        public Vector3 Rotational { get => data.rot; } 
        #endregion

        #region Equality
        public override bool Equals(object? obj)
        {
            return obj is Vector33 vector && Equals(vector);
        }
        public bool Equals(Vector33 vector) => data.Equals(vector.data);

        public override int GetHashCode()
        {
            return 1768953197+data.GetHashCode();
        }

        public static bool operator ==(Vector33 left, Vector33 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Vector33 left, Vector33 right)
        {
            return !(left==right);
        }
        #endregion

        #region Formatting
        public override string ToString() => ToString("g");
        public string ToString(string formatting) => ToString(formatting, null);
        public string ToString(string formatting, IFormatProvider formatProvider)
        {
            return $"[{data.tra.ToString(formatting,formatProvider)}|{data.rot.ToString(formatting,formatProvider)}]";
        }
        #endregion

        #region Algebra
        public static Vector33 AddScale(Vector33 A, Vector33 B, float factorB)
        {
            return new Vector33(A.data.tra + factorB * B.data.tra, A.data.rot + factorB * B.data.rot);
        }
        public static Vector33 Add(Vector33 A, Vector33 B)
        {
            return new Vector33(A.data.tra+B.data.tra, A.data.rot+B.data.rot);
        }
        public static Vector33 Subtract(Vector33 A, Vector33 B)
        {
            return new Vector33(A.data.tra-B.data.tra, A.data.rot-B.data.rot);
        }

        public static Vector33 Scale(double factor, Vector33 A)
        {
            return new Vector33(factor*A.data.tra, factor*A.data.rot);
        }
        public static double Dot(Vector33 A, Vector33 B)
        {
            return Vector3.Dot(A.data.tra, B.data.tra) + Vector3.Dot(A.data.rot, B.data.rot);
        }
        public static Vector33 Cross(Vector33 A, Vector33 B, Coordinates aCoord, Coordinates bCoord)
        {
            switch (aCoord + 2*(int)bCoord)
            {
                case Coordinates.Axis + 2*(int)Coordinates.Axis: 
                    return new Vector33(
                                Vector3.Cross(A.data.rot, B.data.tra) + Vector3.Cross(A.data.tra, B.data.rot),
                                Vector3.Cross(A.data.rot, B.data.rot));
                case Coordinates.Axis + 2*(int)Coordinates.Ray:
                    return new Vector33(
                        Vector3.Cross(A.data.rot, B.data.tra),
                        Vector3.Cross(A.data.tra, B.data.tra) + Vector3.Cross(A.data.rot, B.data.rot));
                case Coordinates.Ray + 2*(int)Coordinates.Axis:
                    return new Vector33(
                        Vector3.Cross(A.data.tra, B.data.rot),
                        Vector3.Cross(A.data.tra, B.data.tra) + Vector3.Cross(A.data.rot, B.data.rot));
                case Coordinates.Ray + 2*(int)Coordinates.Ray:
                    return new Vector33(
                        Vector3.Cross(A.data.rot, B.data.tra) + Vector3.Cross(A.data.tra, B.data.rot),
                        Vector3.Cross(A.data.tra, B.data.tra));
                default:
                    throw new NotSupportedException();
            }
        }
        #endregion

        #region Operators
        public static Vector33 operator +(Vector33 a, Vector33 b) => Add(a, b);
        public static Vector33 operator -(Vector33 a) => Scale(-1, a);
        public static Vector33 operator -(Vector33 a, Vector33 b) => Subtract(a, b);
        public static Vector33 operator *(double a, Vector33 b) => Scale(a, b);
        public static Vector33 operator *(Vector33 a, float b) => Scale(b, a);
        public static Vector33 operator /(Vector33 a, float b) => Scale(1 / b, a);
        public static double operator *(Vector33 a, Vector33 b) => Dot(a, b);
        #endregion

    }
}
