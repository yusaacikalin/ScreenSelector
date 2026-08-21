using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScreenSelector;

[Flags]
public enum HotkeyModifiers : uint
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Win = 8,
    NoRepeat = 0x4000
}

public sealed class AppSettings
{
    public int SettingsSchema { get; set; } = 2;
    public Keys HotkeyKey { get; set; } = Keys.Space;
    public HotkeyModifiers HotkeyModifiers { get; set; } = HotkeyModifiers.Control | HotkeyModifiers.Shift;
    public bool StartWithWindows { get; set; }
    public bool StartMinimized { get; set; } = true;
    public string SourceLanguage { get; set; } = "auto";
    public string TargetLanguage { get; set; } = "tr";
    public string AudDToken { get; set; } = string.Empty;

    [JsonIgnore]
    public static string SettingsDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ScreenSelector");

    [JsonIgnore]
    public static string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new AppSettings();

            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty(nameof(SettingsSchema), out _))
            {
                settings.SettingsSchema = 2;
                settings.SourceLanguage = "auto";
                settings.TargetLanguage = "tr";
                settings.Save();
            }

            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(SettingsDirectory);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }
}
