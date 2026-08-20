using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace QFact.WE2Video;

internal sealed record WorkshopMetadata(
    string WorkshopId,
    string[] Tags,
    int? ResolutionWidth,
    int? ResolutionHeight,
    string? ResolutionTag,
    string? Title,
    string? CreatorSteamId,
    DateTimeOffset? UpdatedAt)
{
    public bool HasResolution => ResolutionWidth is > 0 && ResolutionHeight is > 0;
}

internal sealed class WorkshopMetadataService
{
    private const string DetailsEndpoint = "https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(7);
    private static readonly Regex ResolutionRegex = new(
        @"(?<!\d)(?<w>\d{3,5})\s*[x×X]\s*(?<h>\d{3,5})(?!\d)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly HttpClient _http;
    private readonly Dictionary<string, CacheEntry> _cache = new(StringComparer.Ordinal);
    private readonly string _cachePath;
    private bool _cacheLoaded;

    public WorkshopMetadataService()
    {
        _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(6)
        };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("QFact.WE2Video/1.0.3");

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _cachePath = Path.Combine(AppPaths.CacheDirectory, "workshop-metadata-cache-v1.json");
    }

    public async Task<WorkshopMetadata?> GetAsync(string? workshopId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workshopId) || !workshopId.All(char.IsDigit))
            return null;

        EnsureCacheLoaded();
        if (_cache.TryGetValue(workshopId, out var cached) &&
            DateTimeOffset.UtcNow - cached.FetchedAt <= CacheTtl)
        {
            return cached.Metadata;
        }

        var fetched = await FetchAsync(workshopId, cancellationToken);
        if (fetched != null)
        {
            _cache[workshopId] = new CacheEntry(DateTimeOffset.UtcNow, fetched);
            TrySaveCache();
            return fetched;
        }

        // If Steam is temporarily unavailable, stale metadata is better than no metadata.
        if (cached != null)
            return cached.Metadata;

        return null;
    }

    public void ClearMemoryCache()
    {
        _cache.Clear();
        _cacheLoaded = false;
    }

    private async Task<WorkshopMetadata?> FetchAsync(string workshopId, CancellationToken cancellationToken)
    {
        try
        {
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["itemcount"] = "1",
                ["publishedfileids[0]"] = workshopId
            });

            using var response = await _http.PostAsync(DetailsEndpoint, content, cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty("response", out var responseElement) ||
                !responseElement.TryGetProperty("publishedfiledetails", out var detailsArray) ||
                detailsArray.ValueKind != JsonValueKind.Array ||
                detailsArray.GetArrayLength() == 0)
            {
                return null;
            }

            var item = detailsArray[0];
            if (item.TryGetProperty("result", out var resultElement) &&
                resultElement.ValueKind == JsonValueKind.Number &&
                resultElement.TryGetInt32(out var result) && result != 1)
            {
                return null;
            }

            var tags = ReadTags(item);
            var resolution = PickResolutionTag(tags);
            var title = GetString(item, "title");
            var creator = GetStringOrNumber(item, "creator");
            var updated = ReadUnixTime(item, "time_updated");

            return new WorkshopMetadata(
                workshopId,
                tags,
                resolution.Width,
                resolution.Height,
                resolution.Tag,
                title,
                creator,
                updated);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string[] ReadTags(JsonElement item)
    {
        if (!item.TryGetProperty("tags", out var tagsElement) || tagsElement.ValueKind != JsonValueKind.Array)
            return Array.Empty<string>();

        var tags = new List<string>();
        foreach (var element in tagsElement.EnumerateArray())
        {
            string? tag = null;
            if (element.ValueKind == JsonValueKind.String)
            {
                tag = element.GetString();
            }
            else if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("tag", out var tagProperty))
            {
                tag = tagProperty.ValueKind == JsonValueKind.String ? tagProperty.GetString() : null;
            }

            if (!string.IsNullOrWhiteSpace(tag) && !tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                tags.Add(tag.Trim());
        }

        return tags.ToArray();
    }

    private static (int? Width, int? Height, string? Tag) PickResolutionTag(IReadOnlyList<string> tags)
    {
        var candidates = new List<(int Width, int Height, string Tag)>();
        foreach (var tag in tags)
        {
            var match = ResolutionRegex.Match(tag);
            if (!match.Success) continue;
            if (!int.TryParse(match.Groups["w"].Value, out var width) ||
                !int.TryParse(match.Groups["h"].Value, out var height))
                continue;

            // Reject values that are clearly not display resolutions.
            if (width < 640 || height < 360 || width > 16384 || height > 16384)
                continue;

            candidates.Add((width, height, tag));
        }

        if (candidates.Count == 0) return (null, null, null);

        // Wallpaper Engine normally exposes one resolution tag. If malformed metadata contains
        // several dimension-like tags, prefer the one with the largest pixel area.
        var best = candidates.OrderByDescending(x => (long)x.Width * x.Height).First();
        return (best.Width, best.Height, best.Tag);
    }

    private static string? GetString(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var property)) return null;
        return property.ValueKind == JsonValueKind.String ? property.GetString() : null;
    }

    private static string? GetStringOrNumber(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var property)) return null;
        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.GetRawText(),
            _ => null
        };
    }

    private static DateTimeOffset? ReadUnixTime(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var property)) return null;
        long seconds;
        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt64(out seconds))
            return DateTimeOffset.FromUnixTimeSeconds(seconds);
        if (property.ValueKind == JsonValueKind.String && long.TryParse(property.GetString(), out seconds))
            return DateTimeOffset.FromUnixTimeSeconds(seconds);
        return null;
    }

    private void EnsureCacheLoaded()
    {
        if (_cacheLoaded) return;
        _cacheLoaded = true;

        try
        {
            if (!File.Exists(_cachePath)) return;
            using var stream = File.OpenRead(_cachePath);
            var disk = JsonSerializer.Deserialize<Dictionary<string, CacheEntry>>(stream);
            if (disk == null) return;
            foreach (var pair in disk) _cache[pair.Key] = pair.Value;
        }
        catch
        {
            // Corrupt cache must never block conversion.
        }
    }

    private void TrySaveCache()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_cachePath)!);
            var temp = _cachePath + ".tmp";
            var json = JsonSerializer.Serialize(_cache, new JsonSerializerOptions { WriteIndented = false });
            File.WriteAllText(temp, json);
            File.Move(temp, _cachePath, overwrite: true);
        }
        catch
        {
            // Cache persistence is optional.
        }
    }

    private sealed record CacheEntry(DateTimeOffset FetchedAt, WorkshopMetadata Metadata);
}
