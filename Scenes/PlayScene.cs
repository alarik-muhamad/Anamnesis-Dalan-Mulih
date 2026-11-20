using System;
using System.Collections.Generic;
using System.Drawing;
using AnamnesisDalanMulih.Engine;
using AnamnesisDalanMulih.Game;

namespace AnamnesisDalanMulih.Scenes
{
    /// <summary>
    /// Scene gameplay satu area. Menjalankan pemain, musuh, NPC, item, pintu, dialog, dan HUD.
    /// Controls: A/D gerak, Space lompat, E bicara/buka pintu, 1-3 pakai item, F1 debug, Esc menu.
    /// </summary>
    public sealed class PlayScene : IScene, IGameHost
    {
        private sealed class ToastMsg { public string Text; public float Time; }

        private readonly IGameContext _game;
        private readonly GameState _state;
        private readonly GameState _snapshot;   // kondisi saat masuk area, untuk "Coba lagi"
        private readonly int _areaIndex;
        private readonly AreaDef _def;
        private readonly Tilemap _map;
        private readonly Player _player;
        private readonly Camera _camera = new Camera();
        private readonly DialogRunner _dialog;
        private readonly List<ToastMsg> _toasts = new List<ToastMsg>();

        private float _respawnX, _respawnY;
        private float _time;
        private bool _debug;

        public PlayScene(IGameContext game, GameState state, int areaIndex)
        {
            _game = game;
            _state = state;
            _areaIndex = areaIndex;
            _snapshot = state.Clone();
            _def = Areas.Build(areaIndex);

            Bitmap tileset = AssetManager.TryLoadImage("Tiles/tileset.png"); // boleh null -> outline
            _map = new Tilemap(_def.Level, tileset);

            _player = new Player(_def.SpawnX, _def.SpawnY);
            _respawnX = _def.SpawnX;
            _respawnY = _def.SpawnY;
            _dialog = new DialogRunner(this);

            _camera.Follow(_player.CenterX, _player.CenterY, _map.PixelWidth, _map.PixelHeight);
        }

        // Dipakai tes & IGameHost
        public GameState State { get { return _state; } }
        public Player Hero { get { return _player; } }
        public AreaDef Def { get { return _def; } }
        public bool DialogActive { get { return _dialog.Active; } }
        public Tilemap Map { get { return _map; } }
        public DialogRunner Runner { get { return _dialog; } }

        // ---------------------------------------------------------------- IGameHost
        public void Toast(string message)
        {
            _toasts.Add(new ToastMsg { Text = message, Time = 2.5f });
            while (_toasts.Count > 3) _toasts.RemoveAt(0);
        }

        public void StartDialog(DialogNode node)
        {
            if (node != null) _dialog.Start(node);
        }

        public void GiveItem(ItemId item)
        {
            GiveItemInternal(item, true, null);
        }

        /// <summary>
        /// Tambah item ke inventori. Kalau penuh, tampilkan pilihan membuang item.
        /// forced = true (hadiah NPC): pemain harus membuang sesuatu. false (item lantai): boleh ditinggalkan.
        /// Item penting (Liontin, Kunci) tidak bisa dibuang.
        /// </summary>
        private void GiveItemInternal(ItemId item, bool forced, Pickup source)
        {
            ItemInfo info = Items.Get(item);
            if (_state.TryAdd(item))
            {
                if (source != null) source.Taken = true;
                Toast("Mendapat: " + info.Name);
                return;
            }

            DialogNode prompt = new DialogNode("Inventori penuh", "Tidak ada ruang untuk " + info.Name + ". Buang item yang mana?");
            for (int i = 0; i < GameState.SlotCount; i++)
            {
                ItemId existing = _state.Slots[i];
                if (existing == ItemId.None || Items.Get(existing).Important) continue;

                int slot = i;
                ItemInfo old = Items.Get(existing);
                prompt.Choices.Add(new DialogChoice("Buang " + old.Name, h =>
                {
                    _state.Slots[slot] = item;
                    if (source != null) source.Taken = true;
                    Toast("Membuang " + old.Name + ", mendapat " + info.Name);
                }, null));
            }
            if (!forced)
            {
                prompt.Choices.Add(new DialogChoice("Tinggalkan " + info.Name, h =>
                {
                    if (source != null) source.Declined = true;
                }, null));
            }

            if (prompt.Choices.Count == 0)
            {
                Toast("Inventori penuh item penting");
                return;
            }
            StartDialog(prompt);
        }

        // ---------------------------------------------------------------- Update
        public void Update(float dt)
        {
            InputState input = _game.Input;
            _time += dt;
            TickToasts(dt);
            if (input.DebugToggle) _debug = !_debug;

            if (_dialog.Active)
            {
                _dialog.Update(dt, input);
                return;
            }

            if (input.Cancel)
            {
                _game.ChangeScene(new MenuScene(_game));
                return;
            }

            for (int i = 0; i < GameState.SlotCount; i++)
                if (input.SlotPressed(i)) UseSlot(i);

            float prevBottom = _player.Y + Player.Height;
            _player.Update(dt, input, _map);

            foreach (Npc n in _def.Npcs)
                n.FacingRight = _player.CenterX > n.X + Npc.W / 2f;
            foreach (Enemy e in _def.Enemies)
                if (e.Alive) e.Update(dt, _map);

            if (CheckHazards()) return;
            if (CheckEnemies(prevBottom, input)) return;
            CheckPickups();
            CheckCheckpoints();
            CheckTriggers();
            if (_dialog.Active) return;

            if (input.Interact) TryInteract();

            _camera.Follow(_player.CenterX, _player.CenterY, _map.PixelWidth, _map.PixelHeight);
        }

        private void TickToasts(float dt)
        {
            for (int i = _toasts.Count - 1; i >= 0; i--)
            {
                _toasts[i].Time -= dt;
                if (_toasts[i].Time <= 0f) _toasts.RemoveAt(i);
            }
        }

        private void UseSlot(int i)
        {
            ItemId id = _state.Slots[i];
            if (id == ItemId.None) return;

            ItemInfo info = Items.Get(id);
            if (!info.Usable)
            {
                Toast(info.Name + ": simpan untuk nanti");
                return;
            }
            if (id == ItemId.Herb)
            {
                if (_state.Hp >= _state.MaxHp) { Toast("HP sudah penuh"); return; }
                _state.Hp++;
                _state.Slots[i] = ItemId.None;
                Toast("Ramuan dipakai: HP +1");
            }
        }

        /// <summary>Duri / jurang: kurangi 1 HP, kembali ke checkpoint. true = scene berganti (game over).</summary>
        private bool CheckHazards()
        {
            bool pit = _player.Y > _map.PixelHeight + 32;
            bool spike = _player.Invuln <= 0f && _player.IsTouchingHazard(_map);
            if (!pit && !spike) return false;

            _state.Hp--;
            if (_state.Hp <= 0) { GameOver(); return true; }

            _player.Respawn(_respawnX - Player.Width / 2f, _respawnY - Player.Height);
            Toast("Terluka! Kembali ke tempat istirahat");
            return false;
        }

        /// <summary>Injak musuh = lukai musuh + pantul. Sentuh dari samping = pemain terluka.</summary>
        private bool CheckEnemies(float prevBottom, InputState input)
        {
            foreach (Enemy e in _def.Enemies)
            {
                HitResult r = Combat.Check(_player, e, prevBottom);
                if (r == HitResult.Stomp)
                {
                    bool damaged = e.Hit(1);
                    _player.Bounce(input.JumpHeld);
                    if (damaged && !e.Alive)
                    {
                        Toast(e.Name + " lenyap");
                        if (e.OnDefeat != null) e.OnDefeat(this);
                    }
                    if (_dialog.Active) break;
                }
                else if (r == HitResult.PlayerHurt)
                {
                    _state.Hp--;
                    if (_state.Hp <= 0) { GameOver(); return true; }
                    _player.Hurt(_player.CenterX < e.CenterX ? -1 : 1);
                }
            }
            return false;
        }

        private void CheckPickups()
        {
            foreach (Pickup p in _def.Pickups)
            {
                if (p.Taken) continue;

                if (!p.Overlaps(_player))
                {
                    if (p.Declined && !p.Near(_player, 30f)) p.Declined = false;
                    continue;
                }
                if (p.Declined) continue;

                GiveItemInternal(p.Item, false, p);
                if (_dialog.Active) return;
            }
        }

        private void CheckCheckpoints()
        {
            foreach (Checkpoint c in _def.Checkpoints)
            {
                if (c.Active || !c.Overlaps(_player)) continue;

                foreach (Checkpoint other in _def.Checkpoints) other.Active = false;
                c.Active = true;
                _respawnX = c.X;
                _respawnY = c.Y;

                if (_state.Hp < _state.MaxHp)
                {
                    _state.Hp = _state.MaxHp;
                    Toast("Tempat istirahat: HP pulih");
                }
                else Toast("Tempat istirahat");
            }
        }

        private void CheckTriggers()
        {
            foreach (Trigger t in _def.Triggers)
            {
                if (_state.Flag(t.Flag) || !t.Contains(_player)) continue;
                _state.Set(t.Flag);
                StartDialog(t.Dialog(_state));
                return;
            }
        }

        private void TryInteract()
        {
            foreach (Npc n in _def.Npcs)
            {
                if (n.InRange(_player)) { StartDialog(n.Dialog(_state)); return; }
            }
            foreach (Door d in _def.Doors)
            {
                if (!d.InRange(_player)) continue;

                if (d.CanOpen(_state))
                {
                    if (d.NextArea >= 0 && d.NextArea < Areas.Count)
                        _game.ChangeScene(new IntroScene(_game, _state, d.NextArea));
                    else
                        _game.ChangeScene(new EndingScene(_game, _state, _state.GetEnding()));
                }
                else StartDialog(d.LockedDialog);
                return;
            }
        }

        private void GameOver()
        {
            _snapshot.Deaths = _state.Deaths + 1;
            _snapshot.Hp = _snapshot.MaxHp;
            _game.ChangeScene(new GameOverScene(_game, _snapshot, _areaIndex));
        }

        // ---------------------------------------------------------------- Draw
        public void Draw(Graphics g)
        {
            g.Clear(_def.Bg);

            int cx = _camera.X, cy = _camera.Y;
            _map.Draw(g, cx, cy);

            foreach (Checkpoint c in _def.Checkpoints) c.Draw(g, cx, cy);
            foreach (Door d in _def.Doors) d.Draw(g, cx, cy, d.CanOpen(_state));
            foreach (Pickup p in _def.Pickups) p.Draw(g, cx, cy, _time);
            foreach (Npc n in _def.Npcs) n.Draw(g, cx, cy);
            foreach (Enemy e in _def.Enemies) e.Draw(g, cx, cy);
            _player.Draw(g, cx, cy);

            if (!_dialog.Active) DrawPrompts(g, cx, cy);

            DrawHud(g);
            DrawToasts(g);
            _dialog.Draw(g);
            if (_debug) DrawDebug(g);
        }

        private void DrawPrompts(Graphics g, int cx, int cy)
        {
            Color c = Color.FromArgb(255, 220, 90);
            foreach (Npc n in _def.Npcs)
                if (n.InRange(_player))
                    Ui.DrawText(g, "[E]", Ui.Small, c, (int)n.X - cx - 2, (int)n.Y - cy - 11);
            foreach (Door d in _def.Doors)
                if (d.InRange(_player))
                    Ui.DrawText(g, "[E]", Ui.Small, c, (int)d.X - cx, (int)d.Y - cy - 11);
        }

        private void DrawHud(Graphics g)
        {
            Color line = Color.FromArgb(150, 150, 170);

            for (int i = 0; i < _state.MaxHp; i++)
            {
                Rectangle r = new Rectangle(6 + i * 13, 6, 11, 9);
                Ui.DrawBox(g, r, line);
                if (i < _state.Hp)
                    Ui.FillBox(g, new Rectangle(r.X + 2, r.Y + 2, r.Width - 4, r.Height - 4), Color.FromArgb(235, 90, 110));
            }

            for (int i = 0; i < GameState.SlotCount; i++)
            {
                int x = GameWindow.ViewW - 6 - GameState.SlotCount * 20 + i * 20;
                Ui.DrawBox(g, new Rectangle(x, 6, 18, 18), line);
                Ui.DrawText(g, (i + 1).ToString(), Ui.Small, Color.FromArgb(110, 110, 130), x + 6, 25);

                ItemId id = _state.Slots[i];
                if (id != ItemId.None)
                {
                    ItemInfo info = Items.Get(id);
                    Ui.DrawText(g, info.Glyph.ToString(), Ui.Body, info.Color, x + 5, 9);
                }
            }
        }

        private void DrawToasts(Graphics g)
        {
            for (int i = 0; i < _toasts.Count; i++)
            {
                Ui.DrawCentered(g, _toasts[i].Text, Ui.Small, Color.FromArgb(235, 235, 245), 166 - (_toasts.Count - 1 - i) * 10);
            }
        }

        private void DrawDebug(Graphics g)
        {
            string flags = string.Join(",", new List<string>(_state.Flags).ToArray());
            string text = "Area " + (_areaIndex + 1) + "  HP " + _state.Hp + "/" + _state.MaxHp +
                          "  Karma " + _state.Karma + " (+" + _state.GoodActs + "/-" + _state.BadActs + ")" +
                          "  Ending: " + _state.GetEnding() + "\nFlag: " + flags;
            Ui.DrawWrapped(g, text, Ui.Small, Color.FromArgb(255, 220, 90), new Rectangle(6, 92, 308, 40), false);
        }
    }
}
