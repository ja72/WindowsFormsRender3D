using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using System.Runtime.CompilerServices;

namespace JA.Drawing
{
    using System.Diagnostics;
    using JA.Geometry;

    using static SingleConstants;

    public delegate void CameraPaintHandler(Camera camera, Graphics g);

    public class Camera
    {
        public event CameraPaintHandler Paint;

        /// <summary>
        /// Initializes a new instance of the <see cref="Camera" /> class.
        /// </summary>
        /// <param name="control">The target control to draw scene.</param>
        /// <param name="fov">
        /// The FOV angle (make zero for orthographic projection).
        /// </param>
        /// <param name="sceneSize">Size of the scene across/</param>
        public Camera(string name, Control control, float fov, float sceneSize = 1f)
        {
            Name = name;
            OnControl = control;
            FOV = fov;
            SceneSize = sceneSize;
            LightPos = new Vector3(0 * sceneSize, 0 * sceneSize / 2, -sceneSize);
            Orientation = Quaternion.Identity;
            Target = Vector3.Zero;
            mouse = (Point.Empty, Point.Empty, Point.Empty, MouseButtons.None);
            control.Paint += (s, ev) =>
            {
                Paint?.Invoke(this, ev.Graphics);
            };
            control.MouseDown += (s, ev) =>
            {
                var center = ViewCenter;
                Point current = new Point(ev.X - center.X, ev.Y-center.Y);
                mouse = (current, mouse.msMove, mouse.msUp, ev.Button);
            };
            control.MouseMove += (s, ev) =>
            {
                var center = ViewCenter;
                Point current = new Point(ev.X - center.X, ev.Y-center.Y);
                mouse = (mouse.msDown, current, mouse.msUp, ev.Button);

                if (ev.Button == MouseButtons.Right)
                {
                    var ptDn = UnProject(mouse.msDown, 1.4f);
                    var ptMv = UnProject(mouse.msMove, 1.4f);
                    Vector3 a = ptDn-Target;
                    Vector3 b = ptMv-Target;
                    if (a!=b)
                    {
                        var axis = Vector3.Normalize(Vector3.Cross(a, b));
                        var angle = Geometry.GetAngleBetwenVectors(a, b);
                        Orientation *= Quaternion.CreateFromAxisAngle(axis, angle);
                        mouse = (mouse.msMove, mouse.msMove, mouse.msUp, mouse.buttons);
                    }
                }
            };
            control.MouseUp += (s, ev) =>
            {
                var center = ViewCenter;
                Point current = new Point(ev.X - center.X, ev.Y-center.Y);
                mouse = (mouse.msDown, mouse.msMove, current, ev.Button);
            };
        }

        internal (Point msDown, Point msMove, Point msUp, MouseButtons buttons) mouse;

        public GraphicsState SetupView(Graphics g, SmoothingMode smoothing = SmoothingMode.AntiAlias)
        {
            var gs = g.Save();
            g.SmoothingMode = smoothing;
            var center = ViewCenter;
            g.TranslateTransform(center.X, center.Y);
            return gs;
        }
        public Point ViewCenter => new Point(
            OnControl.Margin.Left + OnControl.ClientSize.Width/2,
            OnControl.Margin.Top + OnControl.ClientSize.Height/2);
        public string Name { get; set; }
        public Control OnControl { get; }
        public float SceneSize { get; set; }
        public float FOV { get; set; }
        public Quaternion Orientation { get; set; }
        public Vector3 LightPos { get; set; }
        public Vector3 Target { get; set; }
        public int ViewSize
        {
            get => Math.Min(
            OnControl.ClientSize.Width - OnControl.Margin.Left - OnControl.Margin.Right,
            OnControl.ClientSize.Height - OnControl.Margin.Top - OnControl.Margin.Bottom);
        }
        public int ViewHalfSize => ViewSize/2;
        public float Scale => ViewHalfSize/SceneSize;
        public float DrawSize
        {
            get => 2 * (float)Math.Tan(FOV / 2 * Math.PI / 180);
            set
            {
                FOV = 360/pi*Atan(value/2);
            }
        }
        /// <summary>
        /// Get the pixels per model unit scale.
        /// </summary>
        public Vector3 EyePos { get => Target + Vector3.Transform(Vector3.UnitZ * SceneSize / DrawSize, Quaternion.Inverse(Orientation)); }
        public float EyeDistance
        {
            get => SceneSize/DrawSize;
            set
            {
                DrawSize = SceneSize/value;
            }
        }
        public Vector3 RightDir { get => Vector3.TransformNormal(Vector3.UnitX, Matrix4x4.CreateFromQuaternion(Quaternion.Inverse(Orientation))); }
        public Vector3 UpDir { get => Vector3.TransformNormal(Vector3.UnitY, Matrix4x4.CreateFromQuaternion(Quaternion.Inverse(Orientation))); }
        public Vector3 EyeDir { get => Vector3.TransformNormal(Vector3.UnitZ, Matrix4x4.CreateFromQuaternion(Quaternion.Inverse(Orientation))); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PointF Project(Vector3 node)
        {
            float r = 2 * (float)Math.Tan(FOV / 2 * Math.PI / 180);
            int sz = ViewHalfSize;
            float f = sz/r;
            float camDist = SceneSize / r;
            var R = Matrix4x4.CreateFromQuaternion(Orientation);
            return Project(node, f, camDist, R);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected PointF Project(Vector3 node, float f, float camDist, Matrix4x4 R)
        {
            var point = Vector3.Transform(node-Target, R);
            PointF pixel = new PointF(
                            +f * point.X / (camDist - point.Z),
                            -f * point.Y / (camDist - point.Z));
            return pixel;
        }
        public RectangleF Project(Bounds bounds)
        {
            var nodes = bounds.GetNodes();
            var points = Project(nodes);
            if (points.Length>0) {

                RectangleF box = new RectangleF(points[0], SizeF.Empty);
                for (int i = 1; i < points.Length; i++)
                {
                    box.X = Math.Min(box.X, points[i].X);
                    box.Y = Math.Min(box.Y, points[i].Y);
                    box.Width = Math.Max(box.Width, points[i].X-box.X);
                    box.Height = Math.Max(box.Height, points[i].Y-box.Y);
                }
                return box;
            }
            return RectangleF.Empty;
        }
        public PointF[] Project(Triangle triangle) => Project(new[] { triangle.A, triangle.B, triangle.C });
        public PointF[] Project(Polygon polygon) => Project(polygon.Nodes);
        /// <summary>
        /// Projects the specified nodes into a 2D canvas by applied the camera 
        /// orientation and projection.
        /// </summary>
        /// <param name="nodes">The nodes to project.</param>
        /// <returns>A list of Gdi points</returns>
        public PointF[] Project(Vector3[] nodes)
        {
            float r = 2 * (float)Math.Tan(FOV / 2 * Math.PI / 180);
            float camDist = SceneSize / r;
            float f = ViewHalfSize/r;
            var R = Matrix4x4.CreateFromQuaternion(Orientation);

            var points = new PointF[nodes.Length];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = Project(nodes[i], f, camDist, R);
            }

            return points;
        }
        /// <summary>
        /// Uses the arc-ball calculation to find the 3D point corresponding to a
        /// particular pixel on the screen
        /// </summary>
        /// <param name="pixel">The pixel with origin on center of control.</param>
        /// <param name="arcBallFactor"></param>
        public Vector3 UnProject(Point pixel, float arcBallFactor = 1)
        {
            Ray ray = CastRayThroughPixel(pixel);
            Sphere arcBall = new Sphere(Target, arcBallFactor * SceneSize/2);
            var Rt = Matrix4x4.CreateFromQuaternion(Quaternion.Inverse(Orientation));
            if (arcBall.Hit(ray, out var t))
            {
                return Vector3.Transform(ray.GetPointAlong(t), Rt);
            }
            return Vector3.Zero;
        }

        public Ray CastRayThroughPixel(Point pixel)
        {
            float r = 2 * (float)Math.Tan(FOV / 2 * Math.PI / 180);
            float camDist = SceneSize / r;
            int sz = ViewHalfSize;
            var Rt = Matrix4x4.CreateFromQuaternion(Quaternion.Inverse(Orientation));
            var origin = Vector3.Transform(new Vector3(0, 0, camDist), Rt);
            var dir = Vector3.TransformNormal(new Vector3(r*pixel.X/sz, -r*pixel.Y/sz, -1), Rt);
            return new Ray(origin, dir);
        }

        public bool IsVisible(Polygon polygon)
            => polygon.Nodes.Length <3 || IsVisible(polygon.Nodes[0]-Target, polygon.Normal);
        /// <summary>
        /// Determines whether a face is visible. 
        /// </summary>
        /// <param name="position">Any position on the face.</param>
        /// <param name="normal">The face normal.</param>
        public bool IsVisible(Vector3 position, Vector3 normal)
        {
            float λ = Vector3.Dot(normal, position - EyePos);

            return λ < 0;
        }

    }
}
