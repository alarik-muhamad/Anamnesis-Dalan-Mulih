using System.Collections.Generic;
using System.Windows.Forms;

namespace AnamnesisDalanMulih.Engine
{
    /// <summary>
    /// Status keyboard per frame.
    /// IsDown = sedang ditahan, WasPressed = baru ditekan, WasReleased = baru dilepas.
    /// </summary>
    public sealed class InputState
    {
        private readonly HashSet<Keys> _down = new HashSet<Keys>();
        private readonly HashSet<Keys> _pressed = new HashSet<Keys>();
        private readonly HashSet<Keys> _released = new HashSet<Keys>();

        public void OnKeyDown(Keys key) { if (_down.Add(key)) _pressed.Add(key); }
        public void OnKeyUp(Keys key) { if (_down.Remove(key)) _released.Add(key); }
        public void EndFrame() { _pressed.Clear(); _released.Clear(); }
        public void Clear() { _down.Clear(); _pressed.Clear(); _released.Clear(); }

        public bool IsDown(Keys key) { return _down.Contains(key); }
        public bool WasPressed(Keys key) { return _pressed.Contains(key); }
        public bool WasReleased(Keys key) { return _released.Contains(key); }

        // Gameplay
        public bool Left => IsDown(Keys.Left) || IsDown(Keys.A);
        public bool Right => IsDown(Keys.Right) || IsDown(Keys.D);
        public bool JumpPressed => WasPressed(Keys.Space) || WasPressed(Keys.W) || WasPressed(Keys.Up);
        public bool JumpReleased => WasReleased(Keys.Space) || WasReleased(Keys.W) || WasReleased(Keys.Up);
        public bool JumpHeld => IsDown(Keys.Space) || IsDown(Keys.W) || IsDown(Keys.Up);
        public bool Interact => WasPressed(Keys.E);

        /// <summary>Tombol angka 1..3 (index 0..2): pakai item / pilih jawaban dialog.</summary>
        public bool SlotPressed(int index) { return WasPressed((Keys)((int)Keys.D1 + index)); }

        // Menu / dialog
        public bool MenuUp => WasPressed(Keys.Up) || WasPressed(Keys.W);
        public bool MenuDown => WasPressed(Keys.Down) || WasPressed(Keys.S);
        public bool Confirm => WasPressed(Keys.Enter) || WasPressed(Keys.Space);
        public bool Cancel => WasPressed(Keys.Escape);
        public bool DebugToggle => WasPressed(Keys.F1);
    }
}
