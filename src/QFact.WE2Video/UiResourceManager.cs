using System.IO.Compression;
using System.Reflection;

namespace QFact.WE2Video;

internal static class UiResourceManager
{
    private const string ResourceName = "QFact.WE2Video.ui.bundle.zip";

    public static string EnsureExtracted()
    {
        AppPaths.EnsureBaseDirectories();
        var uiDir = AppPaths.UiDirectory;
        var marker = Path.Combine(uiDir, ".ready");
        var index = Path.Combine(uiDir, "index.html");
        if (File.Exists(marker) && File.Exists(index)) return uiDir;

        try { if (Directory.Exists(uiDir)) Directory.Delete(uiDir, recursive: true); } catch { }
        Directory.CreateDirectory(uiDir);

        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new FileNotFoundException(AppI18n.T("uiMissing") + $" Resource: {ResourceName}");
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name)) continue;
            var destination = Path.GetFullPath(Path.Combine(uiDir, entry.FullName.Replace('/', Path.DirectorySeparatorChar)));
            if (!destination.StartsWith(Path.GetFullPath(uiDir) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Invalid UI resource path.");
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            entry.ExtractToFile(destination, overwrite: true);
        }
        File.WriteAllText(marker, AppPaths.Version);
        return uiDir;
    }
}
