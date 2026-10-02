using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

internal static class Program
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool IsHungAppWindow(IntPtr window);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

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

            // This Unity title does not honor cursor hiding. Put the pointer in
            // the upper-left corner once its window receives focus instead.
            bool pointerMoved = false;
            DateTime? hungSince = null;
            while (!process.WaitForExit(100))
                {
                    uint foregroundProcess;
                    GetWindowThreadProcessId(GetForegroundWindow(), out foregroundProcess);
                    bool gameHasFocus = foregroundProcess == (uint)process.Id;

                    if (gameHasFocus && !pointerMoved)
                    {
                        SetCursorPos(0, 0);
                        pointerMoved = true;
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
    }
}
