namespace QFact.WE2Video;

internal static class AppLogger
{
    private static readonly object Gate = new();

    public static string LogDirectory
    {
        get
        {
            AppPaths.EnsureBaseDirectories();
            return AppPaths.LogDirectory;
        }
    }

    public static string LogPath => Path.Combine(LogDirectory, "qfact-we2video.log");

    public static void Info(string message) => Write("INFO", message);
    public static void Warn(string message) => Write("WARN", message);
    public static void Error(string message) => Write("ERROR", message);

    private static void Write(string level, string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
        try { Console.WriteLine(line); } catch { }
        try
        {
            lock (Gate) File.AppendAllText(LogPath, line + Environment.NewLine);
        }
        catch { }
    }
}
