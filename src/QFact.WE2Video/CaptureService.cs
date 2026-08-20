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
        var caps = await ffmpeg.ProbeAsync();

        if (!profile.IsSupported(caps))
            throw new InvalidOperationException(profile.MissingEncoderMessage + AppI18n.T("runDoctor"));
        if (includeAudio && profile.SupportsAudio && !profile.IsAudioSupported(caps))
            throw new InvalidOperationException(profile.MissingAudioEncoderMessage);

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        // Video wallpapers are real media files. Keep the direct path; GIF is internally converted through
        // a finite temporary video so palette generation can never wait on an infinite stream.
        if (string.Equals(wallpaper.DisplayType, "video", StringComparison.OrdinalIgnoreCase))
        {
            var result = await ffmpeg.TranscodeVideoAsync(
                wallpaper.LaunchPath, outputPath, width, height, fps, durationSeconds,
                caps, profile, includeAudio && profile.SupportsAudio, cancellationToken);

            return new CaptureOutcome(
                outputPath, result.Backend, profile.Id,
                AudioRequested: includeAudio,
                AudioIncluded: includeAudio && profile.SupportsAudio,
                AudioWarning: profile.SupportsAudio ? null : AppI18n.T("gifNoAudio"),
                BackgroundCapture: true,
                CleanReport: WallpaperPropertiesApplyReport.Empty,
                CleanDetectedCount: 0);
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
        var smartLoopEligible = FfmpegCapture.ShouldAnalyzeSmartLoop(durationSeconds);
        var captureDuration = smartLoopEligible
            ? FfmpegCapture.GetSmartLoopCaptureDuration(durationSeconds)
            : durationSeconds;
        var captureProfile = profile.IsGif
            ? FfmpegCapture.ChooseFiniteIntermediateProfile(caps)
            : profile;
        var capturedVideo = Path.Combine(tempDir, "capture" + captureProfile.Extension);
        var finalSilentVideo = Path.Combine(tempDir, "final" + profile.Extension);
        var tempWav = Path.Combine(tempDir, "wallpaper-audio.wav");
        WallpaperPropertiesApplyReport cleanReport = WallpaperPropertiesApplyReport.Empty;
        string? audioWarning = null;
        var audioIncluded = false;
        var windowOpened = false;
        var windowClosed = false;

        try
        {
            var previousForeground = WindowFinder.GetForegroundWindowHandle();
            using var backgroundGuard = backgroundCapture
                ? WindowFinder.CreateBackgroundWindowGuard(windowTitle, width, height, previousForeground)
                : null;

            await we.OpenInWindowAsync(
                wallpaper.LaunchPath, windowTitle, width, height, backgroundCapture, cancellationToken);
            windowOpened = true;
            cancellationToken.ThrowIfCancellationRequested();

            var hwnd = await WindowFinder.WaitForWindowAsync(
                windowTitle, TimeSpan.FromSeconds(20), activate: !backgroundCapture, cancellationToken: cancellationToken);
            if (hwnd == IntPtr.Zero)
                throw new InvalidOperationException(AppI18n.T("weWindowTimeout"));

            if (backgroundCapture)
            {
                WindowFinder.ConfigureBackgroundCaptureWindow(hwnd, width, height, previousForeground);
                await Task.Delay(180, cancellationToken);
                WindowFinder.ConfigureBackgroundCaptureWindow(hwnd, width, height, previousForeground);
            }

            // Let Scene/Web assets settle before properties or the first Smart Loop reference frames are captured.
            await Task.Delay(1400, cancellationToken);

            if (selectedOverrides.Length > 0)
            {
                cleanReport = await we.ApplyPropertiesAsync(windowTitle, selectedOverrides, cancellationToken);
                await Task.Delay(450, cancellationToken);
            }

            async Task<CaptureResult> CaptureVideoAsync()
                => await ffmpeg.CaptureAsync(
                    hwnd, windowTitle, capturedVideo, width, height, fps, captureDuration,
                    "auto", caps, captureProfile, cancellationToken);

            CaptureResult captureResult;
            if (audioWanted)
            {
                var pid = WindowFinder.GetProcessId(hwnd);
                var audioResult = await _audioCapture.CaptureWhileAsync(
                    pid, tempWav, captureDuration, CaptureVideoAsync, cancellationToken);
                captureResult = audioResult.OperationResult;
                audioWarning = audioResult.Warning;
            }
            else
            {
                captureResult = await CaptureVideoAsync();
                if (includeAudio && !profile.SupportsAudio)
                    audioWarning = AppI18n.T("gifNoAudio");
            }

            // Important: close the Wallpaper Engine pop-out immediately after the finite capture. GIF palette
            // generation, Smart Loop analysis and muxing all run from local files and must not keep the window alive.
            await we.CloseWindowAsync(windowTitle);
            windowClosed = true;

            var smartLoop = smartLoopEligible
                ? await ffmpeg.AnalyzeSmartLoopAsync(
                    capturedVideo, durationSeconds, captureDuration, cancellationToken)
                : SmartLoopResult.NotAnalyzed(durationSeconds);
            var finalDuration = smartLoop.DurationSeconds;

            if (profile.IsGif)
            {
                await ffmpeg.TranscodeFiniteAsync(
                    capturedVideo, outputPath, width, height, fps, finalDuration,
                    caps, profile, includeAudio: false, cancellationToken: cancellationToken);
            }
            else
            {
                string silentVideo;
                if (captureDuration > finalDuration + 0.01)
                {
                    await ffmpeg.TrimVideoAsync(
                        capturedVideo, finalSilentVideo, finalDuration, profile, cancellationToken);
                    silentVideo = finalSilentVideo;
                }
                else
                {
                    silentVideo = capturedVideo;
                }

                if (audioWanted && File.Exists(tempWav))
                {
                    try
                    {
                        audioIncluded = await ffmpeg.MuxAudioAsync(
                            silentVideo, tempWav, outputPath, finalDuration, caps, profile, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Warn("Audio mux failed; preserving video-only export. " + ex.Message);
                        audioWarning = AppI18n.T("audioMuxFailed", ex.Message);
                    }
                }

                if (!audioIncluded)
                {
                    if (File.Exists(outputPath)) File.Delete(outputPath);
                    File.Copy(silentVideo, outputPath, overwrite: true);
                }
            }

            var backend = smartLoop.Applied ? captureResult.Backend + "+smart-loop" : captureResult.Backend;
            return new CaptureOutcome(
                outputPath, backend, profile.Id,
                AudioRequested: includeAudio,
                AudioIncluded: audioIncluded,
                AudioWarning: audioWarning,
                BackgroundCapture: backgroundCapture,
                CleanReport: cleanReport,
                CleanDetectedCount: selectedOverrides.Length);
        }
        finally
        {
            if (windowOpened && !windowClosed)
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
    WallpaperPropertiesApplyReport CleanReport,
    int CleanDetectedCount);
