using System.Text.Json;
using System.Text.Json.Serialization;

namespace EdgePilot.UI;

public enum NotchDisplayMode { Hover, Always, Hidden }

public sealed record NotchPreferences(EdgeSide Edge = EdgeSide.Right,
    NotchDisplayMode Mode = NotchDisplayMode.Hover);

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
            throw new InvalidDataException("Unsupported edge or display mode.");
    }

    public static NotchPreferences Load(string path)
    {
        if (!File.Exists(path)) return new();
        var value = JsonSerializer.Deserialize<NotchPreferences>(File.ReadAllText(path), Options)
            ?? throw new InvalidDataException("The settings file is empty.");
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
