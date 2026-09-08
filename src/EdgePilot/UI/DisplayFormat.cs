using EdgePilot.Localization;

namespace EdgePilot.UI;

internal static class DisplayFormat
{
    public static string Bytes(long bytes)
    {
        if (bytes < 0) bytes = 0;
        var units = Strings.Get("format.bytes.units")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (units.Length == 0) units = ["B", "KB", "MB", "GB", "TB", "PB"];
        double value = bytes;
        var unit = 0;

        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        var decimals = unit >= 3 ? 1 : 0;
        return $"{value.ToString($"F{decimals}")} {units[unit]}";
    }

    public static string Rate(long bytesPerSecond) => $"{Bytes(bytesPerSecond)}/s";

    public static string Uptime(TimeSpan uptime)
    {
        if (uptime.TotalDays >= 1)
            return Strings.Get("format.uptime.days", (int)uptime.TotalDays, uptime.Hours, uptime.Minutes);
        if (uptime.TotalHours >= 1) return Strings.Get("format.uptime.hours", uptime.Hours, uptime.Minutes);
        return Strings.Get("format.uptime.minutes", uptime.Minutes, uptime.Seconds);
    }
}
