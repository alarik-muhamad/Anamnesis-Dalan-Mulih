using System;
using System.Drawing;
using AnamnesisDalanMulih.Engine;

namespace AnamnesisDalanMulih.Game
{
    /// <summary>
    /// Pemain platformer: gerak, lompat (coyote time, jump buffer, variable jump), collision tile.
    /// Kalau PNG sprite belum ada, pemain digambar sebagai kotak outline putih bermata satu.
    /// </summary>
    public sealed class Player : Body
    {
        public const int Width = 10;
        public const int Height = 14;

        // Tuning (ubah angka-angka ini untuk mengatur "game feel")
        private const float MoveSpeed = 90f;     // px/detik
        private const float Gravity = 900f;      // px/detik^2
        private const float MaxFall = 300f;      // kecepatan jatuh maksimum
        private const float JumpSpeed = 300f;    // lompatan penuh ~ 47 px
        private const float CoyoteTime = 0.10f;  // masih boleh lompat sesaat setelah jatuh dari tepi
        private const float JumpBuffer = 0.10f;  // tekan lompat sedikit sebelum mendarat tetap dihitung

        // Ukuran frame sprite PNG (ubah sesuai gambar kalian)
        private const int FrameSize = 16;

        public bool FacingRight = true;
        public float Invuln;   // detik kebal setelah terluka / respawn

        private float _coyote;
        private float _buffer;
        private float _stun;   // detik kontrol dikunci setelah terpukul

        private readonly Animation _idle;
        private readonly Animation _run;
        private readonly Animation _jump;
        private Animation _current;

        public Player(float x, float y) : base(x, y, Width, Height)
        {
            // Semua boleh null: kalau file belum ada, pemain digambar sebagai outline.
            _idle = Load("Sprites/player_idle.png", 4f);
            _run = Load("Sprites/player_run.png", 7f);
            _jump = Load("Sprites/player_jump.png", 1f);
            _current = _idle;
        }

        private static Animation Load(string path, float fps)
        {
            Bitmap bmp = AssetManager.TryLoadImage(path);
            return bmp == null ? null : new Animation(bmp, FrameSize, FrameSize, fps);
        }

        public void Respawn(float x, float y)
        {
            X = x;
            Y = y;
            VX = 0;
            VY = 0;
            _stun = 0;
            Invuln = 1.5f;
        }

        /// <summary>Terpukul musuh: terpental menjauh dan kebal sebentar. dir = -1 (kiri) atau 1 (kanan).</summary>
        public void Hurt(int dir)
        {
            Invuln = 1.2f;
            _stun = 0.25f;
            VX = dir * 110f;
            VY = -170f;
            OnGround = false;
        }

        /// <summary>Memantul setelah menginjak musuh. held = tombol lompat ditahan (pantulan lebih tinggi).</summary>
        public void Bounce(bool held)
        {
            VY = held ? -260f : -190f;
            OnGround = false;
            _coyote = 0f;
        }

        public void Update(float dt, InputState input, Tilemap map)
        {
            if (Invuln > 0f) Invuln -= dt;

            if (_stun > 0f)
            {
                _stun -= dt; // kontrol terkunci, VX dipertahankan dari Hurt()
            }
            else
            {
                float dir = 0f;
                if (input.Left) dir -= 1f;
                if (input.Right) dir += 1f;
                VX = dir * MoveSpeed;
                if (dir != 0f) FacingRight = dir > 0f;

                if (OnGround) _coyote = CoyoteTime; else _coyote -= dt;
                if (input.JumpPressed) _buffer = JumpBuffer; else _buffer -= dt;

                if (_buffer > 0f && _coyote > 0f)
                {
                    VY = -JumpSpeed;
                    _buffer = 0f;
                    _coyote = 0f;
                    OnGround = false;
                }

                // Lepas tombol saat naik = lompatan lebih pendek
                if (input.JumpReleased && VY < 0f) VY *= 0.5f;
            }

            VY += Gravity * dt;
            if (VY > MaxFall) VY = MaxFall;

            MoveX(VX * dt, map);
            MoveY(VY * dt, map);

            UpdateAnimation(dt);
        }

        /// <summary>Menyentuh tile bahaya (duri)? Hitbox dikecilkan 2 px supaya tidak terlalu galak.</summary>
        public bool IsTouchingHazard(Tilemap map)
        {
            int ts = Tilemap.TileSize;
            int left = (int)Math.Floor((X + 2) / ts);
            int right = (int)Math.Floor((X + W - 2) / ts);
            int top = (int)Math.Floor((Y + 2) / ts);
            int bottom = (int)Math.Floor((Y + H - 1) / ts);

            for (int ty = top; ty <= bottom; ty++)
                for (int tx = left; tx <= right; tx++)
                    if (map.IsHazard(tx, ty)) return true;
            return false;
        }

        private void UpdateAnimation(float dt)
        {
            Animation next = !OnGround ? _jump : (Math.Abs(VX) > 0.1f ? _run : _idle);
            if (next == null) next = _idle;

            if (next != _current)
            {
                _current = next;
                if (_current != null) _current.Reset();
            }
            if (_current != null) _current.Update(dt);
        }

        public void Draw(Graphics g, int camX, int camY)
        {
            // Berkedip saat kebal
            if (Invuln > 0f && ((int)(Invuln * 14f) % 2 == 0)) return;

            int px = (int)Math.Round(X) - camX;
            int py = (int)Math.Round(Y) - camY;

            if (_current != null)
            {
                int sx = px - (_current.FrameW - Width) / 2;
                int sy = py + Height - _current.FrameH;
                _current.Draw(g, sx, sy, !FacingRight);
            }
            else
            {
                BoxSprite.Draw(g, px, py, Width, Height, Palette.Player, EyeStyle.One, FacingRight);
            }
        }
    }
}
