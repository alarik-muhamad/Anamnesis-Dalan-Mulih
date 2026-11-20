using AnamnesisDalanMulih.Scenes;

namespace AnamnesisDalanMulih.Engine
{
    /// <summary>Yang dibutuhkan scene dari jendela gim (supaya scene bisa dites tanpa jendela).</summary>
    public interface IGameContext
    {
        InputState Input { get; }
        void ChangeScene(IScene scene);
        void Quit();
    }
}
