using System;
using System.Collections.Generic;
using System.Drawing;
using AnamnesisDalanMulih.Engine;

namespace AnamnesisDalanMulih.Game
{
    /// <summary>
    /// Perilaku musuh. Tiap jenis punya "tanda" (telegraph) yang bisa dibaca pemain.
    /// Patrol  : hanya mondar-mandir.
    /// Charger : melihat pemain -> bergetar + tanda seru -> menerjang lurus -> lelah (mudah diinjak).
    /// Shooter : diam, membidik (titik terang di depan) lalu menembak peluru.
    /// Hider   : bergantian terbuka dan bersembunyi (garis putus-putus = kebal dan tidak berbahaya).
    /// Slammer : bergetar -> melompat dan membanting -> gelombang di lantai kiri-kanan -> lelah.
    /// </summary>
    public enum EnemyBehavior { Patrol, Charger, Shooter, Hider, Slammer }

    /// <summary>
    /// Musuh berupa kotak bermata marah dengan lifebar dan nametag.
    /// Cara melukai: diinjak dari atas (satu injakan = satu HP). Pemain terlempar menjauh,
    /// musuh tidak terdiam dan langsung membalas sesuai perilakunya (lihat OnHit).
    /// </summary>
    public sealed class Enemy : Body
    {
        private enum EState { Patrol, Windup, Dash, Tired, Hidden, Jump }

        private const float HopSpeed = 230f;

        // Charger
        private const float ChargerSight = 96f, ChargerSightY = 20f;
        private const float ChargerWindup = 0.55f, ChargerDashSpeed = 160f, ChargerDashTime = 0.5f;
        private const float ChargerTired = 1.1f, ChargerCooldown = 1.2f;
        // Shooter
        private const float ShooterSight = 140f, ShooterSightY = 40f;
        private const float ShooterWindup = 0.5f, ShooterCooldown = 1.8f, BulletSpeed = 90f;
        // Hider
        private const float HiderExposed = 2.5f, HiderHidden = 1.6f;

        public string Name;
        public Color Color;
        public EyeStyle Eyes = EyeStyle.Angry;
        public EnemyBehavior Behavior = EnemyBehavior.Patrol;
        public int MaxHp;
        public int Hp;
        public float Speed;
        public bool Hops;          // melompat-lompat saat patroli (dipakai Hider)
        public bool Harmless;      // tidak melukai pemain (boneka latihan)
        public bool Hidden;        // sedang bersembunyi: kebal dan tidak berbahaya
        public bool FacingRight;
        public float Invuln;       // detik kebal + terdiam setelah terkena injakan
        public float MinX = float.MinValue;
        public float MaxX = float.MaxValue;
        public Action<IGameHost> OnDefeat;

        private EState _state = EState.Patrol;
        private float _stateTime;
        private float _cooldown;
        private float _tiredLimit = ChargerTired;
        private int _dir = -1;
        private float _hopTimer = 1.2f;
        private float _windupBonus;   // tambahan waktu tanda saat membalas (memberi pemain waktu mendarat)

        public Enemy(string name, float x, float y, int w, int h, int hp, float speed, Color color)
            : base(x, y, w, h)
        {
            Name = name; MaxHp = hp; Hp = hp; Speed = speed; Color = color;
        }

        public bool Alive { get { return Hp > 0; } }
        public string StateName { get { return _state.ToString(); } }
        public bool Enraged { get { return Hp * 2 <= MaxHp; } }

        /// <summary>Sedang memberi tanda sebelum menyerang / bersembunyi.</summary>
        public bool Telegraph
        {
            get
            {
                switch (Behavior)
                {
                    case EnemyBehavior.Charger:
                    case EnemyBehavior.Shooter:
                    case EnemyBehavior.Slammer:
                        return _state == EState.Windup;
                    case EnemyBehavior.Hider:
                        return _state == EState.Patrol && _stateTime > HiderExposed - 0.5f;
                    default:
                        return false;
                }
            }
        }

        /// <summary>Teks nametag di bawah musuh (ditambah status kalau relevan).</summary>
        public string Tag
        {
            get
            {
                if (Hidden) return Name + " [kebal]";
                if (_state == EState.Tired && (Behavior == EnemyBehavior.Charger || Behavior == EnemyBehavior.Slammer))
                    return Name + " [lelah]";
                return Name;
            }
        }

        /// <summary>Kurangi HP. Mengembalikan false kalau sedang kebal, bersembunyi, atau sudah mati.</summary>
        public bool Hit(int damage)
        {
            if (Invuln > 0f || Hp <= 0 || Hidden) return false;
            Hp -= damage;
            if (Hp < 0) Hp = 0;
            Invuln = 0.5f;
            OnHit();
            return true;
        }

        /// <summary>
        /// Reaksi setelah terkena injakan. Musuh membalas, bukan terdiam:
        /// Charger langsung bersiap menerjang, Shooter membidik, Slammer memotong jalannya lalu membanting,
        /// Hider kaget dan bersembunyi. Kalau sedang lelah/menerjang, jadwalnya dibiarkan berjalan.
        /// </summary>
        private void OnHit()
        {
            switch (Behavior)
            {
                case EnemyBehavior.Charger:
                    if (_state == EState.Patrol)
                    {
                        _cooldown = 0f;
                        SetState(EState.Windup);
                        _windupBonus = 0.25f;
                    }
                    break;
                case EnemyBehavior.Hider:
                    SetState(EState.Hidden);
                    break;
                case EnemyBehavior.Slammer:
                    if (_state == EState.Patrol)
                    {
                        SetState(EState.Windup);
                        _windupBonus = 0.25f;
                    }
                    break;
                case EnemyBehavior.Shooter:
                    if (_state != EState.Windup)
                    {
                        _cooldown = 0f;
                        SetState(EState.Windup);
                        _windupBonus = 0.25f;
                    }
                    break;
            }
        }

        private void SetState(EState s)
        {
            _state = s;
            _stateTime = 0f;
            Hidden = (s == EState.Hidden);
            if (s != EState.Windup) _windupBonus = 0f;
        }

        // ------------------------------------------------------------ update
        public void Update(float dt, Tilemap map, Player player, List<Projectile> shots)
        {
            if (Invuln > 0f) Invuln -= dt;
            if (_cooldown > 0f) _cooldown -= dt;
            _stateTime += dt;
            // Catatan: musuh TIDAK terdiam saat terkena injakan. Invuln hanya mencegah kena dobel.
            switch (Behavior)
            {
                case EnemyBehavior.Charger: UpdateCharger(player); break;
                case EnemyBehavior.Shooter: UpdateShooter(player, shots); break;
                case EnemyBehavior.Hider: UpdateHider(dt); break;
                case EnemyBehavior.Slammer: UpdateSlammer(player, shots); break;
                default: VX = Speed <= 0f ? 0f : _dir * Speed; break;
            }

            VY += 900f * dt;
            if (VY > 300f) VY = 300f;

            bool blocked = MoveX(VX * dt, map);
            MoveY(VY * dt, map);

            // Batas jalur patroli, dinding, dan tepi jurang
            if (X < MinX) { X = MinX; HitEdge(); }
            else if (X + W > MaxX) { X = MaxX - W; HitEdge(); }
            else if (blocked) HitEdge();
            else if (OnGround && !GroundAhead(map) &&
                     (_state == EState.Dash || (_state == EState.Patrol && Speed > 0f)))
                HitEdge();

            FacingRight = _dir > 0;
        }

        private void HitEdge()
        {
            if (_state == EState.Dash) SetState(EState.Tired);
            else if (_state == EState.Patrol) _dir = -_dir;
        }

        private bool Sees(Player p, float rangeX, float rangeY)
        {
            return Math.Abs(p.CenterX - CenterX) < rangeX && Math.Abs(p.CenterY - CenterY) < rangeY;
        }

        private void FaceToward(Player p)
        {
            _dir = p.CenterX >= CenterX ? 1 : -1;
        }

        private void UpdateCharger(Player p)
        {
            switch (_state)
            {
                case EState.Patrol:
                    VX = _dir * Speed;
                    if (_cooldown <= 0f && Sees(p, ChargerSight, ChargerSightY))
                    {
                        FaceToward(p);
                        SetState(EState.Windup);
                    }
                    break;
                case EState.Windup:
                    VX = 0f;
                    FaceToward(p); // arah terkunci pada frame terakhir sebelum menerjang
                    if (_stateTime >= ChargerWindup + _windupBonus) SetState(EState.Dash);
                    break;
                case EState.Dash:
                    VX = _dir * ChargerDashSpeed;
                    if (_stateTime >= ChargerDashTime) SetState(EState.Tired);
                    break;
                case EState.Tired:
                    VX = 0f;
                    if (_stateTime >= _tiredLimit)
                    {
                        _tiredLimit = ChargerTired;
                        _cooldown = ChargerCooldown;
                        SetState(EState.Patrol);
                    }
                    break;
                default:
                    SetState(EState.Patrol);
                    break;
            }
        }

        private void UpdateShooter(Player p, List<Projectile> shots)
        {
            VX = 0f;
            switch (_state)
            {
                case EState.Patrol:
                    FaceToward(p);
                    if (_cooldown <= 0f && Sees(p, ShooterSight, ShooterSightY)) SetState(EState.Windup);
                    break;
                case EState.Windup:
                    FaceToward(p);
                    if (_stateTime >= ShooterWindup + _windupBonus)
                    {
                        float bx = _dir > 0 ? X + W : X - 6f;
                        float by = Y + H / 2f - 3f;
                        shots.Add(new Projectile(ProjectileKind.Bullet, bx, by, 6, 6, _dir * BulletSpeed, 0f, 3f, Color));
                        _cooldown = ShooterCooldown;
                        SetState(EState.Patrol);
                    }
                    break;
                default:
                    SetState(EState.Patrol);
                    break;
            }
        }

        private void UpdateHider(float dt)
        {
            switch (_state)
            {
                case EState.Patrol:
                    VX = _dir * Speed;
                    if (Hops && OnGround)
                    {
                        _hopTimer -= dt;
                        if (_hopTimer <= 0f)
                        {
                            VY = -HopSpeed;
                            _hopTimer = 1.6f;
                            OnGround = false;
                        }
                    }
                    if (_stateTime >= HiderExposed) SetState(EState.Hidden);
                    break;
                case EState.Hidden:
                    VX = 0f;
                    if (_stateTime >= HiderHidden) SetState(EState.Patrol);
                    break;
                default:
                    SetState(EState.Patrol);
                    break;
            }
        }

        private void UpdateSlammer(Player p, List<Projectile> shots)
        {
            bool rage = Enraged; // separuh HP terakhir: lebih cepat dan gelombang lebih kencang
            switch (_state)
            {
                case EState.Patrol:
                    VX = _dir * Speed;
                    if (OnGround && _stateTime >= (rage ? 1.2f : 2.5f))
                    {
                        FaceToward(p);
                        SetState(EState.Windup);
                    }
                    break;
                case EState.Windup:
                    VX = 0f;
                    FaceToward(p);
                    if (_stateTime >= (rage ? 0.4f : 0.7f) + _windupBonus)
                    {
                        VY = -260f;
                        OnGround = false;
                        SetState(EState.Jump);
                    }
                    break;
                case EState.Jump:
                    VX = _dir * 55f;
                    if (OnGround && _stateTime > 0.15f)
                    {
                        float speed = rage ? 100f : 70f;
                        float y = Y + H - 8f;
                        shots.Add(new Projectile(ProjectileKind.Wave, X - 14f, y, 14, 8, -speed, 0f, 3.5f, Color));
                        shots.Add(new Projectile(ProjectileKind.Wave, X + W, y, 14, 8, speed, 0f, 3.5f, Color));
                        SetState(EState.Tired);
                    }
                    break;
                case EState.Tired:
                    VX = 0f;
                    if (_stateTime >= (rage ? 0.7f : 0.9f)) SetState(EState.Patrol);
                    break;
                default:
                    SetState(EState.Patrol);
                    break;
            }
        }

        private bool GroundAhead(Tilemap map)
        {
            float probeX = _dir > 0 ? X + W + 1f : X - 1f;
            int col = (int)Math.Floor(probeX / Tilemap.TileSize);
            int row = (int)Math.Floor((Y + H + 1f) / Tilemap.TileSize);
            return map.IsSolid(col, row);
        }

        // ------------------------------------------------------------ draw
        private static Color Lighten(Color c)
        {
            return Color.FromArgb(Math.Min(255, c.R + 90), Math.Min(255, c.G + 90), Math.Min(255, c.B + 90));
        }

        public void Draw(Graphics g, int camX, int camY)
        {
            if (!Alive) return;

            bool tell = Telegraph;
            int shake = tell ? (((int)(_stateTime * 30f) % 2 == 0) ? -1 : 1) : 0;
            int x = (int)Math.Round(X) - camX + shake;
            int y = (int)Math.Round(Y) - camY;

            Color c = Color;
            if (Invuln > 0f && ((int)(Invuln * 20f) % 2 == 0)) c = Color.White;          // berkedip putih saat kena
            else if (tell && ((int)(_stateTime * 14f) % 2 == 0)) c = Lighten(Color);     // berkedip terang saat bersiap

            BoxSprite.Draw(g, x, y, W, H, c, Eyes, FacingRight, Hidden);

            // Lifebar di atas kepala
            int bw = Math.Max(W + 4, 16);
            int bx = x + W / 2 - bw / 2;
            int by = y - 7;
            Ui.DrawBox(g, new Rectangle(bx, by, bw, 4), Color.FromArgb(200, 200, 210));
            int fill = (bw - 2) * Hp / MaxHp;
            if (fill > 0) Ui.FillBox(g, new Rectangle(bx + 1, by + 1, fill, 2), Color.FromArgb(230, 60, 80));

            // Tanda bahaya
            if (tell && (Behavior == EnemyBehavior.Charger || Behavior == EnemyBehavior.Slammer))
                Ui.DrawText(g, "!", Ui.Body, Color.FromArgb(255, 230, 90), x + W / 2 - 2, y - 19);
            if (tell && Behavior == EnemyBehavior.Shooter)
                Ui.FillBox(g, new Rectangle(FacingRight ? x + W : x - 3, y + H / 2 - 1, 3, 3), Color.White); // titik bidik

            // Nametag di bawah
            Ui.DrawTag(g, Tag, x + W / 2, y + H + 2, Color);
        }
    }

    public enum HitResult { None, Stomp, PlayerHurt }

    public static class Combat
    {
        /// <summary>
        /// Injak = pemain sedang jatuh dan kakinya (frame sebelumnya) berada di atas kepala musuh.
        /// Selain itu, sentuhan samping/bawah melukai pemain (kecuali musuh sedang kebal,
        /// bersembunyi, atau tidak berbahaya).
        /// </summary>
        public static HitResult Check(Player p, Enemy e, float prevBottom)
        {
            if (!e.Alive || !p.Overlaps(e)) return HitResult.None;
            if (p.VY > 0f && prevBottom <= e.Y + 5f) return HitResult.Stomp;
            if (e.Harmless || e.Hidden || e.Invuln > 0f || p.Invuln > 0f) return HitResult.None;
            return HitResult.PlayerHurt;
        }
    }
}
