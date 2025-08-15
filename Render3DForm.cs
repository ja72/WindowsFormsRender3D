using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JA
{
    using static JA.Geometry.Geometry;
    using static JA.Dynamics.Dynamics;

    using Scene = Drawing.Scene;
    using Camera = Drawing.Camera;
    using VisibleMesh = Drawing.VisibleMesh;
    using VisibleSphere = Drawing.VisibleSphere;
    using VisibleBezier = Drawing.VisibleBezier;
    using Simulation = Dynamics.Simulation;
    using RigidBody = Dynamics.RigidBody;


    public partial class Render3DForm : Form
    {
        const float pi = (float)Math.PI;
        const float deg = pi / 180;

        public float YawRate { get; set; }
        public float PitchRate { get; set; }
        public float RollRate { get; set; }
        public float Time { get; set; }
        public float TimeFactor { get; set; } = 1;
        public float TimeStep { get => TimeFactor * timer1.Interval/1000f; }
        public Scene Scene { get; }
        public Simulation Simulation { get; }
        public Camera Camera1 { get; }
        public Camera Camera2 { get; }
        public bool Running { get; set; } = false;

        public Render3DForm()
        {
            InitializeComponent();

            this.Scene = new Scene();
            this.Simulation = new Simulation();

            this.Camera1 = new Camera("Static", pictureBox1, 12f, 5f)
            {
                Target = Vector3.Zero/2,
            };
            this.Camera2 = new Camera("Dynamic", pictureBox2, 12f, 5f)
            {
                Target = Vector3.Zero/2,
            };
            this.Camera1.Orientation = Rotation(Axis.X, pi/2);
            this.Camera2.Orientation = Rotation(Axis.X, pi/2);
            this.YawRate = 0.0f;
            this.PitchRate  = 0.00f;
            this.RollRate = 0.0f;
            this.timer1.Interval = 15;
            this.timer1.Tick += (s, ev) =>
            {
                if (Running)
                {
                    Simulation.Run(Simulation.Time + TimeStep);
                }
                Camera1.Orientation = Quaternion.Multiply(Camera1.Orientation, FromRollPitchYaw(TimeStep*RollRate, TimeStep*PitchRate, TimeStep*YawRate));
                Camera2.Orientation = Quaternion.Multiply(Camera2.Orientation, FromRollPitchYaw(TimeStep*RollRate, TimeStep*PitchRate, TimeStep*YawRate));
                pictureBox1.Invalidate();
                pictureBox2.Invalidate();
                propertyGrid1.Refresh();
                propertyGrid2.Refresh();
            };

            this.timer1.Start();            
            this.KeyDown += (s, ev) =>
            {
                switch (ev.KeyCode)
                {
                    case Keys.Escape:
                        this.Close();
                        break;
                    case Keys.Space:
                        Running = !Running;
                        break;
                }
            };

            this.Camera1.Paint += (c, g) =>
            {
                g.DrawString(c.Name, SystemFonts.IconTitleFont, SystemBrushes.ControlText, 2, 2);
                Scene.Render(g, c);
            };
            this.Camera2.Paint += (c, g) =>
            {
                g.DrawString(c.Name, SystemFonts.IconTitleFont, SystemBrushes.ControlText, 2, 2);
                Simulation.Render(c, g);
            };            

            this.Text = "3D Scene Render - Right Mouse Drag to orbit camera.";
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            var m1 = Geometry.Meshes.CreateCube(4f, 0.4f, 2.6f);
            var m2 = Geometry.Meshes.CreatePyramid(2f, 2f);

            var p1 = new Geometry.Pose(
                    4f * Vector3.UnitX,
                    Quaternion.CreateFromAxisAngle(Vector3.UnitY, 0f*deg));

            var p2 = new Geometry.Pose(
                     -4f * Vector3.UnitX,
                     Quaternion.CreateFromYawPitchRoll(0, -90*deg, 0));

            // Add static objects
            var d1 = new VisibleMesh(m1.FromLocal(p1), Color.Blue);
            d1.ElementColors[0]= Color.Red;

            var d2 = new VisibleMesh(m2.FromLocal(p2), Color.BlueViolet);
            d2.ElementColors[0] = Color.Red;

            var d3 = new VisibleBezier(
                    Geometry.BezierCurve.FromTheePoints(
                        -2*Vector3.UnitX,
                         2*Vector3.UnitX,
                         6*Vector3.UnitY+Vector3.UnitZ), Color.DarkRed);
            d3.EndArrow = true;

            var d4 = new VisibleSphere(
                new Geometry.Sphere(Vector3.Zero, 0.75f), Color.Green);

            Scene.AddDrawing(d1);
            Scene.AddDrawing(d2);
            Scene.AddDrawing(d3);
            Scene.AddDrawing(d4);

            // Add dynamic objects            

            var d6 = new VisibleMesh(m1, Color.Blue);
            d6.ElementColors[0]= Color.DarkOrange;
            var c6 = 2*Dynamics.Vector3.UnitX;
            var p6 = new Dynamics.Pose(c6, Rotation(Vector3.UnitY, 0.005f));

            var v6 = new Dynamics.Vector33(
                0.0*Dynamics.Vector3.UnitX,
                5*Dynamics.Vector3.UnitZ);
            _ = Simulation.AddBody(0.12, d6, p6, v6);

            //var p7 = new Dynamics.Pose(-cg);
            //var v7 = Dynamics.Vector33.TwistAt(
            //    5*Dynamics.Vector3.UnitY,
            //    -cg,
            //    0.0*Dynamics.Vector3.UnitX);
            var d7 = new VisibleMesh(m2, Color.BlueViolet);
            var c7 = -2*Dynamics.Vector3.UnitX + (Dynamics.Vector3)d7.Center;
            var p7 = new Dynamics.Pose(c7, FromRollPitchYaw(0.0f, -pi/2, 0));
            var v7 = Dynamics.Vector33.TwistAt(
                5*Dynamics.Vector3.UnitY,
                p7.FromLocalDirection(d7.Center),
                0.0*Dynamics.Vector3.UnitX);
            _ = Simulation.AddBody(0.08, d7, p7, v7);

            Simulation.Reset();

            propertyGrid1.SelectedObject = Simulation;
            propertyGrid2.SelectedObject = Scene;

            //TimeFactor = 0.2f;

            stopButton_Click(this, EventArgs.Empty);
            tabControl1.SelectedIndex = 0;
        }

        private void resetButton_Click(object sender, EventArgs e)
        {
            Running = false;
            Simulation.Reset();            
            tabControl1.SelectedIndex = 1;
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            Running = false;
            playButton.Enabled = true;
            stopButton.Enabled = false;
            tabControl1.SelectedIndex = 1;
        }

        private void playButton_Click(object sender, EventArgs e)
        {
            Running = true;
            playButton.Enabled = false;
            stopButton.Enabled = true;
            tabControl1.SelectedIndex = 1;
        }
    }
}
