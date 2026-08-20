using System.Diagnostics;
using System.Globalization;

namespace QFact.WE2Video;

internal sealed class FfmpegCapture
{
    private const int LoopAnalysisWidth = 96;
    private const int LoopAnalysisHeight = 54;
    private const int LoopAnalysisFps = 6;
    private const int LoopAnalysisWindowFrames = 5;
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

    public static bool ShouldAnalyzeSmartLoop(double targetDurationSeconds)
        => targetDurationSeconds >= 2.0 && targetDurationSeconds <= 300.0;

    public static double GetSmartLoopCaptureDuration(double targetDurationSeconds)
    {
        if (!ShouldAnalyzeSmartLoop(targetDurationSeconds)) return targetDurationSeconds;
        var radius = Math.Clamp(targetDurationSeconds * 0.25, 2.0, 8.0);
        return targetDurationSeconds + radius;
    }

    public static OutputProfile ChooseFiniteIntermediateProfile(FfmpegCapabilities caps)
    {
        if (caps.HasLibX264) return OutputProfiles.Mp4H264;
        if (caps.HasLibX265) return OutputProfiles.Mp4Hevc;
        if (caps.HasLibVpxVp9) return OutputProfiles.WebmVp9;
        throw new InvalidOperationException(
            "GIF export needs a finite intermediate video encoder (libx264, libx265 or libvpx-vp9). " +
            AppI18n.T("updateFfmpeg"));
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
        OutputProfile profile,
        CancellationToken cancellationToken = default)
    {
        if (profile.IsGif)
            throw new InvalidOperationException("Live GIF capture is disabled. Capture to a finite video first.");

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
            cancellationToken.ThrowIfCancellationRequested();
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

            var result = await RunFfmpegAsync(
                args, GetRealtimeCaptureTimeout(durationSeconds), cancellationToken, "FFmpeg capture");
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
        bool includeAudio,
        CancellationToken cancellationToken = default)
    {
        EnsureVideoSupport(profile, caps);
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException(AppI18n.T("videoSourceMissing"), sourcePath);
        if (includeAudio && profile.SupportsAudio && !profile.IsAudioSupported(caps))
            throw new InvalidOperationException(profile.MissingAudioEncoderMessage);

        if (!profile.IsGif)
        {
            if (File.Exists(outputPath)) File.Delete(outputPath);
            var args = BuildLoopedVideoArgs(sourcePath, outputPath, width, height, fps, durationSeconds, profile, includeAudio);
            var result = await RunFfmpegAsync(
                args, GetEncodingTimeout(durationSeconds), cancellationToken, "FFmpeg video transcode");
            if (!IsGoodOutput(result.ExitCode, outputPath))
                throw new InvalidOperationException($"FFmpeg video transcode failed (exit {result.ExitCode}).\n{TrimLog(result.Log)}");
            return new CaptureResult("direct-video", result.Log, profile.Id);
        }

        // A live/infinite GIF palette graph can wait forever on some FFmpeg builds. Make the source finite first.
        var intermediateProfile = ChooseFiniteIntermediateProfile(caps);
        var tempDir = Path.Combine(Path.GetTempPath(), "QFact.WE2Video", "gif-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var intermediate = Path.Combine(tempDir, "finite" + intermediateProfile.Extension);
        try
        {
            var firstPass = BuildLoopedVideoArgs(
                sourcePath, intermediate, width, height, fps, durationSeconds, intermediateProfile, includeAudio: false);
            var firstResult = await RunFfmpegAsync(
                firstPass, GetEncodingTimeout(durationSeconds), cancellationToken, "FFmpeg GIF source preparation");
            if (!IsGoodOutput(firstResult.ExitCode, intermediate))
                throw new InvalidOperationException(
                    $"FFmpeg GIF source preparation failed (exit {firstResult.ExitCode}).\n{TrimLog(firstResult.Log)}");

            var gifResult = await TranscodeFiniteAsync(
                intermediate, outputPath, width, height, fps, durationSeconds, caps, profile,
                includeAudio: false, cancellationToken: cancellationToken);
            return new CaptureResult("direct-video-gif", gifResult.Log, profile.Id);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    public async Task<CaptureResult> TranscodeFiniteAsync(
        string sourcePath,
        string outputPath,
        int width,
        int height,
        int fps,
        double durationSeconds,
        FfmpegCapabilities caps,
        OutputProfile profile,
        bool includeAudio,
        CancellationToken cancellationToken = default)
    {
        EnsureVideoSupport(profile, caps);
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("Finite source video was not found.", sourcePath);
        if (includeAudio && profile.SupportsAudio && !profile.IsAudioSupported(caps))
            throw new InvalidOperationException(profile.MissingAudioEncoderMessage);

        if (File.Exists(outputPath)) File.Delete(outputPath);
        var args = BuildFiniteVideoArgs(sourcePath, outputPath, width, height, fps, durationSeconds, profile, includeAudio);
        var result = await RunFfmpegAsync(
            args, profile.IsGif ? GetGifEncodingTimeout(durationSeconds) : GetEncodingTimeout(durationSeconds), cancellationToken,
            profile.IsGif ? "FFmpeg GIF encoding" : "FFmpeg finite transcode");
        if (!IsGoodOutput(result.ExitCode, outputPath))
            throw new InvalidOperationException(
                $"FFmpeg {(profile.IsGif ? "GIF encoding" : "finite transcode")} failed (exit {result.ExitCode}).\n{TrimLog(result.Log)}");

        return new CaptureResult(profile.IsGif ? "finite-gif" : "finite-video", result.Log, profile.Id);
    }

    public async Task TrimVideoAsync(
        string sourcePath,
        string outputPath,
        double durationSeconds,
        OutputProfile profile,
        CancellationToken cancellationToken = default)
    {
        if (profile.IsGif)
            throw new ArgumentException("GIF cannot be stream-copy trimmed.", nameof(profile));
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("Captured video was not found.", sourcePath);

        if (File.Exists(outputPath)) File.Delete(outputPath);
        var args = new List<string>
        {
            "-y", "-hide_banner", "-loglevel", "warning",
            "-i", sourcePath,
            "-t", F(durationSeconds),
            "-map", "0:v:0",
            "-c:v", "copy",
            "-an"
        };
        if (profile.Id is "mp4-h264" or "mp4-hevc" or "mov-h264")
            args.AddRange(new[] { "-movflags", "+faststart" });
        args.Add(outputPath);

        var result = await RunFfmpegAsync(
            args, GetEncodingTimeout(durationSeconds), cancellationToken, "FFmpeg smart-loop trim");
        if (!IsGoodOutput(result.ExitCode, outputPath))
            throw new InvalidOperationException($"FFmpeg trim failed (exit {result.ExitCode}).\n{TrimLog(result.Log)}");
    }

    public async Task<SmartLoopResult> AnalyzeSmartLoopAsync(
        string sourcePath,
        double targetDurationSeconds,
        double capturedDurationSeconds,
        CancellationToken cancellationToken = default)
    {
        if (!ShouldAnalyzeSmartLoop(targetDurationSeconds) || !File.Exists(sourcePath))
            return SmartLoopResult.NotAnalyzed(targetDurationSeconds);

        var args = new List<string>
        {
            "-hide_banner", "-loglevel", "error",
            "-i", sourcePath,
            "-t", F(capturedDurationSeconds),
            "-vf", $"fps={LoopAnalysisFps},scale={LoopAnalysisWidth}:{LoopAnalysisHeight}:flags=area,format=gray",
            "-an", "-sn", "-dn",
            "-f", "rawvideo", "-pix_fmt", "gray", "pipe:1"
        };

        byte[] raw;
        try
        {
            raw = await RunFfmpegBinaryAsync(
                args, GetAnalysisTimeout(capturedDurationSeconds), cancellationToken, "Smart Loop frame analysis");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            AppLogger.Warn("Smart Loop analysis unavailable: " + ex.Message);
            return SmartLoopResult.NotAnalyzed(targetDurationSeconds);
        }

        var frameSize = LoopAnalysisWidth * LoopAnalysisHeight;
        var frameCount = raw.Length / frameSize;
        if (frameCount < LoopAnalysisWindowFrames * 2)
            return SmartLoopResult.NotAnalyzed(targetDurationSeconds);

        var radiusSeconds = Math.Clamp(targetDurationSeconds * 0.25, 2.0, 8.0);
        var minCandidate = Math.Max(LoopAnalysisWindowFrames, (int)Math.Floor((targetDurationSeconds - radiusSeconds) * LoopAnalysisFps));
        var maxCandidate = Math.Min(
            frameCount - LoopAnalysisWindowFrames,
            (int)Math.Ceiling((targetDurationSeconds + radiusSeconds) * LoopAnalysisFps));
        if (maxCandidate <= minCandidate)
            return SmartLoopResult.NotAnalyzed(targetDurationSeconds);

        var targetIndex = Math.Clamp(
            (int)Math.Round(targetDurationSeconds * LoopAnalysisFps), minCandidate, maxCandidate);
        var exactScore = SequenceDifference(raw, frameSize, 0, targetIndex, LoopAnalysisWindowFrames);

        var bestIndex = targetIndex;
        var bestRawScore = exactScore;
        var bestObjective = exactScore;
        for (var candidate = minCandidate; candidate <= maxCandidate; candidate++)
        {
            var score = SequenceDifference(raw, frameSize, 0, candidate, LoopAnalysisWindowFrames);
            var distancePenalty = Math.Abs(candidate - targetIndex) /
                                  Math.Max(1.0, radiusSeconds * LoopAnalysisFps) * 0.006;
            var objective = score + distancePenalty;
            if (objective < bestObjective)
            {
                bestObjective = objective;
                bestRawScore = score;
                bestIndex = candidate;
            }
        }

        var bestDuration = bestIndex / (double)LoopAnalysisFps;
        var improvement = exactScore - bestRawScore;
        var strongMatch = bestRawScore <= 0.065;
        var meaningfulImprovement = bestRawScore <= 0.10 && improvement >= 0.025;
        var durationChanged = Math.Abs(bestDuration - targetDurationSeconds) >= (0.5 / LoopAnalysisFps);
        var applied = durationChanged && (strongMatch || meaningfulImprovement);

        AppLogger.Info(
            $"Smart Loop: target={targetDurationSeconds:0.###}s, best={bestDuration:0.###}s, " +
            $"score={bestRawScore:0.0000}, exactScore={exactScore:0.0000}, applied={applied}.");

        return new SmartLoopResult(
            Applied: applied,
            DurationSeconds: applied ? bestDuration : targetDurationSeconds,
            MatchScore: bestRawScore,
            ExactTargetScore: exactScore,
            Analyzed: true);
    }

    public async Task<bool> MuxAudioAsync(
        string silentVideoPath,
        string wavPath,
        string outputPath,
        double durationSeconds,
        FfmpegCapabilities caps,
        OutputProfile profile,
        CancellationToken cancellationToken = default)
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

        var result = await RunFfmpegAsync(
            args, GetEncodingTimeout(durationSeconds), cancellationToken, "FFmpeg audio mux");
        if (!IsGoodOutput(result.ExitCode, outputPath))
            throw new InvalidOperationException($"FFmpeg audio mux failed (exit {result.ExitCode}).\n{TrimLog(result.Log)}");
        return true;
    }

    private static IReadOnlyList<string> BuildLoopedVideoArgs(
        string source,
        string output,
        int width,
        int height,
        int fps,
        double duration,
        OutputProfile profile,
        bool includeAudio)
    {
        if (profile.IsGif)
            throw new ArgumentException("Infinite/looped GIF encoding is intentionally disabled.", nameof(profile));

        var resize = ResizeFilter(width, height, fps);
        var args = new List<string>
        {
            "-y", "-hide_banner", "-loglevel", "warning",
            "-stream_loop", "-1", "-i", source,
            "-t", F(duration),
            "-map", "0:v:0", "-vf", resize + ",format=yuv420p"
        };
        if (includeAudio && profile.SupportsAudio)
        {
            args.AddRange(new[] { "-map", "0:a:0?" });
            AddAudioEncoderArgs(args, profile);
        }
        else
        {
            args.Add("-an");
        }
        AddVideoEncoderArgs(args, profile);
        args.Add(output);
        return args;
    }

    private static IReadOnlyList<string> BuildFiniteVideoArgs(
        string source,
        string output,
        int width,
        int height,
        int fps,
        double duration,
        OutputProfile profile,
        bool includeAudio)
    {
        var resize = ResizeFilter(width, height, fps);
        var args = new List<string>
        {
            "-y", "-hide_banner", "-loglevel", "warning",
            "-i", source,
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
        var args = new List<string>
        {
            "-y", "-hide_banner", "-loglevel", "warning",
            "-filter_complex", source + ",format=yuv420p[v]",
            "-map", "[v]",
            "-t", F(duration),
            "-an"
        };
        AddVideoEncoderArgs(args, profile);
        args.Add(output);
        return args;
    }

    private static IReadOnlyList<string> BuildGdiArgs(
        string windowTitle, string output, int width, int height, int fps, double duration, OutputProfile profile)
    {
        var resize = ResizeFilter(width, height, fps);
        var args = new List<string>
        {
            "-y", "-hide_banner", "-loglevel", "warning",
            "-f", "gdigrab", "-draw_mouse", "0",
            "-framerate", fps.ToString(CultureInfo.InvariantCulture),
            "-i", $"title={windowTitle}",
            "-t", F(duration),
            "-vf", resize + ",format=yuv420p",
            "-an"
        };
        AddVideoEncoderArgs(args, profile);
        args.Add(output);
        return args;
    }

    private static string ResizeFilter(int width, int height, int fps)
        => $"scale={width}:{height}:force_original_aspect_ratio=decrease:flags=lanczos," +
           $"pad={width}:{height}:(ow-iw)/2:(oh-ih)/2:black,fps={fps},setsar=1";

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

    private async Task<(int ExitCode, string Log)> RunFfmpegAsync(
        IReadOnlyList<string> args,
        TimeSpan timeout,
        CancellationToken cancellationToken,
        string operationName)
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
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);
        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TryKill(process);
            throw new TimeoutException($"{operationName} timed out after {timeout.TotalSeconds:0} seconds.");
        }
        catch
        {
            TryKill(process);
            throw;
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        return (process.ExitCode, stdout + Environment.NewLine + stderr);
    }

    private async Task<byte[]> RunFfmpegBinaryAsync(
        IReadOnlyList<string> args,
        TimeSpan timeout,
        CancellationToken cancellationToken,
        string operationName)
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
        using var buffer = new MemoryStream();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);
        var copyTask = process.StandardOutput.BaseStream.CopyToAsync(buffer, timeoutCts.Token);
        var errorTask = process.StandardError.ReadToEndAsync(timeoutCts.Token);
        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
            await copyTask;
            var error = await errorTask;
            if (process.ExitCode != 0)
                throw new InvalidOperationException($"{operationName} failed (exit {process.ExitCode}). {TrimLog(error)}");
            return buffer.ToArray();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TryKill(process);
            throw new TimeoutException($"{operationName} timed out after {timeout.TotalSeconds:0} seconds.");
        }
        catch
        {
            TryKill(process);
            throw;
        }
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
            TryKill(process);
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

    private static double SequenceDifference(byte[] data, int frameSize, int firstFrame, int secondFrame, int count)
    {
        long sum = 0;
        var samples = (long)frameSize * count;
        for (var frame = 0; frame < count; frame++)
        {
            var a = (firstFrame + frame) * frameSize;
            var b = (secondFrame + frame) * frameSize;
            for (var i = 0; i < frameSize; i++)
                sum += Math.Abs(data[a + i] - data[b + i]);
        }
        return sum / (samples * 255.0);
    }

    private static bool HasEncoder(string encoderList, string encoder)
        => encoderList.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Any(line => line.Contains(" " + encoder + " ", StringComparison.OrdinalIgnoreCase) ||
                         line.TrimEnd().EndsWith(" " + encoder, StringComparison.OrdinalIgnoreCase) ||
                         line.Contains(encoder, StringComparison.OrdinalIgnoreCase));

    private static bool IsGoodOutput(int exitCode, string path)
        => exitCode == 0 && File.Exists(path) && new FileInfo(path).Length > 0;

    private static TimeSpan GetRealtimeCaptureTimeout(double durationSeconds)
        => TimeSpan.FromSeconds(durationSeconds + Math.Max(25.0, Math.Min(120.0, durationSeconds * 0.5 + 10.0)));

    private static TimeSpan GetEncodingTimeout(double durationSeconds)
        => TimeSpan.FromSeconds(Math.Max(120.0, durationSeconds * 10.0 + 60.0));

    private static TimeSpan GetGifEncodingTimeout(double durationSeconds)
        => TimeSpan.FromSeconds(Math.Max(180.0, durationSeconds * 20.0 + 120.0));

    private static TimeSpan GetAnalysisTimeout(double durationSeconds)
        => TimeSpan.FromSeconds(Math.Max(60.0, durationSeconds * 2.0 + 30.0));

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
        }
        catch { }
    }

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

internal sealed record SmartLoopResult(
    bool Applied,
    double DurationSeconds,
    double MatchScore,
    double ExactTargetScore,
    bool Analyzed)
{
    public static SmartLoopResult NotAnalyzed(double durationSeconds)
        => new(false, durationSeconds, double.NaN, double.NaN, false);
}
