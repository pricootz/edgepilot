using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using EdgePilot.Core;

namespace EdgePilot.UI;

public enum NotchDisplayMode { Hover, Always, Hidden }
public enum HoverSensitivity { Precise, Normal, Wide }
public enum SettingsThemePreference { System, Light, Dark }

[Flags]
public enum VisibleMetrics { Cpu = 1, Memory = 2, Disk = 4, Network = 8, All = 15 }

public sealed record NotchPreferences(EdgeSide Edge = EdgeSide.Right,
    NotchDisplayMode Mode = NotchDisplayMode.Hover)
{
    public string? SelectedDrive { get; init; }
    public bool StartAtLogin { get; init; }
    public int RefreshIntervalMs { get; init; } = 1000;
    public HoverSensitivity Sensitivity { get; init; } = HoverSensitivity.Normal;
    public VisibleMetrics Metrics { get; init; } = VisibleMetrics.All;
    // Null means "follow the system language". Explicit languages use BCP-47-style codes such as it, en or fr.
    public string? Language { get; init; }
    public SettingsThemePreference SettingsTheme { get; init; } = SettingsThemePreference.System;
}

public static class PreferenceStore
{
    public static string DefaultPath => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "EdgePilot", "settings.json");

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly Regex LanguageTag = new(
        "^[A-Za-z]{2,8}(-[A-Za-z0-9]{2,8})*$",
        RegexOptions.CultureInvariant);

    public static void Validate(NotchPreferences value)
    {
        if (!Enum.IsDefined(value.Edge) || !Enum.IsDefined(value.Mode))
            throw new InvalidDataException(Localization.T("prefs.invalidEdgeMode"));
        if (value.RefreshIntervalMs is not (500 or 1000 or 2000 or 5000))
            throw new InvalidDataException(Localization.T("prefs.invalidRefresh"));
        if (!Enum.IsDefined(value.Sensitivity))
            throw new InvalidDataException(Localization.T("prefs.invalidSensitivity"));
        if (value.Metrics == 0 || (value.Metrics & ~VisibleMetrics.All) != 0)
            throw new InvalidDataException(Localization.T("prefs.invalidMetrics"));
        if (!Enum.IsDefined(value.SettingsTheme))
            throw new InvalidDataException(Localization.T("prefs.invalidTheme"));
        if (value.Language is { Length: > 0 } language && !ValidLanguageValue(language))
            throw new InvalidDataException(Localization.T("prefs.invalidLanguage"));
    }

    public static NotchPreferences Load(string path)
    {
        if (!File.Exists(path)) return new();
        var value = JsonSerializer.Deserialize<NotchPreferences>(File.ReadAllText(path), Options)
            ?? throw new InvalidDataException(Localization.T("prefs.emptyFile"));
        Validate(value);

        // v0.2 initially stored enum names (Italian/English/French). Normalize them to locale codes,
        // and gracefully fall back if a future/downgraded build no longer ships an explicit locale.
        if (value.Language is { Length: > 0 } language)
            value = value with { Language = Localization.NormalizePreference(language) };

        return value;
    }

    public static void Save(string path, NotchPreferences value)
    {
        Validate(value);
        var fullPath = System.IO.Path.GetFullPath(path);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath)!);
        var temporary = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(value, Options));
            File.Move(temporary, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private static bool ValidLanguageValue(string language) =>
        language.Equals("Italian", StringComparison.OrdinalIgnoreCase) ||
        language.Equals("English", StringComparison.OrdinalIgnoreCase) ||
        language.Equals("French", StringComparison.OrdinalIgnoreCase) ||
        LanguageTag.IsMatch(language);
}