using System;
using System.ComponentModel;

namespace JA.Dynamics
{
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public readonly struct BodyState : IEquatable<BodyState>
    {
        readonly (Pose pose, Vector33 momentum) data;
        public BodyState(Pose pose, Vector33 momentum) 
        {
            data = (pose,momentum);
        }
        public Pose Pose { get => data.pose; }
        public Vector33 Momentum { get => data.momentum; }
        public BodyDynamics GetDynamics(RigidBody body) => new BodyDynamics(body, this);
        public Vector33 GetLoading(RigidBody body, Vector33 motion, double time, Vector3 gravity)
        {
            var f = Vector33.WrenchAt(body.Mass * gravity, Pose.FromLocal(body.CG));
            if (body.Loading!=null)
            {
                f += body.Loading(time, Pose, motion);
            }
            return f;
        }
        public BodyState GetRate(Simulation simulation, int index, double h = 0)
        {
            //Quaternion.Normalize()
            //BodyState.GetRate(0.000833333314706882)
            //Vector3.Transform(<(-0.707106699056426,0.000828640283403142,0.000828640249121436)|0.707105892253871>)
            //Quaternion.Rotate(False)
            //RigidBody.GetInertiaMatrix(True)
            //Quaternion.ToRotation(False)
            //RigidBody.GetInertiaMatrix(True)
            //Vector3.Transform(<(-0.707106699056426,0.000828640283403142,0.000828640249121436)|0.707105892253871>)
            //Quaternion.Rotate(False)
            //RigidBody.GetMotion(M_C)
            //Quaternion.Multiply()

            var rb = simulation.Bodies[index];
            var q = Pose.Orientation;
            var cg = Vector3.Transform(rb.CG, q);
            var p = Momentum;
            var m = rb.GetMotion(q, p);
            var v = m.Translational;
            var ω = m.Rotational;
            var f = Vector33.WrenchAt(rb.Mass * simulation.Gravity, Pose.Position);
            if (rb.Loading!=null)
            {
                f += rb.Loading(simulation.Time+h, Pose, m);
            }
            var qp = 0.5*ω * q;
            return new BodyState(new Pose(v, qp), f);
        }
        public static BodyState Normalize(BodyState a)
        {
            return new BodyState(Pose.Normalize(a.Pose),a.Momentum);
        }

        public static BodyState AddScale(BodyState a, BodyState b, double factor = 1)
        {
            return new BodyState(a.Pose + factor*b.Pose, a.Momentum + factor*b.Momentum);
        }
        public static BodyState Scale(BodyState a, double factor)
        {
            return new BodyState(factor*a.Pose, factor*a.Momentum);
        }
        public static BodyState operator +(BodyState a, BodyState b)
            => AddScale(a, b);
        public static BodyState operator -(BodyState a, BodyState b)
            => AddScale(a, b, -1);
        public static BodyState operator *(double f, BodyState a)
            => Scale(a, f);
        public static BodyState operator *(BodyState a, double f)
            => Scale(a, f);
        public static BodyState operator /(BodyState a, double d)
            => Scale(a, 1/d);

        public static bool operator ==(BodyState left, BodyState right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BodyState left, BodyState right)
        {
            return !(left==right);
        }

        public override string ToString()
        {
            var q = Pose.Orientation;
            var (_, angle)= q.GetAxisAngle();
            return $"BodyState(r={Pose.Position.Magnitude:g2} θ={angle:g2} p={Momentum.Translational.Magnitude:g3} L={Momentum.Rotational.Magnitude:g3})";
        }

        public override bool Equals(object obj)
        {
            return obj is BodyState state&&Equals(state);
        }
        public bool Equals(BodyState state) => data.Equals(state.data);

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = -762977368;
                hashCode=hashCode*-1521134295+data.GetHashCode();
                return hashCode; 
            }
        }
    }
}
