using System.Text.Json;

namespace QFact.WE2Video;

internal sealed class AppSettings
{
    public string? Language { get; set; }
    public string? ManualEngineRoot { get; set; }
    public string? OutputFolder { get; set; }

    public bool HasLanguageChoice => Language is "ru" or "en";
    public string EffectiveLanguage => HasLanguageChoice ? Language! : AppI18n.SystemDefaultLanguage;

    public static AppSettings Load()
    {
        try
        {
            AppPaths.EnsureBaseDirectories();
            if (!File.Exists(AppPaths.SettingsPath)) return new AppSettings();
            var json = File.ReadAllText(AppPaths.SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            AppPaths.EnsureBaseDirectories();
            File.WriteAllText(AppPaths.SettingsPath, JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }
        catch (Exception ex)
        {
            AppLogger.Warn("Could not save settings: " + ex.Message);
        }
    }
}
