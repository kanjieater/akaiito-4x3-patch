using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

internal static class Program
{
    [DllImport("user32.dll")]
    private static extern int ShowCursor(bool show);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool IsHungAppWindow(IntPtr window);

    [DllImport("user32.dll")]
    private static extern IntPtr CreateCursor(IntPtr instance, int xHotSpot, int yHotSpot,
        int width, int height, byte[] andPlane, byte[] xorPlane);

    [DllImport("user32.dll")]
    private static extern IntPtr SetCursor(IntPtr cursor);

    [DllImport("user32.dll")]
    private static extern IntPtr LoadCursor(IntPtr instance, int cursorId);

    [DllImport("user32.dll")]
    private static extern bool DestroyCursor(IntPtr cursor);

    private const int ArrowCursorId = 32512;

    private static int Main(string[] args)
    {
        int width;
        if (args.Length == 0 || !int.TryParse(args[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out width))
        {
            Console.Error.WriteLine("Usage: AKAIITO4x3Launcher.exe <width> [windowed|fullscreen]");
            return 2;
        }
        if (width < 640 || width > 7680)
        {
            Console.Error.WriteLine("Width must be between 640 and 7680.");
            return 2;
        }

        string launcherDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string game = Path.Combine(launcherDir, "AKAIITO_HD_REMASTER.exe");
        if (!File.Exists(game))
        {
            Console.Error.WriteLine("Could not find AKAIITO_HD_REMASTER.exe beside AKAIITO4x3Launcher.exe.");
            Console.Error.WriteLine("Place the launcher beside the game executable.");
            return 3;
        }

        int height = (int)Math.Round(width * 3.0 / 4.0, MidpointRounding.AwayFromZero);
        if ((height & 1) != 0) height++;
        bool fullscreen = args.Length > 1 &&
            (string.Equals(args[1], "fullscreen", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(args[1], "full", StringComparison.OrdinalIgnoreCase));

        var psi = new ProcessStartInfo
        {
            FileName = game,
            Arguments = string.Format(CultureInfo.InvariantCulture,
                "-screen-width {0} -screen-height {1} -screen-fullscreen {2}",
                width, height, fullscreen ? 1 : 0),
            WorkingDirectory = launcherDir,
            UseShellExecute = false
        };
        using (Process process = Process.Start(psi))
        {
            if (process == null)
            {
                Console.Error.WriteLine("Failed to start AKAIITO_HD_REMASTER.exe.");
                return 4;
            }

            // Unity leaves the pointer visible for this title. Hide it while its
            // window has focus, but restore it immediately when the user alt-tabs.
            int hideCalls = 0;
            bool cursorHidden = false;
            DateTime? hungSince = null;
            // An all-one AND mask plus an all-zero XOR mask is fully transparent.
            // SetCursor is needed because this Unity title re-shows its cursor after
            // the ordinary ShowCursor call.
            byte[] andPlane = new byte[128];
            for (int i = 0; i < andPlane.Length; i++) andPlane[i] = 0xff;
            IntPtr transparentCursor = CreateCursor(IntPtr.Zero, 0, 0, 32, 32,
                andPlane, new byte[128]);
            try
            {
                while (!process.WaitForExit(20))
                {
                    uint foregroundProcess;
                    GetWindowThreadProcessId(GetForegroundWindow(), out foregroundProcess);
                    bool gameHasFocus = foregroundProcess == (uint)process.Id;

                    if (gameHasFocus)
                    {
                        // The game repeatedly makes its cursor visible. Counter it
                        // while focused; every hide is balanced below on alt-tab/exit.
                        ShowCursor(false);
                        hideCalls++;
                        if (transparentCursor != IntPtr.Zero)
                            SetCursor(transparentCursor);
                        cursorHidden = true;
                    }
                    else if (cursorHidden)
                    {
                        while (hideCalls-- > 0) ShowCursor(true);
                        hideCalls = 0;
                        SetCursor(LoadCursor(IntPtr.Zero, ArrowCursorId));
                        cursorHidden = false;
                    }

                    process.Refresh();
                    if (process.MainWindowHandle != IntPtr.Zero && IsHungAppWindow(process.MainWindowHandle))
                    {
                        if (!hungSince.HasValue)
                            hungSince = DateTime.UtcNow;
                        else if ((DateTime.UtcNow - hungSince.Value).TotalSeconds >= 5)
                        {
                            // This Unity build deadlocks after its own title-screen
                            // quit request. Let it try to shut down first, then avoid
                            // leaving the user with a permanent "not responding" window.
                            process.Kill();
                            process.WaitForExit();
                            return 0;
                        }
                    }
                    else
                    {
                        hungSince = null;
                    }
                }
                return process.ExitCode;
            }
            finally
            {
                while (hideCalls-- > 0) ShowCursor(true);
                if (cursorHidden)
                    SetCursor(LoadCursor(IntPtr.Zero, ArrowCursorId));
                if (transparentCursor != IntPtr.Zero)
                    DestroyCursor(transparentCursor);
            }
        }
    }
}
