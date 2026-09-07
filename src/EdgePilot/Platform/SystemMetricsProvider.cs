using System.Runtime.InteropServices;
using EdgePilot.Core;
using EdgePilot.Core.Monitoring;

namespace EdgePilot.Platform;

public sealed class SystemMetricsProvider : ISystemMetricsProvider
{
    private readonly ICpuUsageReader _cpu = CpuUsageReader.Create();
    private readonly NetworkSampler _network = new();

    public SystemSnapshot Capture()
    {
        var (memoryTotal, memoryAvailable) = MemoryReader.Read();

        return new SystemSnapshot(
            Environment.MachineName,
            $"{RuntimeInformation.OSDescription.Trim()} · {RuntimeInformation.OSArchitecture}",
            _cpu.ReadPercent(),
            memoryTotal,
            memoryAvailable,
            TimeSpan.FromMilliseconds(Environment.TickCount64),
            _network.Capture(),
            ReadDrives(),
            DateTimeOffset.Now);
    }

    private static IReadOnlyList<DriveSnapshot> ReadDrives()
    {
        var result = new List<DriveSnapshot>();

        foreach (var drive in DriveInfo.GetDrives())
        {
            try
            {
                if (!drive.IsReady || drive.DriveType is not (DriveType.Fixed or DriveType.Removable or DriveType.Network) || drive.TotalSize <= 0) continue;

                result.Add(new DriveSnapshot(
                    drive.Name,
                    string.IsNullOrWhiteSpace(drive.VolumeLabel) ? drive.Name : drive.VolumeLabel,
                    drive.TotalSize,
                    drive.AvailableFreeSpace));
            }
            catch
            {
                // A removable or transient mount can disappear while enumerating.
            }
        }

        return result
            .OrderByDescending(static drive => drive.TotalBytes)
            .ToArray();
    }
}
