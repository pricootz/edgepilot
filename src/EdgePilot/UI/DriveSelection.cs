using EdgePilot.Core;

namespace EdgePilot.UI;

public sealed record DriveChoice(string? Name, string Caption)
{
    public override string ToString() => Caption;
}

public static class DriveSelection
{
    public static StringComparer PathComparer => OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    public static DriveSnapshot? Resolve(IReadOnlyList<DriveSnapshot> drives, string? selected)
    {
        if (selected is null) return drives.FirstOrDefault();
        return drives.FirstOrDefault(d => PathComparer.Equals(d.Name, selected));
    }

    public static IReadOnlyList<DriveChoice> Choices(IReadOnlyList<DriveSnapshot> drives, string? selected)
    {
        var choices = new List<DriveChoice> { new(null, "Automatico (disco più capiente)") };
        foreach (var drive in drives)
        {
            var capacity = drive.TotalBytes >= 1_000_000_000_000L
                ? $"{drive.TotalBytes / 1_000_000_000_000d:0.##} TB"
                : $"{drive.TotalBytes / 1_000_000_000d:0.##} GB";
            choices.Add(new(drive.Name, $"{drive.Label} · {drive.Name} · {capacity}"));
        }
        if (selected is not null && Resolve(drives, selected) is null)
            choices.Add(new(selected, $"{selected} · non disponibile"));
        return choices;
    }
}
