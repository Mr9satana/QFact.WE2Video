using System.Diagnostics;
using System.Globalization;

namespace QFact.WE2Video;

internal sealed class CaptureVisualValidator
{
    private readonly string _ffmpeg;

    public CaptureVisualValidator(string ffmpeg) => _ffmpeg = ffmpeg;

    public async Task<CaptureVisualProbe> ProbeAsync(
        IntPtr hwnd,
        string windowTitle,
        int fps,
        string requestedBackend,
        FfmpegCapabilities caps,
        CancellationToken cancellationToken = default)
    {
        var backends = requestedBackend switch
        {
            "gfx" => new[] { "gfx" },
            "gdi" => new[] { "gdi" },
            _ => caps.HasGfxCapture ? new[] { "gfx", "gdi" } : new[] { "gdi" }
        };

        CaptureVisualProbe? last = null;
        foreach (var backend in backends)
        {
            if (backend == "gfx" && !caps.HasGfxCapture) continue;
            if (backend == "gdi" && !caps.HasGdiGrab) continue;

            try
            {
                var current = await ProbeBackendAsync(hwnd, windowTitle, fps, backend, cancellationToken);
                last = current;
                if (current.Success && !current.IsLikelyBlack) return current;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                AppLogger.Warn($"Visual probe failed for {backend}: {ex.Message}");
                last = new CaptureVisualProbe(backend, false, true, 1, 0, ex.Message);
            }
        }

        return last ?? new CaptureVisualProbe(requestedBackend, false, true, 1, 0, "No capture backend available for probe.");
    }

    private async Task<CaptureVisualProbe> ProbeBackendAsync(
        IntPtr hwnd,
        string windowTitle,
        int fps,
        string backend,
        CancellationToken cancellationToken)
    {
        const int probeWidth = 160;
        const int probeHeight = 90;
        const double probeSeconds = 0.8;
        var probeFps = Math.Clamp(fps, 6, 12);
        var args = new List<string> { "-hide_banner", "-loglevel", "error" };

        if (backend == "gfx")
        {
            var source =
                $"gfxcapture=hwnd={hwnd.ToInt64()}:max_framerate={probeFps}:capture_cursor=0:capture_border=0," +
                $"hwdownload,format=bgra,scale={probeWidth}:{probeHeight}:flags=fast_bilinear,format=gray,fps={probeFps}[v]";
            args.AddRange(new[] { "-filter_complex", source, "-map", "[v]" });
        }
        else
        {
            args.AddRange(new[]
            {
                "-f", "gdigrab", "-draw_mouse", "0",
                "-framerate", probeFps.ToString(CultureInfo.InvariantCulture),
                "-i", $"title={windowTitle}",
                "-vf", $"scale={probeWidth}:{probeHeight}:flags=fast_bilinear,format=gray"
            });
        }

        args.AddRange(new[]
        {
            "-t", F(probeSeconds), "-an",
            "-f", "rawvideo", "-pix_fmt", "gray", "pipe:1"
        });

        var result = await RunBinaryAsync(args, cancellationToken);
        var frameSize = probeWidth * probeHeight;
        var frames = result.Data.Length / frameSize;
        if (result.ExitCode != 0 || frames == 0)
            return new CaptureVisualProbe(backend, false, true, 1, frames, Compact(result.Error));

        var blackFrames = 0;
        double meanSum = 0;
        for (var frame = 0; frame < frames; frame++)
        {
            long sum = 0;
            var nearZero = 0;
            var offset = frame * frameSize;
            for (var i = 0; i < frameSize; i++)
            {
                var value = result.Data[offset + i];
                sum += value;
                if (value <= 7) nearZero++;
            }

            var mean = sum / (double)frameSize;
            meanSum += mean;
            if (mean <= 2.5 && nearZero >= frameSize * 0.997) blackFrames++;
        }

        var blackRatio = blackFrames / (double)frames;
        var averageLuma = meanSum / frames;
        return new CaptureVisualProbe(
            backend,
            true,
            blackRatio >= 0.8,
            blackRatio,
            frames,
            $"avgY={averageLuma:0.0}; blackFrames={blackFrames}/{frames}");
    }

    private async Task<BinaryResult> RunBinaryAsync(
        IReadOnlyList<string> args,
        CancellationToken cancellationToken)
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
        using var memory = new MemoryStream();
        var stdoutTask = process.StandardOutput.BaseStream.CopyToAsync(memory, cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        await stdoutTask;
        var stderr = await stderrTask;
        return new BinaryResult(process.ExitCode, memory.ToArray(), stderr);
    }

    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string Compact(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        text = text.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return text.Length <= 900 ? text : text[^900..];
    }

    private sealed record BinaryResult(int ExitCode, byte[] Data, string Error);
}

internal sealed record CaptureVisualProbe(
    string Backend,
    bool Success,
    bool IsLikelyBlack,
    double BlackFrameRatio,
    int FrameCount,
    string? Details);
