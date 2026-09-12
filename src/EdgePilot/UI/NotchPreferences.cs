using System.Text.Json;
using System.Text.Json.Serialization;
using EdgePilot.Core;

namespace EdgePilot.UI;

public enum NotchDisplayMode { Hover, Always, Hidden }
public enum HoverSensitivity { Precise, Normal, Wide }
public enum SettingsThemePreference { System, Light, Dark }
public enum SettingsBackdrop { Flat, Mica, Acrylic }

public static class SettingsBackdropSupport
{
    // Avalonia supports AcrylicBlur from Windows 10 1803 (build 17134+) and Mica from Windows 11.
    // Keep the capability rules centralized so UI, persistence, and rendering cannot disagree.
    public static bool IsAcrylicSupported => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17134);
    public static bool IsMicaSupported => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000);
    public static bool HasSurfaceChoices => IsAcrylicSupported || IsMicaSupported;

    public static bool IsSupported(SettingsBackdrop backdrop) => backdrop switch
    {
        SettingsBackdrop.Flat => true,
        SettingsBackdrop.Acrylic => IsAcrylicSupported,
        SettingsBackdrop.Mica => IsMicaSupported,
        _ => false
    };

    public static SettingsBackdrop Coerce(SettingsBackdrop backdrop) =>
        IsSupported(backdrop) ? backdrop : SettingsBackdrop.Flat;
}

[Flags]
public enum VisibleMetrics { Cpu = 1, Memory = 2, Disk = 4, Network = 8, All = 15 }

public sealed record NotchPreferences(EdgeSide Edge = EdgeSide.Right,
    NotchDisplayMode Mode = NotchDisplayMode.Hover)
{
    // Null preserves the v0.2 behavior: follow the current primary display.
    public DisplayTarget? Display { get; init; }
    public string? SelectedDrive { get; init; }
    public bool StartAtLogin { get; init; }
    public int RefreshIntervalMs { get; init; } = 1000;
    public HoverSensitivity Sensitivity { get; init; } = HoverSensitivity.Normal;
    public VisibleMetrics Metrics { get; init; } = VisibleMetrics.All;
    // Null means "follow the system language".
    public Language? Language { get; init; }
    public SettingsThemePreference SettingsTheme { get; init; } = SettingsThemePreference.System;
    public SettingsBackdrop Backdrop { get; init; } = SettingsBackdrop.Flat;
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
        if (!Enum.IsDefined(value.Backdrop))
            throw new InvalidDataException(Localization.T("prefs.invalidBackdrop"));
        if (value.Language is { } language && string.IsNullOrWhiteSpace(language.Code))
            throw new InvalidDataException(Localization.T("prefs.invalidLanguage"));
        if (value.Display is { } display &&
            (display.WorkingArea is null || !display.WorkingArea.IsValid ||
             !double.IsFinite(display.Scaling) || display.Scaling is < 0.25 or > 8 ||
             display.SessionId?.Length > 512 || display.Name?.Length > 512))
            throw new InvalidDataException(Localization.T("prefs.invalidDisplay"));
    }

    public static NotchPreferences CoerceForPlatform(NotchPreferences value)
    {
        var backdrop = SettingsBackdropSupport.Coerce(value.Backdrop);
        return backdrop == value.Backdrop ? value : value with { Backdrop = backdrop };
    }

    public static NotchPreferences Load(string path)
    {
        if (!File.Exists(path)) return new();
        var value = JsonSerializer.Deserialize<NotchPreferences>(File.ReadAllText(path), Options)
            ?? throw new InvalidDataException(Localization.T("prefs.emptyFile"));
        Validate(value);
        value = CoerceForPlatform(value);

        // Older v0.2 settings stored enum names such as "Italian". LanguageJsonConverter
        // accepts those names and turns them into locale codes. A locale no longer shipped
        // by the current build resolves safely to the English fallback.
        if (value.Language is { } language)
            value = value with { Language = Localization.NormalizePreference(language) };

        return value;
    }

    public static void Save(string path, NotchPreferences value)
    {
        Validate(value);
        value = CoerceForPlatform(value);
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
}
