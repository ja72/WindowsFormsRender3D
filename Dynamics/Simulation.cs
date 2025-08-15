using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JA.Dynamics
{
    using System.ComponentModel;
    using JA.Drawing;
    using JA.Geometry;
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class Simulation
    {
        public event EventHandler<SimulationEventArgs>? Step; // Declare the event as nullable to resolve CS8618

        readonly List<RigidBody> bodies;
        readonly List<BodyState> states;

        public Simulation()
        {
            this.bodies=new List<RigidBody>();
            this.states=new List<BodyState>();
            Triad=new VisibleTriad("W");
        }
        [Category("Model")]
        public Vector3 Gravity { get; set; } = 0*Vector3.UnitY;
        [Category("Model")]
        public RigidBody[] Bodies => bodies.ToArray();
        [Category("State")]
        public BodyState[] States => states.ToArray();
        [Category("State")]
        public double Time { get; private set; }
        public void Reset()
        {
            Time=0;
            states.Clear();
            states.AddRange(
                bodies.Select((rb) =>
                    new BodyState(
                        rb.InitialPosition,
                        rb.GetMomentum(rb.InitialPosition.Orientation, rb.InitialMotion))
                    )
                );
        }

        public IEnumerable<Vector33> GetLoading(double time, IEnumerable<BodyState> current)
        {
            int index = 0;
            foreach (var s in current)
            {
                var rb = bodies[index++];
                var cg = s.Pose.FromLocalDirection(rb.CG);
                var M_C = rb.GetInertiaMatrix(s.Pose.Orientation, true);
                var v = rb.GetMotion(s.Momentum, M_C, cg);
                var f = Vector33.WrenchAt(rb.Mass * Gravity, s.Pose.Position);
                if (rb.Loading!=null)
                {
                    f+=rb.Loading(time, s.Pose, v);
                }
                yield return f;
            }
        }
        public IEnumerable<BodyState> GetRate(IEnumerable<BodyState> current, double h = 0)
        {
            return current.Select((state, index) => state.GetRate(this, index, h));
        }
        public IEnumerable<BodyState> GetRate(IEnumerable<BodyState> current, double h, IEnumerable<BodyState> rates)
        {
            int index = 0;
            foreach (var staterate in Enumerable.Zip(current, rates, (state, rate) => (state, rate)))
            {
                var (state, rate)=staterate;
                var next = BodyState.Normalize(BodyState.AddScale(state, rate, h));
                yield return next.GetRate(this, index++, h);
            }
        }

        public IEnumerable<BodyState> Integrate(IEnumerable<BodyState> current, double h)
        {
            // Implement RK4
            var k0 = GetRate(current).ToArray();
            var k1 = GetRate(current, h/2, k0).ToArray();
            var k2 = GetRate(current, h/2, k1).ToArray();
            var k3 = GetRate(current, h, k2).ToArray();

            var next = current.ToArray().Clone() as BodyState[];
            double h3 = h/3, h6 = h/6;

            for (int i = 0; i<k0.Length; i++)
            {
                next[i]=BodyState.Normalize(next[i]+h6*k0[i]+h3*k1[i]+h3*k2[i]+h6*k3[i]);
            }
            return next;
        }

        public double EstimateMaxTimeStep()
        {
            if (bodies.Count==0) return 1;
            var ω_max = states.Select((s, index) => bodies[index]
                        .GetMotion(s.Pose.Orientation, s.Momentum).Rotational)
                        .Max((ω) => ω.Magnitude);
            return ω_max>0 ? Math.PI/( 360*ω_max ) : 1;
        }
        public void Run(double endTime)
        {
            double h = EstimateMaxTimeStep();
            int n_steps = (int) Math.Ceiling((endTime-Time)/h);
            Run(endTime, n_steps);
        }
        public void Run(int n_steps)
        {
            double h = EstimateMaxTimeStep();
            Run(Time+h*n_steps, n_steps);
        }

        public void Run(double endTime, int n_steps)
        {
            if (states.Count==0)
            {
                Reset();
            }
            double h = (endTime-Time)/n_steps;
            Step?.Invoke(this, new SimulationEventArgs(this));
            while (Time<endTime)
            {
                double h_max = Math.Min(h, EstimateMaxTimeStep());
                double h_next = Math.Min(h_max, endTime-Time);

                var next = Integrate(states, h_next);
                // Handle collisions

                states.Clear();
                states.AddRange(next);
                Time+=h_next;
                Step?.Invoke(this, new SimulationEventArgs(this));
            }
        }
        public RigidBody AddBody(double mass, VisibleSphere sphere, Pose initialPose, Vector33 initialMotion)
        {
            var body = new RigidBody(mass, sphere, initialPose, initialMotion);
            bodies.Add(body);
            return body;
        }

        public RigidBody AddBody(double mass, VisibleMesh mesh, Pose initialPose, Vector33 initialMotion)
        {
            var body = new RigidBody(mass, mesh, initialPose, initialMotion);
            bodies.Add(body);
            return body;
        }
        public RigidBody AddBody(RigidBody body, Pose initial)
        {
            body.InitialPosition=initial;
            bodies.Add(body);
            return body;
        }
        public RigidBody AddBody(RigidBody body)
        {
            bodies.Add(body);
            return body;
        }

        [Category("Model")]
        public VisibleTriad Triad { get; }
        public void Render(Camera camera, Graphics g)
        {
            var state = g.Save();
            g.SmoothingMode=SmoothingMode.AntiAlias;
            g.TranslateTransform(camera.OnControl.ClientSize.Width/2f, camera.OnControl.ClientSize.Height/2f);
            //var light = camera.LightPos.Unit();
            //var R = System.Numerics.Matrix4x4.CreateFromQuaternion(System.Numerics.Quaternion.Inverse(camera.Orientation));
            //light = System.Numerics.Vector3.TransformNormal(light, R);

            Triad.Render(g, camera, Pose.Identity.ToFloat());

            if (states.Count==0)
            {
                Reset();
            }

            int index = 0;
            foreach (var rb in Bodies)
            {
                if (rb.Graphics==null) continue;
                var pose = states[index++].Pose;
                rb.Render(g, camera, pose.ToFloat());
            }
            g.Restore(state);

        }
    }

    public class SimulationEventArgs : EventArgs
    {
        public SimulationEventArgs(Simulation simulation)
        {
            Simulation=simulation??throw new ArgumentNullException(nameof(simulation));
        }

        public Simulation Simulation { get; }
    }
}
