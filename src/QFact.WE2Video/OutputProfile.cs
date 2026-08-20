namespace QFact.WE2Video;

internal sealed record OutputProfile(
    string Id,
    string Label,
    string Extension,
    string CodecLabel,
    string? RequiredEncoder,
    bool IsGif = false,
    bool SupportsAudio = true)
{
    public override string ToString() => Label;

    public string Description => AppI18n.T("formatDesc." + Id);

    public bool IsSupported(FfmpegCapabilities caps) => RequiredEncoder switch
    {
        "libx264" => caps.HasLibX264,
        "libx265" => caps.HasLibX265,
        "libvpx-vp9" => caps.HasLibVpxVp9,
        "gif" => caps.HasGif,
        null => true,
        _ => false
    };

    public bool IsAudioSupported(FfmpegCapabilities caps)
        => SupportsAudio && (Id != "webm-vp9" || caps.HasLibOpus);

    public string MissingEncoderMessage => RequiredEncoder switch
    {
        "libx264" => AppI18n.T("missingX264"),
        "libx265" => AppI18n.T("missingX265"),
        "libvpx-vp9" => AppI18n.T("missingVp9"),
        "gif" => AppI18n.T("missingGif"),
        _ => AppI18n.T("missingProfile")
    };

    public string MissingAudioEncoderMessage => Id == "webm-vp9"
        ? AppI18n.T("missingOpus")
        : AppI18n.T("audioUnsupported");
}

internal static class OutputProfiles
{
    public static readonly OutputProfile Mp4H264 = new(
        "mp4-h264", "MP4 • H.264", ".mp4", "H.264 / AVC", "libx264");

    public static readonly OutputProfile Mp4Hevc = new(
        "mp4-hevc", "MP4 • HEVC", ".mp4", "H.265 / HEVC", "libx265");

    public static readonly OutputProfile WebmVp9 = new(
        "webm-vp9", "WebM • VP9", ".webm", "VP9", "libvpx-vp9");

    public static readonly OutputProfile MkvH264 = new(
        "mkv-h264", "MKV • H.264", ".mkv", "H.264 / AVC", "libx264");

    public static readonly OutputProfile MovH264 = new(
        "mov-h264", "MOV • H.264", ".mov", "H.264 / AVC", "libx264");

    public static readonly OutputProfile Gif = new(
        "gif", "GIF • Animated", ".gif", "GIF", "gif", IsGif: true, SupportsAudio: false);

    public static readonly IReadOnlyList<OutputProfile> All =
        new[] { Mp4H264, Mp4Hevc, WebmVp9, MkvH264, MovH264, Gif };

    public static OutputProfile Resolve(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return Mp4H264;
        return All.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException(AppI18n.T("formatUnknown", id));
    }
}
