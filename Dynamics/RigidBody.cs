using System;
using System.Linq;
using System.ComponentModel;
using System.Drawing;

namespace JA.Dynamics
{
    using System.Diagnostics;
    using JA.Geometry;

    using static DoubleConstants;

    public delegate Vector33 BodyLoading(double time, Pose pose, Vector33 motion);
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class RigidBody : IRender
    {
        public RigidBody(double mass, Drawing.VisibleSolid solid, Pose pose, Vector33 velocity)
        {
            Mass=mass;
            Graphics = solid;
            InitialPosition = pose;
            InitialMotion = velocity;

            Vector3 cg = solid.Geometry.Center;
            double V = solid.Geometry.Volume;
            Mmoi = solid.Geometry.GetMmoiAtCenter(mass);
            InvMmoi = Mmoi.Inverse();
        }
        public RigidBody(double mass, Drawing.VisibleSphere sphere, Pose pose, Vector33 velocity)
        {
            Mass=mass;
            Graphics = sphere;
            InitialPosition = pose;
            InitialMotion = velocity;

            Vector3 cg = sphere.Sphere.Center;
            double V = sphere.Sphere.Volume;
            double R = sphere.Sphere.Radius;
            double I0 = 2*mass*R*R/5;
            Volume = V;
            CG = cg;
            Mmoi = Matrix3.Scalar(I0);
            InvMmoi = Matrix3.Scalar(1/I0);
        }
        public RigidBody(double mass, Drawing.VisibleMesh localMesh, Pose pose, Vector33 velocity)
        {
            Mass=mass;
            Graphics = localMesh;
            InitialPosition = pose;
            InitialMotion = velocity;

            Vector3 cg = Vector3.Zero;
            double V = 0.0;
            Matrix3 I0 = Matrix3.Zero;

            foreach (var poly in localMesh.Mesh)
            {
                foreach (var trig in poly.GetTriangles(false))
                {
                    var A = (Vector3)trig.A;
                    var B = (Vector3)trig.B;
                    var C = (Vector3)trig.C;

                    double dV = Vector3.Dot(A, Vector3.Cross(B, C))/6;
                    V += dV;
                    Vector3 dCG = (A+B+C)/4;
                    cg += dV*dCG;
                    Matrix3 dI = (Dynamics.Mmoi(A+B) + Dynamics.Mmoi(B+C) + Dynamics.Mmoi(C+A))/20;
                    I0 += dV*dI;
                }
            }
            Volume = V;
            CG = cg/V;
            var ρ = mass/V;
            Mmoi = ρ*I0 - Dynamics.Mmoi(CG, mass);
            if (!Mmoi.IsSingular)
            {
                InvMmoi = Mmoi.Inverse();
            }
            else
            {
                InvMmoi = Matrix3.Zero;
            }
        }
        public double Volume { get; }
        public double Density { get => Mass/Volume; }
        public double Mass { get; }
        [Browsable(false)]
        public Matrix3 Mmoi { get; }
        [Browsable(false)]
        public Matrix3 InvMmoi { get; }
        public Vector3 CG { get; }

        public Pose InitialPosition { get; set; }
        public Vector33 InitialMotion { get; set; }
        public BodyLoading Loading { get; set; }
        public Drawing.VisibleObject Graphics { get; }
        public Matrix3 GetInertiaMatrix(Quaternion orientation, bool inverse = false)
        {
            return GetInertiaMatrix(orientation.ToRotation(), inverse);
        }
        public Matrix3 GetInertiaMatrix(Matrix3 rotation, bool inverse = false)
        {
            Matrix3 inv_rotation = rotation.Transpose();
            if (!inverse)
            {
                return rotation * Mmoi * inv_rotation;
            }
            else
            {
                return rotation * InvMmoi * inv_rotation;
            }
        }
        public Vector33 GetMomentum(Quaternion orientation, Vector33 motion)
        {
            return GetMomentum(motion, GetInertiaMatrix(orientation), Vector3.Transform(CG, orientation));
        }

        public Vector33 GetMomentum(Vector33 velocity, Matrix3 I_C, Vector3 cg)
        {
            //tex: Momentum from motion summed not at the cg
            // $$\begin{aligned}
            // p&=m\left(v_{A}+\omega\times c\right)\\
            // L_{A}& =I_{C}\omega+c\times p
            // \end{aligned}$$

            var v_A = velocity.Translational;
            var ω = velocity.Rotational;
            var p = Mass * (v_A + Vector3.Cross(ω, cg));
            var L_A = I_C*ω + Vector3.Cross(cg, p);
            return new Vector33(p, L_A);
        }
        public Vector33 GetMotion(Quaternion orientation, Vector33 momentum)
            => GetMotion(momentum, GetInertiaMatrix(orientation, true), Vector3.Transform(CG, orientation));
        public Vector33 GetMotion(Vector33 momentum, Matrix3 M_C, Vector3 cg)
        {
            //tex: Motion from momentum summed not at the cg
            // $$\begin{aligned}
            // \omega & =I_{C}^{-1}\left(L_{A}-c\times p\right)\\
            // v_{A} & =\tfrac{1}{m}p-\omega\times c
            //\end{aligned}$$

            var p = momentum.Translational;
            var L_A = momentum.Rotational;
            var ω = M_C*(L_A - Vector3.Cross(cg, p));
            var v_A = p/Mass - Vector3.Cross(ω, cg);
            return new Vector33(v_A, ω);
        }

        public Vector33 GetNetLoad(Vector33 acceleration, Vector33 velocity, Matrix3 I_C, Vector3 cg)
        {
            //tex: Force from acceleration not at cg
            //$$\begin{aligned}
            //F&=m\left(a_{A}+\alpha\times c+\omega\times\left(\omega\times c\right)\right)\\
            //\tau_{A}&=I_{C}\,\alpha+\omega\times I_{C}\,\omega+ c\times F
            // \end{aligned}$$


            var ω = velocity.Rotational;
            var a_A = acceleration.Translational;
            var α = acceleration.Rotational;
            var F = Mass*(a_A + Vector3.Cross(α, cg) + Vector3.Cross(cg, Vector3.Cross(cg, ω)));
            var τ_A = I_C*α + Vector3.Cross(ω, I_C*ω) + Vector3.Cross(cg, F);
            return new Vector33(F, τ_A);
        }
        public Vector33 GetAcceleration(Vector33 netLoad, Vector33 velocity, Matrix3 M_C, Matrix3 I_C, Vector3 cg)
        {
            //tex: Force from acceleration not at cg
            //$$\begin{aligned}
            //\alpha& =I_{C}^{-1}\left(\tau_{A}-c\times F-\omega\times I_{C}\,\omega\right)\\
            //a_{A}&=\tfrac{1}{m}F-\alpha\times c-\omega\times\left(\omega\times c\right)
            // \end{aligned}$$

            
            var ω = velocity.Rotational;
            var F = netLoad.Translational;
            var τ_A = netLoad.Rotational;
            var α = M_C*(τ_A - Vector3.Cross(cg, F) - Vector3.Cross(ω, I_C*ω));
            var a_A = 1/Mass*F - Vector3.Cross(α, cg) - Vector3.Cross(cg, Vector3.Cross(cg, ω));
            return new Vector33(a_A, α);
        }
        public Vector33 GetAcceleration(Vector33 netLoad, Vector33 momentum, Matrix3 M_C, Vector3 cg)
        {
            //tex: Force from acceleration not at cg
            //$$\begin{aligned}
            //G&=F-\omega\times p\\
            //\alpha& =I_{C}^{-1}\left(\tau_{A}-\omega\times L_{A}-v_{A}\times p-c\times G\right)\\
            //a_{A}&=\tfrac{1}{m}G-\alpha\times c-v_{A}\times\omega
            // \end{aligned}$$

            var velocity = GetMotion(momentum, M_C, cg);
            var v_A = velocity.Translational;
            var ω = velocity.Rotational;
            var p = momentum.Translational;
            var L_A = momentum.Rotational;
            var F = netLoad.Translational;
            var τ_A = netLoad.Rotational;
            var G = F -  Vector3.Cross(ω, p);
            var α = M_C*(τ_A - Vector3.Cross(ω, L_A) - Vector3.Cross(v_A, p)- Vector3.Cross(cg, G));
            var a_A = 1/Mass*G - Vector3.Cross(α, cg) - Vector3.Cross(v_A, ω);
            return new Vector33(a_A, α);
        }

        public void Render(Graphics g, Drawing.Camera camera, JA.Geometry.Pose pose)
        {
            if (CG.MagnitudeSquared > tiny*tiny)
            {
                // only draw handle if way from CG
                var handle = new Drawing.VisibleTriad("A", 0.5f);
                handle.Render(g, camera, pose);
            }
            var center = new Drawing.VisibleTriad("C", CG.ToFloat(), 0.5f);
            center.Render(g, camera, pose);
            Graphics.Render(g, camera, pose);
        }

        public override string ToString()
        {
            var q = InitialPosition.Orientation;
            var (_, angle)= q.GetAxisAngle();
            return $"RigidBody(m={Mass} r={InitialPosition.Position.Magnitude:g2} θ={angle:g2} v={InitialMotion.Translational.Magnitude:g3} ω={InitialMotion.Rotational.Magnitude:g3})";
        }
    }

}
