using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace QFact.WE2Video;

internal static class SteamLibraryLocator
{
    public static IReadOnlyList<string> FindSteamLibraries(string? manualRoot = null)
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddManualSteamCandidates(roots, manualRoot);
        AddIfDirectory(roots, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"));
        AddIfDirectory(roots, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam"));

        foreach (var key in new[]
        {
            @"HKEY_CURRENT_USER\Software\Valve\Steam",
            @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam",
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam"
        })
        {
            try
            {
                AddIfDirectory(roots, Registry.GetValue(key, "SteamPath", null) as string);
                AddIfDirectory(roots, Registry.GetValue(key, "InstallPath", null) as string);
            }
            catch { }
        }

        // libraryfolders.vdf exists in each known Steam root. Iterate a snapshot because
        // it can add other library roots to the same set.
        var scanQueue = new Queue<string>(roots);
        var scanned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (scanQueue.Count > 0)
        {
            var steamRoot = scanQueue.Dequeue();
            if (!scanned.Add(steamRoot)) continue;
            var before = roots.Count;
            ReadLibraryFolders(steamRoot, roots);
            if (roots.Count > before)
            {
                foreach (var root in roots)
                    if (!scanned.Contains(root)) scanQueue.Enqueue(root);
            }
        }

        return roots.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public static IReadOnlyList<string> FindWallpaperEngineInstallations(string? manualRoot = null)
    {
        var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddManualWallpaperEngineCandidates(results, manualRoot);
        foreach (var root in FindSteamLibraries(manualRoot))
        {
            var dir = Path.Combine(root, "steamapps", "common", "wallpaper_engine");
            if (Directory.Exists(dir)) results.Add(Path.GetFullPath(dir));
        }
        return results.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public static bool LooksLikeSteamOrWallpaperEngineRoot(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        try
        {
            path = NormalizePath(path);
            if (File.Exists(path)) path = Path.GetDirectoryName(path)!;
            if (!Directory.Exists(path)) return false;
            if (File.Exists(Path.Combine(path, "wallpaper64.exe")) || File.Exists(Path.Combine(path, "wallpaper32.exe"))) return true;
            if (Directory.Exists(Path.Combine(path, "steamapps"))) return true;
            if (string.Equals(new DirectoryInfo(path).Name, "steamapps", StringComparison.OrdinalIgnoreCase)) return true;
            if (Directory.Exists(Path.Combine(path, "common", "wallpaper_engine"))) return true;
            return false;
        }
        catch { return false; }
    }

    private static void ReadLibraryFolders(string steamRoot, HashSet<string> roots)
    {
        var vdf = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(vdf)) return;
        try
        {
            var text = File.ReadAllText(vdf);
            foreach (Match match in Regex.Matches(text, "\\\"path\\\"\\s+\\\"([^\\\"]+)\\\""))
            {
                var path = match.Groups[1].Value.Replace("\\\\", "\\");
                AddIfDirectory(roots, path);
            }
        }
        catch { }
    }

    private static void AddManualSteamCandidates(HashSet<string> roots, string? manualRoot)
    {
        if (string.IsNullOrWhiteSpace(manualRoot)) return;
        try
        {
            var path = NormalizePath(manualRoot);
            if (File.Exists(path)) path = Path.GetDirectoryName(path)!;
            if (!Directory.Exists(path)) return;

            // User selected a normal Steam / Steam-library root.
            if (Directory.Exists(Path.Combine(path, "steamapps"))) AddIfDirectory(roots, path);

            // User selected the steamapps folder itself.
            if (string.Equals(new DirectoryInfo(path).Name, "steamapps", StringComparison.OrdinalIgnoreCase))
                AddIfDirectory(roots, Directory.GetParent(path)?.FullName);

            // User selected wallpaper_engine (or a nested folder). Walk upwards until a
            // directory containing steamapps is found.
            var cursor = new DirectoryInfo(path);
            for (var i = 0; i < 7 && cursor != null; i++, cursor = cursor.Parent)
            {
                if (Directory.Exists(Path.Combine(cursor.FullName, "steamapps")))
                {
                    AddIfDirectory(roots, cursor.FullName);
                    break;
                }
            }
        }
        catch { }
    }

    private static void AddManualWallpaperEngineCandidates(HashSet<string> results, string? manualRoot)
    {
        if (string.IsNullOrWhiteSpace(manualRoot)) return;
        try
        {
            var path = NormalizePath(manualRoot);
            if (File.Exists(path)) path = Path.GetDirectoryName(path)!;
            if (!Directory.Exists(path)) return;

            if (File.Exists(Path.Combine(path, "wallpaper64.exe")) || File.Exists(Path.Combine(path, "wallpaper32.exe")))
                results.Add(Path.GetFullPath(path));

            var standard = Path.Combine(path, "steamapps", "common", "wallpaper_engine");
            if (Directory.Exists(standard)) results.Add(Path.GetFullPath(standard));

            if (string.Equals(new DirectoryInfo(path).Name, "steamapps", StringComparison.OrdinalIgnoreCase))
            {
                var fromSteamapps = Path.Combine(path, "common", "wallpaper_engine");
                if (Directory.Exists(fromSteamapps)) results.Add(Path.GetFullPath(fromSteamapps));
            }
        }
        catch { }
    }

    private static string NormalizePath(string path)
        => Environment.ExpandEnvironmentVariables(path.Trim().Trim('"')).Replace('/', '\\');

    private static void AddIfDirectory(HashSet<string> set, string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        path = NormalizePath(path);
        try
        {
            if (Directory.Exists(path)) set.Add(Path.GetFullPath(path));
        }
        catch { }
    }
}
