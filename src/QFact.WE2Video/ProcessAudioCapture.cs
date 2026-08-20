using System.Diagnostics;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace QFact.WE2Video;

/// <summary>
/// Captures audio rendered by the Wallpaper Engine process tree using WASAPI process loopback.
/// The QPC timestamp supplied by NAudio is used to preserve leading/intermediate silence so
/// captured audio stays aligned with the video timeline.
/// </summary>
internal sealed class ProcessAudioCapture
{
    public async Task<ProcessAudioCaptureResult<T>> CaptureWhileAsync<T>(
        uint processId,
        string wavPath,
        double targetDurationSeconds,
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        if (processId == 0)
        {
            var value = await operation();
            return new ProcessAudioCaptureResult<T>(value, wavPath, false, AppI18n.T("audioPidMissing"));
        }

        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
        {
            var value = await operation();
            return new ProcessAudioCaptureResult<T>(value, wavPath, false,
                AppI18n.T("audioWindowsRequired"));
        }

        Directory.CreateDirectory(Path.GetDirectoryName(wavPath)!);
        if (File.Exists(wavPath)) File.Delete(wavPath);

        WasapiRecorder? recorder = null;
        WaveFileWriter? writer = null;
        long realAudioBytes = 0;
        long writtenBytes = 0;
        long captureStartQpc100ns = 0;
        long nextExpectedQpc100ns = 0;
        var havePacketClock = false;
        var gate = new object();
        var stopped = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            recorder = await new WasapiRecorderBuilder()
                .WithProcessLoopback(processId, ProcessLoopbackMode.IncludeTargetProcessTree)
                .BuildAsync();
            writer = new WaveFileWriter(wavPath, recorder.WaveFormat);
        }
        catch (Exception ex)
        {
            AppLogger.Warn("Process audio loopback initialization unavailable: " + ex.Message);
            try { writer?.Dispose(); } catch { }
            try { if (recorder != null) await recorder.DisposeAsync(); } catch { }
            var value = await operation();
            return new ProcessAudioCaptureResult<T>(value, wavPath, false,
                AppI18n.T("audioCaptureFailed", ex.Message));
        }

        recorder.DataAvailable += (buffer, flags, devicePosition, qpcPosition) =>
        {
            if (buffer.Length == 0) return;
            lock (gate)
            {
                if (writer == null) return;

                // NAudio exposes qpcPosition in 100 ns units. WASAPI process loopback emits no
                // packet while the target is silent, so explicitly materialize those gaps in WAV.
                var packetQpc100ns = Convert.ToInt64(qpcPosition);
                if (!havePacketClock)
                {
                    nextExpectedQpc100ns = captureStartQpc100ns;
                    havePacketClock = true;
                }

                var gap100ns = packetQpc100ns - nextExpectedQpc100ns;
                if (gap100ns > 20_000) // > 2 ms; avoids padding harmless timestamp jitter.
                {
                    var gapBytes = Duration100nsToBytes(gap100ns, recorder.WaveFormat);
                    gapBytes = AlignToBlock(gapBytes, recorder.WaveFormat.BlockAlign);
                    WriteSilence(writer, gapBytes);
                    writtenBytes += gapBytes;
                }

                writer.Write(buffer);
                realAudioBytes += buffer.Length;
                writtenBytes += buffer.Length;

                var frames = buffer.Length / Math.Max(1, recorder.WaveFormat.BlockAlign);
                var packetDuration100ns = (long)Math.Round(frames * 10_000_000d / recorder.WaveFormat.SampleRate);
                nextExpectedQpc100ns = packetQpc100ns + Math.Max(0, packetDuration100ns);
            }
        };
        recorder.RecordingStopped += (_, _) => stopped.TrySetResult(true);

        try
        {
            captureStartQpc100ns = GetQpc100ns();
            nextExpectedQpc100ns = captureStartQpc100ns;
            recorder.StartRecording();

            T operationResult;
            try
            {
                operationResult = await operation();
            }
            finally
            {
                recorder.StopRecording();
                try
                {
                    await stopped.Task.WaitAsync(TimeSpan.FromSeconds(3), cancellationToken);
                }
                catch (TimeoutException)
                {
                    AppLogger.Warn("Timed out waiting for WASAPI process-loopback recorder to stop cleanly.");
                }
            }

            lock (gate)
            {
                // Keep WAV exactly as long as requested. Missing packets at the end are silence.
                var expectedBytes = targetDurationSeconds > 0
                    ? (long)Math.Ceiling(recorder.WaveFormat.AverageBytesPerSecond * targetDurationSeconds)
                    : writtenBytes;
                expectedBytes = AlignToBlock(expectedBytes, recorder.WaveFormat.BlockAlign);
                var tailBytes = Math.Max(0, expectedBytes - writtenBytes);
                WriteSilence(writer, tailBytes);
                writtenBytes += tailBytes;
                writer.Flush();
                writer.Dispose();
                writer = null;
            }

            var hasAudio = realAudioBytes > Math.Max(256, recorder.WaveFormat.AverageBytesPerSecond / 100);
            return new ProcessAudioCaptureResult<T>(operationResult, wavPath, hasAudio,
                hasAudio ? null : AppI18n.T("audioNoPackets"));
        }
        finally
        {
            try { writer?.Dispose(); } catch { }
            try { await recorder.DisposeAsync(); } catch { }
        }
    }

    private static long GetQpc100ns()
        => (long)Math.Round(Stopwatch.GetTimestamp() * 10_000_000d / Stopwatch.Frequency);

    private static long Duration100nsToBytes(long duration100ns, WaveFormat format)
        => duration100ns <= 0 ? 0 : (long)Math.Round(duration100ns / 10_000_000d * format.AverageBytesPerSecond);

    private static long AlignToBlock(long value, int blockAlign)
    {
        var align = Math.Max(1, blockAlign);
        return value - (value % align);
    }

    private static void WriteSilence(WaveFileWriter writer, long byteCount)
    {
        if (byteCount <= 0) return;
        var buffer = new byte[64 * 1024];
        while (byteCount > 0)
        {
            var count = (int)Math.Min(buffer.Length, byteCount);
            writer.Write(buffer.AsSpan(0, count));
            byteCount -= count;
        }
    }
}

internal sealed record ProcessAudioCaptureResult<T>(
    T OperationResult,
    string WavPath,
    bool HasAudio,
    string? Warning);
