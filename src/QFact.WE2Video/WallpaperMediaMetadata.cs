using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace QFact.WE2Video;

internal sealed record WallpaperMediaMetadata(
    int? NativeWidth,
    int? NativeHeight,
    string NativeResolutionSource,
    double? VideoDurationSeconds,
    double? AudioDurationSeconds,
    int AudioAssetCount,
    bool AudioKnownAbsent,
    string? Note,
    IReadOnlyList<string>? WorkshopTags = null,
    string? WorkshopResolutionTag = null)
{
    public bool HasNativeResolution => NativeWidth is > 0 && NativeHeight is > 0;
}

internal sealed class WallpaperMediaMetadataService
{
    private static readonly HashSet<string> MediaExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".ogg", ".wav", ".m4a", ".aac", ".flac", ".opus", ".wma",
        ".mp4", ".webm", ".mkv", ".mov", ".avi"
    };

    private readonly Dictionary<string, WallpaperMediaMetadata> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly WorkshopMetadataService _workshopMetadata = new();

    public async Task<WallpaperMediaMetadata> GetAsync(WallpaperInfo wallpaper, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(wallpaper.ProjectJsonPath, out var cached))
            return cached;

        var result = await InspectAsync(wallpaper, cancellationToken);
        _cache[wallpaper.ProjectJsonPath] = result;
        return result;
    }

    public void ClearCache() => _cache.Clear();

    private async Task<WallpaperMediaMetadata> InspectAsync(WallpaperInfo wallpaper, CancellationToken cancellationToken)
    {
        var (jsonWidth, jsonHeight, jsonSource) = TryReadProjectResolution(wallpaper.ProjectJsonPath);
        int? nativeWidth = jsonWidth;
        int? nativeHeight = jsonHeight;
        var nativeSource = jsonSource;

        var ffprobe = DependencyLocator.FindFfprobe(null);
        if (ffprobe == null)
        {
            var workshopOnly = await TryWorkshopFallbackAsync(wallpaper, nativeWidth, nativeHeight, nativeSource, cancellationToken);
            return new WallpaperMediaMetadata(
                workshopOnly.Width, workshopOnly.Height, workshopOnly.Source,
                null, null, 0, false,
                AppI18n.T("ffprobeMissing"),
                workshopOnly.Tags, workshopOnly.ResolutionTag);
        }

        // Video wallpapers have a real source video. Probe it first because its dimensions and duration are authoritative.
        if (wallpaper.DisplayType == "video" && File.Exists(wallpaper.LaunchPath))
        {
            var probe = await ProbeFileAsync(ffprobe, wallpaper.LaunchPath, cancellationToken);
            if (probe != null)
            {
                if (probe.Width is > 0 && probe.Height is > 0)
                {
                    nativeWidth = probe.Width;
                    nativeHeight = probe.Height;
                    nativeSource = AppI18n.T("nativeVideo");
                }

                return new WallpaperMediaMetadata(
                    nativeWidth, nativeHeight, nativeSource,
                    probe.DurationSeconds,
                    probe.HasAudio ? probe.DurationSeconds : null,
                    probe.HasAudio ? 1 : 0,
                    AudioKnownAbsent: !probe.HasAudio,
                    Note: null);
            }
        }

        // Scene/Web projects may expose audio/video assets as separate files. Packed scene.pkg contents are not directly inspectable.
        var mediaFiles = EnumerateMediaFiles(wallpaper.FolderPath)
            .Where(path => !IsPreviewFile(path, wallpaper.PreviewPath))
            .Take(24)
            .ToArray();

        double? longestAudio = null;
        var audioAssets = 0;
        foreach (var media in mediaFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var probe = await ProbeFileAsync(ffprobe, media, cancellationToken);
            if (probe?.HasAudio != true) continue;

            audioAssets++;
            if (probe.DurationSeconds is > 0 && (longestAudio == null || probe.DurationSeconds > longestAudio))
                longestAudio = probe.DurationSeconds;
        }

        string? note = null;
        if (wallpaper.DisplayType == "scene" && mediaFiles.Length == 0)
            note = AppI18n.T("scenePackedAudio");
        else if (audioAssets == 0 && wallpaper.DisplayType is "scene" or "web")
            note = AppI18n.T("noOpenAudio");

        var workshop = await TryWorkshopFallbackAsync(wallpaper, nativeWidth, nativeHeight, nativeSource, cancellationToken);

        return new WallpaperMediaMetadata(
            workshop.Width, workshop.Height, workshop.Source,
            null,
            longestAudio,
            audioAssets,
            AudioKnownAbsent: false,
            note,
            workshop.Tags,
            workshop.ResolutionTag);
    }

    private async Task<(int? Width, int? Height, string Source, IReadOnlyList<string>? Tags, string? ResolutionTag)> TryWorkshopFallbackAsync(
        WallpaperInfo wallpaper,
        int? currentWidth,
        int? currentHeight,
        string currentSource,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(wallpaper.WorkshopId))
            return (currentWidth, currentHeight, currentSource, null, null);

        var workshop = await _workshopMetadata.GetAsync(wallpaper.WorkshopId, cancellationToken);
        if (workshop == null)
            return (currentWidth, currentHeight, currentSource, null, null);

        if (currentWidth is > 0 && currentHeight is > 0)
            return (currentWidth, currentHeight, currentSource, workshop.Tags, workshop.ResolutionTag);

        if (workshop.HasResolution)
        {
            return (
                workshop.ResolutionWidth,
                workshop.ResolutionHeight,
                $"Workshop tag: {workshop.ResolutionTag}",
                workshop.Tags,
                workshop.ResolutionTag);
        }

        return (currentWidth, currentHeight, currentSource, workshop.Tags, null);
    }

    private static IEnumerable<string> EnumerateMediaFiles(string folder)
    {
        if (!Directory.Exists(folder)) yield break;

        IEnumerator<string>? enumerator = null;
        try
        {
            enumerator = Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).GetEnumerator();
            while (true)
            {
                string file;
                try
                {
                    if (!enumerator.MoveNext()) break;
                    file = enumerator.Current;
                }
                catch { break; }

                if (MediaExtensions.Contains(Path.GetExtension(file)))
                    yield return file;
            }
        }
        finally
        {
            enumerator?.Dispose();
        }
    }

    private static bool IsPreviewFile(string path, string? previewPath)
    {
        if (previewPath != null && string.Equals(Path.GetFullPath(path), Path.GetFullPath(previewPath), StringComparison.OrdinalIgnoreCase))
            return true;
        return Path.GetFileNameWithoutExtension(path).Equals("preview", StringComparison.OrdinalIgnoreCase);
    }

    private static (int? Width, int? Height, string Source) TryReadProjectResolution(string projectJson)
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
            if (TryReadWidthHeight(root, out var w, out var h))
                return (w, h, "project.json");

            if (root.TryGetProperty("general", out var general) && general.ValueKind == JsonValueKind.Object)
            {
                if (TryReadWidthHeight(general, out w, out h))
                    return (w, h, "project.json/general");
            }
        }
        catch { }

        return (null, null, AppI18n.T("notSpecified"));
    }

    private static bool TryReadWidthHeight(JsonElement element, out int width, out int height)
    {
        width = 0;
        height = 0;

        if (TryGetPositiveInt(element, "width", out width) && TryGetPositiveInt(element, "height", out height))
            return true;

        if (element.TryGetProperty("resolution", out var resolution))
        {
            if (resolution.ValueKind == JsonValueKind.String)
            {
                var text = resolution.GetString() ?? string.Empty;
                var parts = text.ToLowerInvariant().Split('x', '×');
                if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out width) && int.TryParse(parts[1].Trim(), out height) && width > 0 && height > 0)
                    return true;
            }
            else if (resolution.ValueKind == JsonValueKind.Object &&
                     TryGetPositiveInt(resolution, "width", out width) && TryGetPositiveInt(resolution, "height", out height))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryGetPositiveInt(JsonElement element, string name, out int value)
    {
        value = 0;
        if (!element.TryGetProperty(name, out var property)) return false;
        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out value) && value > 0) return true;
        if (property.ValueKind == JsonValueKind.String && int.TryParse(property.GetString(), out value) && value > 0) return true;
        value = 0;
        return false;
    }

    private static async Task<FileProbe?> ProbeFileAsync(string ffprobe, string path, CancellationToken cancellationToken)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = ffprobe,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            foreach (var arg in new[]
            {
                "-v", "error",
                "-show_entries", "format=duration:stream=codec_type,width,height,duration",
                "-of", "json",
                path
            }) psi.ArgumentList.Add(arg);

            using var process = Process.Start(psi);
            if (process == null) return null;

            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(8));
            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                throw;
            }
            var json = await stdoutTask;
            _ = await stderrTask;
            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(json)) return null;

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            int? width = null;
            int? height = null;
            var hasAudio = false;
            double? maxStreamDuration = null;

            if (root.TryGetProperty("streams", out var streams) && streams.ValueKind == JsonValueKind.Array)
            {
                foreach (var stream in streams.EnumerateArray())
                {
                    var type = stream.TryGetProperty("codec_type", out var codecType) ? codecType.GetString() : null;
                    if (type == "video" && width == null)
                    {
                        if (stream.TryGetProperty("width", out var w) && w.TryGetInt32(out var wi)) width = wi;
                        if (stream.TryGetProperty("height", out var h) && h.TryGetInt32(out var hi)) height = hi;
                    }
                    else if (type == "audio")
                    {
                        hasAudio = true;
                    }

                    if (stream.TryGetProperty("duration", out var sd) && TryParseDouble(sd, out var streamDuration) && streamDuration > 0)
                        maxStreamDuration = maxStreamDuration == null ? streamDuration : Math.Max(maxStreamDuration.Value, streamDuration);
                }
            }

            double? duration = null;
            if (root.TryGetProperty("format", out var format) && format.TryGetProperty("duration", out var fd) &&
                TryParseDouble(fd, out var formatDuration) && formatDuration > 0)
            {
                duration = formatDuration;
            }
            else
            {
                duration = maxStreamDuration;
            }

            return new FileProbe(width, height, duration, hasAudio);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static bool TryParseDouble(JsonElement element, out double value)
    {
        value = 0;
        if (element.ValueKind == JsonValueKind.Number) return element.TryGetDouble(out value);
        if (element.ValueKind == JsonValueKind.String)
            return double.TryParse(element.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        return false;
    }

    private sealed record FileProbe(int? Width, int? Height, double? DurationSeconds, bool HasAudio);
}
