# Third-party components

This repository does not bundle Wallpaper Engine, FFmpeg or the WebView2 Runtime.

- **Wallpaper Engine** is a separate commercial application. QFact.WE2Video invokes its documented command-line controls against the user's installed copy for Scene/Web capture and `applyProperties`.
- **FFmpeg** is installed separately by the user. QFact.WE2Video uses it for Windows Graphics Capture / GDI fallback, direct Video-wallpaper transcoding, scaling, muxing and H.264 / HEVC / VP9 / GIF output.
- **Microsoft Edge WebView2 SDK** is referenced as a NuGet dependency for the desktop UI. The Evergreen WebView2 Runtime is installed separately by `install_prereqs.bat` when needed.
- **NAudio 3.0.1** (MIT) is referenced as a NuGet dependency for Windows WASAPI per-process loopback recording used by Scene/Web audio capture.

QFact.WE2Video does not download Workshop content and does not modify Wallpaper Engine project files.

## Branding asset

`qfact-logo.png` and `qfact-logo.ico` are app/UI versions of the QFact logo supplied for this project; they do not contain generated artwork.
