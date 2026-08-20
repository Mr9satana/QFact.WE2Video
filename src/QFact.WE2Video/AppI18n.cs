using System.Globalization;

namespace QFact.WE2Video;

internal static class AppI18n
{
    private static string _language = "ru";
    public static string Language => _language;

    public static void SetLanguage(string? language)
        => _language = string.Equals(language, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "ru";

    public static string SystemDefaultLanguage =>
        string.Equals(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, "ru", StringComparison.OrdinalIgnoreCase)
            ? "ru" : "en";

    public static string T(string key, params object?[] args)
    {
        var source = _language == "en" ? En : Ru;
        if (!source.TryGetValue(key, out var value) && !Ru.TryGetValue(key, out value)) value = key;
        return args.Length == 0 ? value : string.Format(CultureInfo.CurrentCulture, value, args);
    }

    private static readonly IReadOnlyDictionary<string, string> Ru = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["ready"] = "Готово",
        ["working"] = "Работаю…",
        ["scan"] = "Сканирую библиотеку Wallpaper Engine…",
        ["found"] = "Найдено обоев: {0}",
        ["empty"] = "Библиотека пуста",
        ["scanError"] = "Ошибка сканирования: {0}",
        ["selectWallpaper"] = "Сначала выбери обои.",
        ["evenDimensions"] = "Для видеопрофиля ширина и высота должны быть чётными.",
        ["runDoctor"] = " Запусти doctor.bat / install_prereqs.bat.",
        ["updateFfmpeg"] = " Запусти install_prereqs.bat для обновления FFmpeg.",
        ["convert"] = "Конвертирую {0}…",
        ["render"] = "Рендерю {0}…",
        ["done"] = "Готово · {0} · {1:0.0} MB",
        ["exportReadyTitle"] = "QFact.WE2Video — экспорт готов",
        ["exportErrorTitle"] = "QFact.WE2Video — ошибка экспорта",
        ["exportError"] = "Ошибка экспорта: {0}",
        ["error"] = "Ошибка",
        ["ffmpegMissing"] = "FFmpeg не найден",
        ["captureReady"] = "WGC / FFmpeg готов",
        ["gdiFallback"] = "GDI fallback",
        ["captureUnavailable"] = "Захват недоступен",
        ["resolutionAdaptive"] = "Адаптивное / не указано",
        ["notSpecified"] = "Не указано",
        ["metadataFailed"] = "Не удалось определить",
        ["metadataUnavailable"] = "Метаданные недоступны",
        ["none"] = "Нет",
        ["unknown"] = "Не определена",
        ["longestOf"] = " · длиннейший из {0}",
        ["outputFolderDialog"] = "Куда сохранять экспорт QFact.WE2Video",
        ["engineFolderDialog"] = "Выбери корневую папку Steam, Steam Library или wallpaper_engine",
        ["badEngineFolder"] = "Папка не похожа на корень Steam / Steam Library / Wallpaper Engine.\n\nСохранить её всё равно и попробовать сканирование?",
        ["engineFolderTitle"] = "QFact.WE2Video — путь к Wallpaper Engine",
        ["uiStartError"] = "Не удалось запустить интерфейс QFact.WE2Video.\n\n{0}\n\nЕсли ошибка связана с WebView2 Runtime, обнови Microsoft Edge / WebView2 и повтори запуск.",
        ["uiMissing"] = "Встроенный интерфейс QFact.WE2Video не удалось подготовить.",
        ["nativeResolution"] = "Исходное",
        ["customResolution"] = "Пользовательское",
        ["videoSourceMissing"] = "Исходный видеофайл Wallpaper Engine не найден.",
        ["gifNoAudio"] = "GIF не поддерживает звук.",
        ["audioMuxFailed"] = "Видео записано, но добавить звук не удалось: {0}",
        ["ffprobeMissing"] = "ffprobe не найден — длительность медиа недоступна",
        ["nativeVideo"] = "исходный видеофайл",
        ["scenePackedAudio"] = "Scene может хранить ресурсы внутри scene.pkg — их длительность без распаковки не видна",
        ["noOpenAudio"] = "Открытых аудиофайлов в папке проекта не найдено",
        ["audioPidMissing"] = "Не удалось определить PID окна Wallpaper Engine.",
        ["audioWindowsRequired"] = "Process audio loopback требует Windows 10 2004 (build 19041) или новее.",
        ["audioCaptureFailed"] = "Не удалось включить захват звука Wallpaper Engine: {0}",
        ["audioNoPackets"] = "Wallpaper Engine не отдал аудиопакеты; экспорт оставлен без звука.",
        ["weNotFound"] = "Wallpaper Engine не найден. Выбери папку Steam / Wallpaper Engine и повтори.",
        ["weWindowTimeout"] = "Wallpaper Engine не создал окно захвата за 20 секунд.",
        ["ffmpegNotFoundRun"] = "FFmpeg не найден. Установи FFmpeg и повтори.",
        ["ffmpegCaptureMissing"] = "В FFmpeg отсутствуют gfxcapture и gdigrab.",
        ["engineStartTimeout"] = "Wallpaper Engine не запустился за 15 секунд. Запусти его через Steam и повтори.",
        ["formatUnknown"] = "Неизвестный формат: {0}.",
        ["missingX264"] = "FFmpeg не содержит libx264 (H.264).",
        ["missingX265"] = "FFmpeg не содержит libx265 (HEVC/H.265).",
        ["missingVp9"] = "FFmpeg не содержит libvpx-vp9 (VP9/WebM).",
        ["missingGif"] = "FFmpeg не содержит GIF encoder.",
        ["missingProfile"] = "Выбранный профиль не поддерживается этой сборкой FFmpeg.",
        ["missingOpus"] = "Для звука в WebM этой сборке FFmpeg нужен libopus.",
        ["audioUnsupported"] = "Звук не поддерживается выбранным форматом.",
        ["formatDesc.mp4-h264"] = "Максимальная совместимость. Хороший вариант по умолчанию.",
        ["formatDesc.mp4-hevc"] = "Меньше размер при похожем качестве, но кодируется тяжелее.",
        ["formatDesc.webm-vp9"] = "Удобно для веба и современных плееров.",
        ["formatDesc.mkv-h264"] = "Гибкий контейнер Matroska с H.264.",
        ["formatDesc.mov-h264"] = "MOV-контейнер для монтажных и медиаприложений.",
        ["formatDesc.gif"] = "Анимированный GIF. Звук для GIF недоступен."
    };

    private static readonly IReadOnlyDictionary<string, string> En = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["ready"] = "Ready",
        ["working"] = "Working…",
        ["scan"] = "Scanning Wallpaper Engine library…",
        ["found"] = "Wallpapers found: {0}",
        ["empty"] = "Library is empty",
        ["scanError"] = "Library scan failed: {0}",
        ["selectWallpaper"] = "Select a wallpaper first.",
        ["evenDimensions"] = "Video profile width and height must be even numbers.",
        ["runDoctor"] = " Run doctor.bat / install_prereqs.bat.",
        ["updateFfmpeg"] = " Run install_prereqs.bat to update FFmpeg.",
        ["convert"] = "Converting {0}…",
        ["render"] = "Rendering {0}…",
        ["done"] = "Done · {0} · {1:0.0} MB",
        ["exportReadyTitle"] = "QFact.WE2Video — export complete",
        ["exportErrorTitle"] = "QFact.WE2Video — export failed",
        ["exportError"] = "Export failed: {0}",
        ["error"] = "Error",
        ["ffmpegMissing"] = "FFmpeg not found",
        ["captureReady"] = "WGC / FFmpeg ready",
        ["gdiFallback"] = "GDI fallback",
        ["captureUnavailable"] = "Capture unavailable",
        ["resolutionAdaptive"] = "Adaptive / not specified",
        ["notSpecified"] = "Not specified",
        ["metadataFailed"] = "Could not determine",
        ["metadataUnavailable"] = "Metadata unavailable",
        ["none"] = "None",
        ["unknown"] = "Unknown",
        ["longestOf"] = " · longest of {0}",
        ["outputFolderDialog"] = "Choose QFact.WE2Video export folder",
        ["engineFolderDialog"] = "Choose Steam, Steam Library or wallpaper_engine root folder",
        ["badEngineFolder"] = "This folder does not look like a Steam / Steam Library / Wallpaper Engine root.\n\nSave it anyway and try scanning?",
        ["engineFolderTitle"] = "QFact.WE2Video — Wallpaper Engine path",
        ["uiStartError"] = "Could not start QFact.WE2Video UI.\n\n{0}\n\nIf this is related to WebView2 Runtime, update Microsoft Edge / WebView2 and try again.",
        ["uiMissing"] = "The embedded QFact.WE2Video UI could not be prepared.",
        ["nativeResolution"] = "Native",
        ["customResolution"] = "Custom",
        ["videoSourceMissing"] = "Wallpaper Engine source video file was not found.",
        ["gifNoAudio"] = "GIF does not support audio.",
        ["audioMuxFailed"] = "Video was recorded, but audio could not be added: {0}",
        ["ffprobeMissing"] = "ffprobe not found — media duration unavailable",
        ["nativeVideo"] = "source video file",
        ["scenePackedAudio"] = "Scene may store assets inside scene.pkg — duration is unavailable without unpacking",
        ["noOpenAudio"] = "No directly accessible audio files found in the project folder",
        ["audioPidMissing"] = "Could not determine Wallpaper Engine window PID.",
        ["audioWindowsRequired"] = "Process audio loopback requires Windows 10 2004 (build 19041) or newer.",
        ["audioCaptureFailed"] = "Could not start Wallpaper Engine audio capture: {0}",
        ["audioNoPackets"] = "Wallpaper Engine produced no audio packets; export was kept without audio.",
        ["weNotFound"] = "Wallpaper Engine was not found. Choose the Steam / Wallpaper Engine folder and retry.",
        ["weWindowTimeout"] = "Wallpaper Engine did not create the capture window within 20 seconds.",
        ["ffmpegNotFoundRun"] = "FFmpeg was not found. Install FFmpeg and retry.",
        ["ffmpegCaptureMissing"] = "FFmpeg contains neither gfxcapture nor gdigrab.",
        ["engineStartTimeout"] = "Wallpaper Engine did not start within 15 seconds. Start it through Steam and retry.",
        ["formatUnknown"] = "Unknown format: {0}.",
        ["missingX264"] = "FFmpeg does not contain libx264 (H.264).",
        ["missingX265"] = "FFmpeg does not contain libx265 (HEVC/H.265).",
        ["missingVp9"] = "FFmpeg does not contain libvpx-vp9 (VP9/WebM).",
        ["missingGif"] = "FFmpeg does not contain the GIF encoder.",
        ["missingProfile"] = "The selected profile is not supported by this FFmpeg build.",
        ["missingOpus"] = "This FFmpeg build needs libopus for WebM audio.",
        ["audioUnsupported"] = "Audio is not supported by the selected format.",
        ["formatDesc.mp4-h264"] = "Maximum compatibility. A strong default choice.",
        ["formatDesc.mp4-hevc"] = "Smaller files at similar quality, but heavier to encode.",
        ["formatDesc.webm-vp9"] = "Useful for web delivery and modern players.",
        ["formatDesc.mkv-h264"] = "Flexible Matroska container with H.264.",
        ["formatDesc.mov-h264"] = "MOV container for editing and media applications.",
        ["formatDesc.gif"] = "Animated GIF. Audio is not available for GIF."
    };
}
