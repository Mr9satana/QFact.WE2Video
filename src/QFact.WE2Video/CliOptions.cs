namespace QFact.WE2Video;

internal sealed class CliOptions
{
    public string? WallpaperPath { get; set; }
    public string? WallpaperEnginePath { get; set; }
    public string? FfmpegPath { get; set; }
    public string? OutputPath { get; set; }
    public int Width { get; set; } = 1920;
    public int Height { get; set; } = 1080;
    public int Fps { get; set; } = 60;
    public double DurationSeconds { get; set; } = 10;
    public string CaptureBackend { get; set; } = "auto";
    public string OutputFormat { get; set; } = "mp4-h264";
    public bool Doctor { get; set; }
    public bool ListLibrary { get; set; }
    public bool Help { get; set; }
    public bool KeepWindowOpen { get; set; }
    public string? WorkshopMetadataId { get; set; }

    public static CliOptions Parse(string[] args)
    {
        var options = new CliOptions();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg.ToLowerInvariant())
            {
                case "--wallpaper": case "-w": options.WallpaperPath = NeedValue(args, ref i, arg); break;
                case "--we": options.WallpaperEnginePath = NeedValue(args, ref i, arg); break;
                case "--ffmpeg": options.FfmpegPath = NeedValue(args, ref i, arg); break;
                case "--output": case "-o": options.OutputPath = NeedValue(args, ref i, arg); break;
                case "--format": options.OutputFormat = NeedValue(args, ref i, arg).ToLowerInvariant(); OutputProfiles.Resolve(options.OutputFormat); break;
                case "--width": options.Width = ParseInt(NeedValue(args, ref i, arg), arg, 64, 16384); break;
                case "--height": options.Height = ParseInt(NeedValue(args, ref i, arg), arg, 64, 16384); break;
                case "--fps": options.Fps = ParseInt(NeedValue(args, ref i, arg), arg, 1, 240); break;
                case "--duration": options.DurationSeconds = ParseDouble(NeedValue(args, ref i, arg), arg, 0.1, 86400); break;
                case "--capture":
                    options.CaptureBackend = NeedValue(args, ref i, arg).ToLowerInvariant();
                    if (options.CaptureBackend is not ("auto" or "gfx" or "gdi"))
                        throw new ArgumentException("--capture must be: auto, gfx or gdi.");
                    break;
                case "--doctor": options.Doctor = true; break;
                case "--list": options.ListLibrary = true; break;
                case "--workshop-meta": options.WorkshopMetadataId = NeedValue(args, ref i, arg); break;
                case "--keep-window": options.KeepWindowOpen = true; break;
                case "--help": case "-h": case "/?": options.Help = true; break;
                default: throw new ArgumentException($"Unknown argument: {arg}");
            }
        }
        return options;
    }

    private static string NeedValue(string[] args, ref int i, string name)
    {
        if (i + 1 >= args.Length) throw new ArgumentException($"Missing value after {name}.");
        return args[++i];
    }

    private static int ParseInt(string value, string name, int min, int max)
    {
        if (!int.TryParse(value, out var parsed) || parsed < min || parsed > max)
            throw new ArgumentException($"{name} must be an integer from {min} to {max}.");
        return parsed;
    }

    private static double ParseDouble(string value, string name, double min, double max)
    {
        if (!double.TryParse(value, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var parsed) || parsed < min || parsed > max)
            throw new ArgumentException($"{name} must be a number from {min} to {max}, use a dot as decimal separator.");
        return parsed;
    }
}
