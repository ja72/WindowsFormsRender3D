using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace JA.Drawing
{
    using JA.Geometry;
    using System.Diagnostics;
    using static SingleConstants;

    public delegate void CameraPaintHandler(Camera camera, Graphics g);

    public class Camera
    {
        public event CameraPaintHandler Paint;

        // Zoom settings
        public float ZoomFactor { get; set; } = 1.1f; // How much to zoom per wheel tick
        public float MinEyeDistance { get; set; } = 0.1f; // Minimum zoom in
        public float MaxEyeDistance { get; set; } = 10f; // Maximum zoom out

        // Pan sensitivity
        public float PanSensitivity { get; set; } = 1.0f;

        // Rotation sensitivity
        public float RotationSensitivity { get; set; } = 1.0f;

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

            // Set reasonable defaults for zoom limits based on scene size
            MaxEyeDistance = sceneSize * 5f;
            MinEyeDistance = sceneSize * 0.1f;

            control.Paint += (s, ev) =>
            {
                Paint?.Invoke(this, ev.Graphics);
            };

            control.MouseDown += (s, ev) =>
            {
                var center = ViewCenter;
                Point current = new Point(ev.X - center.X, ev.Y - center.Y);
                mouse = (current, mouse.msMove, mouse.msUp, ev.Button);
            };

            control.MouseMove += (s, ev) =>
            {
                var center = ViewCenter;
                Point current = new Point(ev.X - center.X, ev.Y - center.Y);
                mouse = (mouse.msDown, current, mouse.msUp, ev.Button);

                // Right mouse button - rotation (improved)
                if (ev.Button == MouseButtons.Right)
                {
                    RotateCamera(mouse.msDown, mouse.msMove);
                    mouse = (mouse.msMove, mouse.msMove, mouse.msUp, mouse.buttons);
                }
                // Middle mouse button - panning
                else if (ev.Button == MouseButtons.Middle)
                {
                    Pan(mouse.msDown, mouse.msMove);
                    mouse = (mouse.msMove, mouse.msMove, mouse.msUp, mouse.buttons);
                }
            };

            control.MouseUp += (s, ev) =>
            {
                var center = ViewCenter;
                Point current = new Point(ev.X - center.X, ev.Y - center.Y);
                mouse = (mouse.msDown, mouse.msMove, current, ev.Button);
            };

            // Add mouse wheel event for zooming
            control.MouseWheel += (s, ev) =>
            {
                Zoom(ev.Location, ev.Delta);
            };
        }

        internal (Point msDown, Point msMove, Point msUp, MouseButtons buttons) mouse;

        /// <summary>
        /// Zooms the camera in or out based on mouse wheel delta
        /// </summary>
        /// <param name="delta">Mouse wheel delta (positive = zoom in, negative = zoom out)</param>
#pragma warning disable IDE0060 // Remove unused parameter
        public void Zoom(Point location, int delta)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            // Get current scene size
            float currentSceneSize = SceneSize;
            float newSceneSize;

            if (delta > 0)
            {
                // Zoom in (decrease scene size to see less area but more detail)
                newSceneSize = currentSceneSize / ZoomFactor;
            }
            else
            {
                // Zoom out (increase scene size to see more area but less detail)
                newSceneSize = currentSceneSize * ZoomFactor;
            }

            // Calculate the equivalent eye distance limits for scene size
            // FOV stays constant, so DrawSize = 2 * tan(FOV/2) stays constant
            float drawSize = DrawSize; // This is constant based on FOV
            float minSceneSize = MinEyeDistance * drawSize;
            float maxSceneSize = MaxEyeDistance * drawSize;

            // Clamp to limits
            newSceneSize = Math.Max(minSceneSize, Math.Min(maxSceneSize, newSceneSize));

            // Set the new scene size (FOV stays the same, only distance changes)
            SceneSize = newSceneSize;
        }

        /// <summary>
        /// Rotates the camera around the target based on mouse movement
        /// </summary>
        /// <param name="startPixel">Starting pixel position</param>
        /// <param name="currentPixel">Current pixel position</param>
        private void RotateCamera(Point startPixel, Point currentPixel)
        {
            float deltaX = (currentPixel.X - startPixel.X) * RotationSensitivity;
            float deltaY = (currentPixel.Y - startPixel.Y) * RotationSensitivity;

            // Convert pixel movement to radians (adjust sensitivity as needed)
            float sensitivity = 0.005f; // Adjust this to control rotation speed
            float yawAngle = -deltaX * sensitivity;   // Horizontal movement = yaw (around Y axis)
            float pitchAngle = -deltaY * sensitivity; // Vertical movement = pitch (around X axis)

            // Get current camera orientation vectors
            Vector3 right = RightDir;
            Vector3 up = Vector3.UnitY; // Use world up for yaw to prevent rolling

            // Create rotation quaternions
            Quaternion yawRotation = Quaternion.CreateFromAxisAngle(up, yawAngle);
            Quaternion pitchRotation = Quaternion.CreateFromAxisAngle(right, pitchAngle);

            // Apply rotations: pitch first (around camera right), then yaw (around world up)
            Orientation = yawRotation * pitchRotation * Orientation;

            // Normalize to prevent accumulation of floating point errors
            Orientation = Quaternion.Normalize(Orientation);
        }

        /// <summary>
        /// Pans the camera target based on mouse movement
        /// </summary>
        /// <param name="startPixel">Starting pixel position</param>
        /// <param name="currentPixel">Current pixel position</param>
        private void Pan(Point startPixel, Point currentPixel)
        {
            // Calculate pixel delta
            float deltaX = (currentPixel.X - startPixel.X) * PanSensitivity;
            float deltaY = (currentPixel.Y - startPixel.Y) * PanSensitivity;

            // Convert pixel movement to world space movement
            float pixelsPerUnit = Scale;
            float worldDeltaX = deltaX / pixelsPerUnit;
            float worldDeltaY = deltaY / pixelsPerUnit;

            // Apply camera orientation to the pan movement
            Vector3 rightVector = RightDir;
            Vector3 upVector = UpDir;

            // Move target in camera-relative directions
            Vector3 panOffset = rightVector * (-worldDeltaX) + upVector * worldDeltaY;
            Target += panOffset;
        }

        /// <summary>
        /// Resets the camera to its default position and orientation
        /// </summary>
        public void ResetView()
        {
            Target = Vector3.Zero;
            Orientation = Quaternion.Identity;
            EyeDistance = 1.0f;
        }

        public GraphicsState SetupView(Graphics g, SmoothingMode smoothing = SmoothingMode.AntiAlias)
        {
            var gs = g.Save();
            g.SmoothingMode = smoothing;
            var center = ViewCenter;
            g.TranslateTransform(center.X, center.Y);
            return gs;
        }

        public Point ViewCenter => new Point(
            OnControl.Margin.Left + OnControl.ClientSize.Width / 2,
            OnControl.Margin.Top + OnControl.ClientSize.Height / 2);
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
        public int ViewHalfSize => ViewSize / 2;
        public float Scale => ViewHalfSize / SceneSize;
        public float DrawSize
        {
            get => 2 * (float)Math.Tan(FOV / 2 * Math.PI / 180);
            set
            {
                FOV = 360 / pi * Atan(value / 2);
            }
        }
        /// <summary>
        /// Get the pixels per model unit scale.
        /// </summary>
        public Vector3 EyePos { get => Target + Vector3.Transform(Vector3.UnitZ * SceneSize / DrawSize, Quaternion.Inverse(Orientation)); }
        public float EyeDistance
        {
            get => SceneSize / DrawSize;
            set
            {
                DrawSize = SceneSize / value;
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
            float f = sz / r;
            float camDist = SceneSize / r;
            var R = Matrix4x4.CreateFromQuaternion(Orientation);
            return Project(node, f, camDist, R);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected PointF Project(Vector3 node, float f, float camDist, Matrix4x4 R)
        {
            var point = Vector3.Transform(node - Target, R);
            PointF pixel = new PointF(
                            +f * point.X / (camDist - point.Z),
                            -f * point.Y / (camDist - point.Z));
            return pixel;
        }
        public RectangleF Project(Bounds bounds)
        {
            var nodes = bounds.GetNodes();
            var points = Project(nodes);
            if (points.Length > 0)
            {
                RectangleF box = new RectangleF(points[0], SizeF.Empty);
                for (int i = 1; i < points.Length; i++)
                {
                    box.X = Math.Min(box.X, points[i].X);
                    box.Y = Math.Min(box.Y, points[i].Y);
                    box.Width = Math.Max(box.Width, points[i].X - box.X);
                    box.Height = Math.Max(box.Height, points[i].Y - box.Y);
                }
                return box;
            }
            return RectangleF.Empty;
        }
        public PointF[] Project(Triangle triangle) => Project(triangle.A, triangle.B, triangle.C);
        public PointF[] Project(Polygon polygon) => Project(polygon.Nodes);
        /// <summary>
        /// Projects the specified nodes into a 2D canvas by applied the camera 
        /// orientation and projection.
        /// </summary>
        /// <param name="nodes">The nodes to project.</param>
        /// <returns>A list of Gdi points</returns>
        public PointF[] Project(params Vector3[] nodes)
        {
            float r = 2 * (float)Math.Tan(FOV / 2 * Math.PI / 180);
            float camDist = SceneSize / r;
            float f = ViewHalfSize / r;
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
            Sphere arcBall = new Sphere(Target, arcBallFactor * SceneSize / 2);
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
            var dir = Vector3.TransformNormal(new Vector3(r * pixel.X / sz, -r * pixel.Y / sz, -1), Rt);
            return new Ray(origin, dir);
        }

        public bool IsVisible(Polygon polygon)
            => polygon.Nodes.Length < 3 || IsVisible(polygon.Nodes[0] - Target, polygon.Normal);
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