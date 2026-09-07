using System.Globalization;
using System.Runtime.InteropServices;

namespace EdgePilot.Platform;

internal interface ICpuUsageReader
{
    double ReadPercent();
}

internal static class CpuUsageReader
{
    public static ICpuUsageReader Create()
    {
        if (OperatingSystem.IsWindows()) return new WindowsCpuUsageReader();
        if (OperatingSystem.IsLinux()) return new LinuxCpuUsageReader();
        return new NullCpuUsageReader();
    }
}

internal sealed class NullCpuUsageReader : ICpuUsageReader
{
    public double ReadPercent() => 0;
}

internal sealed class WindowsCpuUsageReader : ICpuUsageReader
{
    private ulong _lastIdle;
    private ulong _lastKernel;
    private ulong _lastUser;
    private bool _hasPrevious;

    public double ReadPercent()
    {
        if (!GetSystemTimes(out var idle, out var kernel, out var user)) return 0;

        var idleValue = ToUInt64(idle);
        var kernelValue = ToUInt64(kernel);
        var userValue = ToUInt64(user);

        if (!_hasPrevious)
        {
            _lastIdle = idleValue;
            _lastKernel = kernelValue;
            _lastUser = userValue;
            _hasPrevious = true;
            return 0;
        }

        var idleDelta = idleValue - _lastIdle;
        var kernelDelta = kernelValue - _lastKernel;
        var userDelta = userValue - _lastUser;
        var totalDelta = kernelDelta + userDelta;

        _lastIdle = idleValue;
        _lastKernel = kernelValue;
        _lastUser = userValue;

        if (totalDelta == 0) return 0;
        var busy = 1.0 - (double)idleDelta / totalDelta;
        return Math.Clamp(busy * 100.0, 0, 100);
    }

    private static ulong ToUInt64(FileTime value) => ((ulong)value.High << 32) | value.Low;

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(out FileTime idleTime, out FileTime kernelTime, out FileTime userTime);

    [StructLayout(LayoutKind.Sequential)]
    private struct FileTime
    {
        public uint Low;
        public uint High;
    }
}

internal sealed class LinuxCpuUsageReader : ICpuUsageReader
{
    private ulong _lastIdle;
    private ulong _lastTotal;
    private bool _hasPrevious;

    public double ReadPercent()
    {
        var line = File.ReadLines("/proc/stat").FirstOrDefault();
        if (string.IsNullOrWhiteSpace(line)) return 0;

        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 5 || !string.Equals(parts[0], "cpu", StringComparison.Ordinal)) return 0;

        var values = parts.Skip(1)
            .Select(static part => ulong.Parse(part, CultureInfo.InvariantCulture))
            .ToArray();

        var idle = values.ElementAtOrDefault(3) + values.ElementAtOrDefault(4);
        var total = values.Aggregate(0UL, static (sum, value) => sum + value);

        if (!_hasPrevious)
        {
            _lastIdle = idle;
            _lastTotal = total;
            _hasPrevious = true;
            return 0;
        }

        var idleDelta = idle - _lastIdle;
        var totalDelta = total - _lastTotal;
        _lastIdle = idle;
        _lastTotal = total;

        if (totalDelta == 0) return 0;
        return Math.Clamp((double)(totalDelta - idleDelta) / totalDelta * 100.0, 0, 100);
    }
}
