using System;
using System.Drawing;

namespace AnamnesisDalanMulih.Game
{
    public enum EyeStyle
    {
        One,    // satu garis kecil     -> pemain
        Two,    // dua titik            -> NPC
        Angry   // dua titik + alis     -> musuh
    }

    /// <summary>
    /// "Sprite" kotak outline yang dipakai pemain, NPC, dan musuh.
    /// Beda karakter = beda warna + gaya mata. Digambar lewat kode, bukan gambar.
    /// </summary>
    public static class BoxSprite
    {
        public static void Draw(Graphics g, int x, int y, int w, int h, Color color, EyeStyle eyes, bool facingRight)
        {
            using (Pen pen = new Pen(color))
            using (SolidBrush brush = new SolidBrush(color))
            {
                g.DrawRectangle(pen, x, y, w - 1, h - 1);

                int ey = y + Math.Max(3, h / 4);

                if (eyes == EyeStyle.One)
                {
                    int eyeX = facingRight ? x + w - 3 : x + 2;
                    g.DrawLine(pen, eyeX, ey, eyeX, ey + 2);
                    return;
                }

                int gap = Math.Max(4, w / 3);
                int front = facingRight ? x + w - 4 : x + 2;
                int back = facingRight ? front - gap : front + gap;
                g.FillRectangle(brush, front, ey, 2, 2);
                g.FillRectangle(brush, back, ey, 2, 2);

                if (eyes == EyeStyle.Angry)
                {
                    int lx = Math.Min(front, back);
                    int rx = Math.Max(front, back);
                    // alis miring ke arah tengah = tampang marah
                    g.DrawLine(pen, lx - 1, ey - 2, lx + 2, ey - 1);
                    g.DrawLine(pen, rx + 3, ey - 2, rx, ey - 1);
                }
            }
        }
    }
}
