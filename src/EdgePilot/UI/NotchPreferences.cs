using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using EdgePilot.Localization;

namespace EdgePilot.UI;

public enum NotchDisplayMode { Hover, Always, Hidden }

public enum HoverSensitivity { Precise, Normal, Wide }
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

    /// <summary>Interface language code; null follows the operating system.</summary>
    public string? Language { get; init; }
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

    // Only the shape is checked. A code for a language this build no longer
    // ships resolves to a fallback instead of rejecting the whole file, so a
    // downgrade can never lock the user out of the settings that would fix it.
    private static readonly Regex LanguageTag = new(@"^[A-Za-z]{2,8}(-[A-Za-z0-9]{2,8})*$",
        RegexOptions.CultureInvariant);

    public static void Validate(NotchPreferences value)
    {
        if (!Enum.IsDefined(value.Edge) || !Enum.IsDefined(value.Mode))
            throw new InvalidDataException(Strings.Get("preferences.error.edge"));
        if (value.RefreshIntervalMs is not (500 or 1000 or 2000 or 5000))
            throw new InvalidDataException(Strings.Get("preferences.error.interval"));
        if (!Enum.IsDefined(value.Sensitivity))
            throw new InvalidDataException(Strings.Get("preferences.error.sensitivity"));
        if (value.Metrics == 0 || (value.Metrics & ~VisibleMetrics.All) != 0)
            throw new InvalidDataException(Strings.Get("preferences.error.metrics"));
        if (value.Language is not null && !LanguageTag.IsMatch(value.Language))
            throw new InvalidDataException(Strings.Get("preferences.error.language"));
    }

    public static NotchPreferences Load(string path)
    {
        if (!File.Exists(path)) return new();
        var value = JsonSerializer.Deserialize<NotchPreferences>(File.ReadAllText(path), Options)
            ?? throw new InvalidDataException(Strings.Get("preferences.error.empty"));
        Validate(value);
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
}
