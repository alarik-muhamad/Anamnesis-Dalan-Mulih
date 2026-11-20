using System.Drawing;
using AnamnesisDalanMulih.Engine;
using AnamnesisDalanMulih.Game;

namespace AnamnesisDalanMulih.Scenes
{
    public sealed class MenuScene : IScene
    {
        private readonly IGameContext _game;
        private readonly string[] _items = { "Mulai", "Credits", "Keluar" };
        private int _index;

        public MenuScene(IGameContext game) { _game = game; }

        public void Update(float dt)
        {
            InputState input = _game.Input;

            if (input.MenuUp) _index = (_index + _items.Length - 1) % _items.Length;
            if (input.MenuDown) _index = (_index + 1) % _items.Length;

            if (input.Confirm)
            {
                switch (_index)
                {
                    case 0: _game.ChangeScene(new IntroScene(_game, new GameState(), 0)); break;
                    case 1: _game.ChangeScene(new CreditsScene(_game)); break;
                    case 2: _game.Quit(); break;
                }
            }
        }

        public void Draw(Graphics g)
        {
            g.Clear(Color.FromArgb(14, 14, 24));

            Ui.DrawCentered(g, "ANAMNESIS", Ui.Title, Color.White, 22);
            Ui.DrawCentered(g, "Dalan Mulih", Ui.Body, Color.FromArgb(170, 170, 190), 46);

            // Tombol outline
            int w = 120, h = 20, x = (GameWindow.ViewW - w) / 2;
            for (int i = 0; i < _items.Length; i++)
            {
                Rectangle r = new Rectangle(x, 78 + i * 28, w, h);
                Ui.DrawButton(g, r, _items[i], i == _index);
            }

            Ui.DrawCentered(g, "W/S atau panah: pilih   Enter: konfirmasi", Ui.Small, Color.FromArgb(120, 120, 140), 164);
        }
    }
}
