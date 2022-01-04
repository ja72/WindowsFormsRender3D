using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using JA.Model;

namespace JA.UI
{
    public delegate void CameraPaintHandler(Camera camera, Graphics g);

    public class Camera
    {
        public event CameraPaintHandler Paint;

        /// <summary>
        /// Initializes a new instance of the <see cref="Camera" /> class.
        /// </summary>
        /// <param name="target">The target control to draw scene.</param>
        /// <param name="fov">
        /// The FOV angle (make zero for orthographic projection).
        /// </param>
        /// <param name="sceneSize">Size of the scene across/</param>
        public Camera(Control target, float fov, float sceneSize = 1f)
        {
            Target = target;
            FOV = fov;
            SceneSize = sceneSize;
            LightPos = new Vector3(0 * sceneSize, 0 * sceneSize / 2, -sceneSize);
            Orientation = Quaternion.Identity;
            target.Paint += (s, ev) =>
            {
                Paint?.Invoke(this, ev.Graphics);
            };
        }

        public Control Target { get; }
        public float SceneSize { get; set; }
        public float FOV { get; set; }
        public Quaternion Orientation { get; set; }
        public Vector3 LightPos { get; set; }

        public float DrawSize { get => 2 * (float)Math.Tan(FOV / 2 * Math.PI / 180); }
        public Vector3 EyePos { get => Vector3.Transform(Vector3.UnitZ * SceneSize / DrawSize, Quaternion.Inverse(Orientation)); }

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
            float L = SceneSize / r;
            int wt = Target.ClientSize.Width - Target.Margin.Left - Target.Margin.Right;
            int ht = Target.ClientSize.Height - Target.Margin.Top - Target.Margin.Bottom;
            int sz = Math.Min(ht, wt);
            var R = Matrix4x4.CreateFromQuaternion(Orientation);

            var points = new PointF[nodes.Length];
            for (int i = 0; i < points.Length; i++)
            {
                var point = Vector3.Transform(nodes[i], R);
                points[i] = new PointF(
                    +sz / 2 * point.X / (r * (L - point.Z)),
                    -sz / 2 * point.Y / (r * (L - point.Z)));
            }

            return points;
        }
        public bool IsVisible(Polygon polygon)
            => polygon.Nodes.Length <3 || IsVisible(polygon.Nodes[0], polygon.Normal);
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
