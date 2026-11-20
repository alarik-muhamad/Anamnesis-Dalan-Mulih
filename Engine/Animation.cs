using System;
using System.Drawing;

namespace AnamnesisDalanMulih.Engine
{
    /// <summary>
    /// Animasi dari satu strip horizontal (semua frame berukuran sama).
    /// Contoh: player_run.png = 6 frame 16x16 -> 96x16 piksel.
    /// </summary>
    public sealed class Animation
    {
        private readonly Bitmap _sheet;
        private float _time;

        public int FrameW { get; private set; }
        public int FrameH { get; private set; }
        public int FrameCount { get; private set; }
        public float Fps { get; private set; }

        public Animation(Bitmap sheet, int frameW, int frameH, float fps)
        {
            _sheet = sheet;
            FrameW = frameW;
            FrameH = frameH;
            Fps = fps;
            FrameCount = Math.Max(1, sheet.Width / frameW);
        }

        public int CurrentFrame
        {
            get { return ((int)(_time * Fps)) % FrameCount; }
        }

        public void Reset() { _time = 0f; }

        public void Update(float dt)
        {
            _time += dt;
            float total = FrameCount / Fps;
            if (_time >= total) _time %= total;
        }

        /// <summary>flipX = cermin horizontal (dipakai saat karakter hadap kiri).</summary>
        public void Draw(Graphics g, int x, int y, bool flipX)
        {
            RectangleF src = new RectangleF(CurrentFrame * FrameW, 0, FrameW, FrameH);
            PointF[] dst;
            if (flipX)
            {
                dst = new PointF[]
                {
                    new PointF(x + FrameW, y), new PointF(x, y), new PointF(x + FrameW, y + FrameH)
                };
            }
            else
            {
                dst = new PointF[]
                {
                    new PointF(x, y), new PointF(x + FrameW, y), new PointF(x, y + FrameH)
                };
            }
            g.DrawImage(_sheet, dst, src, GraphicsUnit.Pixel);
        }
    }
}
