using System.Diagnostics;
using System.Text.Json;

namespace QFact.WE2Video;

internal sealed class WallpaperEngineController
{
    private readonly string _exePath;

    public WallpaperEngineController(string exePath) => _exePath = exePath;

    public async Task EnsureRunningAsync()
    {
        if (IsRunning()) return;

        AppLogger.Info("Wallpaper Engine is not running. Starting it.");
        Process.Start(new ProcessStartInfo
        {
            FileName = _exePath,
            WorkingDirectory = Path.GetDirectoryName(_exePath)!,
            UseShellExecute = true
        });

        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            if (IsRunning())
            {
                await Task.Delay(1500);
                return;
            }
            await Task.Delay(300);
        }

        throw new InvalidOperationException(
            AppI18n.T("engineStartTimeout"));
    }

    public async Task OpenInWindowAsync(
        string wallpaperPath,
        string windowTitle,
        int width,
        int height,
        bool backgroundCapture = false,
        CancellationToken cancellationToken = default)
    {
        var psi = CreateControlProcess(redirectOutput: true);
        psi.ArgumentList.Add("-control");
        psi.ArgumentList.Add("openWallpaper");
        psi.ArgumentList.Add("-file");
        psi.ArgumentList.Add(wallpaperPath);
        psi.ArgumentList.Add("-playInWindow");
        psi.ArgumentList.Add(windowTitle);
        psi.ArgumentList.Add("-width");
        psi.ArgumentList.Add(width.ToString());
        psi.ArgumentList.Add("-height");
        psi.ArgumentList.Add(height.ToString());
        psi.ArgumentList.Add("-x");
        psi.ArgumentList.Add(backgroundCapture ? "-32000" : "80");
        psi.ArgumentList.Add("-y");
        psi.ArgumentList.Add(backgroundCapture ? "-32000" : "80");
        psi.ArgumentList.Add("-borderless");
        // Wallpaper Engine only brings a pop-out to foreground when -activate is requested.
        // Never pass it for background capture.
        if (!backgroundCapture) psi.ArgumentList.Add("-activate");

        var result = await RunControlAsync(psi, cancellationToken);
        if (result.ExitCode != 0)
            throw new InvalidOperationException(
                $"Wallpaper Engine control process returned exit code {result.ExitCode}. {Compact(result.Output)}");
    }

    /// <summary>
    /// Applies Wallpaper Engine user properties using the CLI's RAW~({...})~END syntax.
    /// IMPORTANT: this deliberately uses ProcessStartInfo.Arguments instead of ArgumentList for the RAW JSON payload.
    /// Wallpaper Engine's RAW wrapper is a command-line escaping mechanism; letting .NET quote the JSON as an
    /// individual ArgumentList entry can alter the literal payload before Wallpaper Engine parses it.
    /// </summary>
    public async Task<WallpaperPropertiesApplyReport> ApplyPropertiesAsync(
        string windowTitle,
        IReadOnlyList<CleanExportOverride> overrides,
        CancellationToken cancellationToken = default)
    {
        if (overrides.Count == 0)
            return WallpaperPropertiesApplyReport.Empty;

        var batch = overrides.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
        var batchResult = await RunRawApplyPropertiesAsync(windowTitle, batch, cancellationToken);
        if (batchResult.ExitCode == 0)
        {
            AppLogger.Info($"Clean Export batch applied ({overrides.Count} properties). JSON={JsonSerializer.Serialize(batch)}");
            return new WallpaperPropertiesApplyReport(
                overrides.Select(x => new WallpaperPropertyApplyResult(
                    x.Key, x.Label, x.Value, true, 0, batchResult.Output, 1)).ToArray(),
                BatchSucceeded: true);
        }

        AppLogger.Warn(
            $"Clean Export batch failed (exit={batchResult.ExitCode}). Falling back to one property per command. " +
            $"Output={Compact(batchResult.Output)}");

        var results = new List<WallpaperPropertyApplyResult>(overrides.Count);
        foreach (var item in overrides)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WallpaperPropertyApplyResult? final = null;

            for (var attempt = 1; attempt <= 2; attempt++)
            {
                var single = new Dictionary<string, object?>(StringComparer.Ordinal) { [item.Key] = item.Value };
                var result = await RunRawApplyPropertiesAsync(windowTitle, single, cancellationToken);
                final = new WallpaperPropertyApplyResult(
                    item.Key, item.Label, item.Value, result.ExitCode == 0, result.ExitCode, result.Output, attempt);

                if (final.Success)
                {
                    AppLogger.Info($"Clean Export applied: key='{item.Key}', value={JsonSerializer.Serialize(item.Value)}, attempt={attempt}.");
                    break;
                }

                AppLogger.Warn(
                    $"Clean Export failed: key='{item.Key}', value={JsonSerializer.Serialize(item.Value)}, " +
                    $"exit={result.ExitCode}, attempt={attempt}, output={Compact(result.Output)}");
                if (attempt < 2) await Task.Delay(350, cancellationToken);
            }

            if (final != null) results.Add(final);
            await Task.Delay(90, cancellationToken);
        }

        return new WallpaperPropertiesApplyReport(results, BatchSucceeded: false);
    }

    private async Task<ControlProcessResult> RunRawApplyPropertiesAsync(
        string windowTitle,
        IReadOnlyDictionary<string, object?> properties,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(properties);
        var psi = CreateControlProcess(redirectOutput: true);

        // Do not replace this with ArgumentList for -properties. RAW~()~END must reach Wallpaper Engine literally.
        psi.Arguments = $"-control applyProperties -properties RAW~({json})~END -location {QuoteWindowsArgument(windowTitle)}";
        return await RunControlAsync(psi, cancellationToken);
    }

    public async Task CloseWindowAsync(string windowTitle)
    {
        try
        {
            var psi = CreateControlProcess(redirectOutput: true);
            psi.ArgumentList.Add("-control");
            psi.ArgumentList.Add("closeWallpaper");
            psi.ArgumentList.Add("-location");
            psi.ArgumentList.Add(windowTitle);
            var result = await RunControlAsync(psi);
            if (result.ExitCode != 0)
                AppLogger.Warn($"Could not close pop-out '{windowTitle}', exit={result.ExitCode}: {Compact(result.Output)}");
        }
        catch (Exception ex)
        {
            AppLogger.Warn($"Could not close pop-out automatically: {ex.Message}");
        }
    }

    private ProcessStartInfo CreateControlProcess(bool redirectOutput) => new()
    {
        FileName = _exePath,
        WorkingDirectory = Path.GetDirectoryName(_exePath)!,
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = redirectOutput,
        RedirectStandardError = redirectOutput
    };

    private static async Task<ControlProcessResult> RunControlAsync(
        ProcessStartInfo psi,
        CancellationToken cancellationToken = default)
    {
        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start Wallpaper Engine control process.");
        Task<string>? stdout = psi.RedirectStandardOutput ? process.StandardOutput.ReadToEndAsync() : null;
        Task<string>? stderr = psi.RedirectStandardError ? process.StandardError.ReadToEndAsync() : null;
        await process.WaitForExitAsync(cancellationToken);
        var output = string.Join(Environment.NewLine, new[]
        {
            stdout == null ? string.Empty : await stdout,
            stderr == null ? string.Empty : await stderr
        }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
        return new ControlProcessResult(process.ExitCode, output);
    }

    private static string QuoteWindowsArgument(string value)
        => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    private static string Compact(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var text = value.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return text.Length <= 500 ? text : text[..500] + "…";
    }

    private static bool IsRunning()
    {
        foreach (var name in new[] { "wallpaper64", "wallpaper32" })
        {
            Process[] processes = Array.Empty<Process>();
            try
            {
                processes = Process.GetProcessesByName(name);
                if (processes.Length > 0) return true;
            }
            finally
            {
                foreach (var p in processes) p.Dispose();
            }
        }
        return false;
    }

    private sealed record ControlProcessResult(int ExitCode, string Output);
}

internal sealed record WallpaperPropertyApplyResult(
    string Key,
    string Label,
    object? Value,
    bool Success,
    int ExitCode,
    string Output,
    int Attempts);

internal sealed record WallpaperPropertiesApplyReport(
    IReadOnlyList<WallpaperPropertyApplyResult> Results,
    bool BatchSucceeded)
{
    public static readonly WallpaperPropertiesApplyReport Empty = new(Array.Empty<WallpaperPropertyApplyResult>(), false);
    public int SuccessCount => Results.Count(x => x.Success);
    public int FailureCount => Results.Count(x => !x.Success);
}
