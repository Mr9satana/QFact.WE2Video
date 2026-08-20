namespace QFact.WE2Video;

internal static class AppPaths
{
    public const string ProductName = "QFact.WE2Video";
    public const string Version = "1.1.0-dev";

    public static string DataDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ProductName);

    public static string SettingsPath => Path.Combine(DataDirectory, "settings.json");
    public static string LogDirectory => Path.Combine(DataDirectory, "logs");
    public static string CacheDirectory => Path.Combine(DataDirectory, "cache");
    public static string RuntimeDirectory => Path.Combine(DataDirectory, "runtime");
    public static string WebViewDirectory => Path.Combine(DataDirectory, "webview2");
    public static string UiDirectory => Path.Combine(RuntimeDirectory, "ui", Version);

    public static string DefaultExportDirectory
    {
        get
        {
            var videos = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
            if (string.IsNullOrWhiteSpace(videos)) videos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(videos, ProductName, "Exports");
        }
    }

    public static void EnsureBaseDirectories()
    {
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(LogDirectory);
        Directory.CreateDirectory(CacheDirectory);
        Directory.CreateDirectory(RuntimeDirectory);
        Directory.CreateDirectory(WebViewDirectory);
    }
}
