using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace RazerDiamondbackController
{
    static class Program
    {
        private const string MutexName = "RazerDiamondbackController_App_Mutex";
        public const string ShowWindowMessageName = "RazerDiamondbackController_ShowMe_Msg";

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint RegisterWindowMessage(string lpString);

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private const int HWND_BROADCAST = 0xffff;

        [STAThread]
        static void Main(string[] args)
        {
            bool startInTray = false;
            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i];
                    if (string.Equals(arg, "--tray", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(arg, "-tray", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(arg, "/tray", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(arg, "--minimized", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(arg, "-minimized", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(arg, "/minimized", StringComparison.OrdinalIgnoreCase))
                    {
                        startInTray = true;
                        break;
                    }
                }
            }

            bool createdNew;
            using (Mutex mutex = new Mutex(true, MutexName, out createdNew))
            {
                if (!createdNew)
                {
                    uint wmShowMe = RegisterWindowMessage(ShowWindowMessageName);
                    PostMessage((IntPtr)HWND_BROADCAST, wmShowMe, IntPtr.Zero, IntPtr.Zero);
                    return;
                }

                Application app = new Application();
                app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                MainWindow mainWindow = new MainWindow(startInTray);

                if (!startInTray)
                {
                    mainWindow.Show();
                }

                app.Run();
            }
        }
    }
}
