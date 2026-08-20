namespace QFact.WE2Video;

internal sealed class CaptureService
{
    private readonly CleanExportAnalyzer _cleanAnalyzer = new();
    private readonly ProcessAudioCapture _audioCapture = new();

    public CleanExportPlan AnalyzeCleanExport(WallpaperInfo wallpaper) => _cleanAnalyzer.Analyze(wallpaper);

    public async Task<CaptureOutcome> CaptureAsync(
        WallpaperInfo wallpaper,
        string outputPath,
        int width,
        int height,
        int fps,
        double durationSeconds,
        OutputProfile profile,
        bool includeAudio,
        bool backgroundCapture,
        IReadOnlyList<CleanExportOverride>? cleanOverrides,
        string? manualEngineRoot,
        CancellationToken cancellationToken = default)
    {
        var ffmpegPath = DependencyLocator.FindFfmpeg(null)
            ?? throw new FileNotFoundException(AppI18n.T("ffmpegNotFoundRun"));
        var ffmpeg = new FfmpegCapture(ffmpegPath);
        var smartLoopProcessor = new SmartLoopProcessor(ffmpegPath);
        var visualValidator = new CaptureVisualValidator(ffmpegPath);
        var caps = await ffmpeg.ProbeAsync();
        // v1.1: Smart Loop is automatic for wallpaper-style exports >= 2 seconds.
        var effectiveSmartLoop = SmartLoopProcessor.IsEligible(durationSeconds);
        var captureDuration = effectiveSmartLoop
            ? SmartLoopProcessor.GetCaptureDuration(durationSeconds)
            : durationSeconds;

        if (!profile.IsSupported(caps))
            throw new InvalidOperationException(profile.MissingEncoderMessage + AppI18n.T("runDoctor"));
        if (includeAudio && profile.SupportsAudio && !profile.IsAudioSupported(caps))
            throw new InvalidOperationException(profile.MissingAudioEncoderMessage);

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        // Video wallpapers are already media files. Capturing a Wallpaper Engine pop-out adds a render generation,
        // can return black frames on some systems, and throws away the original audio stream. Transcode directly.
        if (string.Equals(wallpaper.DisplayType, "video", StringComparison.OrdinalIgnoreCase))
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "QFact.WE2Video", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var workingOutput = effectiveSmartLoop
                ? Path.Combine(tempDir, "smart-loop" + profile.Extension)
                : outputPath;
            try
            {
                var result = await ffmpeg.TranscodeVideoAsync(
                    wallpaper.LaunchPath, workingOutput, width, height, fps, captureDuration,
                    caps, profile, includeAudio && profile.SupportsAudio);

                var loopResult = SmartLoopResult.Disabled(durationSeconds);
                if (effectiveSmartLoop)
                    loopResult = await smartLoopProcessor.AnalyzeAndTrimAsync(
                        workingOutput, outputPath, durationSeconds, cancellationToken);

                return new CaptureOutcome(
                    outputPath, result.Backend, profile.Id,
                    AudioRequested: includeAudio,
                    AudioIncluded: includeAudio && profile.SupportsAudio,
                    AudioWarning: profile.SupportsAudio ? null : AppI18n.T("gifNoAudio"),
                    BackgroundCapture: true,
                    BackgroundMode: "direct-video",
                    BackgroundWarning: null,
                    SmartLoop: loopResult,
                    CleanReport: WallpaperPropertiesApplyReport.Empty,
                    CleanDetectedCount: 0);
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch { }
            }
        }

        if (!caps.HasGfxCapture && !caps.HasGdiGrab)
            throw new InvalidOperationException(AppI18n.T("ffmpegCaptureMissing"));

        var wePath = DependencyLocator.FindWallpaperEngine(manualEngineRoot)
            ?? throw new FileNotFoundException(AppI18n.T("weNotFound"));
        var we = new WallpaperEngineController(wePath);
        await we.EnsureRunningAsync();
        cancellationToken.ThrowIfCancellationRequested();

        var selectedOverrides = cleanOverrides?.GroupBy(x => x.Key, StringComparer.Ordinal)
            .Select(x => x.First())
            .ToArray() ?? Array.Empty<CleanExportOverride>();
        var windowTitle = $"QFact-WE2Video-Capture-{Guid.NewGuid():N}"[..25];
        var tempDir = Path.Combine(Path.GetTempPath(), "QFact.WE2Video", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var audioWanted = includeAudio && profile.SupportsAudio;
        var tempVideo = (audioWanted || effectiveSmartLoop)
            ? Path.Combine(tempDir, "video" + profile.Extension)
            : outputPath;
        var tempWav = Path.Combine(tempDir, "wallpaper-audio.wav");
        WallpaperPropertiesApplyReport cleanReport = WallpaperPropertiesApplyReport.Empty;
        string? audioWarning = null;
        var audioIncluded = false;
        var backgroundMode = backgroundCapture ? "safe" : "visible";
        string? backgroundWarning = null;
        var requestedBackend = "auto";
        var loopResult = SmartLoopResult.Disabled(durationSeconds);

        try
        {
            var previousForeground = WindowFinder.GetForegroundWindowHandle();
            using var backgroundGuard = backgroundCapture
                ? WindowFinder.CreateBackgroundWindowGuard(windowTitle, width, height, previousForeground)
                : null;

            await we.OpenInWindowAsync(
                wallpaper.LaunchPath, windowTitle, width, height, backgroundCapture, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var hwnd = await WindowFinder.WaitForWindowAsync(
                windowTitle, TimeSpan.FromSeconds(20), activate: !backgroundCapture, cancellationToken);
            if (hwnd == IntPtr.Zero)
                throw new InvalidOperationException(AppI18n.T("weWindowTimeout"));

            if (backgroundCapture)
            {
                WindowFinder.ConfigureBackgroundCaptureWindow(hwnd, width, height, previousForeground);
                await Task.Delay(180, cancellationToken);
                WindowFinder.ConfigureBackgroundCaptureWindow(hwnd, width, height, previousForeground);
            }

            // The event guard is only needed while the pop-out is being created. Disable it before
            // compatibility/visible fallbacks so a later SHOW event cannot move the window back.
            backgroundGuard?.Dispose();

            await Task.Delay(1600, cancellationToken);

            if (selectedOverrides.Length > 0)
            {
                cleanReport = await we.ApplyPropertiesAsync(windowTitle, selectedOverrides, cancellationToken);
                await Task.Delay(450, cancellationToken);
            }

            if (backgroundCapture)
            {
                var probe = await visualValidator.ProbeAsync(hwnd, windowTitle, fps, "auto", caps, cancellationToken);
                if (!probe.Success || probe.IsLikelyBlack)
                {
                    AppLogger.Warn($"Background safe probe was black/invalid ({probe.Backend}: {probe.Details}). Trying compatibility placement.");
                    WindowFinder.ConfigureCompatibilityCaptureWindow(hwnd, width, height, previousForeground);
                    await Task.Delay(700, cancellationToken);
                    probe = await visualValidator.ProbeAsync(hwnd, windowTitle, fps, "auto", caps, cancellationToken);
                    backgroundMode = "compatibility";
                }

                if (!probe.Success || probe.IsLikelyBlack)
                {
                    AppLogger.Warn($"Background compatibility probe was black/invalid ({probe.Backend}: {probe.Details}). Falling back to visible capture.");
                    WindowFinder.ConfigureVisibleCaptureFallback(hwnd, width, height);
                    await Task.Delay(700, cancellationToken);
                    probe = await visualValidator.ProbeAsync(hwnd, windowTitle, fps, "auto", caps, cancellationToken);
                    backgroundMode = "visible-fallback";
                    backgroundWarning = "Background capture was not renderable on this PC, so QFact.WE2Video switched to visible capture instead of exporting a black video.";
                }

                if (probe.Success && !probe.IsLikelyBlack)
                    requestedBackend = probe.Backend;
                else
                    AppLogger.Warn($"Visual validation is still inconclusive ({probe.Backend}: {probe.Details}); FFmpeg auto fallback will be used.");
            }

            async Task<CaptureResult> CaptureVideoAsync()
                => await ffmpeg.CaptureAsync(
                    hwnd, windowTitle, tempVideo, width, height, fps, captureDuration,
                    requestedBackend, caps, profile);

            CaptureResult captureResult;
            if (audioWanted)
            {
                var pid = WindowFinder.GetProcessId(hwnd);
                var audioResult = await _audioCapture.CaptureWhileAsync(
                    pid, tempWav, captureDuration, CaptureVideoAsync, cancellationToken);
                captureResult = audioResult.OperationResult;
                audioWarning = audioResult.Warning;

                var muxedOutput = effectiveSmartLoop
                    ? Path.Combine(tempDir, "muxed" + profile.Extension)
                    : outputPath;
                if (audioResult.HasAudio && File.Exists(tempWav))
                {
                    try
                    {
                        audioIncluded = await ffmpeg.MuxAudioAsync(
                            tempVideo, tempWav, muxedOutput, captureDuration, caps, profile);
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Warn("Audio mux failed; preserving video-only export. " + ex.Message);
                        audioWarning = AppI18n.T("audioMuxFailed", ex.Message);
                    }
                }

                if (!audioIncluded)
                {
                    if (File.Exists(muxedOutput)) File.Delete(muxedOutput);
                    File.Move(tempVideo, muxedOutput, overwrite: true);
                }

                if (effectiveSmartLoop)
                    loopResult = await smartLoopProcessor.AnalyzeAndTrimAsync(
                        muxedOutput, outputPath, durationSeconds, cancellationToken);
            }
            else
            {
                captureResult = await CaptureVideoAsync();
                if (includeAudio && !profile.SupportsAudio)
                    audioWarning = AppI18n.T("gifNoAudio");

                if (effectiveSmartLoop)
                    loopResult = await smartLoopProcessor.AnalyzeAndTrimAsync(
                        tempVideo, outputPath, durationSeconds, cancellationToken);
            }

            return new CaptureOutcome(
                outputPath, captureResult.Backend, profile.Id,
                AudioRequested: includeAudio,
                AudioIncluded: audioIncluded,
                AudioWarning: audioWarning,
                BackgroundCapture: backgroundMode != "visible-fallback" && backgroundCapture,
                BackgroundMode: backgroundMode,
                BackgroundWarning: backgroundWarning,
                SmartLoop: loopResult,
                CleanReport: cleanReport,
                CleanDetectedCount: selectedOverrides.Length);
        }
        finally
        {
            await we.CloseWindowAsync(windowTitle);
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }
}

internal sealed record CaptureOutcome(
    string OutputPath,
    string Backend,
    string OutputProfileId,
    bool AudioRequested,
    bool AudioIncluded,
    string? AudioWarning,
    bool BackgroundCapture,
    string BackgroundMode,
    string? BackgroundWarning,
    SmartLoopResult SmartLoop,
    WallpaperPropertiesApplyReport CleanReport,
    int CleanDetectedCount);
