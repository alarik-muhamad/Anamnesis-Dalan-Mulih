using System;
using System.Drawing;
using AnamnesisDalanMulih.Engine;

namespace AnamnesisDalanMulih.Game
{
    public enum ProjectileKind
    {
        Bullet, // peluru kecil melayang lurus (Keraguan)
        Wave    // gelombang rendah menyusuri lantai (Depresi)
    }

    /// <summary>Serangan jarak jauh musuh. Melukai pemain 1 HP kalau kena; bisa dilompati.</summary>
    public sealed class Projectile
    {
        public ProjectileKind Kind;
        public float X, Y, VX, VY;
        public int W, H;
        public float Life;
        public bool Dead;
        public Color Color;

        public Projectile(ProjectileKind kind, float x, float y, int w, int h, float vx, float vy, float life, Color color)
        {
            Kind = kind; X = x; Y = y; W = w; H = h; VX = vx; VY = vy; Life = life; Color = color;
        }

        public void Update(float dt, Tilemap map)
        {
            X += VX * dt;
            Y += VY * dt;
            Life -= dt;
            if (Life <= 0f) { Dead = true; return; }

            int ts = Tilemap.TileSize;
            if (Kind == ProjectileKind.Bullet)
            {
                int col = (int)Math.Floor((X + W / 2f) / ts);
                int row = (int)Math.Floor((Y + H / 2f) / ts);
                if (map.IsSolid(col, row)) Dead = true; // kena dinding
            }
            else
            {
                // Gelombang hilang di dinding dan di tepi jurang
                float front = VX > 0f ? X + W : X;
                int col = (int)Math.Floor(front / ts);
                int rowMid = (int)Math.Floor((Y + H / 2f) / ts);
                int rowBelow = (int)Math.Floor((Y + H + 1f) / ts);
                if (map.IsSolid(col, rowMid) || !map.IsSolid(col, rowBelow)) Dead = true;
            }
        }

        public bool Overlaps(Player p)
        {
            return p.Overlaps(X, Y, W, H);
        }

        public void Draw(Graphics g, int camX, int camY)
        {
            int x = (int)Math.Round(X) - camX;
            int y = (int)Math.Round(Y) - camY;
            using (Pen pen = new Pen(Color))
            {
                if (Kind == ProjectileKind.Bullet)
                {
                    int cx = x + W / 2, cy = y + H / 2;
                    g.DrawPolygon(pen, new Point[]
                    {
                        new Point(cx, cy - 3), new Point(cx + 3, cy), new Point(cx, cy + 3), new Point(cx - 3, cy)
                    });
                }
                else
                {
                    g.DrawRectangle(pen, x, y, W - 1, H - 1);
                    g.DrawLine(pen, x + 2, y + 3, x + W - 3, y + 3);
                }
            }
        }
    }
}
