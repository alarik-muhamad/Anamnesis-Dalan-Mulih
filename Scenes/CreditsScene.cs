using System.Drawing;
using AnamnesisDalanMulih.Engine;

namespace AnamnesisDalanMulih.Scenes
{
    /// <summary>Layar credits (nama + NIM + atribusi aset). Wajib ada menurut ketentuan proyek.</summary>
    public sealed class CreditsScene : IScene
    {
        private static readonly string[] Lines =
        {
            "Anamnesis: Dalan Mulih",
            "Kelompok: Perahu Lompat",
            "",
            "Faris Dian Pradipta - 235150200111068",
            "Alarik Muhamad Nakharry - 235150200111071",
            "I Gede Ikhsa Garbhacita NP - 235150207111072",
            "",
            "Dosen Pengampu: Agi Putra Kharisma",
            "",
            "Aset & atribusi:",
            "(isi nama kreator + lisensi di sini)"
        };

        private readonly IGameContext _game;

        public CreditsScene(IGameContext game) { _game = game; }

        public void Update(float dt)
        {
            if (_game.Input.Confirm || _game.Input.Cancel)
                _game.ChangeScene(new MenuScene(_game));
        }

        public void Draw(Graphics g)
        {
            g.Clear(Color.FromArgb(14, 14, 24));

            float y = 14;
            foreach (string line in Lines)
            {
                Ui.DrawCentered(g, line, Ui.Body, Color.White, y);
                y += 12;
            }

            Rectangle back = new Rectangle((GameWindow.ViewW - 100) / 2, 150, 100, 20);
            Ui.DrawButton(g, back, "Kembali", true);
        }
    }
}
