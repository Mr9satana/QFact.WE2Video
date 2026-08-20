using System.Diagnostics;
using System.Globalization;

namespace QFact.WE2Video;

internal sealed class SmartLoopProcessor
{
    private const int AnalysisWidth = 64;
    private const int AnalysisHeight = 36;
    private const int AnalysisFps = 30;
    private const double ReferenceSeconds = 0.5;
    private const double MinimumAcceptedSimilarity = 0.935;

    private readonly string _ffmpeg;

    public SmartLoopProcessor(string ffmpeg) => _ffmpeg = ffmpeg;

    public static bool IsEligible(double requestedDurationSeconds)
        => requestedDurationSeconds >= 2.0;

    public static double GetSearchRadius(double requestedDurationSeconds)
        => Math.Clamp(requestedDurationSeconds * 0.35, 2.0, 8.0);

    public static double GetCaptureDuration(double requestedDurationSeconds)
    {
        if (!IsEligible(requestedDurationSeconds)) return requestedDurationSeconds;
        return requestedDurationSeconds + GetSearchRadius(requestedDurationSeconds) + ReferenceSeconds + 0.25;
    }

    public async Task<SmartLoopResult> AnalyzeAndTrimAsync(
        string inputPath,
        string outputPath,
        double requestedDurationSeconds,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(inputPath))
            throw new FileNotFoundException("Smart Loop input is missing.", inputPath);

        var targetDuration = requestedDurationSeconds;
        var bestSimilarity = 0d;
        var applied = false;
        string? note = null;

        if (IsEligible(requestedDurationSeconds))
        {
            try
            {
                var radius = GetSearchRadius(requestedDurationSeconds);
                var searchStart = Math.Max(0.75, requestedDurationSeconds - radius);
                var searchEnd = requestedDurationSeconds + radius + ReferenceSeconds;

                var reference = await ExtractFramesAsync(
                    inputPath, 0, ReferenceSeconds, cancellationToken);
                var search = await ExtractFramesAsync(
                    inputPath, searchStart, Math.Max(ReferenceSeconds, searchEnd - searchStart), cancellationToken);

                var windowFrames = Math.Min(reference.Count, Math.Max(4, (int)Math.Round(ReferenceSeconds * AnalysisFps)));
                if (windowFrames >= 4 && search.Count >= windowFrames)
                {
                    var bestTime = requestedDurationSeconds;
                    var bestDistance = double.MaxValue;

                    for (var i = 0; i <= search.Count - windowFrames; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var candidateTime = searchStart + i / (double)AnalysisFps;
                        if (candidateTime < 1.0) continue;

                        var similarity = WindowSimilarity(reference, search, i, windowFrames);
                        var distance = Math.Abs(candidateTime - requestedDurationSeconds);
                        if (similarity > bestSimilarity + 0.001 ||
                            (Math.Abs(similarity - bestSimilarity) <= 0.001 && distance < bestDistance))
                        {
                            bestSimilarity = similarity;
                            bestTime = candidateTime;
                            bestDistance = distance;
                        }
                    }

                    if (bestSimilarity >= MinimumAcceptedSimilarity)
                    {
                        targetDuration = Math.Max(0.5, Math.Round(bestTime * AnalysisFps) / AnalysisFps);
                        applied = true;
                        note = $"Natural loop match {bestSimilarity:P1} at {targetDuration:0.###}s.";
                    }
                    else
                    {
                        note = $"No strong natural loop match ({bestSimilarity:P1}); kept requested duration.";
                    }
                }
                else
                {
                    note = "Not enough decoded frames for Smart Loop analysis.";
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                AppLogger.Warn("Smart Loop analysis failed; using requested duration. " + ex.Message);
                note = "Smart Loop analysis failed; kept requested duration.";
            }
        }
        else
        {
            note = "Smart Loop is skipped for clips shorter than 2 seconds.";
        }

        await TrimAsync(inputPath, outputPath, targetDuration, cancellationToken);
        return new SmartLoopResult(applied, requestedDurationSeconds, targetDuration, bestSimilarity, note);
    }

    private async Task<List<byte[]>> ExtractFramesAsync(
        string inputPath,
        double startSeconds,
        double durationSeconds,
        CancellationToken cancellationToken)
    {
        var args = new[]
        {
            "-hide_banner", "-loglevel", "error",
            "-ss", F(startSeconds),
            "-i", inputPath,
            "-t", F(durationSeconds),
            "-an",
            "-vf", $"fps={AnalysisFps},scale={AnalysisWidth}:{AnalysisHeight}:flags=fast_bilinear,format=gray",
            "-f", "rawvideo", "-pix_fmt", "gray", "pipe:1"
        };

        var result = await RunBinaryAsync(args, cancellationToken);
        if (result.ExitCode != 0)
            throw new InvalidOperationException("FFmpeg frame analysis failed: " + Compact(result.Error));

        var frameSize = AnalysisWidth * AnalysisHeight;
        var frameCount = result.Data.Length / frameSize;
        var frames = new List<byte[]>(frameCount);
        for (var i = 0; i < frameCount; i++)
        {
            var frame = new byte[frameSize];
            Buffer.BlockCopy(result.Data, i * frameSize, frame, 0, frameSize);
            frames.Add(frame);
        }
        return frames;
    }

    private static double WindowSimilarity(
        IReadOnlyList<byte[]> reference,
        IReadOnlyList<byte[]> search,
        int searchOffset,
        int windowFrames)
    {
        double spatial = 0;
        double motion = 0;
        var motionPairs = 0;

        for (var frame = 0; frame < windowFrames; frame++)
        {
            spatial += FrameSimilarity(reference[frame], search[searchOffset + frame]);
            if (frame == 0) continue;

            motion += MotionSimilarity(
                reference[frame - 1], reference[frame],
                search[searchOffset + frame - 1], search[searchOffset + frame]);
            motionPairs++;
        }

        spatial /= windowFrames;
        if (motionPairs == 0) return spatial;
        motion /= motionPairs;

        // Motion similarity prevents a mostly-static background from hiding a badly phased animated detail.
        return spatial * 0.68 + motion * 0.32;
    }

    private static double FrameSimilarity(byte[] a, byte[] b)
    {
        long diff = 0;
        for (var i = 0; i < a.Length; i++)
            diff += Math.Abs(a[i] - b[i]);
        return 1.0 - diff / (a.Length * 255.0);
    }

    private static double MotionSimilarity(byte[] a0, byte[] a1, byte[] b0, byte[] b1)
    {
        long diff = 0;
        for (var i = 0; i < a0.Length; i++)
        {
            var ma = a1[i] - a0[i];
            var mb = b1[i] - b0[i];
            diff += Math.Abs(ma - mb);
        }
        return 1.0 - diff / (a0.Length * 510.0);
    }

    private async Task TrimAsync(
        string inputPath,
        string outputPath,
        double durationSeconds,
        CancellationToken cancellationToken)
    {
        if (File.Exists(outputPath)) File.Delete(outputPath);

        var args = new List<string>
        {
            "-y", "-hide_banner", "-loglevel", "warning",
            "-i", inputPath,
            "-t", F(durationSeconds),
            "-map", "0",
            "-c", "copy"
        };
        if (Path.GetExtension(outputPath).Equals(".mp4", StringComparison.OrdinalIgnoreCase) ||
            Path.GetExtension(outputPath).Equals(".mov", StringComparison.OrdinalIgnoreCase))
        {
            args.AddRange(new[] { "-movflags", "+faststart" });
        }
        args.Add(outputPath);

        var result = await RunTextAsync(args, cancellationToken);
        if (result.ExitCode != 0 || !File.Exists(outputPath) || new FileInfo(outputPath).Length == 0)
            throw new InvalidOperationException("Smart Loop trim failed: " + Compact(result.Error));
    }

    private async Task<BinaryResult> RunBinaryAsync(
        IEnumerable<string> args,
        CancellationToken cancellationToken)
    {
        var psi = CreateProcess(args);
        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Could not start ffmpeg.exe.");
        using var memory = new MemoryStream();
        var stdoutTask = process.StandardOutput.BaseStream.CopyToAsync(memory, cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        await stdoutTask;
        var stderr = await stderrTask;
        return new BinaryResult(process.ExitCode, memory.ToArray(), stderr);
    }

    private async Task<TextResult> RunTextAsync(
        IEnumerable<string> args,
        CancellationToken cancellationToken)
    {
        var psi = CreateProcess(args);
        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Could not start ffmpeg.exe.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        _ = await stdoutTask;
        return new TextResult(process.ExitCode, await stderrTask);
    }

    private ProcessStartInfo CreateProcess(IEnumerable<string> args)
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
        return psi;
    }

    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string Compact(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        text = text.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return text.Length <= 700 ? text : text[^700..];
    }

    private sealed record BinaryResult(int ExitCode, byte[] Data, string Error);
    private sealed record TextResult(int ExitCode, string Error);
}

internal sealed record SmartLoopResult(
    bool Applied,
    double RequestedDurationSeconds,
    double OutputDurationSeconds,
    double Similarity,
    string? Note)
{
    public static SmartLoopResult Disabled(double durationSeconds)
        => new(false, durationSeconds, durationSeconds, 0, null);
}
