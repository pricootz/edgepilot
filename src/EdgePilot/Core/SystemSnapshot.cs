namespace EdgePilot.Core;

public sealed record DriveSnapshot(
    string Name,
    string Label,
    long TotalBytes,
    long FreeBytes)
{
    public long UsedBytes => Math.Max(0, TotalBytes - FreeBytes);
    public double UsedPercent => TotalBytes <= 0 ? 0 : (double)UsedBytes / TotalBytes * 100.0;
}

public sealed record NetworkSnapshot(
    string InterfaceName,
    bool Connected,
    long ReceiveBytesPerSecond,
    long SendBytesPerSecond,
    long LinkSpeedBitsPerSecond);

public sealed record SystemSnapshot(
    string HostName,
    string OperatingSystem,
    double CpuPercent,
    long MemoryTotalBytes,
    long MemoryAvailableBytes,
    TimeSpan Uptime,
    NetworkSnapshot Network,
    IReadOnlyList<DriveSnapshot> Drives,
    DateTimeOffset CapturedAt)
{
    public long MemoryUsedBytes => Math.Max(0, MemoryTotalBytes - MemoryAvailableBytes);
    public double MemoryUsedPercent => MemoryTotalBytes <= 0
        ? 0
        : (double)MemoryUsedBytes / MemoryTotalBytes * 100.0;
}
