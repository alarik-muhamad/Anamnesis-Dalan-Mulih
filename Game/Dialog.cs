using System;
using System.Collections.Generic;
using System.Drawing;
using AnamnesisDalanMulih.Engine;

namespace AnamnesisDalanMulih.Game
{
    /// <summary>Yang boleh dipakai efek dialog untuk mengubah dunia gim.</summary>
    public interface IGameHost
    {
        GameState State { get; }
        void Toast(string message);
        /// <summary>Beri item. Kalau inventori penuh, pemain diminta membuang salah satu.</summary>
        void GiveItem(ItemId item);
        void StartDialog(DialogNode node);
    }

    public sealed class DialogChoice
    {
        public string Text;
        public Func<GameState, bool> Condition;   // null = selalu tampil
        public Action<IGameHost> Effect;          // karma, flag, item, dll
        public DialogNode Next;                   // null = dialog selesai

        public DialogChoice(string text, Action<IGameHost> effect, DialogNode next)
        {
            Text = text; Effect = effect; Next = next;
        }
    }

    public sealed class DialogNode
    {
        public string Speaker;
        public string Text;
        public Color SpeakerColor;
        public DialogNode Next;
        public Action<IGameHost> OnEnter;
        public List<DialogChoice> Choices = new List<DialogChoice>();

        public DialogNode(string speaker, string text)
        {
            Speaker = speaker;
            Text = text;
            SpeakerColor = Palette.ForSpeaker(speaker);
        }
    }

    /// <summary>Pembantu penyusun dialog.</summary>
    public static class Dlg
    {
        public static DialogNode Say(string speaker, string text)
        {
            return new DialogNode(speaker, text);
        }

        /// <summary>Sambungkan node berurutan, kembalikan node pertama.</summary>
        public static DialogNode Chain(params DialogNode[] nodes)
        {
            for (int i = 0; i < nodes.Length - 1; i++) nodes[i].Next = nodes[i + 1];
            return nodes[0];
        }
    }

    /// <summary>Warna per karakter. Dipakai oleh sprite kotak dan nama pembicara.</summary>
    public static class Palette
    {
        public static readonly Color Player = Color.White;
        public static readonly Color Laras = Color.FromArgb(90, 210, 230);
        public static readonly Color Sarwo = Color.FromArgb(235, 195, 80);
        public static readonly Color Lastri = Color.FromArgb(245, 150, 80);
        public static readonly Color Darman = Color.FromArgb(235, 130, 190);
        public static readonly Color Lurah = Color.FromArgb(120, 215, 140);
        public static readonly Color Raka = Color.FromArgb(130, 170, 245);
        public static readonly Color Cemas = Color.FromArgb(230, 80, 80);
        public static readonly Color TidakAman = Color.FromArgb(240, 130, 50);
        public static readonly Color Depresi = Color.FromArgb(165, 125, 255);
        public static readonly Color Dummy = Color.FromArgb(150, 150, 170);
        public static readonly Color Whisper = Color.FromArgb(190, 170, 255);

        public static Color ForSpeaker(string name)
        {
            switch (name)
            {
                case "Laras": return Laras;
                case "Mbah Sarwo": return Sarwo;
                case "Bu Lastri": return Lastri;
                case "Kang Darman": return Darman;
                case "Pak Lurah": return Lurah;
                case "Raka": return Raka;
                case "Rasa Tidak Aman": return TidakAman;
                case "Depresi": return Depresi;
                case "Bisikan": return Whisper;
                default: return Color.White;
            }
        }
    }

    /// <summary>
    /// Menjalankan dialog: teks muncul per huruf, lalu Enter/E/Space untuk lanjut.
    /// Kalau ada pilihan: W/S + Enter, atau tekan angka 1-3.
    /// </summary>
    public sealed class DialogRunner
    {
        private const float CharsPerSecond = 55f;

        private readonly IGameHost _host;
        private readonly List<DialogChoice> _visible = new List<DialogChoice>();
        private DialogNode _node;
        private float _chars;
        private int _sel;

        public DialogRunner(IGameHost host) { _host = host; }

        public bool Active { get { return _node != null; } }
        public DialogNode Node { get { return _node; } }
        public int ChoiceCount { get { return _visible.Count; } }

        public void Start(DialogNode node)
        {
            _node = node;
            _chars = 0f;
            _sel = 0;

            _visible.Clear();
            if (node != null)
            {
                foreach (DialogChoice c in node.Choices)
                {
                    if (c.Condition == null || c.Condition(_host.State)) _visible.Add(c);
                }
                if (node.OnEnter != null) node.OnEnter(_host); // boleh memulai dialog lain
            }
        }

        public void Update(float dt, InputState input)
        {
            if (_node == null) return;

            bool advance = input.Confirm || input.Interact;
            bool full = _chars >= _node.Text.Length;

            if (!full)
            {
                _chars += dt * CharsPerSecond;
                if (advance) _chars = _node.Text.Length; // lewati animasi huruf
                return;
            }

            if (_visible.Count > 0)
            {
                int n = _visible.Count;
                if (input.MenuUp) _sel = (_sel + n - 1) % n;
                if (input.MenuDown) _sel = (_sel + 1) % n;
                for (int i = 0; i < n && i < 9; i++)
                {
                    if (input.SlotPressed(i)) { _sel = i; Choose(_sel); return; }
                }
                if (advance) Choose(_sel);
            }
            else if (advance)
            {
                Go(_node.Next);
            }
        }

        private void Choose(int index)
        {
            DialogChoice c = _visible[index];
            DialogNode before = _node;
            if (c.Effect != null) c.Effect(_host);
            if (_node != before) return; // efek memulai dialog baru, biarkan
            Go(c.Next);
        }

        private void Go(DialogNode next)
        {
            if (next == null) { _node = null; _visible.Clear(); }
            else Start(next);
        }

        public void Draw(Graphics g)
        {
            if (_node == null) return;

            Rectangle box = new Rectangle(8, 30, 304, 52);
            Ui.FillBox(g, box, Color.FromArgb(235, 8, 8, 16));
            Ui.DrawBox(g, box, Color.FromArgb(150, 150, 170));

            Ui.DrawText(g, _node.Speaker, Ui.Body, _node.SpeakerColor, 14, 33);

            int shown = Math.Min(_node.Text.Length, (int)_chars);
            Ui.DrawWrapped(g, _node.Text.Substring(0, shown), Ui.Body, Color.White, new Rectangle(14, 46, 292, 36), false);

            bool full = _chars >= _node.Text.Length;
            if (!full) return;

            if (_visible.Count == 0)
            {
                Ui.DrawText(g, "[E]", Ui.Small, Color.FromArgb(150, 150, 170), 290, 72);
                return;
            }

            // Kotak pilihan di bawah kotak dialog, rata kanan
            int h = _visible.Count * 13 + 6;
            Rectangle cbox = new Rectangle(130, 86, 182, h);
            Ui.FillBox(g, cbox, Color.FromArgb(235, 8, 8, 16));
            Ui.DrawBox(g, cbox, Color.FromArgb(150, 150, 170));
            for (int i = 0; i < _visible.Count; i++)
            {
                bool sel = i == _sel;
                string label = (sel ? "> " : "  ") + (i + 1) + ". " + _visible[i].Text;
                Ui.DrawText(g, label, Ui.Body, sel ? Color.White : Color.FromArgb(140, 140, 160), 134, 89 + i * 13);
            }
        }
    }
}
