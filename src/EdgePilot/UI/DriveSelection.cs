using EdgePilot.Core;
using EdgePilot.Localization;

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
        var choices = new List<DriveChoice> { new(null, Strings.Get("drive.automatic")) };
        foreach (var drive in drives)
        {
            var capacity = drive.TotalBytes >= 1_000_000_000_000L
                ? Strings.Get("drive.capacity.tb", drive.TotalBytes / 1_000_000_000_000d)
                : Strings.Get("drive.capacity.gb", drive.TotalBytes / 1_000_000_000d);
            choices.Add(new(drive.Name, Strings.Get("drive.caption", drive.Label, drive.Name, capacity)));
        }
        if (selected is not null && Resolve(drives, selected) is null)
            choices.Add(new(selected, Strings.Get("drive.unavailable", selected)));
        return choices;
    }
}
