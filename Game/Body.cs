using System;
using AnamnesisDalanMulih.Engine;

namespace AnamnesisDalanMulih.Game
{
    /// <summary>
    /// Benda berfisika (pemain, musuh): posisi, kecepatan, ukuran hitbox, dan gerak
    /// dengan tabrakan tile. X,Y = pojok kiri-atas hitbox.
    /// </summary>
    public class Body
    {
        public float X, Y, VX, VY;
        public int W, H;
        public bool OnGround;

        public Body(float x, float y, int w, int h)
        {
            X = x; Y = y; W = w; H = h;
        }

        public float CenterX { get { return X + W / 2f; } }
        public float CenterY { get { return Y + H / 2f; } }

        public bool Overlaps(Body o)
        {
            return X < o.X + o.W && X + W > o.X && Y < o.Y + o.H && Y + H > o.Y;
        }

        public bool Overlaps(float rx, float ry, int rw, int rh)
        {
            return X < rx + rw && X + W > rx && Y < ry + rh && Y + H > ry;
        }

        /// <summary>Gerak horizontal. Mengembalikan true kalau menabrak tile padat.</summary>
        protected bool MoveX(float dx, Tilemap map)
        {
            X += dx;
            int ts = Tilemap.TileSize;
            int top = (int)Math.Floor(Y / ts);
            int bottom = (int)Math.Floor((Y + H - 0.01f) / ts);
            int left = (int)Math.Floor(X / ts);
            int right = (int)Math.Floor((X + W - 0.01f) / ts);

            for (int ty = top; ty <= bottom; ty++)
            {
                for (int tx = left; tx <= right; tx++)
                {
                    if (!map.IsSolid(tx, ty)) continue;

                    if (dx > 0f) X = tx * ts - W;
                    else if (dx < 0f) X = (tx + 1) * ts;
                    VX = 0f;
                    return true;
                }
            }
            return false;
        }

        /// <summary>Gerak vertikal. Mengembalikan true kalau menabrak; OnGround diisi di sini.</summary>
        protected bool MoveY(float dy, Tilemap map)
        {
            OnGround = false;
            Y += dy;
            int ts = Tilemap.TileSize;
            int top = (int)Math.Floor(Y / ts);
            int bottom = (int)Math.Floor((Y + H - 0.01f) / ts);
            int left = (int)Math.Floor(X / ts);
            int right = (int)Math.Floor((X + W - 0.01f) / ts);

            for (int ty = top; ty <= bottom; ty++)
            {
                for (int tx = left; tx <= right; tx++)
                {
                    if (!map.IsSolid(tx, ty)) continue;

                    if (dy > 0f)
                    {
                        Y = ty * ts - H;
                        OnGround = true;
                    }
                    else if (dy < 0f)
                    {
                        Y = (ty + 1) * ts;
                    }
                    VY = 0f;
                    return true;
                }
            }
            return false;
        }
    }
}
