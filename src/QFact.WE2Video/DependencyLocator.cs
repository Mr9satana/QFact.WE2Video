using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace QFact.WE2Video;

internal static class DependencyLocator
{
    public static string? FindWallpaperEngine(string? explicitPath)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            var direct = NormalizeExecutable(explicitPath, "wallpaper64.exe", "wallpaper32.exe");
            if (direct != null) return direct;

            foreach (var dir in SteamLibraryLocator.FindWallpaperEngineInstallations(explicitPath))
            {
                var exe64 = Path.Combine(dir, "wallpaper64.exe");
                if (File.Exists(exe64)) return exe64;
                var exe32 = Path.Combine(dir, "wallpaper32.exe");
                if (File.Exists(exe32)) return exe32;
            }
        }

        // Prefer the exact binary of an already-running instance when no manual root is usable.
        foreach (var processName in new[] { "wallpaper64", "wallpaper32" })
        {
            try
            {
                foreach (var p in Process.GetProcessesByName(processName))
                {
                    try
                    {
                        var path = p.MainModule?.FileName;
                        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                            return path;
                    }
                    catch { }
                    finally { p.Dispose(); }
                }
            }
            catch { }
        }

        foreach (var dir in SteamLibraryLocator.FindWallpaperEngineInstallations())
        {
            var exe64 = Path.Combine(dir, "wallpaper64.exe");
            if (File.Exists(exe64)) return exe64;
            var exe32 = Path.Combine(dir, "wallpaper32.exe");
            if (File.Exists(exe32)) return exe32;
        }

        return null;
    }

    public static string? FindFfprobe(string? explicitPath)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
            return NormalizeExecutable(explicitPath, "ffprobe.exe");

        var ffmpeg = FindFfmpeg(null);
        if (!string.IsNullOrWhiteSpace(ffmpeg))
        {
            var sibling = Path.Combine(Path.GetDirectoryName(ffmpeg)!, "ffprobe.exe");
            if (File.Exists(sibling)) return Path.GetFullPath(sibling);
        }

        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "where.exe",
                Arguments = "ffprobe.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(3000);
                var path = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault(File.Exists);
                if (path != null) return path;
            }
        }
        catch { }

        return null;
    }

    public static string? FindFfmpeg(string? explicitPath)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
            return NormalizeExecutable(explicitPath, "ffmpeg.exe");

        var localCandidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "tools", "ffmpeg", "bin", "ffmpeg.exe"),
            Path.Combine(AppContext.BaseDirectory, "tools", "ffmpeg.exe"),
            Path.Combine(Environment.CurrentDirectory, "tools", "ffmpeg", "bin", "ffmpeg.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "WinGet", "Links", "ffmpeg.exe")
        };

        foreach (var candidate in localCandidates)
            if (File.Exists(candidate)) return Path.GetFullPath(candidate);

        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "where.exe",
                Arguments = "ffmpeg.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(3000);
                var path = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault(File.Exists);
                if (path != null) return path;
            }
        }
        catch { }

        // Last chance: search portable WinGet package directories without assuming a version.
        try
        {
            var packages = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "WinGet", "Packages");
            if (Directory.Exists(packages))
            {
                var candidates = Directory.EnumerateFiles(packages, "ffmpeg.exe", SearchOption.AllDirectories)
                    .Take(20)
                    .ToArray();
                var preferred = candidates.FirstOrDefault(x =>
                    x.Contains("Gyan.FFmpeg", StringComparison.OrdinalIgnoreCase) ||
                    x.Contains("BtbN.FFmpeg", StringComparison.OrdinalIgnoreCase));
                if (preferred != null) return preferred;
                if (candidates.Length > 0) return candidates[0];
            }
        }
        catch { }

        return null;
    }

    private static string? NormalizeExecutable(string path, params string[] names)
    {
        path = Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'));
        if (File.Exists(path)) return Path.GetFullPath(path);
        if (!Directory.Exists(path)) return null;
        foreach (var name in names)
        {
            var candidate = Path.Combine(path, name);
            if (File.Exists(candidate)) return Path.GetFullPath(candidate);
        }
        return null;
    }

    private static void AddIfDirectory(HashSet<string> set, string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        path = Environment.ExpandEnvironmentVariables(path).Replace('/', '\\');
        if (Directory.Exists(path)) set.Add(Path.GetFullPath(path));
    }
}
