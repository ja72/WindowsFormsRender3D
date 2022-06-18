using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Numerics;
using System.ComponentModel;

namespace JA.Drawing
{
    using JA.Geometry;

    public class Scene
    {
        readonly List<VisibleObject> drawable;

        public Scene()
        {
            drawable = new List<VisibleObject>();
            Triad = new VisibleTriad("W");
        }
        [Category("Model")]
        public VisibleObject[] Drawable => drawable.ToArray();
        public T AddDrawing<T>(T drawing) where T : VisibleObject
        {
            drawable.Add(drawing);
            return drawing;
        }
        [Category("Model")]
        public VisibleTriad Triad { get; }
        public void Render(Graphics g, Camera camera)
        {
            var state = camera.SetupView(g);
            //var light = camera.LightPos.Unit();
            //var R = Matrix4x4.CreateFromQuaternion(Quaternion.Inverse(camera.Orientation));
            //light = Vector3.TransformNormal(light, R);

            Triad.Render(g, camera, Pose.Identity);

            foreach (var item in drawable)
            {
                item.Render(g, camera, Pose.Identity);
            }
            Gdi.Style.Clear();
            g.Restore(state);

        }

    }


}
