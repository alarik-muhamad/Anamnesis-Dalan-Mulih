using System.Drawing;
using AnamnesisDalanMulih.Engine;
using AnamnesisDalanMulih.Game;

namespace AnamnesisDalanMulih.Scenes
{
    /// <summary>HP habis. Coba lagi dari awal area, atau menyerah (= bad ending).</summary>
    public sealed class GameOverScene : IScene
    {
        private readonly IGameContext _game;
        private readonly GameState _snapshot;
        private readonly int _area;
        private readonly string[] _items = { "Coba lagi", "Menyerah" };
        private int _index;

        public GameOverScene(IGameContext game, GameState snapshot, int area)
        {
            _game = game;
            _snapshot = snapshot;
            _area = area;
        }

        public void Update(float dt)
        {
            InputState input = _game.Input;
            if (input.MenuUp) _index = (_index + _items.Length - 1) % _items.Length;
            if (input.MenuDown) _index = (_index + 1) % _items.Length;

            if (input.Confirm)
            {
                if (_index == 0) _game.ChangeScene(new PlayScene(_game, _snapshot, _area));
                else _game.ChangeScene(new EndingScene(_game, _snapshot, Ending.Bad));
            }
        }

        public void Draw(Graphics g)
        {
            g.Clear(Color.FromArgb(18, 6, 10));
            Ui.DrawCentered(g, "KAMU TERJATUH", Ui.Title, Color.FromArgb(230, 80, 80), 36);
            Ui.DrawCentered(g, "Bayangan menang... untuk sekarang.", Ui.Body, Color.FromArgb(190, 160, 165), 66);

            int w = 120, h = 20, x = (GameWindow.ViewW - w) / 2;
            for (int i = 0; i < _items.Length; i++)
                Ui.DrawButton(g, new Rectangle(x, 100 + i * 28, w, h), _items[i], i == _index);

            Ui.DrawCentered(g, "Kalah: " + _snapshot.Deaths + " kali", Ui.Small, Color.FromArgb(120, 100, 105), 164);
        }
    }
}
