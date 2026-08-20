# QFact.WE2Video — Wallpaper Engine to Video / GIF Converter

**QFact.WE2Video** is a Windows **Wallpaper Engine converter** that exports installed Wallpaper Engine wallpapers to **MP4, WebM, MKV, MOV or GIF**. It supports **Scene, Video and Web wallpapers**, optional audio, background capture and manual Clean Export.

**RU:** QFact.WE2Video — конвертер Wallpaper Engine в видео и GIF для Windows. Он позволяет **сохранить обои Wallpaper Engine в MP4**, WebM, MKV, MOV или GIF и использовать результат вне Wallpaper Engine. Поддерживаются Scene, Video и Web-обои, звук, фоновый захват и ручной Clean Export.

> Looking for “Wallpaper Engine to video”, “Wallpaper Engine to MP4”, “Wallpaper Engine converter”, “how to export Wallpaper Engine wallpaper as video”, «конвертер Wallpaper Engine», «как сохранить обои Wallpaper Engine в видео» or «как сделать видео из обоев WE»? This is exactly what QFact.WE2Video is built for.

## Features / Возможности

- **Wallpaper Engine → video/GIF:** MP4/H.264, MP4/HEVC, WebM/VP9, MKV/H.264, MOV/H.264 and animated GIF.
- **Scene / Web:** rendered through Wallpaper Engine and captured with Windows Graphics Capture, with fallback where available.
- **Video wallpapers:** converted directly from the source media instead of recording a black pop-out window.
- **Audio on/off:** source audio for Video; per-process WASAPI loopback for Scene/Web.
- **Background capture:** the dedicated Wallpaper Engine render window is kept outside the normal desktop flow so the user can keep working.
- **Manual Clean Export:** lists root switches/modules exposed by the wallpaper and lets the user choose what to turn off for the capture.
- **Resolution presets:** 720p, 1080p, QHD/2K, 4K, native and custom.
- **Steam / Wallpaper Engine discovery:** automatic detection plus manual root-folder fallback.
- **RU / EN:** full Russian and English UI, with language selection on first launch.
- **Export notifications:** a Windows notification when the export succeeds or fails.

## Быстрый старт / Quick start

1. Download `QFact.WE2Video.exe` from the latest GitHub Release.
2. Launch it and choose Russian or English.
3. Select a Wallpaper Engine wallpaper from the library.
4. Choose format, resolution, FPS, duration and audio.
5. For Scene/Web wallpapers, optionally use manual Clean Export.
6. Click **Export** and wait for the completion notification.

## Typical use cases / Частые задачи

- Convert a Wallpaper Engine Scene wallpaper to MP4.
- Export a Wallpaper Engine Video wallpaper without screen recording.
- Save a Wallpaper Engine wallpaper as GIF.
- Turn a dual-monitor / ultrawide wallpaper into a normal video file.
- Record Wallpaper Engine wallpaper with or without audio.
- Export wallpaper while hiding optional clock, media-player or visualizer modules exposed by the author.

## Requirements

Runtime:
- Windows 10 2004 (build 19041) or newer; Windows 11 recommended.
- Wallpaper Engine for Scene/Web wallpapers.
- FFmpeg/ffprobe available on PATH.
- Microsoft Edge WebView2 Runtime.

Building from source additionally requires .NET 9 SDK.

## Build from source

1. Run `install_prereqs.bat` once if required.
2. Run `publish_release.bat`.
3. The self-contained Windows binary is created at `release\win-x64\QFact.WE2Video.exe`.
4. SHA-256 is written to `release\SHA256SUMS.txt`.

The .NET runtime, Web UI and managed/native app dependencies are packed into the published single-file EXE. FFmpeg, Wallpaper Engine and the WebView2 Runtime remain external runtime dependencies.

## Clean Export

Clean Export does **not** guess which elements are unwanted. It shows root boolean/on-off controls exposed by the wallpaper. If a root control owns dependent settings, it is shown as one module; child settings are hidden and compatible child switches are handled as a cascade.

This only works for properties the wallpaper exposes through Wallpaper Engine User Properties. QFact.WE2Video does not patch arbitrary hard-coded layers inside `scene.pkg`.

## App data

`%LOCALAPPDATA%\QFact.WE2Video\`

- `settings.json` — language, manual Steam/WE path and export folder.
- `logs\` — application logs.
- `cache\` — Workshop metadata cache.
- `runtime\` — extracted embedded Web UI.
- `webview2\` — WebView2 profile data.

Default exports: `%USERPROFILE%\Videos\QFact.WE2Video\Exports`.

## Support the project / Поддержать разработку

If QFact.WE2Video saved you time, you can support the project here: https://dalink.to/daewri

Если программа оказалась полезной, поддержать разработку можно здесь: https://dalink.to/daewri

## Third-party / Disclaimer

See `THIRD_PARTY.md`. Wallpaper Engine is a separate commercial product and is **not** bundled with QFact.WE2Video. QFact.WE2Video is an independent project and is not affiliated with Wallpaper Engine or Valve.
