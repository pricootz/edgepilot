namespace EdgePilot.UI;

internal static class DisplayFormat
{
    public static string Bytes(long bytes)
    {
        if (bytes < 0) bytes = 0;
        string[] units = ["B", "KB", "MB", "GB", "TB", "PB"];
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
        if (uptime.TotalDays >= 1) return $"{(int)uptime.TotalDays} g {uptime.Hours} h {uptime.Minutes} min";
        if (uptime.TotalHours >= 1) return $"{uptime.Hours} h {uptime.Minutes} min";
        return $"{uptime.Minutes} min {uptime.Seconds} s";
    }
}
