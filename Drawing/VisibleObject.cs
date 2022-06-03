using JA.Geometry;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Numerics;

namespace JA.Drawing
{
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public abstract class VisibleObject : IRender
    {

        public abstract void Render(Graphics g, Camera camera, Pose pose);

        [Experimental]
        public void RayTrace(Graphics g, Camera camera, IRayTarget shape, Color color)
        {
            var box = shape.GetBounds();
            RectangleF bounds = camera.Project(box);
            var center = new PointF(
                bounds.X+bounds.Width/2,
                bounds.Y+bounds.Height/2);
            var size = Math.Max(bounds.Width, bounds.Height);
            int n = (int)Math.Ceiling(size);
            var img = new Bitmap(n, n);

            (float h, float s, float l) = color.GetHsl();
            var f = new FastBitmap(img, ImageLockMode.ReadWrite);
            f.FillColors((i, j) =>
            {
                int y = (int)(center.X- size/2 + size*i/(n-1));
                int x = (int)(center.Y- size/2 + size*j/(n-1));
                var ray = camera.CastRayThroughPixel(new Point(x, y));
                color = Color.Transparent;
                if (shape.Hit(ray, out var t))
                {
                    var position = ray.GetPointAlong(t);
                    var norm = shape.GetNormal(position);
                    float λ = -Vector3.Dot(norm, position - camera.EyePos)/Vector3.Distance(position, camera.EyePos);
                    color = (h, s, l*λ).GetColor();
                }
                return color;
            });
            f.Dispose();
            g.DrawImage(img, center.X-size/2, center.Y-size/2);

        }

        public abstract override string ToString();
    }
    public class VisibleTriad : VisibleObject
    {
        public VisibleTriad(string label, float scale = 1)
            : this(label, Pose.Identity, scale)
        { }
        public VisibleTriad(string label, Pose origin, float scale = 1)
        {
            Geometry = origin;
            Label=label;
            Scale=scale;
        }
        public string Label { get; set; }
        public float Scale { get; set; }
        public Pose Geometry { get; set; }

        public override void Render(Graphics g, Camera camera, Pose pose)
        {
            var nodes = new Vector3[] {
                    Vector3.Zero,
                    Scale*Vector3.UnitX,
                    Scale*Vector3.UnitY,
                    Scale*Vector3.UnitZ };

            var triad = pose.FromLocal( Geometry.FromLocal(nodes) );

            var cs = camera.Project(triad);

            Gdi.Style.Stroke.Width = 1;
            Gdi.Style.AddEndArrow();
            g.DrawLine(Color.Red, cs[0], cs[1]);
            g.DrawLine(Color.Green, cs[0], cs[2]);
            g.DrawLine(Color.Blue, cs[0], cs[3]);            
            Gdi.Style.Clear();

            g.DrawLabel(Color.Black, cs[0], Label, ContentAlignment.TopRight);
        }
        public override string ToString() => $"Triad(Label={Label}, Origin={Geometry})";
    }

    public abstract class VisibleSolid : VisibleObject
    {
        protected VisibleSolid(ISolid geometry, Color color)
        {
            Color=color;
            Geometry=geometry;
        }

        public Color Color { get; set; }
        public ISolid Geometry { get; set; }
    }

    public abstract class VisibleGeometry : VisibleObject 
    {
        protected VisibleGeometry(IGeometry geometry, Color color)
        {
            Color=color;
            Geometry=geometry;
        }

        public Color Color { get; set; }
        public IGeometry Geometry { get; set; }
    }
    public class VisibleSphere : VisibleSolid
    {
        public VisibleSphere(Sphere sphere, Color color) : base(sphere, color)
        {
            Sphere = sphere;
        }
        public Sphere Sphere { get; }
        public override void Render(Graphics g, Camera camera, Pose pose)
        {
            var center = camera.Project(pose.FromLocal(Geometry.Center));
            var radius = camera.Scale * Sphere.Radius;

            var gp = new GraphicsPath();
            gp.AddEllipse(center.X - radius, center.Y-radius, 2*radius, 2*radius);
            g.DrawPath(gp, Color, true);
            Gdi.Style.Clear();
        }
        public override string ToString() => $"SphereObject({Geometry})";
    }
    public class VisibleBezier : VisibleGeometry
    {
        public VisibleBezier(BezierCurve curve, Color color) : base(curve, color)
        {
            StartArrow = false;
            EndArrow = false;
            Curve = curve;
        }
        public bool StartArrow { get; set; }
        public bool EndArrow { get; set; }
        public BezierCurve Curve { get; }
        public override void Render(Graphics g, Camera camera, Pose pose)
        {
            float ds = 8*camera.SceneSize/ camera.ViewSize;
            int numDivisions = (int)Math.Ceiling(Curve.Span.Length()/ds);
            if (numDivisions == 0) return;
            var nodes = pose.FromLocal(Curve.GetNodes(numDivisions));
            var points = camera.Project(nodes);
            Gdi.Style.Stroke.Width = 1;
            Gdi.Style.EndArrow = EndArrow;
            Gdi.Style.StartArrow = StartArrow;
            g.DrawCurve(points, Color, false);
            Gdi.Style.Clear();
        }
        public override string ToString() => $"BezierObject({Geometry})";
    }

    public class VisibleMesh : VisibleSolid
    {
        public VisibleMesh(Mesh mesh, Color color) : base(mesh, color)
        {
            Mesh= mesh;
            ElementColors = Enumerable.Repeat(color, mesh.Elements.Count).ToArray();
        }
        public Mesh Mesh { get; }
        public Vector3 Center { get => Geometry.Center; }
        public float Volume { get => Geometry.Volume; }
        public Color[] ElementColors { get; }
        public override void Render(Graphics g, Camera camera, Pose pose)
        {
            Gdi.Style.Clear();
            Gdi.Style.Stroke.Width = 0;
            for (int k = 0; k < Mesh.Elements.Count; k++)
            {
                var element = Mesh.Elements[k];
                var color = ElementColors[k];
                var poly = Mesh.GetPolygon(k).FromLocal(pose);

                var gp = new GraphicsPath();
                gp.AddPolygon(camera.Project(poly));
                bool fill = camera.IsVisible(poly);
                g.DrawPath(gp, color, fill);
            }
            Gdi.Style.Clear();
        }
        public override string ToString() => $"MeshObject({Geometry})";
    }
}
