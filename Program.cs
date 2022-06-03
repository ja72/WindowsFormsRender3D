using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JA
{
    using static JA.SingleConstants;
    static class Program
    {
        static readonly Random rng = new Random();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //CheckGeom();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Render3DForm());
        }

        static void CheckGeom()
        {
            double mass = 0.12;
            float a = 4.0f, b = 0.4f, c = 2.6f;
            var m1 = Geometry.Meshes.CreateCube(a, b, c);
            var rb1 = new Dynamics.RigidBody(mass,
                new Drawing.VisibleMesh(m1, Color.Blue),
                Dynamics.Pose.Identity,
                Dynamics.Vector33.Zero);

            var cg = rb1.CG;
            var mmoi = rb1.Mmoi;

            Debug.WriteLine("Center of mass should be at origin:");

            Debug.WriteLine(cg.X);
            Debug.WriteLine(cg.Y);
            Debug.WriteLine(cg.Z);

            double Ixx = mass/12*(b*b+c*c);
            double Iyy = mass/12*(a*a+c*c);
            double Izz = mass/12*(b*b+a*a);

            Debug.WriteLine("MMoi deviation from theory:");

            Debug.WriteLine(mmoi.A11 - Ixx);
            Debug.WriteLine(mmoi.A22 - Iyy);
            Debug.WriteLine(mmoi.A33 - Izz);
        }

        static void CheckRoots()
        {
            var c_0 = 2*rng.NextDouble()-1;
            var c_1 = 4*rng.NextDouble()-2;
            var c_2 = 1*rng.NextDouble()-0.5;
            var c_3 = 2*rng.NextDouble()-1;

            double f(double x) => c_0 + x*(c_1 + x*(c_2 + x*c_3));

            if (DoubleConstants.TryCubicRoot(c_0, c_1, c_2, c_3, out var x))
            {
                var z1 = f(x);
            }

            if (DoubleConstants.TryCubicRoot(c_0, c_1, c_2, c_3, out x, true))
            {
                var z2 = f(x);
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    sealed class ExperimentalAttribute : Attribute
    {
        public ExperimentalAttribute()
        {
            Debug.WriteLine("Experimental Feature Used.");
        }
    }
}
