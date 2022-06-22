using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JA.Dynamics
{
    [Experimental]
    public readonly struct BodyDynamics
    {
        public BodyDynamics(RigidBody body, BodyState state)
        {
            Pose = state.Pose;
            Momentum = state.Momentum;
            Rotation = state.Pose.Orientation.ToRotation();
            Mass = body.Mass;
            IC = body.GetInertiaMatrix(Rotation, false);
            MC = body.GetInertiaMatrix(Rotation, true);
            CG = state.Pose.FromLocalDirection(body.CG);
            Motion = body.GetMotion(state.Momentum, MC, CG);
        }
        public Matrix3 Rotation { get; }
        public double Mass { get; }
        public Vector3 CG { get; }
        public Matrix3 IC { get; }
        public Matrix3 MC { get; }
        public Pose Pose { get; }
        public Vector33 Motion { get; }
        public Vector33 Momentum { get; }

        public BodyState GetMomentumRate(Vector33 loading)
        {
            var q = Pose.Orientation;
            var v = Motion.Translational;
            var ω = Motion.Rotational;
            var qp = 0.5*ω * q;
            return new BodyState(new Pose(v, qp), loading);
        }

        public bool SolveForAcceleration(Vector33 loading, out Vector33 acceleration)
        {            
            Vector3 ω = Motion.Rotational;
            Vector3 τ_b = loading.Rotational;
            Vector3 F = loading.Translational;
            if (Mass > 0 && !MC.IsZero)
            {
                //tex: $$\begin{matrix}\underline{\dot{v}}_{b}=\tfrac{1}{m}\underline{F}-\underline{\omega}\times\left(\underline{\omega}\times\underline{c}\right)+\underline{c}\times\underline{\dot{\omega}}\\
                //\underline{\dot{\omega}}={\rm I}_{c}^{-1}\left(\underline{\tau}_{b}-\underline{\omega}\times{\rm I}_{c}\underline{\omega}-\underline{c}\times\underline{F}\right)
                //\end{matrix}$$
                Vector3 ωp = MC * (τ_b - Vector3.Cross(ω, IC * ω) - Vector3.Cross(CG, F));
                Vector3 vp_b = F / Mass + Vector3.Cross(CG, ωp) - Vector3.Cross(ω, Vector3.Cross(ω, CG));

                acceleration = new Vector33(vp_b, ωp);
                return true;
            }
            acceleration = Vector33.Zero;
            return false;
        }
        public bool SolveForLoading(Vector33 acceleration, out Vector33 loading)
        {
            Vector3 ω = Motion.Rotational;
            Vector3 ωp = acceleration.Rotational;
            Vector3 vp_b = acceleration.Translational;
            if (Mass > 0 && !IC.IsZero)
            {
                //tex:$$\begin{matrix}\underline{F}=m\left(\underline{\dot{v}}_{b}-\underline{c}\times\underline{\dot{\omega}}+\underline{\omega}\times\left(\underline{\omega}\times\underline{c}\right)\right)\\
                //\underline{\tau}_{b}={\rm I}_{c}\underline{\dot{\omega}}+\underline{\omega}\times{\rm I}_{c}\underline{\omega}+\underline{c}\times \underline{F}
                //\end{matrix}$$

                Vector3 F = Mass * (vp_b - Vector3.Cross(CG, ωp) + Vector3.Cross(ω, Vector3.Cross(ω, CG)));
                Vector3 τ_b = IC * ωp + Vector3.Cross(ω, IC * ω) + Vector3.Cross(CG, F);
                loading = new Vector33(F, τ_b);
                return true;
            }
            loading = Vector33.Zero;
            return false;
        }
        public bool SolveForPin(Vector3 translationalAcceleration, Vector3 momentAboutPin, out Vector3 rotationalAcceleration, out Vector3 force)
        {
            Vector3 ω = Motion.Rotational;
            Vector3 vp_b = translationalAcceleration;
            Vector3 τ_b = momentAboutPin;            
            Matrix3 Mb = (IC - Dynamics.Mmoi(CG, Mass)).Inverse();
            if (!Mb.IsSingular)
            {
                Vector3 ωp = Mb * (τ_b - Vector3.Cross(ω, IC * ω) - Vector3.Cross(CG, Mass * (vp_b + Vector3.Cross(ω, Vector3.Cross(ω, CG)))));
                Vector3 F = Mass * (vp_b - Vector3.Cross(CG, ωp) + Vector3.Cross(ω, Vector3.Cross(ω, CG)));
                rotationalAcceleration = ωp;
                force = F;
                return true;
            }
            rotationalAcceleration = Vector3.Zero;
            force = Vector3.Zero;
            return false;
        }
        public bool SolveForSlider(Vector3 rotationalAcceleration, Vector3 force, out Vector3 translationalAcceleration, out Vector3 momentAboutPin)
        {
            Vector3 ω = Motion.Rotational;
            Vector3 ωp = rotationalAcceleration;
            Vector3 F = force;

            if (Mass > 0)
            {
                Vector3 vp_b = F / Mass + Vector3.Cross(CG, ωp) - Vector3.Cross(ω, Vector3.Cross(ω, CG));
                Vector3 τ_b = IC * ωp + Vector3.Cross(ω, IC * ω) + Vector3.Cross(CG, F);
                translationalAcceleration = vp_b;
                momentAboutPin = τ_b;
                return true;
            } 

            translationalAcceleration = Vector3.Zero;
            momentAboutPin = Vector3.Zero;
            return false;
        }
    }
}
