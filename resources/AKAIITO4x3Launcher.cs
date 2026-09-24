using System;
using System.Diagnostics;
using System.Globalization;

internal static class Program
{
    private const string Exe = @"G:\VN\AkaiIto\AKAIITO_HD_REMASTER.exe";

    private static int Main(string[] args)
    {
        int width;
        if (args.Length == 0 || !int.TryParse(args[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out width))
        {
            Console.WriteLine("Usage: AKAIITO4x3Launcher.exe <width> [windowed|fullscreen]");
            return 2;
        }
        if (width < 640 || width > 7680)
        {
            Console.Error.WriteLine("Width must be between 640 and 7680.");
            return 2;
        }

        int height = (int)Math.Round(width * 3.0 / 4.0, MidpointRounding.AwayFromZero);
        if ((height & 1) != 0) height++;
        bool fullscreen = args.Length > 1 &&
            (string.Equals(args[1], "fullscreen", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(args[1], "full", StringComparison.OrdinalIgnoreCase));

        string mode = fullscreen ? "1" : "0";
        var psi = new ProcessStartInfo
        {
            FileName = Exe,
            Arguments = string.Format("-screen-width {0} -screen-height {1} -screen-fullscreen {2}", width, height, mode),
            WorkingDirectory = @"G:\VN\AkaiIto",
            UseShellExecute = false
        };
        Process.Start(psi);
        Console.WriteLine("Launching AKAIITO at {0}x{1} ({2}).", width, height, fullscreen ? "fullscreen" : "windowed");
        return 0;
    }
}
