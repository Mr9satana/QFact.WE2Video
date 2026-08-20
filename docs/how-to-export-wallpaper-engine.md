---
layout: default
title: How to Export Wallpaper Engine Wallpapers to Video — QFact.WE2Video
description: Step-by-step guide to export Wallpaper Engine Scene, Web and Video wallpapers to MP4, WebM, MKV, MOV or GIF.
---

# How to Export Wallpaper Engine Wallpapers to Video

Wallpaper Engine Scene and Web wallpapers are live-rendered projects, not ordinary movie files. **QFact.WE2Video** automates the practical export workflow and also handles Video wallpapers directly.

## Step by step

1. Install and open **QFact.WE2Video**.
2. Let the app detect Steam / Wallpaper Engine, or choose the Steam/WE root folder manually.
3. Pick an installed wallpaper from the library.
4. Choose the output format: MP4, WebM, MKV, MOV or GIF.
5. Select resolution, FPS and duration.
6. Enable audio if the wallpaper contains sound you want to keep.
7. For Scene/Web wallpapers, optionally open **Manual Clean Export** and disable any exposed root modules you do not want in the final recording.
8. Click **Export**.
9. Keep working while Background Capture is active; QFact.WE2Video shows a notification when the file is ready.

## Which pipeline is used?

### Scene wallpapers
Rendered by Wallpaper Engine in a dedicated window, captured with Windows Graphics Capture, then encoded by FFmpeg.

### Web wallpapers
Rendered by Wallpaper Engine and captured through the same dedicated-window workflow.

### Video wallpapers
The original media file is used as the FFmpeg input directly, so no screen capture is needed.

## If Steam or Wallpaper Engine is not detected

Use the **Steam / WE** button in the header and select a Steam root, Steam library, `steamapps` folder or the Wallpaper Engine installation folder. The selected path is saved for later launches.

## If you get a black export

For Video wallpapers, current QFact.WE2Video releases use direct media conversion specifically to avoid black pop-out captures. For Scene/Web, check that Wallpaper Engine itself can open the wallpaper correctly and that Windows Graphics Capture is available.

## Related guides

- [Wallpaper Engine to MP4](wallpaper-engine-to-mp4.md)
- [Wallpaper Engine to GIF](wallpaper-engine-to-gif.md)
- [Wallpaper Engine to video](wallpaper-engine-to-video.md)
- [Русская инструкция](ru-kak-sohranit-wallpaper-engine-v-video.md)

[Download the latest QFact.WE2Video release](https://github.com/Mr9satana/QFact.WE2Video/releases/latest)
