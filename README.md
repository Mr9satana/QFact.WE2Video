# Wallpaper Engine to Video / MP4 / GIF Converter — QFact.WE2Video

**QFact.WE2Video** is a free Windows utility for exporting installed **Wallpaper Engine** wallpapers to **MP4, WebM, MKV, MOV or GIF**. It supports **Scene, Video and Web wallpapers**, optional audio, background capture, Smart Loop and manual Clean Export.

**RU:** QFact.WE2Video — конвертер **Wallpaper Engine в видео / MP4 / GIF** для Windows. Программа позволяет сохранить установленные обои Wallpaper Engine в обычный видеофайл и использовать результат вне Wallpaper Engine.

[![Latest release](https://img.shields.io/github/v/release/Mr9satana/QFact.WE2Video?display_name=tag&sort=semver)](https://github.com/Mr9satana/QFact.WE2Video/releases/latest)
[![Windows](https://img.shields.io/badge/platform-Windows%2010%2F11-0078D4)](#requirements)
[![Release build](https://img.shields.io/github/actions/workflow/status/Mr9satana/QFact.WE2Video/release.yml?label=release)](https://github.com/Mr9satana/QFact.WE2Video/actions)

## Download / Скачать

**[Download the latest QFact.WE2Video.exe](https://github.com/Mr9satana/QFact.WE2Video/releases/latest)**

**Project website / GitHub Pages:** https://mr9satana.github.io/QFact.WE2Video/

No installer is required. The app is published as a self-contained `win-x64` executable. Wallpaper Engine and Microsoft Edge WebView2 Runtime are still required for the relevant features; FFmpeg/ffprobe must be available on PATH.

## What it does / Что умеет

- **Wallpaper Engine → video/GIF:** MP4/H.264, MP4/HEVC, WebM/VP9, MKV/H.264, MOV/H.264 and animated GIF.
- **Scene / Web wallpapers:** rendered through Wallpaper Engine and captured with Windows Graphics Capture.
- **Smart Loop:** for Scene/Web exports from 2 to 300 seconds, QFact.WE2Video records a short safety tail and compares frame sequences around the requested endpoint. The duration changes only when a strong visual match is found.
- **Reliable GIF pipeline:** Scene/Web GIFs are encoded from a finite temporary capture after the Wallpaper Engine pop-out has already been closed; Video → GIF also uses a finite two-stage pipeline.
- **Video wallpapers:** converted directly from the source media instead of recording a black pop-out window.
- **Audio on/off:** source audio for Video; per-process WASAPI loopback for Scene/Web.
- **Background capture:** the dedicated Wallpaper Engine render window is kept outside the normal desktop flow so you can keep working.
- **Manual Clean Export:** shows root switches/modules exposed by the wallpaper and lets you choose what should be disabled for the export.
- **Resolution presets:** 720p, 1080p, QHD/2K, 4K, native and custom.
- **Steam / Wallpaper Engine discovery:** automatic detection plus manual root-folder fallback.
- **RU / EN:** complete Russian and English UI, with language selection on first launch.
- **Export notifications:** Windows notification when the export succeeds or fails.

## Quick start / Быстрый старт

1. Download `QFact.WE2Video.exe` from **Releases**.
2. Launch it and choose Russian or English.
3. Select an installed Wallpaper Engine wallpaper from the library.
4. Choose format, resolution, FPS, duration and audio.
5. For Scene/Web wallpapers, optionally configure **Clean Export**.
6. Click **Export** and wait for the completion notification.

## Common searches / Частые запросы

QFact.WE2Video is built for tasks such as:

- **Wallpaper Engine to video**
- **Wallpaper Engine to MP4**
- **Wallpaper Engine converter**
- **export Wallpaper Engine wallpaper as video**
- **convert Wallpaper Engine scene to MP4**
- **save Wallpaper Engine wallpaper as GIF**
- **конвертер Wallpaper Engine**
- **как сохранить обои Wallpaper Engine в видео**
- **как сделать видео из обоев WE**
- **как конвертировать Wallpaper Engine в MP4**

## Focused guides / Гайды

- **[Wallpaper Engine → video / MP4 / GIF](docs/wallpaper-engine-to-video.md)** — общий сценарий экспорта.
- **[Wallpaper Engine to MP4](docs/wallpaper-engine-to-mp4.md)** — MP4/H.264 и рекомендуемые настройки.
- **[Wallpaper Engine to GIF](docs/wallpaper-engine-to-gif.md)** — экспорт коротких анимаций в GIF.
- **[How to export Wallpaper Engine wallpapers](docs/how-to-export-wallpaper-engine.md)** — пошаговый workflow и troubleshooting.
- **[Как сохранить обои Wallpaper Engine в видео](docs/ru-kak-sohranit-wallpaper-engine-v-video.md)** — русская инструкция.

## FAQ

### Как сохранить обои Wallpaper Engine в видео?
Выберите Scene, Web или Video-обои в QFact.WE2Video, задайте `MP4 · H.264`, разрешение, FPS и длительность, затем нажмите **Экспортировать**. Scene/Web будут отрендерены через Wallpaper Engine, а Video-конвертированы напрямую из исходного файла.

### Как конвертировать Wallpaper Engine в MP4?
Для максимальной совместимости выберите **MP4 · H.264**. QFact.WE2Video автоматически использует подходящий pipeline в зависимости от типа обоев.

### How do I export Wallpaper Engine wallpaper to video or MP4?
Open QFact.WE2Video, select the installed wallpaper, choose MP4 · H.264 and the output settings, then click **Export**. Scene/Web wallpapers are rendered and captured; Video wallpapers are converted directly.

### Что делает Smart Loop?
Для Scene/Web-обоев QFact.WE2Video может записать небольшой запас после выбранной длительности и сравнить последовательности кадров возле точки обрезки. Если найдено достаточно хорошее совпадение с началом ролика, конечная точка аккуратно сдвигается; если уверенного совпадения нет, программа оставляет указанную длительность без изменений.

### Можно ли сделать GIF из Wallpaper Engine?
Да. Выберите формат **GIF**. GIF не содержит звук, поэтому аудио отключается автоматически. Начиная с v1.1.0 GIF кодируется из конечного временного видео, поэтому Wallpaper Engine не остаётся висеть открытым на этапе построения палитры.

### Нужна ли обычная запись экрана?
Нет. QFact.WE2Video uses Windows Graphics Capture for Scene/Web and direct FFmpeg conversion for Video wallpapers.

### Меняет ли программа оригинальные обои?
Нет. Оригинальные Workshop-файлы не изменяются. Clean Export применяет только доступные User Properties к отдельному окну захвата.

## Clean Export

Clean Export does **not** guess which elements are unwanted. It lists root boolean/on-off controls exposed by the wallpaper. If a root control owns dependent settings, it is shown as one module; child settings are hidden and compatible child switches are handled as a cascade.

This only works for controls the wallpaper exposes through Wallpaper Engine User Properties. QFact.WE2Video does not patch arbitrary hard-coded layers inside `scene.pkg`.

## Requirements

Runtime:

- Windows 10 2004 (build 19041) or newer; Windows 11 recommended.
- Wallpaper Engine for Scene/Web wallpapers.
- FFmpeg and ffprobe available on `PATH`.
- Microsoft Edge WebView2 Runtime.

Building from source additionally requires **.NET 9 SDK**.

## Build from source

```text
MAKE_EXE.bat
```

or manually:

```text
prepare_ui.ps1
publish_release.bat
```

The release binary is written to:

```text
release\win-x64\QFact.WE2Video.exe
```

## App data

```text
%LOCALAPPDATA%\QFact.WE2Video\
```

- `settings.json` — language, manual Steam/WE path and export folder.
- `logs\` — application logs.
- `cache\` — Workshop metadata cache.
- `runtime\` — extracted embedded Web UI.
- `webview2\` — WebView2 profile data.

Default exports: `%USERPROFILE%\Videos\QFact.WE2Video\Exports`.

## Support the project / Поддержать разработку

QFact.WE2Video is free. If it saved you time, you can support development here:

**[♥ DaLink — daewri](https://dalink.to/daewri)**

Donations are optional. The application never asks for or stores card/payment details. You can also help by starring the repository or reporting reproducible bugs.

See **[SUPPORT.md](SUPPORT.md)**.

## Project status

Current stable release: **v1.0.3**. **v1.1.0** (Smart Loop + GIF reliability) is built and undergoing runtime smoke testing before release.

Bug reports and reproducible compatibility issues are welcome through GitHub Issues.

## Third-party / Disclaimer

See **[THIRD_PARTY.md](THIRD_PARTY.md)**. Wallpaper Engine is a separate commercial product and is **not** bundled with QFact.WE2Video. QFact.WE2Video is an independent project and is not affiliated with Wallpaper Engine, Valve or Steam.
