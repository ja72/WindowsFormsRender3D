using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;

namespace JA.Drawing
{
    public unsafe class FastBitmap : IDisposable
    {
        private readonly byte* ptr;
        public FastBitmap(Bitmap bmp, ImageLockMode lockMode)
        {
            Image = bmp;
            Lockmode = lockMode;

            PixelLength = System.Drawing.Image.GetPixelFormatSize(bmp.PixelFormat) / 8;
            var rect = new Rectangle(0, 0, Width, Height);
            Data = bmp.LockBits(rect, lockMode, PixelFormat);
            ptr = (byte*)Data.Scan0.ToPointer();
        }

        #region Properties
        public int Width { get => Image.Width; }
        public int Height { get => Image.Height; }
        public PixelFormat PixelFormat { get => Image.PixelFormat; }

        public ImageLockMode Lockmode { get; }

        public Bitmap Image { get; }

        public int PixelLength { get; }

        public BitmapData Data { get; }

        #endregion


        public void FillColors(Func<int, int, Color> map)
        {
            for (int i = 0; i < Image.Height; i++)
            {
                for (int j = 0; j < Image.Width; j++)
                {
                    this[j, i] = map(j, i);
                }
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> GetSpan(int x, int y)
        {
            var pixel = ptr + y * Data.Stride + x * PixelLength;
            return new Span<byte>(pixel, PixelLength);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetSpan(int x, int y, Span<byte> bgra)
        {
            bgra.CopyTo(GetSpan(x, y));
        }

        public Color this[int x, int y]
        {
            get
            {
                var s = GetSpan(x, y); //BGRA
                return Color.FromArgb(s[3], s[2], s[1], s[0]);
            }
            set
            {
                var s = GetSpan(x, y); //BGRA
                s[0] = value.B;
                s[1] = value.G;
                s[2] = value.R;
                s[3] = value.A;
            }
        }

        public void Dispose()
        {
            Image.UnlockBits(Data);
        }
    }
}
