# Changelog

## 1.1.0 — 2026-08-21

### Smart Loop
- Added automatic Smart Loop analysis for Scene/Web exports from 2 to 300 seconds.
- The capture records a small finite tail beyond the requested duration, downsamples frames for analysis, and compares short frame sequences around the requested endpoint.
- The export duration is adjusted only when a sufficiently strong visual match is found; otherwise the exact requested duration is preserved.
- Smart Loop trimming uses stream copy for normal video formats, avoiding an unnecessary second encode.

### GIF reliability
- Scene/Web GIF export no longer runs `palettegen/paletteuse` directly against a live Wallpaper Engine capture.
- Scene/Web GIF now captures a finite temporary video first, closes the Wallpaper Engine pop-out immediately, and then encodes the GIF from the local file.
- Direct Video → GIF uses the same finite two-stage approach instead of feeding an infinite loop into the GIF palette graph.
- Added FFmpeg watchdog timeouts and process-tree cleanup so a stuck encode cannot wait forever.
- Wallpaper Engine pop-outs are guaranteed to be closed during cleanup if capture or post-processing fails.

### Version
- Bumped application version to **1.1.0**.

## 1.0.3 — 2026-08-20

- Fixed the developer support button: the WebView now sends the `openExternal` message correctly to the host application.
- No export/capture pipeline changes.

## 1.0.2 — 2026-08-20

- Added an always-visible **Support developer / Поддержать разработчика** button to the main header.
- The header support button opens the same external DaLink page as the Help dialog support action.
- No export, capture, Clean Export, audio or discovery logic was changed.

## 1.0.1 — 2026-08-20

### Final polish
- Added a minimal built-in **? Help** guide next to the language selector, fully localized in Russian and English.
- Help covers quick export, formats, audio/background capture, manual Clean Export, Steam/WE path fallback, safety and troubleshooting.
- Preview now uses a stable **Fit / contain** mode: the full image is always visible without cropping or distortion, while the blurred backdrop fills unused space.
- No changes to the proven capture/export pipeline.

## 1.0.0 — 2026-08-20

### Release
- Product finalized as **QFact.WE2Video**.
- Full Russian/English UI with first-run language choice and runtime language switching.
- Self-contained win-x64 single-file publish configuration.
- Embedded WebView2 HTML/CSS/JS UI bundle; no loose UI files are required next to the EXE.
- App data migrated to `%LOCALAPPDATA%\QFact.WE2Video`.

### Background capture
- Wallpaper Engine pop-out now receives its off-screen position from the CLI before creation.
- Added a WinEvent guard installed **before** `openWallpaper` to catch the pop-out as soon as Windows creates/shows it.
- Background window gets `WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW`, loses `WS_EX_APPWINDOW`, is moved to `-32000,-32000`, placed at the bottom of Z-order and shown without activation.
- Polling fallback reduced to 15 ms and reapplies background configuration after creation.

### Existing finalized features
- Direct Video-wallpaper transcoding.
- Scene/Web WGC capture with GDI fallback.
- Source/process audio toggle.
- Manual module-aware Clean Export.
- Manual Steam/Wallpaper Engine root path.
- MP4 H.264 / HEVC, WebM VP9, MKV, MOV, GIF.
- 720p through 4K, native and custom resolutions.
