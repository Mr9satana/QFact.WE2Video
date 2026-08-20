using Microsoft.Web.WebView2.Core;
using System.Runtime.InteropServices;

namespace QFact.WE2Video;

internal static class CliRunner
{
    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            var options = CliOptions.Parse(args);
            if (options.Help) { PrintHelp(); return 0; }
            if (!string.IsNullOrWhiteSpace(options.WorkshopMetadataId))
                return await PrintWorkshopMetadataAsync(options.WorkshopMetadataId);

            var wePath = DependencyLocator.FindWallpaperEngine(options.WallpaperEnginePath);
            var ffmpegPath = DependencyLocator.FindFfmpeg(options.FfmpegPath);
            if (options.Doctor) return await RunDoctorAsync(wePath, ffmpegPath);
            if (options.ListLibrary) return await ListLibraryAsync();

            if (wePath == null) throw new FileNotFoundException("Wallpaper Engine was not found. Start it through Steam or pass --we.");
            if (ffmpegPath == null) throw new FileNotFoundException("FFmpeg was not found. Run install_prereqs.bat.");

            var profile = OutputProfiles.Resolve(options.OutputFormat);
            var wallpaperPath = ResolveWallpaperPath(options.WallpaperPath);
            var outputPath = ResolveOutputPath(options.OutputPath, profile);
            var ffmpeg = new FfmpegCapture(ffmpegPath);
            var caps = await ffmpeg.ProbeAsync();
            if (!profile.IsSupported(caps) || (!caps.HasGfxCapture && !caps.HasGdiGrab))
                throw new InvalidOperationException("FFmpeg capture/output dependencies are incomplete. Run --doctor.");

            var we = new WallpaperEngineController(wePath);
            await we.EnsureRunningAsync();
            var windowTitle = $"QFact-WE2Video-Capture-{Guid.NewGuid():N}"[..25];
            try
            {
                await we.OpenInWindowAsync(wallpaperPath, windowTitle, options.Width, options.Height);
                var hwnd = await WindowFinder.WaitForWindowAsync(windowTitle, TimeSpan.FromSeconds(20));
                if (hwnd == IntPtr.Zero) throw new InvalidOperationException("Wallpaper Engine pop-out window was not found.");
                await Task.Delay(2000);
                var result = await ffmpeg.CaptureAsync(hwnd, windowTitle, outputPath, options.Width, options.Height,
                    options.Fps, options.DurationSeconds, options.CaptureBackend, caps, profile);
                Console.WriteLine($"SUCCESS: {outputPath} ({result.Backend}, {profile.Label})");
                return 0;
            }
            finally
            {
                if (!options.KeepWindowOpen) await we.CloseWindowAsync(windowTitle);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("ERROR: " + ex.Message);
            return 1;
        }
    }

    private static async Task<int> PrintWorkshopMetadataAsync(string workshopId)
    {
        var metadata = await new WorkshopMetadataService().GetAsync(workshopId);
        if (metadata == null)
        {
            Console.Error.WriteLine("Workshop metadata unavailable. Check internet access and Workshop ID.");
            return 2;
        }
        Console.WriteLine($"Workshop ID: {metadata.WorkshopId}");
        Console.WriteLine($"Title:       {metadata.Title ?? "-"}");
        Console.WriteLine($"Resolution:  {(metadata.HasResolution ? $"{metadata.ResolutionWidth}x{metadata.ResolutionHeight}" : "not found")}");
        Console.WriteLine($"Res. tag:    {metadata.ResolutionTag ?? "-"}");
        Console.WriteLine($"Tags:        {(metadata.Tags.Length == 0 ? "-" : string.Join(", ", metadata.Tags))}");
        return 0;
    }

    private static async Task<int> ListLibraryAsync()
    {
        var items = await new WallpaperLibraryScanner().ScanAsync();
        foreach (var item in items)
            Console.WriteLine($"{item.DisplayType,-12} {item.WorkshopId ?? "-",-14} {item.Title} | {item.ProjectJsonPath}");
        Console.WriteLine($"\nTotal: {items.Count}");
        return 0;
    }

    private static async Task<int> RunDoctorAsync(string? wePath, string? ffmpegPath)
    {
        Console.WriteLine("QFact.WE2Video 1.0.3 diagnostics");
        Console.WriteLine($"OS:                  {RuntimeInformation.OSDescription}");
        Console.WriteLine($"Wallpaper Engine:    {wePath ?? "NOT FOUND"}");
        Console.WriteLine($"FFmpeg:              {ffmpegPath ?? "NOT FOUND"}");
        var ffprobePath = DependencyLocator.FindFfprobe(null);
        Console.WriteLine($"FFprobe metadata:    {ffprobePath ?? "NOT FOUND (metadata limited)"}");

        string? webViewVersion = null;
        try { webViewVersion = CoreWebView2Environment.GetAvailableBrowserVersionString(); } catch { }
        Console.WriteLine($"WebView2 Runtime:     {webViewVersion ?? "NOT FOUND (GUI unavailable)"}");

        var ok = wePath != null && ffmpegPath != null && webViewVersion != null;
        if (ffmpegPath != null)
        {
            var caps = await new FfmpegCapture(ffmpegPath).ProbeAsync();
            Console.WriteLine($"FFmpeg version:      {caps.VersionLine}");
            Console.WriteLine($"gfxcapture (WGC):    {(caps.HasGfxCapture ? "YES" : "NO")}");
            Console.WriteLine($"gdigrab fallback:    {(caps.HasGdiGrab ? "YES" : "NO")}");
            Console.WriteLine($"libx264 / H.264:     {(caps.HasLibX264 ? "YES" : "NO")}");
            Console.WriteLine($"libx265 / HEVC:      {(caps.HasLibX265 ? "YES" : "NO")}");
            Console.WriteLine($"libvpx-vp9 / WebM:  {(caps.HasLibVpxVp9 ? "YES" : "NO")}");
            Console.WriteLine($"libopus / WebM audio:{(caps.HasLibOpus ? "YES" : "NO")}");
            Console.WriteLine($"GIF encoder:         {(caps.HasGif ? "YES" : "NO")}");
            Console.WriteLine($"Process audio:       {(OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041) ? "YES" : "NO (requires Windows 10 2004+)")}");
            Console.WriteLine("Output profiles:");
            foreach (var profile in OutputProfiles.All)
                Console.WriteLine($"  {(profile.IsSupported(caps) ? "READY" : "MISSING"),-8} {profile.Id,-12} {profile.Label}");
            ok &= caps.HasLibX264 && (caps.HasGfxCapture || caps.HasGdiGrab);
        }

        try
        {
            var steamLibraries = SteamLibraryLocator.FindSteamLibraries();
            var wallpapers = await new WallpaperLibraryScanner().ScanAsync();
            Console.WriteLine($"Steam libraries:     {steamLibraries.Count}");
            Console.WriteLine($"Wallpapers indexed:  {wallpapers.Count}");
        }
        catch (Exception ex) { Console.WriteLine($"Library scan:        WARNING ({ex.Message})"); }

        Console.WriteLine(ok ? "Doctor result: READY" : "Doctor result: NOT READY");
        return ok ? 0 : 2;
    }

    private static string ResolveWallpaperPath(string? supplied)
    {
        if (string.IsNullOrWhiteSpace(supplied)) throw new ArgumentException("Use --wallpaper <path> in CLI mode.");
        var path = Environment.ExpandEnvironmentVariables(supplied.Trim().Trim('"'));
        if (File.Exists(path)) return Path.GetFullPath(path);
        if (Directory.Exists(path))
        {
            foreach (var name in new[] { "project.json", "scene.pkg", "index.html" })
            {
                var candidate = Path.Combine(path, name);
                if (File.Exists(candidate)) return Path.GetFullPath(candidate);
            }
        }
        throw new FileNotFoundException("Wallpaper file was not found.", path);
    }

    private static string ResolveOutputPath(string? supplied, OutputProfile profile)
    {
        var path = supplied;
        if (string.IsNullOrWhiteSpace(path))
            path = Path.Combine(Environment.CurrentDirectory, "exports", $"we2video_{DateTime.Now:yyyyMMdd_HHmmss}{profile.Extension}");
        path = Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'));
        if (!Path.IsPathRooted(path)) path = Path.GetFullPath(path);
        if (string.IsNullOrEmpty(Path.GetExtension(path))) path += profile.Extension;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        return path;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("QFact.WE2Video 1.0.3 CLI");
        Console.WriteLine("Run without arguments to open the GUI.");
        Console.WriteLine("--wallpaper/-w <path> --output/-o <path> --format mp4-h264|mp4-hevc|webm-vp9|mkv-h264|mov-h264|gif");
        Console.WriteLine("--width --height --fps --duration --capture auto|gfx|gdi --doctor --list --workshop-meta <id>");
    }
}
