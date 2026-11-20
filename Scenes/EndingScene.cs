using System.Drawing;
using AnamnesisDalanMulih.Engine;
using AnamnesisDalanMulih.Game;

namespace AnamnesisDalanMulih.Scenes
{
    /// <summary>Ending (Good Samaritan / Criminal / Indecisive / Bad). Setelah itu otomatis ke credits.</summary>
    public sealed class EndingScene : IScene
    {
        private readonly IGameContext _game;
        private readonly string[] _pages;
        private readonly Color _accent;
        private int _page;
        private float _t;

        public Ending Kind { get; private set; }

        public EndingScene(IGameContext game, GameState state, Ending ending)
        {
            _game = game;
            Kind = ending;
            _pages = BuildPages(ending, state);
            _accent = ending == Ending.GoodSamaritan ? Color.FromArgb(120, 230, 140)
                    : ending == Ending.Criminal ? Color.FromArgb(230, 80, 80)
                    : ending == Ending.Indecisive ? Color.FromArgb(235, 195, 80)
                    : Color.FromArgb(150, 150, 170);
        }

        private static string[] BuildPages(Ending e, GameState s)
        {
            string stats = "Menolong: " + s.GoodActs + "    Merugikan: " + s.BadActs + "    Kalah: " + s.Deaths + " kali";

            switch (e)
            {
                case Ending.GoodSamaritan:
                    return new string[]
                    {
                        "ENDING: GOOD SAMARITAN",
                        "Kamu ingat sekarang. Bukan nama, bukan tempat, tapi caramu memilih saat tak ada yang melihat.",
                        "Setiap tangan yang kau ulurkan ternyata adalah jalan yang kau tempuh pulang. Dalan mulih bukan sekadar arah. Ia adalah orang yang kau pilih untuk menjadi.",
                        stats
                    };
                case Ending.Criminal:
                    return new string[]
                    {
                        "ENDING: CRIMINAL",
                        "Pintu terbuka, dan kamu pulang dengan tangan penuh. Tapi hatimu kosong.",
                        "Kamu ingat siapa dirimu: seseorang yang selalu mengambil lebih dulu. Jalan pulang itu nyata. Hanya saja rumah di ujungnya tak lagi menunggumu.",
                        stats
                    };
                case Ending.Indecisive:
                    return new string[]
                    {
                        "ENDING: INDECISIVE",
                        "Kamu pulang, tapi tak tahu pulang sebagai siapa. Terlalu sering kamu mengikuti, jarang menimbang.",
                        "Bisikan itu masih ada. Ia hanya bertanya pelan: \"Kali ini, siapa yang memutuskan?\"",
                        stats
                    };
                default:
                    return new string[]
                    {
                        "ENDING: TERLUPAKAN",
                        "Cahaya memudar. Bisikan itu berhenti. Kamu menyerah di tengah jalan, dan jalan pun berhenti menunggu.",
                        "Tak ada yang tahu namamu. Termasuk dirimu sendiri.",
                        stats
                    };
            }
        }

        public void Update(float dt)
        {
            _t += dt;
            if (_t > 0.4f && _game.Input.Confirm)
            {
                _t = 0f;
                _page++;
                if (_page >= _pages.Length) _game.ChangeScene(new CreditsScene(_game));
            }
        }

        public void Draw(Graphics g)
        {
            g.Clear(Color.FromArgb(8, 8, 14));

            if (_page == 0)
                Ui.DrawCentered(g, _pages[0], Ui.Title, _accent, 78);
            else if (_page < _pages.Length)
                Ui.DrawWrapped(g, _pages[_page], Ui.Body, _page == _pages.Length - 1 ? _accent : Color.FromArgb(210, 210, 225),
                               new Rectangle(30, 70, 260, 70), true);

            Ui.DrawCentered(g, "[Enter]", Ui.Small, Color.FromArgb(110, 110, 130), 160);
        }
    }
}
