using System.Drawing;
using AnamnesisDalanMulih.Engine;
using AnamnesisDalanMulih.Game;

namespace AnamnesisDalanMulih.Scenes
{
    /// <summary>Layar narasi sebelum tiap area. Enter untuk lanjut.</summary>
    public sealed class IntroScene : IScene
    {
        private readonly IGameContext _game;
        private readonly GameState _state;
        private readonly int _area;
        private readonly AreaDef _def;
        private int _page;
        private float _t;

        public IntroScene(IGameContext game, GameState state, int area)
        {
            _game = game;
            _state = state;
            _area = area;
            _def = Areas.Build(area);
            _state.Hp = _state.MaxHp; // HP pulih tiap masuk area baru
        }

        public void Update(float dt)
        {
            _t += dt;
            if (_def.Intro.Length == 0 || (_t > 0.3f && _game.Input.Confirm))
            {
                _page++;
                _t = 0f;
                if (_page >= _def.Intro.Length)
                    _game.ChangeScene(new PlayScene(_game, _state, _area));
            }
        }

        public void Draw(Graphics g)
        {
            g.Clear(Color.FromArgb(8, 8, 14));
            Ui.DrawCentered(g, _def.Title, Ui.Title, Color.White, 30);

            if (_page < _def.Intro.Length)
                Ui.DrawWrapped(g, _def.Intro[_page], Ui.Body, Color.FromArgb(200, 200, 215), new Rectangle(40, 84, 240, 50), true);

            Ui.DrawCentered(g, "[Enter]", Ui.Small, Color.FromArgb(110, 110, 130), 160);
        }
    }
}
