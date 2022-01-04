using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Numerics;
using JA.UI;

namespace JA.Model
{
    public class Scene
    {
        public Scene()
        {
            Meshes = new List<Mesh>();
        }

        public List<Mesh> Meshes { get; }

        public void Render(Camera camera, Graphics g)
        {
            var state = g.Save();
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TranslateTransform(camera.Target.ClientSize.Width / 2f, camera.Target.ClientSize.Height / 2f);
            using (var pen = new Pen(Color.Black, 0))
            using (var fill = new SolidBrush(Color.Black))
            {
                var light = camera.LightPos.Unit();
                var R = Matrix4x4.CreateFromQuaternion(Quaternion.Inverse(camera.Orientation));
                light = Vector3.TransformNormal(light, R);

                foreach (var mesh in Meshes)
                {
                    for (int index = 0; index < mesh.Elements.Count; index++)
                    {
                        var element = mesh.Elements[index];
                        var gp = new GraphicsPath();
                        var poly = mesh.GetPolygon(index);

                        gp.AddPolygon(camera.Project(poly));

                        if (camera.IsVisible(poly))
                        {
                            var (H, S, L) = element.Color.GetHsl();
                            var color = (H, S, L).GetColor(0.5f);
                            fill.Color = color;
                            g.FillPath(fill, gp);
                        }
                        pen.Color = element.Color;
                        g.DrawPath(pen, gp);
                    }
                }
            }
            g.Restore(state);
        }
    }
}
