using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace JA.Geometry
{

    using static SingleConstants;

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public readonly struct Ray : ICurve
    {
        readonly (Vector3 origin, Vector3 direction) data;

        public Ray(Vector3 origin, Vector3 direction)
        {
            data = (origin, Vector3.Normalize(direction));
        }
        public Ray FromLocal(Pose pose) => new Ray(pose.FromLocal(Origin), pose.FromLocalDirection(Direction));
        public Vector3 Origin { get => data.origin; }
        public Vector3 Direction { get => data.direction; }

        public Vector3 GetPointAlong(float t) => data.origin + t * data.direction;
        public Vector3 GetTangent(float t) => data.direction;

        public float GetDistanceTo(Vector3 point)
            => Vector3.Dot(Direction, point-Origin);

        public bool Contains(Vector3 target)
        {
            return Vector3.Distance(target, GetPointAlong(GetDistanceTo(target))) <= small;
        }

        public Bounds GetBounds()
        {
            return Bounds.FromPointCloud(Origin, Direction/small);
        }

        public bool Intersect(Plane plane, out float distance)
        {
            float denom = Vector3.Dot(Direction, plane.Normal);
            float numer = Vector3.Dot(Direction, Origin) - plane.D;
            if (Math.Abs(denom) >= tiny)
            {
                distance = numer/denom;
                return true;
            }
            distance = 0;
            return false;
        }

        public override string ToString()
        {
            return $"{{pos={Origin},dir={Direction}}}";
        }

    }
}
