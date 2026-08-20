# QFact.WE2Video v1.1.0

QFact.WE2Video v1.1.0 adds Smart Loop for Scene/Web captures and replaces the fragile live GIF pipeline with a finite, cleanup-safe export flow.

## Highlights

- **Smart Loop for Scene/Web:** QFact.WE2Video records a short safety tail and compares downscaled frame sequences around the requested endpoint to find a smoother loop boundary.
- Smart Loop changes the duration only when the visual match is strong enough; otherwise the requested duration is kept unchanged.
- Normal video formats are trimmed with stream copy after Smart Loop analysis, so the feature does not add a second video encode.
- **GIF reliability fix:** Scene/Web GIF capture now records a finite temporary video, closes the Wallpaper Engine pop-out, and only then runs palette generation / GIF encoding.
- **Direct Video → GIF** also uses a finite two-stage pipeline instead of an infinite input loop.
- FFmpeg operations now have watchdog timeouts and process-tree cleanup to prevent permanent hangs.
- Existing Scene/Web WGC capture with GDI fallback, audio capture, Background Capture and Manual Clean Export remain supported.
- Export formats: MP4 H.264, MP4 HEVC, WebM VP9, MKV H.264, MOV H.264 and GIF.

## Smart Loop scope

Smart Loop is currently applied to **Scene and Web wallpapers** for requested durations from 2 to 300 seconds. Direct Video wallpapers keep the existing source-media conversion/loop behavior.

## Requirements

Windows 10 2004+ or Windows 11. Wallpaper Engine is required for Scene/Web wallpapers. FFmpeg/ffprobe and Microsoft Edge WebView2 Runtime are required at runtime.

QFact.WE2Video is independent software and is not affiliated with Wallpaper Engine, Valve or Steam.

Release channel: stable.
