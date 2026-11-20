using System.Drawing;

namespace AnamnesisDalanMulih.Engine
{
    /// <summary>Helper teks, tombol, dan kotak outline. Ganti font ke .ttf sendiri nanti lewat PrivateFontCollection.</summary>
    public static class Ui
    {
        public static readonly Font Title = new Font("Arial", 20f, FontStyle.Bold, GraphicsUnit.Pixel);
        public static readonly Font Body = new Font("Arial", 10f, FontStyle.Regular, GraphicsUnit.Pixel);
        public static readonly Font Small = new Font("Arial", 8f, FontStyle.Regular, GraphicsUnit.Pixel);

        private static readonly StringFormat CenterFormat = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        public static void DrawText(Graphics g, string text, Font font, Color color, float x, float y)
        {
            using (SolidBrush brush = new SolidBrush(color))
            {
                g.DrawString(text, font, brush, x, y);
            }
        }

        /// <summary>Teks satu baris, rata tengah horizontal layar.</summary>
        public static void DrawCentered(Graphics g, string text, Font font, Color color, float y)
        {
            using (SolidBrush brush = new SolidBrush(color))
            {
                RectangleF area = new RectangleF(0, y, GameWindow.ViewW, font.Height + 2);
                g.DrawString(text, font, brush, area, new StringFormat { Alignment = StringAlignment.Center });
            }
        }

        /// <summary>Teks yang otomatis turun baris di dalam area.</summary>
        public static void DrawWrapped(Graphics g, string text, Font font, Color color, Rectangle area, bool center)
        {
            using (SolidBrush brush = new SolidBrush(color))
            {
                StringFormat sf = new StringFormat();
                if (center) sf.Alignment = StringAlignment.Center;
                g.DrawString(text, font, brush, new RectangleF(area.X, area.Y, area.Width, area.Height), sf);
            }
        }

        /// <summary>Tombol outline (tanpa gambar). selected = outline lebih terang.</summary>
        public static void DrawButton(Graphics g, Rectangle rect, string label, bool selected)
        {
            Color c = selected ? Color.White : Color.FromArgb(120, 120, 140);
            using (Pen pen = new Pen(c))
            using (SolidBrush brush = new SolidBrush(c))
            {
                g.DrawRectangle(pen, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
                g.DrawString(label, Body, brush, rect, CenterFormat);
            }
        }

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
