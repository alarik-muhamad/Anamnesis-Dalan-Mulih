using System;
using System.Drawing;
using AnamnesisDalanMulih.Engine;

namespace AnamnesisDalanMulih.Game
{
    /// <summary>
    /// Musuh berupa kotak merah/oranye/ungu bermata marah dengan lifebar.
    /// Cara melukai: diinjak dari atas (satu injakan = satu HP).
    /// Berpatroli kiri-kanan di antara MinX..MaxX, berbalik di tepi jurang dan dinding.
    /// </summary>
    public sealed class Enemy : Body
    {
        private const float HopSpeed = 230f;

        public string Name;
        public Color Color;
        public EyeStyle Eyes = EyeStyle.Angry;
        public int MaxHp;
        public int Hp;
        public float Speed;
        public bool Hops;          // melompat-lompat (untuk boss)
        public bool Harmless;      // tidak melukai pemain (boneka latihan)
        public bool FacingRight;
        public float Invuln;       // detik kebal + terdiam setelah terkena injakan
        public float MinX = float.MinValue;
        public float MaxX = float.MaxValue;
        public Action<IGameHost> OnDefeat;

        private int _dir = -1;
        private float _hopTimer = 1.2f;

        public Enemy(string name, float x, float y, int w, int h, int hp, float speed, Color color)
            : base(x, y, w, h)
        {
            Name = name; MaxHp = hp; Hp = hp; Speed = speed; Color = color;
        }

        public bool Alive { get { return Hp > 0; } }

        /// <summary>Kurangi HP. Mengembalikan false kalau sedang kebal atau sudah mati.</summary>
        public bool Hit(int damage)
        {
            if (Invuln > 0f || Hp <= 0) return false;
            Hp -= damage;
            if (Hp < 0) Hp = 0;
            Invuln = 0.6f;
            return true;
        }

        public void Update(float dt, Tilemap map)
        {
            if (Invuln > 0f) Invuln -= dt;
            bool stunned = Invuln > 0f;

            VX = (stunned || Speed <= 0f) ? 0f : _dir * Speed;

            if (Hops && OnGround && !stunned)
            {
                _hopTimer -= dt;
                if (_hopTimer <= 0f)
                {
                    VY = -HopSpeed;
                    _hopTimer = 1.6f;
                    OnGround = false;
                }
            }

            VY += 900f * dt;
            if (VY > 300f) VY = 300f;

            bool blocked = MoveX(VX * dt, map);
            MoveY(VY * dt, map);

            if (Speed > 0f)
            {
                if (blocked) _dir = -_dir;
                else if (X < MinX) { X = MinX; _dir = 1; }
                else if (X + W > MaxX) { X = MaxX - W; _dir = -1; }
                else if (OnGround && !stunned && !GroundAhead(map)) _dir = -_dir;
            }
            FacingRight = _dir > 0;
        }

        private bool GroundAhead(Tilemap map)
        {
            float probeX = _dir > 0 ? X + W + 1f : X - 1f;
            int col = (int)Math.Floor(probeX / Tilemap.TileSize);
            int row = (int)Math.Floor((Y + H + 1f) / Tilemap.TileSize);
            return map.IsSolid(col, row);
        }

        public void Draw(Graphics g, int camX, int camY)
        {
            if (!Alive) return;

            int x = (int)Math.Round(X) - camX;
            int y = (int)Math.Round(Y) - camY;

            Color c = Color;
            if (Invuln > 0f && ((int)(Invuln * 20f) % 2 == 0)) c = Color.White; // berkedip putih saat kena
            BoxSprite.Draw(g, x, y, W, H, c, Eyes, FacingRight);

            // Lifebar di atas kepala
            int bw = Math.Max(W + 4, 16);
            int bx = x + W / 2 - bw / 2;
            int by = y - 7;
            Ui.DrawBox(g, new Rectangle(bx, by, bw, 4), Color.FromArgb(200, 200, 210));
            int fill = (bw - 2) * Hp / MaxHp;
            if (fill > 0) Ui.FillBox(g, new Rectangle(bx + 1, by + 1, fill, 2), Color.FromArgb(230, 60, 80));
        }
    }

    public enum HitResult { None, Stomp, PlayerHurt }

    public static class Combat
    {
        /// <summary>
        /// Injak = pemain sedang jatuh dan kakinya (frame sebelumnya) berada di atas kepala musuh.
        /// Selain itu, sentuhan samping/bawah melukai pemain (kecuali musuh sedang kebal atau tidak berbahaya).
        /// </summary>
        public static HitResult Check(Player p, Enemy e, float prevBottom)
        {
            if (!e.Alive || !p.Overlaps(e)) return HitResult.None;
            if (p.VY > 0f && prevBottom <= e.Y + 5f) return HitResult.Stomp;
            if (e.Harmless || e.Invuln > 0f || p.Invuln > 0f) return HitResult.None;
            return HitResult.PlayerHurt;
        }
    }
}
