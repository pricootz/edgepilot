using System.Text.Json;
using System.Text.Json.Serialization;

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
            throw new InvalidDataException("Bordo o modalità di visualizzazione non supportati.");
        if (value.RefreshIntervalMs is not (500 or 1000 or 2000 or 5000))
            throw new InvalidDataException("Intervallo di aggiornamento non supportato.");
        if (!Enum.IsDefined(value.Sensitivity))
            throw new InvalidDataException("Sensibilità non supportata.");
        if (value.Metrics == 0 || (value.Metrics & ~VisibleMetrics.All) != 0)
            throw new InvalidDataException("Seleziona almeno una metrica valida.");
    }

    public static NotchPreferences Load(string path)
    {
        if (!File.Exists(path)) return new();
        var value = JsonSerializer.Deserialize<NotchPreferences>(File.ReadAllText(path), Options)
            ?? throw new InvalidDataException("Il file delle impostazioni è vuoto.");
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
