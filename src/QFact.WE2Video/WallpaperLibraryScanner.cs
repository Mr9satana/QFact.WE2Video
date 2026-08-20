using System.Text.Json;

namespace QFact.WE2Video;

internal sealed class WallpaperLibraryScanner
{
    private const string WallpaperEngineAppId = "431960";

    public Task<IReadOnlyList<WallpaperInfo>> ScanAsync(CancellationToken cancellationToken = default)
        => ScanAsync(null, cancellationToken);

    public Task<IReadOnlyList<WallpaperInfo>> ScanAsync(string? manualRoot, CancellationToken cancellationToken = default)
        => Task.Run(() => Scan(manualRoot, cancellationToken), cancellationToken);

    private static IReadOnlyList<WallpaperInfo> Scan(string? manualRoot, CancellationToken cancellationToken)
    {
        var projectFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Workshop subscriptions. Scan every Steam library, not only the one containing Wallpaper Engine.
        foreach (var steamRoot in SteamLibraryLocator.FindSteamLibraries(manualRoot))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var workshopRoot = Path.Combine(steamRoot, "steamapps", "workshop", "content", WallpaperEngineAppId);
            AddProjectJsonChildren(projectFiles, workshopRoot, cancellationToken);
        }

        // User-created and built-in projects live beneath the Wallpaper Engine installation.
        foreach (var weRoot in SteamLibraryLocator.FindWallpaperEngineInstallations(manualRoot))
        {
            cancellationToken.ThrowIfCancellationRequested();
            AddProjectJsonRecursive(projectFiles, Path.Combine(weRoot, "projects", "myprojects"), cancellationToken, maxDepth: 3);
            AddProjectJsonRecursive(projectFiles, Path.Combine(weRoot, "projects", "defaultprojects"), cancellationToken, maxDepth: 3);
        }

        var wallpapers = new List<WallpaperInfo>();
        foreach (var projectJson in projectFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var info = TryParseProject(projectJson);
            if (info != null) wallpapers.Add(info);
        }

        return wallpapers
            .OrderBy(x => x.Title, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(x => x.WorkshopId ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void AddProjectJsonChildren(HashSet<string> results, string root, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(root)) return;
        try
        {
            foreach (var dir in Directory.EnumerateDirectories(root))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var project = Path.Combine(dir, "project.json");
                if (File.Exists(project)) results.Add(Path.GetFullPath(project));
            }
        }
        catch { }
    }

    private static void AddProjectJsonRecursive(
        HashSet<string> results, string root, CancellationToken cancellationToken, int maxDepth)
    {
        if (!Directory.Exists(root)) return;
        Walk(root, 0);

        void Walk(string dir, int depth)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (depth > maxDepth) return;

            try
            {
                var project = Path.Combine(dir, "project.json");
                if (File.Exists(project)) results.Add(Path.GetFullPath(project));
                foreach (var child in Directory.EnumerateDirectories(dir)) Walk(child, depth + 1);
            }
            catch { }
        }
    }

    private static WallpaperInfo? TryParseProject(string projectJson)
    {
        try
        {
            using var stream = File.OpenRead(projectJson);
            using var doc = JsonDocument.Parse(stream, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });

            var root = doc.RootElement;
            var folder = Path.GetDirectoryName(projectJson)!;
            var title = GetString(root, "title") ?? new DirectoryInfo(folder).Name;
            var type = (GetString(root, "type") ?? InferType(folder)).ToLowerInvariant();
            var file = GetString(root, "file");
            var preview = GetString(root, "preview");
            var workshopId = GetStringOrNumber(root, "workshopid") ?? InferWorkshopId(folder);
            var description = GetString(root, "description");
            var author = GetString(root, "author");

            var previewPath = ResolveExistingPath(folder, preview);
            previewPath ??= FindPreviewFallback(folder);

            var launchPath = ResolveLaunchPath(projectJson, folder, type, file);
            var source = IsWorkshopPath(projectJson) ? "Workshop" : "Local";

            return new WallpaperInfo(
                Title: title,
                Type: type,
                ProjectJsonPath: Path.GetFullPath(projectJson),
                LaunchPath: launchPath,
                PreviewPath: previewPath,
                WorkshopId: workshopId,
                Source: source,
                Description: description,
                Author: author);
        }
        catch
        {
            return null;
        }
    }

    private static string ResolveLaunchPath(string projectJson, string folder, string type, string? file)
    {
        // Wallpaper Engine's CLI guidance uses project.json for Scene wallpapers,
        // the media file for Video and the HTML entry point for Web.
        if (type == "scene") return Path.GetFullPath(projectJson);

        var configured = ResolveExistingPath(folder, file);
        if (configured != null) return configured;

        if (type == "web")
        {
            var index = Path.Combine(folder, "index.html");
            if (File.Exists(index)) return Path.GetFullPath(index);
        }

        if (type == "video")
        {
            foreach (var ext in new[] { "*.mp4", "*.webm", "*.mkv", "*.mov", "*.avi" })
            {
                try
                {
                    var candidate = Directory.EnumerateFiles(folder, ext, SearchOption.TopDirectoryOnly).FirstOrDefault();
                    if (candidate != null) return Path.GetFullPath(candidate);
                }
                catch { }
            }
        }

        // project.json is a safe final fallback for Wallpaper Engine-managed projects.
        return Path.GetFullPath(projectJson);
    }

    private static string? ResolveExistingPath(string folder, string? relative)
    {
        if (string.IsNullOrWhiteSpace(relative)) return null;
        try
        {
            var path = relative!;
            if (!Path.IsPathRooted(path)) path = Path.Combine(folder, path);
            return File.Exists(path) ? Path.GetFullPath(path) : null;
        }
        catch { return null; }
    }

    private static string? FindPreviewFallback(string folder)
    {
        foreach (var name in new[] { "preview.jpg", "preview.jpeg", "preview.png", "preview.gif", "preview.webp" })
        {
            var path = Path.Combine(folder, name);
            if (File.Exists(path)) return Path.GetFullPath(path);
        }
        return null;
    }

    private static string? GetString(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value)) return null;
        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    private static string? GetStringOrNumber(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value)) return null;
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null
        };
    }

    private static string InferType(string folder)
    {
        if (File.Exists(Path.Combine(folder, "scene.pkg")) || File.Exists(Path.Combine(folder, "scene.json"))) return "scene";
        if (File.Exists(Path.Combine(folder, "index.html"))) return "web";
        if (Directory.EnumerateFiles(folder, "*.mp4", SearchOption.TopDirectoryOnly).Any()) return "video";
        return "unknown";
    }

    private static bool IsWorkshopPath(string path)
        => path.Contains($"{Path.DirectorySeparatorChar}workshop{Path.DirectorySeparatorChar}content{Path.DirectorySeparatorChar}{WallpaperEngineAppId}{Path.DirectorySeparatorChar}",
            StringComparison.OrdinalIgnoreCase);

    private static string? InferWorkshopId(string folder)
    {
        var name = new DirectoryInfo(folder).Name;
        return name.All(char.IsDigit) ? name : null;
    }
}
