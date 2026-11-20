using System.Drawing;

namespace AnamnesisDalanMulih.Scenes
{
    public interface IScene
    {
        void Update(float dt);
        void Draw(Graphics g);
    }
}
