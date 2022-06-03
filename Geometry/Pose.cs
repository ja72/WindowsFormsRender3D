using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace JA.Geometry
{
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public readonly struct Pose 
        : IEquatable<Pose>
    {
        readonly (Vector3 position, Quaternion orientation) data;
        public Pose(Quaternion orientation) : this(Vector3.Zero, orientation) { }
        public Pose(Vector3 position) : this(position, Quaternion.Identity) { }
        public Pose(Vector3 position, Quaternion orientation) : this()
        {
            data = (position, orientation);
        }
        public static readonly Pose Identity = new Pose(Vector3.Zero, Quaternion.Identity);
        public static implicit operator Pose(Vector3 posiiton) => new Pose(posiiton);
        public static implicit operator Pose(Quaternion rotation) => new Pose(rotation);
        public Vector3 Position { get => data.position; }
        public Quaternion Orientation { get => data.orientation; }

        public Vector3 FromLocal(Vector3 position)
            => Position +  Vector3.Transform(position, Orientation);
        public Vector3[] FromLocal(Vector3[] positions)
        {
            var R = Matrix4x4.CreateFromQuaternion(Orientation);
            Vector3[] result = new Vector3[positions.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = Position + Vector3.Transform(positions[i], R);
            }
            return result;
        }
        public Vector3 FromLocalDirection(Vector3 direction)
            => Vector3.Transform(direction, Orientation);
        public Vector3[] FromLocalDirection(Vector3[] directions)
        {
            var R = Matrix4x4.CreateFromQuaternion(Orientation);
            Vector3[] result = new Vector3[directions.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = Vector3.TransformNormal(directions[i], R);
            }
            return result;
        }
        public Quaternion FromLocal(Quaternion orientation)
            => Quaternion.Multiply(Orientation, orientation);
        public Pose FromLocal(Pose local) 
            => new Pose(FromLocal(local.Position), FromLocal(local.Orientation));
        public Vector3 ToLocal(Vector3 position)
            => Vector3.Transform(position-Position, Quaternion.Inverse(Orientation));
        public Vector3[] ToLocal(Vector3[] positions)
        {
            var R = Matrix4x4.CreateFromQuaternion(Quaternion.Inverse(Orientation));
            Vector3[] result = new Vector3[positions.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = Vector3.Transform(positions[i]-Position, R);
            }
            return result;
        }
        public Vector3 ToLocalDirection(Vector3 direction)
            => Vector3.Transform(direction, Quaternion.Inverse(Orientation));
        public Vector3[] ToLocalDirection(Vector3[] directions)
        {
            var R = Matrix4x4.CreateFromQuaternion(Quaternion.Inverse(Orientation));
            Vector3[] result = new Vector3[directions.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = Vector3.TransformNormal(directions[i], R);
            }
            return result;
        }
        public Quaternion ToLocal(Quaternion orientation)
            => Quaternion.Multiply(orientation, Quaternion.Inverse(Orientation));
        public Pose ToLocal(Pose pose)
            => new Pose(ToLocal(pose.Position), ToLocal(pose.Orientation));

        #region IEquatable Members
        /// <summary>
        /// Equality overrides from <see cref="System.Object"/>
        /// </summary>
        /// <param name="obj">The object to compare this with</param>
        /// <returns>False if object is a different type, otherwise it calls <code>Equals(Pose)</code></returns>
        public override bool Equals(object obj)
        {
            return obj is Pose item && Equals(item);
        }

        /// <summary>
        /// Checks for equality among <see cref="Pose"/> classes
        /// </summary>
        /// <returns>True if equal</returns>
        public bool Equals(Pose other) => data.Equals(other.data);
        /// <summary>
        /// Calculates the hash code for the <see cref="Pose"/>
        /// </summary>
        /// <returns>The int hash value</returns>
        public override int GetHashCode() => data.GetHashCode();
        public static bool operator ==(Pose target, Pose other) { return target.Equals(other); }
        public static bool operator !=(Pose target, Pose other) { return !target.Equals(other); }

        #endregion

        #region Formatting
        public override string ToString() => ToString("g");
        public string ToString(string formatting) => ToString(formatting, null);
        public string ToString(string formatting, IFormatProvider formatProvider)
        {
            return $"{Position.ToString(formatting, formatProvider)}-{Orientation.ToString(formatting, formatProvider)}";
        }
        #endregion

        #region Algebra
        public static Pose Add(Pose A, Pose B)
        {
            return new Pose(A.data.position+B.data.position, A.data.orientation+B.data.orientation);
        }
        public static Pose Subtract(Pose A, Pose B)
        {
            return new Pose(A.data.position+B.data.position, A.data.orientation-B.data.orientation);
        }

        public static Pose Scale(float factor, Pose A)
        {
            return new Pose(factor*A.data.position, Quaternion.Multiply(A.data.orientation, factor));
        }
        #endregion

        #region Operators
        public static Pose operator +(Pose a, Pose b) => Add(a, b);
        public static Pose operator -(Pose a) => Scale(-1, a);
        public static Pose operator -(Pose a, Pose b) => Subtract(a, b);
        public static Pose operator *(float a, Pose b) => Scale(a, b);
        public static Pose operator *(Pose a, float b) => Scale(b, a);
        public static Pose operator /(Pose a, float b) => Scale(1 / b, a);
        #endregion
    }

}
