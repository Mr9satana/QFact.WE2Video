# Contributing to QFact.WE2Video

Thanks for helping improve QFact.WE2Video.

## Bug reports

Please include:

- QFact.WE2Video version;
- Windows version;
- wallpaper type: Scene / Web / Video;
- whether audio, Background Capture or Clean Export were enabled;
- exact export format and resolution;
- the Wallpaper Engine Workshop ID when the issue is wallpaper-specific;
- the relevant log excerpt from `%LOCALAPPDATA%\QFact.WE2Video\logs\`.

A short, reproducible sequence of steps is much more useful than a generic “it doesn't work”.

## Pull requests

Keep changes focused. Avoid unrelated UI redesigns or feature additions in bug-fix pull requests. Do not commit generated `bin/`, `obj/`, `release/` or temporary runtime files.

The Windows release target is `win-x64`, .NET 9, self-contained and single-file.
