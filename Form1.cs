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
    public partial class Form1 : Form
    {
        readonly Camera camera;
        const float pi = (float)Math.PI;
        const float deg = pi / 180;

        public float Yaw { get; set; }
        public float Pitch { get; set; }
        public float Time { get; set; }
        public Scene Scene { get; set; }

        public Form1()
        {
            InitializeComponent();

            this.camera = new Camera(pictureBox1, 15f, 3f);
            this.Scene = new Scene();
            this.Time = 0f;
            this.Yaw = 0f;
            this.Pitch  = 0f;
            this.timer1.Interval = 15;
            this.timer1.Tick += (s, ev) =>
            {
                Time += timer1.Interval/1000f;
                Yaw += 0.01f;
                Pitch += 0.003f;
                camera.Orientation = Quaternion.CreateFromYawPitchRoll(Yaw, Pitch - 90*deg, 0);
                pictureBox1.Invalidate();
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
                        if (timer1.Enabled)
                        {
                            timer1.Stop();
                        }
                        else
                        {
                            timer1.Start();
                        }
                        break;
                }
            };

            this.camera.Paint += (c, g) =>
            {
                Scene.Render(c, g);
            };
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            {
                var mesh = Mesh.CreatePyramid(Color.Blue, 2f, 2f);
                mesh.Elements[0].Color = Color.Red;
                mesh.Orientation = Quaternion.CreateFromYawPitchRoll(0, -90 * deg, 0);
                mesh.Position = 1f * Vector3.UnitY;
                Scene.Meshes.Add(mesh);
            }
            {
                var mesh = Mesh.CreateCube(Color.Blue, 2f);
                mesh.Elements[0].Color = Color.Red;
                //mesh.Orientation = Quaternion.CreateFromYawPitchRoll(0, 90 * deg, 0);
                mesh.Position = -0.75f * Vector3.UnitY;
                Scene.Meshes.Add(mesh);
            }
        }
    }
}
