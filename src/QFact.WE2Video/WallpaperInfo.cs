namespace QFact.WE2Video;

internal sealed record WallpaperInfo(
    string Title,
    string Type,
    string ProjectJsonPath,
    string LaunchPath,
    string? PreviewPath,
    string? WorkshopId,
    string Source,
    string? Description,
    string? Author)
{
    public string DisplayType => string.IsNullOrWhiteSpace(Type) ? "unknown" : Type.ToLowerInvariant();
    public string FolderPath => Path.GetDirectoryName(ProjectJsonPath) ?? string.Empty;

    public override string ToString() => Title;
}
