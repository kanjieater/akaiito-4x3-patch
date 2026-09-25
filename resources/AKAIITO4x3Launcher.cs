using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;

internal static class Program
{
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
        Process.Start(psi);
        Console.WriteLine("Launching AKAIITO at {0}x{1} ({2}).", width, height,
            fullscreen ? "fullscreen" : "windowed");
        return 0;
    }
}
