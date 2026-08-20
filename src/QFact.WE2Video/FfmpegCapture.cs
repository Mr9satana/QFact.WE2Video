using System.Diagnostics;
using System.Globalization;

namespace QFact.WE2Video;

internal sealed class FfmpegCapture
{
    private readonly string _ffmpeg;

    public FfmpegCapture(string ffmpeg) => _ffmpeg = ffmpeg;

    public async Task<FfmpegCapabilities> ProbeAsync()
    {
        var version = await RunAndCaptureAsync(new[] { "-hide_banner", "-version" }, 8000);
        var filters = await RunAndCaptureAsync(new[] { "-hide_banner", "-filters" }, 15000);
        var encoders = await RunAndCaptureAsync(new[] { "-hide_banner", "-encoders" }, 15000);

        var firstLine = version.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ?? "unknown";

        return new FfmpegCapabilities(
            VersionLine: firstLine,
            HasGfxCapture: filters.Output.Contains("gfxcapture", StringComparison.OrdinalIgnoreCase),
            HasGdiGrab: filters.Output.Contains("gdigrab", StringComparison.OrdinalIgnoreCase) || await HasGdiDeviceAsync(),
            HasLibX264: HasEncoder(encoders.Output, "libx264"),
            HasLibX265: HasEncoder(encoders.Output, "libx265"),
            HasLibVpxVp9: HasEncoder(encoders.Output, "libvpx-vp9"),
            HasLibOpus: HasEncoder(encoders.Output, "libopus"),
            HasGif: HasEncoder(encoders.Output, "gif"));
    }

    public async Task<CaptureResult> CaptureAsync(
        IntPtr hwnd,
        string windowTitle,
        string outputPath,
        int width,
        int height,
        int fps,
        double durationSeconds,
        string requestedBackend,
        FfmpegCapabilities caps,
        OutputProfile profile)
    {
        EnsureVideoSupport(profile, caps);

        var backends = requestedBackend switch
        {
            "gfx" => new[] { "gfx" },
            "gdi" => new[] { "gdi" },
            _ => caps.HasGfxCapture ? new[] { "gfx", "gdi" } : new[] { "gdi" }
        };

        var errors = new List<string>();
        foreach (var backend in backends)
        {
            if (backend == "gfx" && !caps.HasGfxCapture)
            {
                errors.Add("gfxcapture is not available in this FFmpeg build.");
                continue;
            }
            if (backend == "gdi" && !caps.HasGdiGrab)
            {
                errors.Add("gdigrab is not available in this FFmpeg build.");
                continue;
            }

            if (File.Exists(outputPath)) File.Delete(outputPath);
            var args = backend == "gfx"
                ? BuildGfxArgs(hwnd, outputPath, width, height, fps, durationSeconds, profile)
                : BuildGdiArgs(windowTitle, outputPath, width, height, fps, durationSeconds, profile);

            var result = await RunFfmpegAsync(args);
            if (IsGoodOutput(result.ExitCode, outputPath))
                return new CaptureResult(backend, result.Log, profile.Id);

            errors.Add($"Backend {backend} failed (exit {result.ExitCode}):\n{TrimLog(result.Log)}");
            if (backend == "gfx" && backends.Contains("gdi"))
                AppLogger.Warn("gfxcapture failed; retrying with GDI fallback.");
        }

        throw new InvalidOperationException("FFmpeg capture failed.\n\n" + string.Join("\n\n", errors));
    }

    public async Task<CaptureResult> TranscodeVideoAsync(
        string sourcePath,
        string outputPath,
        int width,
        int height,
        int fps,
        double durationSeconds,
        FfmpegCapabilities caps,
        OutputProfile profile,
        bool includeAudio)
    {
        EnsureVideoSupport(profile, caps);
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException(AppI18n.T("videoSourceMissing"), sourcePath);
        if (includeAudio && profile.SupportsAudio && !profile.IsAudioSupported(caps))
            throw new InvalidOperationException(profile.MissingAudioEncoderMessage);

        if (File.Exists(outputPath)) File.Delete(outputPath);
        var args = BuildDirectVideoArgs(sourcePath, outputPath, width, height, fps, durationSeconds, profile, includeAudio);
        var result = await RunFfmpegAsync(args);
        if (!IsGoodOutput(result.ExitCode, outputPath))
            throw new InvalidOperationException($"FFmpeg video transcode failed (exit {result.ExitCode}).\n{TrimLog(result.Log)}");

        return new CaptureResult("direct-video", result.Log, profile.Id);
    }

    public async Task<bool> MuxAudioAsync(
        string silentVideoPath,
        string wavPath,
        string outputPath,
        double durationSeconds,
        FfmpegCapabilities caps,
        OutputProfile profile)
    {
        if (!profile.SupportsAudio || profile.IsGif || !File.Exists(wavPath)) return false;
        if (!profile.IsAudioSupported(caps))
            throw new InvalidOperationException(profile.MissingAudioEncoderMessage);

        if (File.Exists(outputPath)) File.Delete(outputPath);
        var args = new List<string>
        {
            "-y", "-hide_banner", "-loglevel", "warning",
            "-i", silentVideoPath,
            "-i", wavPath,
            "-map", "0:v:0",
            "-map", "1:a:0",
            "-c:v", "copy"
        };
        AddAudioEncoderArgs(args, profile);
        args.AddRange(new[] { "-t", F(durationSeconds) });
        if (profile.Id is "mp4-h264" or "mp4-hevc" or "mov-h264")
            args.AddRange(new[] { "-movflags", "+faststart" });
        args.Add(outputPath);

        var result = await RunFfmpegAsync(args);
        if (!IsGoodOutput(result.ExitCode, outputPath))
            throw new InvalidOperationException($"FFmpeg audio mux failed (exit {result.ExitCode}).\n{TrimLog(result.Log)}");
        return true;
    }

    private static IReadOnlyList<string> BuildDirectVideoArgs(
        string source,
        string output,
        int width,
        int height,
        int fps,
        double duration,
        OutputProfile profile,
        bool includeAudio)
    {
        var resize = $"scale={width}:{height}:force_original_aspect_ratio=decrease:flags=lanczos," +
                     $"pad={width}:{height}:(ow-iw)/2:(oh-ih)/2:black,fps={fps},setsar=1";
        var args = new List<string>
        {
            "-y", "-hide_banner", "-loglevel", "warning",
            "-stream_loop", "-1", "-i", source,
            "-t", F(duration)
        };

        if (profile.IsGif)
        {
            var graph = $"[0:v]{resize},trim=duration={F(duration)},setpts=PTS-STARTPTS," +
                        "split[s0][s1];[s0]palettegen=max_colors=256:stats_mode=diff[p];" +
                        "[s1][p]paletteuse=dither=sierra2_4a[v]";
            args.AddRange(new[] { "-filter_complex", graph, "-map", "[v]", "-an" });
        }
        else
        {
            args.AddRange(new[] { "-map", "0:v:0", "-vf", resize + ",format=yuv420p" });
            if (includeAudio && profile.SupportsAudio)
            {
                args.AddRange(new[] { "-map", "0:a:0?" });
                AddAudioEncoderArgs(args, profile);
            }
            else
            {
                args.Add("-an");
            }
        }

        AddVideoEncoderArgs(args, profile);
        args.Add(output);
        return args;
    }

    private static IReadOnlyList<string> BuildGfxArgs(
        IntPtr hwnd, string output, int width, int height, int fps, double duration, OutputProfile profile)
    {
        var source =
            $"gfxcapture=hwnd={hwnd.ToInt64()}:max_framerate={fps}:capture_cursor=0:capture_border=0:" +
            $"width={width}:height={height}:resize_mode=scale_aspect," +
            $"hwdownload,format=bgra,fps={fps},setsar=1";
        var args = new List<string> { "-y", "-hide_banner", "-loglevel", "warning" };

        if (profile.IsGif)
        {
            var graph = source +
                        $",trim=duration={F(duration)},setpts=PTS-STARTPTS," +
                        "split[s0][s1];[s0]palettegen=max_colors=256:stats_mode=diff[p];" +
                        "[s1][p]paletteuse=dither=sierra2_4a[v]";
            args.AddRange(new[] { "-filter_complex", graph, "-map", "[v]" });
        }
        else
        {
            args.AddRange(new[] { "-filter_complex", source + ",format=yuv420p[v]", "-map", "[v]" });
        }

        args.AddRange(new[] { "-t", F(duration), "-an" });
        AddVideoEncoderArgs(args, profile);
        args.Add(output);
        return args;
    }

    private static IReadOnlyList<string> BuildGdiArgs(
        string windowTitle, string output, int width, int height, int fps, double duration, OutputProfile profile)
    {
        var resize = $"scale={width}:{height}:force_original_aspect_ratio=decrease:flags=lanczos," +
                     $"pad={width}:{height}:(ow-iw)/2:(oh-ih)/2:black,fps={fps},setsar=1";
        var args = new List<string>
        {
            "-y", "-hide_banner", "-loglevel", "warning",
            "-f", "gdigrab", "-draw_mouse", "0",
            "-framerate", fps.ToString(CultureInfo.InvariantCulture),
            "-i", $"title={windowTitle}", "-t", F(duration)
        };

        if (profile.IsGif)
        {
            var graph = $"[0:v]{resize},trim=duration={F(duration)},setpts=PTS-STARTPTS," +
                        "split[s0][s1];[s0]palettegen=max_colors=256:stats_mode=diff[p];" +
                        "[s1][p]paletteuse=dither=sierra2_4a[v]";
            args.AddRange(new[] { "-filter_complex", graph, "-map", "[v]" });
        }
        else
        {
            args.AddRange(new[] { "-vf", resize + ",format=yuv420p" });
        }

        args.Add("-an");
        AddVideoEncoderArgs(args, profile);
        args.Add(output);
        return args;
    }

    private static void AddVideoEncoderArgs(List<string> args, OutputProfile profile)
    {
        switch (profile.Id)
        {
            case "mp4-h264":
                args.AddRange(new[] { "-c:v", "libx264", "-preset", "ultrafast", "-crf", "18", "-movflags", "+faststart" });
                break;
            case "mp4-hevc":
                args.AddRange(new[] { "-c:v", "libx265", "-preset", "ultrafast", "-crf", "22", "-tag:v", "hvc1", "-movflags", "+faststart" });
                break;
            case "webm-vp9":
                args.AddRange(new[] { "-c:v", "libvpx-vp9", "-deadline", "realtime", "-cpu-used", "6", "-crf", "30", "-b:v", "0" });
                break;
            case "mkv-h264":
                args.AddRange(new[] { "-c:v", "libx264", "-preset", "ultrafast", "-crf", "18" });
                break;
            case "mov-h264":
                args.AddRange(new[] { "-c:v", "libx264", "-preset", "ultrafast", "-crf", "18", "-movflags", "+faststart" });
                break;
            case "gif":
                args.AddRange(new[] { "-c:v", "gif", "-loop", "0" });
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(profile), $"Unsupported output profile: {profile.Id}");
        }
    }

    private static void AddAudioEncoderArgs(List<string> args, OutputProfile profile)
    {
        if (!profile.SupportsAudio) return;
        if (profile.Id == "webm-vp9")
            args.AddRange(new[] { "-c:a", "libopus", "-b:a", "160k" });
        else
            args.AddRange(new[] { "-c:a", "aac", "-b:a", "192k" });
    }

    private static void EnsureVideoSupport(OutputProfile profile, FfmpegCapabilities caps)
    {
        if (!profile.IsSupported(caps))
            throw new InvalidOperationException(profile.MissingEncoderMessage + AppI18n.T("runDoctor"));
    }

    private async Task<(int ExitCode, string Log)> RunFfmpegAsync(IReadOnlyList<string> args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _ffmpeg,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };
        foreach (var arg in args) psi.ArgumentList.Add(arg);

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Could not start ffmpeg.exe.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        return (process.ExitCode, stdout + Environment.NewLine + stderr);
    }

    private async Task<CommandResult> RunAndCaptureAsync(IEnumerable<string> args, int timeoutMs)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _ffmpeg,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (var arg in args) psi.ArgumentList.Add(arg);

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Could not start ffmpeg.exe.");
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        using var cts = new CancellationTokenSource(timeoutMs);
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            throw new TimeoutException("FFmpeg capability check timed out.");
        }

        return new CommandResult(process.ExitCode, (await outputTask) + Environment.NewLine + (await errorTask));
    }

    private async Task<bool> HasGdiDeviceAsync()
    {
        try
        {
            var result = await RunAndCaptureAsync(new[] { "-hide_banner", "-devices" }, 8000);
            return result.Output.Contains("gdigrab", StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    private static bool HasEncoder(string encoderList, string encoder)
        => encoderList.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Any(line => line.Contains(" " + encoder + " ", StringComparison.OrdinalIgnoreCase) ||
                         line.TrimEnd().EndsWith(" " + encoder, StringComparison.OrdinalIgnoreCase) ||
                         line.Contains(encoder, StringComparison.OrdinalIgnoreCase));

    private static bool IsGoodOutput(int exitCode, string path)
        => exitCode == 0 && File.Exists(path) && new FileInfo(path).Length > 0;

    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string TrimLog(string log)
    {
        const int max = 5000;
        log = log.Trim();
        return log.Length <= max ? log : "..." + log[^max..];
    }

    private sealed record CommandResult(int ExitCode, string Output);
}

internal sealed record FfmpegCapabilities(
    string VersionLine,
    bool HasGfxCapture,
    bool HasGdiGrab,
    bool HasLibX264,
    bool HasLibX265,
    bool HasLibVpxVp9,
    bool HasLibOpus,
    bool HasGif);

internal sealed record CaptureResult(string Backend, string Log, string OutputProfileId);
