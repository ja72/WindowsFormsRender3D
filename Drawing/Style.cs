using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JA.Drawing
{
    public class Style : IDisposable, ICloneable
    {
        public Style()
        {
            Stroke = new Pen(Color.Black, 1);
            Fill = new SolidBrush(Color.Black);
            Font = new Font(SystemFonts.CaptionFont, FontStyle.Regular);
        }
        public Style(Color color, FontStyle font = FontStyle.Regular)
        {
            Stroke = new Pen(color, 1);
            Fill = new SolidBrush(color);
            Font = new Font(SystemFonts.CaptionFont, font);
        }

        public Style(Pen stroke, SolidBrush fill, Font font)
        {
            Stroke=stroke;
            Fill=fill;
            Font=font;
        }
        public Style(Style copy)
        {
            Stroke = copy.Stroke.Clone() as Pen;
            Fill = copy.Fill.Clone() as SolidBrush;
            Font = copy.Font.Clone() as Font;
        }
        public static implicit operator Style(Color color) => new Style(color);

        public void Clear()
        {
            Stroke.Transform.Reset();
            Stroke.Color = Color.Black;
            Stroke.Width=1;
            Stroke.DashCap = DashCap.Flat;
            Stroke.EndCap = LineCap.NoAnchor;
            Stroke.StartCap = LineCap.NoAnchor;
            Stroke.DashStyle = DashStyle.Solid;
            Stroke.LineJoin = LineJoin.Round;

            Fill.Color = Color.Black;            
        }
        public Pen Stroke { get; }
        public SolidBrush Fill { get; }
        public Font Font { get; private set; }
        public bool StartArrow
        {
            get => Stroke.StartCap == LineCap.Custom;
            set
            {
                if (value)
                {
                    AddStartArrow();
                }
                else
                {
                    Stroke.StartCap = LineCap.NoAnchor;
                }
            }
        }
        public bool EndArrow
        {
            get => Stroke.EndCap == LineCap.Custom;
            set
            {
                if (value)
                {
                    AddEndArrow();
                }
                else
                {
                    Stroke.EndCap = LineCap.NoAnchor;
                }
            }
        }


        public static implicit operator Pen(Style style) => style.Stroke;
        public static implicit operator Brush(Style style) => style.Fill;

        public void AddStartArrow(float factor = 1)
        {
            Stroke.CustomStartCap = new AdjustableArrowCap(factor*Stroke.Width*5/2f, factor*12/2f*Stroke.Width);
        }
        public void AddEndArrow(float factor = 1)
        {
            Stroke.CustomEndCap = new AdjustableArrowCap(factor*Stroke.Width*5/2f, factor*12/2f*Stroke.Width);
        }
        public void ClearStartArrow() => Stroke.StartCap = LineCap.NoAnchor;
        public void ClearEndArrow() => Stroke.EndCap= LineCap.NoAnchor;

        #region ICloneable Members
        public Style Clone() => new Style(this);
        object ICloneable.Clone() => Clone();
        #endregion

        #region Disposing
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Stroke.Dispose();
                    Fill.Dispose();
                    Font.Dispose();
                }

                disposedValue=true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
