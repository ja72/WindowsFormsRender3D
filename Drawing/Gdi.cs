using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JA.Drawing
{
    using static SingleConstants;
     
    public static class Gdi
    {
        /// <summary> 
        /// Converts RGB to HSL 
        /// </summary> 
        /// <remarks>Takes advantage of whats already built in to .NET by using the Color.GetHue, Color.GetSaturation and Color.GetBrightness methods</remarks> 
        /// <param name="color">A Color to convert</param> 
        /// <returns>An HSL tuple</returns> 
        public static (float H, float S, float L) GetHsl(this Color color)
        {
            var H = color.GetHue() / 360f;
            var L = color.GetBrightness();
            var S = color.GetSaturation();

            return (H, S, L);
        }
        /// <summary>
        /// Converts a color from HSL to RGB
        /// </summary>
        /// <remarks>Adapted from the algorithm in Foley and Van-Dam</remarks>
        /// <param name="hsl">The HSL tuple</param>
        /// <returns>A Color structure containing the equivalent RGB values</returns>
        public static Color GetColor(this (float H, float S, float L) hsl, float alpha = 1f)
        {
            float r, g, b;
            float temp1, temp2;
            // Clamp HSL between 0..1
            hsl = (
                Math.Max(0, Math.Min(1f, hsl.H)),
                Math.Max(0, Math.Min(1f, hsl.S)),
                Math.Max(0, Math.Min(1f, hsl.L)));
            if (hsl.L == 0) r = g = b = 0;
            else
            {
                if (hsl.S == 0) r = g = b = hsl.L;
                else
                {
                    temp2 = hsl.L <= 0.5f ? hsl.L * (1f + hsl.S) : hsl.L + hsl.S - hsl.L * hsl.S;
                    temp1 = 2f * hsl.L - temp2;

                    var t3 = new[] { hsl.H + 1 / 3f, hsl.H, hsl.H - 1 / 3f };
                    var clr = new float[] { 0, 0, 0 };
                    for (int i = 0; i < 3; i++)
                    {
                        if (t3[i] < 0) t3[i] += 1f;
                        if (t3[i] > 1) t3[i] -= 1f;

                        if (6.0 * t3[i] < 1.0) clr[i] = temp1 + (temp2 - temp1) * t3[i] * 6f;
                        else if (2.0 * t3[i] < 1.0) clr[i] = temp2;
                        else if (3.0 * t3[i] < 2.0) clr[i] = temp1 + (temp2 - temp1) * (2 / 3f - t3[i]) * 6;
                        else clr[i] = temp1;
                    }
                    r = clr[0];
                    g = clr[1];
                    b = clr[2];
                }
            }

            return Color.FromArgb((int)(255*alpha), (int)(255 * r), (int)(255 * g), (int)(255 * b));
        }

        public static Style Style { get; } = new Style();

        public static void DrawLine(this Graphics g, Color color, PointF start, PointF end, float width = 1f)
        {
            Style.Stroke.Color = color;
            Style.Stroke.Width = width;
            g.DrawLine(Style.Stroke, start, end);
        }
        public static void DrawArrow(this Graphics g, Color color, PointF start, PointF end, float width = 1f)
        {
            Style.Stroke.Color = color;
            Style.Stroke.Width = width;
            Style.AddEndArrow(width);
            g.DrawLine(Style.Stroke, start, end);
            Style.ClearEndArrow();
        }

        public static void DrawPoint(this Graphics g, Color color, PointF point, float size = 4f)
        {
            Style.Clear();
            Style.Fill.Color = color;
            g.FillEllipse(Style.Fill, point.X - size/2, point.Y - size/2, size, size);
        }
        public static void DrawLabel(this Graphics g, Color color, PointF point, string text, ContentAlignment alignment, int offset = 2)
        {
            float x = point.X, y = point.Y;
            var box = g.MeasureString(text, Style.Font);
            switch (alignment)
            {
                case ContentAlignment.TopLeft:
                    x -= box.Width - offset;
                    y -= box.Height - offset;
                    break;
                case ContentAlignment.TopCenter:
                    x -= box.Width/2;
                    y -= box.Height - offset;
                    break;
                case ContentAlignment.TopRight:
                    x += offset;
                    y -= box.Height - offset;
                    break;
                case ContentAlignment.MiddleLeft:
                    x -= box.Width - offset;
                    y -= box.Height/2;
                    break;
                case ContentAlignment.MiddleCenter:
                    x -= box.Width/2;
                    y -= box.Height/2;
                    break;
                case ContentAlignment.MiddleRight:
                    x += offset;
                    y -= box.Height/2;
                    break;
                case ContentAlignment.BottomLeft:
                    x -= box.Width - offset;
                    y += offset;
                    break;
                case ContentAlignment.BottomCenter:
                    x -= box.Width/2;
                    y += offset;
                    break;
                case ContentAlignment.BottomRight:
                    x += offset;
                    y += offset;
                    break;
                default:
                    throw new NotSupportedException();
            }
            Style.Fill.Color = color;
            g.DrawString(text, Style.Font, Style.Fill, x, y);
        }

        public static void DrawPath(this Graphics g, GraphicsPath path, Color color, bool fill = true)
        {
            if (fill)
            {
                var (H, S, L) = color.GetHsl();
                var fillColor = (H, S, L).GetColor(0.5f);
                Style.Fill.Color = fillColor;
                g.FillPath(Style.Fill, path);
            }
            Style.Stroke.LineJoin = LineJoin.Round;
            Style.Stroke.Color = color;
            Style.Stroke.Width = 1;
            g.DrawPath(Style.Stroke, path);
            Style.Clear();
        }

        public static void DrawCircle(this Graphics g, PointF center, float radius, Color color, bool fill = true)
        {
            var gp = new GraphicsPath();
            gp.AddEllipse(center.X-radius, center.Y-radius, 2*radius,2*radius);
            DrawPath(g, gp, color, fill);
        }
        public static void DrawEllipse(this Graphics g, PointF center, float majorAxis, float minorAxis, Color color, bool fill = true)
        {
            var gp = new GraphicsPath();
            gp.AddEllipse(center.X-majorAxis, center.Y-minorAxis, 2*majorAxis, 2*minorAxis);
            DrawPath(g, gp, color, fill);
        }
        public static void DrawEllipse(this Graphics g, PointF center, float majorAxis, float minorAxis, float angle, Color color, bool fill = true)
        {
            var gp = new GraphicsPath();
            gp.AddEllipse(-majorAxis, -minorAxis, 2*majorAxis, 2*minorAxis);
            var gs = g.Save();
            g.TranslateTransform(center.X, center.Y);
            g.RotateTransform(angle/deg);
            DrawPath(g, gp, color, fill);
            g.Restore(gs);
        }
        public static void DrawCurve(this Graphics g, PointF[] points, Color color, bool fill = true)
        {
            var gp = new GraphicsPath();
            gp.AddCurve(points);
            DrawPath(g, gp, color, fill);
        }
        public static void DrawClosedCurve(this Graphics g, PointF[] points, Color color, bool fill = true)
        {
            var gp = new GraphicsPath();
            gp.AddClosedCurve(points);
            DrawPath(g, gp, color, fill);
        }
        public static void DrawPolygon(this Graphics g, PointF[] points, Color color, bool fill = true)
        {
            var gp = new GraphicsPath();
            gp.AddPolygon(points);
            DrawPath(g, gp, color, fill);
        }
    }
}
