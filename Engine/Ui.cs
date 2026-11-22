using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace AnamnesisDalanMulih.Engine
{
    /// <summary>
    /// Helper teks, tombol, dan kotak outline.
    ///
    /// PENTING soal teks: gambar gim digambar di buffer kecil 320x180 lalu diperbesar. Kalau teks ikut digambar
    /// di buffer kecil, font setinggi 8-10 piksel punya lebar huruf yang dibulatkan ke piksel utuh, sehingga
    /// spasi antar kata hampir tidak terlihat. Karena itu semua teks di sini TIDAK digambar langsung: ia dicatat
    /// (antrean), lalu digambar oleh GameWindow pada resolusi layar (FlushText) memakai font yang diperbesar
    /// sesuai skala jendela. Cara memanggilnya tidak berubah (DrawText, DrawCentered, DrawWrapped, ...).
    /// Konsekuensi: teks selalu tampil di ATAS semua gambar lain di frame yang sama.
    /// </summary>
    public static class Ui
    {
        // Ukuran font dalam PIKSEL LOGIS (koordinat 320x180). Skala ke layar dilakukan otomatis.
        public static readonly Font Title = new Font("Arial", 20f, FontStyle.Bold, GraphicsUnit.Pixel);
        public static readonly Font Body = new Font("Arial", 10f, FontStyle.Regular, GraphicsUnit.Pixel);
        public static readonly Font Small = new Font("Arial", 8f, FontStyle.Regular, GraphicsUnit.Pixel);

        private sealed class TextItem
        {
            public string Text;
            public Font Font;
            public Color Color;
            public RectangleF Area;           // koordinat logis
            public StringAlignment Align;     // horizontal
            public StringAlignment VAlign;    // vertikal
            public bool Wrap;
        }

        private static readonly List<TextItem> Pending = new List<TextItem>();
        private static readonly Dictionary<string, Font> FontCache = new Dictionary<string, Font>();

        // Untuk mengukur lebar teks (nametag) dengan metrik tanpa pembulatan
        private static readonly Bitmap MeasureBitmap = new Bitmap(1, 1);
        private static readonly Graphics MeasureGfx = Graphics.FromImage(MeasureBitmap);

        // ------------------------------------------------------------ antrean teks
        /// <summary>Dipanggil GameWindow sebelum scene menggambar satu frame.</summary>
        public static void BeginFrame()
        {
            Pending.Clear();
        }

        private static void Queue(string text, Font font, Color color, RectangleF area,
                                  StringAlignment align, StringAlignment valign, bool wrap)
        {
            if (string.IsNullOrEmpty(text)) return;
            Pending.Add(new TextItem
            {
                Text = text, Font = font, Color = color, Area = area,
                Align = align, VAlign = valign, Wrap = wrap
            });
        }

        /// <summary>
        /// Menggambar semua teks yang dicatat pada resolusi layar. originX/originY = pojok kiri-atas area gim
        /// di layar, scale = pembesaran bulat (mis. 4 untuk 1280x720). Dipanggil GameWindow di OnPaint.
        /// </summary>
        public static void FlushText(Graphics g, int originX, int originY, int scale)
        {
            if (Pending.Count == 0) return;

            GraphicsState state = g.Save();
            g.SetClip(new Rectangle(originX, originY, GameWindow.ViewW * scale, GameWindow.ViewH * scale));
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            foreach (TextItem t in Pending)
            {
                Font f = GetFont(t.Font, scale);
                RectangleF r = new RectangleF(
                    originX + t.Area.X * scale, originY + t.Area.Y * scale,
                    t.Area.Width * scale, t.Area.Height * scale);

                using (SolidBrush brush = new SolidBrush(t.Color))
                using (StringFormat sf = MakeFormat(t.Align, t.VAlign, t.Wrap))
                {
                    g.DrawString(t.Text, f, brush, r, sf);
                }
            }
            g.Restore(state);
        }

        /// <summary>
        /// Format tanpa padding bawaan GDI+ dan tanpa pembulatan lebar huruf. Teks yang kepanjangan
        /// sengaja dibiarkan meluber keluar kotak (tidak dipotong diam-diam) supaya kelihatan saat mengedit dialog.
        /// </summary>
        private static StringFormat MakeFormat(StringAlignment align, StringAlignment valign, bool wrap)
        {
            StringFormat sf = new StringFormat(StringFormat.GenericTypographic);
            sf.Alignment = align;
            sf.LineAlignment = valign;
            if (wrap) sf.FormatFlags &= ~(StringFormatFlags.NoWrap | StringFormatFlags.LineLimit);
            else sf.FormatFlags |= StringFormatFlags.NoWrap;
            return sf;
        }

        /// <summary>Font logis (piksel) diperbesar sesuai skala layar. Di-cache supaya tidak dibuat tiap frame.</summary>
        private static Font GetFont(Font logical, float scale)
        {
            float px = logical.Unit == GraphicsUnit.Pixel ? logical.Size : logical.SizeInPoints * 96f / 72f;
            string key = logical.FontFamily.Name + "|" + px + "|" + (int)logical.Style + "|" + scale;
            Font f;
            if (!FontCache.TryGetValue(key, out f))
            {
                f = new Font(logical.FontFamily, px * scale, logical.Style, GraphicsUnit.Pixel);
                FontCache[key] = f;
            }
            return f;
        }

        /// <summary>Lebar teks dalam piksel logis, diukur pada ukuran besar lalu dibagi (tanpa pembulatan).</summary>
        public static float MeasureWidth(string text, Font font)
        {
            const float RefScale = 8f;
            if (string.IsNullOrEmpty(text)) return 0f;
            using (StringFormat sf = MakeFormat(StringAlignment.Near, StringAlignment.Near, false))
            {
                SizeF size = MeasureGfx.MeasureString(text, GetFont(font, RefScale), 100000, sf);
                return size.Width / RefScale;
            }
        }

        // ------------------------------------------------------------ teks
        /// <summary>Teks satu baris dengan pojok kiri-atas di (x, y) piksel logis.</summary>
        public static void DrawText(Graphics g, string text, Font font, Color color, float x, float y)
        {
            Queue(text, font, color, new RectangleF(x, y, GameWindow.ViewW * 2f, font.Height + 2f),
                  StringAlignment.Near, StringAlignment.Near, false);
        }

        /// <summary>Teks satu baris, rata tengah horizontal layar.</summary>
        public static void DrawCentered(Graphics g, string text, Font font, Color color, float y)
        {
            Queue(text, font, color, new RectangleF(0, y, GameWindow.ViewW, font.Height + 2f),
                  StringAlignment.Center, StringAlignment.Near, false);
        }

        /// <summary>Teks yang otomatis turun baris di dalam area.</summary>
        public static void DrawWrapped(Graphics g, string text, Font font, Color color, Rectangle area, bool center)
        {
            Queue(text, font, color, new RectangleF(area.X, area.Y, area.Width, area.Height),
                  center ? StringAlignment.Center : StringAlignment.Near, StringAlignment.Near, true);
        }

        /// <summary>Tombol outline (tanpa gambar). selected = outline lebih terang.</summary>
        public static void DrawButton(Graphics g, Rectangle rect, string label, bool selected)
        {
            Color c = selected ? Color.White : Color.FromArgb(120, 120, 140);
            using (Pen pen = new Pen(c))
            {
                g.DrawRectangle(pen, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
            }
            Queue(label, Body, c, new RectangleF(rect.X, rect.Y, rect.Width, rect.Height),
                  StringAlignment.Center, StringAlignment.Center, false);
        }

        /// <summary>Nametag kecil dengan latar gelap, rata tengah di centerX, mulai dari y = top.</summary>
        public static void DrawTag(Graphics g, string text, int centerX, int top, Color color)
        {
            int w = (int)Math.Ceiling(MeasureWidth(text, Small)) + 4;
            Rectangle r = new Rectangle(centerX - w / 2, top, w, Small.Height + 1);
            FillBox(g, r, Color.FromArgb(190, 8, 8, 16));
            Queue(text, Small, color, new RectangleF(r.X, r.Y, r.Width, r.Height),
                  StringAlignment.Center, StringAlignment.Center, false);
        }

        // ------------------------------------------------------------ bentuk
        /// <summary>Kotak outline kosong (slot inventori, HP bar, dll).</summary>
        public static void DrawBox(Graphics g, Rectangle rect, Color color)
        {
            using (Pen pen = new Pen(color))
            {
                g.DrawRectangle(pen, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
            }
        }

        public static void FillBox(Graphics g, Rectangle rect, Color color)
        {
            using (SolidBrush brush = new SolidBrush(color))
            {
                g.FillRectangle(brush, rect);
            }
        }
    }
}
