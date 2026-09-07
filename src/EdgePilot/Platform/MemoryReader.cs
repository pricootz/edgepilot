using System.Globalization;
using System.Runtime.InteropServices;

namespace EdgePilot.Platform;

internal static class MemoryReader
{
    public static (long TotalBytes, long AvailableBytes) Read()
    {
        if (OperatingSystem.IsWindows()) return ReadWindows();
        if (OperatingSystem.IsLinux()) return ReadLinux();
        return (0, 0);
    }

    private static (long, long) ReadWindows()
    {
        var status = new MemoryStatusEx
        {
            Length = (uint)Marshal.SizeOf<MemoryStatusEx>()
        };

        if (!GlobalMemoryStatusEx(ref status)) return (0, 0);
        return (ToLong(status.TotalPhysical), ToLong(status.AvailablePhysical));
    }

    private static (long, long) ReadLinux()
    {
        long total = 0;
        long available = 0;
        long free = 0;
        long buffers = 0;
        long cached = 0;

        foreach (var line in File.ReadLines("/proc/meminfo"))
        {
            if (line.StartsWith("MemTotal:", StringComparison.Ordinal)) total = ParseKb(line);
            else if (line.StartsWith("MemAvailable:", StringComparison.Ordinal)) available = ParseKb(line);
            else if (line.StartsWith("MemFree:", StringComparison.Ordinal)) free = ParseKb(line);
            else if (line.StartsWith("Buffers:", StringComparison.Ordinal)) buffers = ParseKb(line);
            else if (line.StartsWith("Cached:", StringComparison.Ordinal)) cached = ParseKb(line);
        }

        if (available == 0) available = free + buffers + cached;
        return (total, available);
    }

    private static long ParseKb(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return 0;
        return long.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var kb)
            ? checked(kb * 1024)
            : 0;
    }

    private static long ToLong(ulong value) => value > long.MaxValue ? long.MaxValue : (long)value;

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }
}
