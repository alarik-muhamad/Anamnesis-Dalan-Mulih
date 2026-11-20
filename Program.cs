using System;
using System.Windows.Forms;
using AnamnesisDalanMulih.Engine;

namespace AnamnesisDalanMulih
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new GameWindow());
        }
    }
}
