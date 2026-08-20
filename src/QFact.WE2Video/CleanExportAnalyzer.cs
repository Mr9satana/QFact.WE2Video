using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace QFact.WE2Video;

/// <summary>
/// A single Wallpaper Engine property that WE2Video knows how to turn off.
/// These are the concrete properties sent to applyProperties.
/// </summary>
internal sealed record CleanExportOverride(
    string Key,
    string Label,
    string PropertyType,
    object? Value,
    object? CurrentValue,
    string Reason);

/// <summary>
/// A user-facing root switch. Child controls that are conditionally owned by another
/// switch are deliberately not shown. CascadeKeys contains this switch and every
/// disable-able descendant switch so turning off a module is deterministic.
/// </summary>
internal sealed record CleanExportChoice(
    string Key,
    string Label,
    string PropertyType,
    object? CurrentValue,
    bool IsModule,
    int ChildCount,
    int HiddenSwitchCount,
    IReadOnlyList<string> CascadeKeys);

internal sealed record CleanExportPlan(
    IReadOnlyList<CleanExportOverride> Overrides,
    IReadOnlyList<CleanExportChoice> Choices)
{
    public static readonly CleanExportPlan Empty = new(
        Array.Empty<CleanExportOverride>(),
        Array.Empty<CleanExportChoice>());

    public IReadOnlyList<CleanExportOverride> SelectKeys(IEnumerable<string>? selectedRootKeys)
    {
        if (selectedRootKeys == null || Choices.Count == 0 || Overrides.Count == 0)
            return Array.Empty<CleanExportOverride>();

        var selected = new HashSet<string>(selectedRootKeys.Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.Ordinal);
        if (selected.Count == 0) return Array.Empty<CleanExportOverride>();

        var allowedRoots = Choices.ToDictionary(x => x.Key, StringComparer.Ordinal);
        var concreteKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var key in selected)
        {
            if (!allowedRoots.TryGetValue(key, out var choice)) continue;
            foreach (var cascadeKey in choice.CascadeKeys) concreteKeys.Add(cascadeKey);
        }

        var byKey = Overrides.ToDictionary(x => x.Key, StringComparer.Ordinal);
        return concreteKeys
            .Select(key => byKey.TryGetValue(key, out var item) ? item : null)
            .Where(item => item != null)
            .Cast<CleanExportOverride>()
            .ToArray();
    }
}

/// <summary>
/// 1.0.0 manual Clean Export analyzer.
///
/// It does NOT guess what a switch means from English/Russian keywords. Instead it:
/// 1) finds every user property that can safely be switched off (bool/checkbox or a combo with an Off value),
/// 2) builds the dependency graph from Wallpaper Engine's `condition` expressions,
/// 3) exposes only root switches in the UI,
/// 4) cascades a selected root to any hidden child switches.
///
/// This works regardless of the author's language or naming convention.
/// </summary>
internal sealed class CleanExportAnalyzer
{
    private static readonly Regex ConditionReferenceRegex = new(
        @"(?<key>[\p{L}\p{N}_$\-]+)\.value",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex HtmlTagRegex = new("<[^>]+>", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex WhitespaceRegex = new("\\s+", RegexOptions.Compiled);

    private static readonly string[] OffWords =
    {
        "off", "none", "disabled", "disable", "hidden", "hide", "no", "false", "0",
        "выкл", "выключ", "нет", "скрыт", "скрыть", "отключ",
        "关闭", "关", "無", "なし"
    };

    public CleanExportPlan Analyze(WallpaperInfo wallpaper)
    {
        if (!File.Exists(wallpaper.ProjectJsonPath)) return CleanExportPlan.Empty;

        try
        {
            using var stream = File.OpenRead(wallpaper.ProjectJsonPath);
            using var doc = JsonDocument.Parse(stream, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });

            var root = doc.RootElement;
            if (!root.TryGetProperty("general", out var general) || general.ValueKind != JsonValueKind.Object)
                return CleanExportPlan.Empty;
            if (!general.TryGetProperty("properties", out var properties) || properties.ValueKind != JsonValueKind.Object)
                return CleanExportPlan.Empty;

            var localization = BuildLocalization(root, general);
            var specs = new Dictionary<string, PropertySpec>(StringComparer.Ordinal);

            foreach (var property in properties.EnumerateObject())
            {
                if (property.Value.ValueKind != JsonValueKind.Object) continue;
                var json = property.Value;
                var type = (GetString(json, "type") ?? string.Empty).Trim().ToLowerInvariant();
                var textToken = GetString(json, "text") ?? string.Empty;
                var label = ResolveLabel(property.Name, textToken, localization);
                var condition = GetString(json, "condition") ?? string.Empty;
                var parents = ParseConditionParents(condition);
                var order = TryGetInt(json, "order") ?? TryGetInt(json, "index") ?? int.MaxValue;
                var currentValue = json.TryGetProperty("value", out var current) ? JsonToClr(current) : null;

                var canDisable = TryGetDisableValue(json, type, localization, out var disableValue, out var reason);
                specs[property.Name] = new PropertySpec(
                    property.Name, label, string.IsNullOrWhiteSpace(type) ? "unknown" : type,
                    currentValue, canDisable, disableValue, reason, parents, order);
            }

            if (specs.Count == 0) return CleanExportPlan.Empty;

            var children = specs.Keys.ToDictionary(x => x, _ => new List<string>(), StringComparer.Ordinal);
            foreach (var spec in specs.Values)
            {
                foreach (var parent in spec.Parents)
                {
                    if (children.TryGetValue(parent, out var list) && !list.Contains(spec.Key, StringComparer.Ordinal))
                        list.Add(spec.Key);
                }
            }

            var overrides = specs.Values
                .Where(x => x.CanDisable)
                .OrderBy(x => x.Order)
                .ThenBy(x => x.Label, StringComparer.CurrentCultureIgnoreCase)
                .Select(x => new CleanExportOverride(
                    x.Key, x.Label, x.PropertyType, x.DisableValue, x.CurrentValue, x.Reason))
                .ToArray();

            if (overrides.Length == 0)
                return new CleanExportPlan(Array.Empty<CleanExportOverride>(), Array.Empty<CleanExportChoice>());

            var disableable = overrides.Select(x => x.Key).ToHashSet(StringComparer.Ordinal);
            var choices = new List<CleanExportChoice>();

            foreach (var spec in specs.Values
                         .Where(x => x.CanDisable)
                         .OrderBy(x => x.Order)
                         .ThenBy(x => x.Label, StringComparer.CurrentCultureIgnoreCase))
            {
                if (HasDisableableAncestor(spec.Key, specs, disableable)) continue;

                var descendants = GetDescendants(spec.Key, children);
                var cascade = new List<string> { spec.Key };
                cascade.AddRange(descendants.Where(disableable.Contains));
                cascade = cascade.Distinct(StringComparer.Ordinal).ToList();

                choices.Add(new CleanExportChoice(
                    spec.Key,
                    spec.Label,
                    spec.PropertyType,
                    spec.CurrentValue,
                    IsModule: descendants.Count > 0,
                    ChildCount: descendants.Count,
                    HiddenSwitchCount: Math.Max(0, cascade.Count - 1),
                    CascadeKeys: cascade));
            }

            return new CleanExportPlan(overrides, choices);
        }
        catch (Exception ex)
        {
            AppLogger.Warn("Manual Clean Export analyzer failed: " + ex.Message);
            return CleanExportPlan.Empty;
        }
    }

    private static bool HasDisableableAncestor(
        string key,
        IReadOnlyDictionary<string, PropertySpec> specs,
        IReadOnlySet<string> disableable)
    {
        if (!specs.TryGetValue(key, out var start)) return false;
        var visited = new HashSet<string>(StringComparer.Ordinal) { key };
        var queue = new Queue<string>(start.Parents);

        while (queue.Count > 0)
        {
            var parent = queue.Dequeue();
            if (!visited.Add(parent)) continue;
            if (disableable.Contains(parent)) return true;
            if (!specs.TryGetValue(parent, out var parentSpec)) continue;
            foreach (var ancestor in parentSpec.Parents) queue.Enqueue(ancestor);
        }
        return false;
    }

    private static IReadOnlyList<string> GetDescendants(
        string root,
        IReadOnlyDictionary<string, List<string>> children)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal) { root };
        var queue = new Queue<string>();
        if (children.TryGetValue(root, out var direct))
            foreach (var child in direct) queue.Enqueue(child);

        while (queue.Count > 0)
        {
            var key = queue.Dequeue();
            if (!seen.Add(key)) continue;
            result.Add(key);
            if (children.TryGetValue(key, out var nested))
                foreach (var child in nested) queue.Enqueue(child);
        }
        return result;
    }

    private static IReadOnlyList<string> ParseConditionParents(string condition)
    {
        if (string.IsNullOrWhiteSpace(condition)) return Array.Empty<string>();
        return ConditionReferenceRegex.Matches(condition)
            .Select(m => m.Groups["key"].Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static bool TryGetDisableValue(
        JsonElement spec,
        string type,
        IReadOnlyDictionary<string, IReadOnlyList<string>> localization,
        out object? value,
        out string reason)
    {
        value = null;
        reason = string.Empty;

        if (type is "bool" or "checkbox" ||
            (string.IsNullOrWhiteSpace(type) && spec.TryGetProperty("value", out var current) &&
             current.ValueKind is JsonValueKind.True or JsonValueKind.False))
        {
            value = false;
            reason = "boolean → false";
            return true;
        }

        if (type == "combo" && spec.TryGetProperty("options", out var options) && options.ValueKind == JsonValueKind.Array)
        {
            foreach (var option in options.EnumerateArray())
            {
                if (option.ValueKind != JsonValueKind.Object) continue;
                var labelToken = GetString(option, "label") ?? string.Empty;
                var optionTexts = new List<string> { labelToken };
                if (localization.TryGetValue(labelToken, out var translated)) optionTexts.AddRange(translated);
                if (option.TryGetProperty("value", out var optionValue)) optionTexts.Add(optionValue.ToString());

                var blob = Normalize(string.Join(" ", optionTexts));
                if (!OffWords.Any(x => ContainsWordish(blob, Normalize(x)))) continue;

                if (option.TryGetProperty("value", out var rawValue))
                {
                    value = JsonToClr(rawValue);
                    reason = "combo → off";
                    return true;
                }
            }
        }

        return false;
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> BuildLocalization(JsonElement root, JsonElement general)
    {
        var map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        if (general.TryGetProperty("localization", out var gLoc)) ReadLocalization(gLoc, map);
        if (root.TryGetProperty("localization", out var rLoc)) ReadLocalization(rLoc, map);
        return map.ToDictionary(x => x.Key, x => (IReadOnlyList<string>)x.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static void ReadLocalization(JsonElement localization, Dictionary<string, List<string>> map)
    {
        if (localization.ValueKind != JsonValueKind.Object) return;
        foreach (var language in localization.EnumerateObject())
        {
            if (language.Value.ValueKind != JsonValueKind.Object) continue;
            foreach (var item in language.Value.EnumerateObject())
            {
                if (item.Value.ValueKind != JsonValueKind.String) continue;
                var value = item.Value.GetString();
                if (string.IsNullOrWhiteSpace(value)) continue;
                if (!map.TryGetValue(item.Name, out var list)) map[item.Name] = list = new List<string>();
                var cleaned = CleanLabel(value);
                if (!string.IsNullOrWhiteSpace(cleaned) && !list.Contains(cleaned, StringComparer.CurrentCultureIgnoreCase))
                    list.Add(cleaned);
            }
        }
    }

    private static string ResolveLabel(
        string key,
        string textToken,
        IReadOnlyDictionary<string, IReadOnlyList<string>> localization)
    {
        if (!string.IsNullOrWhiteSpace(textToken) && localization.TryGetValue(textToken, out var values) && values.Count > 0)
        {
            return values.FirstOrDefault(ContainsLatinLetters) ?? values[0];
        }

        var cleaned = CleanLabel(textToken);
        if (!string.IsNullOrWhiteSpace(cleaned))
        {
            if (cleaned.StartsWith("ui_", StringComparison.OrdinalIgnoreCase)) return HumanizeKey(cleaned);
            return cleaned;
        }

        return HumanizeKey(key);
    }

    private static string CleanLabel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var decoded = WebUtility.HtmlDecode(value);
        decoded = Regex.Replace(decoded, "<br\\s*/?>", " ", RegexOptions.IgnoreCase);
        decoded = HtmlTagRegex.Replace(decoded, " ");
        decoded = WhitespaceRegex.Replace(decoded, " ").Trim();
        return decoded;
    }

    private static bool ContainsLatinLetters(string value)
        => value.Any(ch => (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z'));

    private static string HumanizeKey(string key)
    {
        var withSpaces = Regex.Replace(key, "([a-zа-я0-9])([A-ZА-Я])", "$1 $2");
        withSpaces = withSpaces.Replace('_', ' ').Replace('-', ' ');
        return WhitespaceRegex.Replace(withSpaces, " ").Trim();
    }

    private static string Normalize(string value)
    {
        var lower = value.ToLowerInvariant().Replace('_', ' ').Replace('-', ' ');
        return WhitespaceRegex.Replace(lower, " ").Trim();
    }

    private static bool ContainsWordish(string haystack, string needle)
    {
        if (needle.Length <= 1)
            return haystack.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(needle);
        return haystack.Contains(needle, StringComparison.Ordinal);
    }

    private static object? JsonToClr(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number when element.TryGetInt64(out var integer) => integer,
        JsonValueKind.Number when element.TryGetDouble(out var number) => number,
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        _ => element.ToString()
    };

    private static string? GetString(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int? TryGetInt(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value)) return null;
        return value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number) ? number : null;
    }

    private sealed record PropertySpec(
        string Key,
        string Label,
        string PropertyType,
        object? CurrentValue,
        bool CanDisable,
        object? DisableValue,
        string Reason,
        IReadOnlyList<string> Parents,
        int Order);
}
