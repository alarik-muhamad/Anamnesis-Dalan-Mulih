using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using AnamnesisDalanMulih.Scenes;

namespace AnamnesisDalanMulih.Engine
{
    /// <summary>
    /// Jendela + game loop buatan sendiri.
    /// - Resolusi internal 320x180, digambar ke backbuffer lalu diperbesar bulat (4x, 6x, ...)
    ///   dengan NearestNeighbor supaya pixel art tetap tajam.
    /// - Update memakai fixed timestep 60 Hz (akumulator), render sekali per iterasi.
    /// </summary>
    public sealed class GameWindow : Form, IGameContext
    {
        public const int ViewW = 320;
        public const int ViewH = 180;
        private const double Dt = 1.0 / 60.0;

        public InputState Input { get; private set; }

        private readonly Bitmap _back;
        private readonly Graphics _gfx;
        private readonly ImageAttributes _attr;
        private readonly Stopwatch _clock = Stopwatch.StartNew();

        private double _last;
        private double _acc;
        private bool _running = true;
        private IScene _scene;
        private IScene _nextScene;

        public GameWindow()
        {
            Input = new InputState();

            Text = "Anamnesis: Dalan Mulih";
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(ViewW * 4, ViewH * 4);
            MinimumSize = SizeFromClientSize(new Size(ViewW, ViewH));
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.Black;
            KeyPreview = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

            _back = new Bitmap(ViewW, ViewH, PixelFormat.Format32bppPArgb);
            _gfx = Graphics.FromImage(_back);
            _gfx.InterpolationMode = InterpolationMode.NearestNeighbor;
            _gfx.PixelOffsetMode = PixelOffsetMode.Half;
            _gfx.SmoothingMode = SmoothingMode.None;

            _attr = new ImageAttributes();
            _attr.SetWrapMode(WrapMode.TileFlipXY); // cegah tepi gambar "bocor" saat diperbesar

            KeyDown += delegate(object s, KeyEventArgs e) { Input.OnKeyDown(e.KeyCode); };
            KeyUp += delegate(object s, KeyEventArgs e) { Input.OnKeyUp(e.KeyCode); };
            Deactivate += delegate { Input.Clear(); };

            ChangeScene(new MenuScene(this));
            ApplyPendingScene();

            Application.Idle += OnIdle;
        }

        /// <summary>Ganti scene. Baru berlaku setelah update frame berjalan selesai.</summary>
        public void ChangeScene(IScene scene) { _nextScene = scene; }

        public void Quit()
        {
            _running = false;
            Close();
        }

        private void ApplyPendingScene()
        {
            if (_nextScene != null)
            {
                _scene = _nextScene;
                _nextScene = null;
            }
        }

        // ---------------- Game loop ----------------

        private void OnIdle(object sender, EventArgs e)
        {
            while (_running && !IsDisposed && IsIdle())
            {
                Tick();
            }
        }

        private void Tick()
        {
            double now = _clock.Elapsed.TotalSeconds;
            _acc += Math.Min(now - _last, 0.25); // batasi supaya tidak "spiral of death"
            _last = now;

            if (_acc < Dt)
            {
                Thread.Sleep(1); // jangan makan CPU 100%
                return;
            }

            while (_acc >= Dt && _running)
            {
                _scene.Update((float)Dt);
                Input.EndFrame();
                ApplyPendingScene();
                _acc -= Dt;
            }

            if (!_running) return;

            _gfx.Clear(Color.Black);
            Ui.BeginFrame();            // teks dicatat dulu, digambar di OnPaint pada resolusi layar
            _scene.Draw(_gfx);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.Black);

            int scale = Math.Max(1, Math.Min(ClientSize.Width / ViewW, ClientSize.Height / ViewH));
            int w = ViewW * scale;
            int h = ViewH * scale;
            int x = (ClientSize.Width - w) / 2;
            int y = (ClientSize.Height - h) / 2;

            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.DrawImage(_back, new Rectangle(x, y, w, h), 0, 0, ViewW, ViewH, GraphicsUnit.Pixel, _attr);
            Ui.FlushText(g, x, y, scale);   // teks digambar tajam pada ukuran layar (bukan di buffer 320x180)
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _running = false;
            Application.Idle -= OnIdle;
            base.OnFormClosed(e);
        }

        // ---- Deteksi "aplikasi idle" (tidak ada pesan Windows yang menunggu) ----

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeMessage
        {
            public IntPtr Handle;
            public uint Message;
            public IntPtr WParameter;
            public IntPtr LParameter;
            public uint Time;
            public Point Location;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PeekMessage(out NativeMessage msg, IntPtr hWnd, uint filterMin, uint filterMax, uint remove);

        private static bool IsIdle()
        {
            NativeMessage msg;
            return !PeekMessage(out msg, IntPtr.Zero, 0, 0, 0);
        }
    }
}
