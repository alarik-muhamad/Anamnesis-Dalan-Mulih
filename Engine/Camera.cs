using System;

namespace AnamnesisDalanMulih.Engine
{
    public sealed class Camera
    {
        public int X { get; private set; }
        public int Y { get; private set; }

        /// <summary>Pusatkan kamera ke target, tapi jangan keluar dari batas peta.</summary>
        public void Follow(float targetX, float targetY, int mapW, int mapH)
        {
            int tx = (int)Math.Round(targetX) - GameWindow.ViewW / 2;
            int ty = (int)Math.Round(targetY) - GameWindow.ViewH / 2;
            X = Math.Max(0, Math.Min(tx, mapW - GameWindow.ViewW));
            Y = Math.Max(0, Math.Min(ty, mapH - GameWindow.ViewH));
        }
    }
}
