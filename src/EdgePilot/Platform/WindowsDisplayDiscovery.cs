using System.Runtime.InteropServices;

namespace EdgePilot.Platform;

internal sealed record WindowsDisplayMetadata(
    string? StableId,
    string? FriendlyName,
    string? ConnectorName,
    int? ConnectorNumber,
    bool? IsAvailable);

/// <summary>
/// Enriches Avalonia's Windows screens with CCD metadata. HMONITOR is only a session handle;
/// the monitor device path is stable enough to persist, while targetAvailable reports whether
/// Windows considers the display path usable. A monitor whose physical power button is off can
/// still be reported as active by its driver, so this is not a physical-power detector.
/// </summary>
internal static class WindowsDisplayDiscovery
{
    private const uint QdcOnlyActivePaths = 0x00000002;
    private const int ErrorSuccess = 0;
    private const int ErrorInsufficientBuffer = 122;
    private const uint GetSourceName = 1;
    private const uint GetTargetName = 2;
    private const uint EddGetDeviceInterfaceName = 0x00000001;

    public static IReadOnlyDictionary<IntPtr, WindowsDisplayMetadata> Capture(
        IReadOnlyList<IntPtr> monitorHandles)
    {
        if (!OperatingSystem.IsWindows() || monitorHandles.Count == 0)
            return new Dictionary<IntPtr, WindowsDisplayMetadata>();

        try
        {
            var catalog = ReadActivePaths();
            var result = new Dictionary<IntPtr, WindowsDisplayMetadata>();

            foreach (var handle in monitorHandles.Where(handle => handle != IntPtr.Zero).Distinct())
            {
                var connector = GetConnectorName(handle);
                if (connector is null)
                    continue;

                var candidates = catalog.Paths
                    .Where(path => string.Equals(path.ConnectorName, connector,
                        StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                var preferred = candidates.FirstOrDefault(path => path.IsAvailable) ??
                                candidates.FirstOrDefault();
                var legacy = preferred?.StableId is null || preferred.FriendlyName is null
                    ? ReadLegacyMonitor(connector)
                    : default;
                bool? available = candidates.Length > 0
                    ? candidates.Any(path => path.IsAvailable)
                    : catalog.AllSourceNamesResolved ? false : null;

                result[handle] = new WindowsDisplayMetadata(
                    Normalize(preferred?.StableId) ?? Normalize(legacy.StableId),
                    Normalize(preferred?.FriendlyName) ?? Normalize(legacy.FriendlyName),
                    connector,
                    ConnectorNumber(connector),
                    available);
            }

            return result;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or
                                   BadImageFormatException or MarshalDirectiveException or
                                   OverflowException)
        {
            System.Diagnostics.Trace.WriteLine($"EdgePilot could not read Windows display metadata: {ex.Message}");
            return new Dictionary<IntPtr, WindowsDisplayMetadata>();
        }
    }

    internal static int? ConnectorNumber(string? connector)
    {
        connector = Normalize(connector);
        if (connector is null)
            return null;

        var end = connector.Length;
        var start = end;
        while (start > 0 && char.IsAsciiDigit(connector[start - 1]))
            start--;

        return start < end && int.TryParse(connector.AsSpan(start), out var number) && number > 0
            ? number
            : null;
    }

    internal static bool InteropLayoutIsExpected =>
        Marshal.SizeOf<DisplayConfigPathInfo>() == 72 &&
        Marshal.SizeOf<DisplayConfigModeInfo>() == 64 &&
        Marshal.SizeOf<DisplayConfigSourceDeviceName>() == 84 &&
        Marshal.SizeOf<DisplayConfigTargetDeviceName>() == 420 &&
        Marshal.SizeOf<DisplayDevice>() == 840 &&
        Marshal.SizeOf<MonitorInfoEx>() == 104;

    private static DisplayCatalog ReadActivePaths()
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var result = GetDisplayConfigBufferSizes(QdcOnlyActivePaths, out var pathCount, out var modeCount);
            if (result != ErrorSuccess)
                return DisplayCatalog.Unknown;

            var paths = new DisplayConfigPathInfo[checked((int)pathCount)];
            var modes = new DisplayConfigModeInfo[checked((int)modeCount)];
            result = QueryDisplayConfig(QdcOnlyActivePaths, ref pathCount, paths,
                ref modeCount, modes, IntPtr.Zero);
            if (result == ErrorInsufficientBuffer)
                continue;
            if (result != ErrorSuccess)
                return DisplayCatalog.Unknown;

            var resolved = new List<DisplayPath>((int)pathCount);
            var allSourceNamesResolved = true;
            for (var index = 0; index < pathCount; index++)
            {
                var path = paths[index];
                var connector = ReadSourceName(path.SourceInfo);
                if (connector is null)
                {
                    allSourceNamesResolved = false;
                    continue;
                }

                var target = ReadTargetName(path.TargetInfo);
                resolved.Add(new DisplayPath(
                    connector,
                    target.StableId,
                    target.FriendlyName,
                    path.TargetInfo.TargetAvailable != 0));
            }

            return new DisplayCatalog(resolved, allSourceNamesResolved);
        }

        return DisplayCatalog.Unknown;
    }

    private static string? ReadSourceName(DisplayConfigPathSourceInfo source)
    {
        var value = new DisplayConfigSourceDeviceName
        {
            Header = new DisplayConfigDeviceInfoHeader
            {
                Type = GetSourceName,
                Size = (uint)Marshal.SizeOf<DisplayConfigSourceDeviceName>(),
                AdapterId = source.AdapterId,
                Id = source.Id
            },
            ViewGdiDeviceName = string.Empty
        };

        return DisplayConfigGetSourceInfo(ref value) == ErrorSuccess
            ? Normalize(value.ViewGdiDeviceName)
            : null;
    }

    private static (string? StableId, string? FriendlyName) ReadTargetName(
        DisplayConfigPathTargetInfo target)
    {
        var value = new DisplayConfigTargetDeviceName
        {
            Header = new DisplayConfigDeviceInfoHeader
            {
                Type = GetTargetName,
                Size = (uint)Marshal.SizeOf<DisplayConfigTargetDeviceName>(),
                AdapterId = target.AdapterId,
                Id = target.Id
            },
            MonitorFriendlyDeviceName = string.Empty,
            MonitorDevicePath = string.Empty
        };

        return DisplayConfigGetTargetInfo(ref value) == ErrorSuccess
            ? (Normalize(value.MonitorDevicePath), Normalize(value.MonitorFriendlyDeviceName))
            : default;
    }

    private static (string? StableId, string? FriendlyName) ReadLegacyMonitor(string connector)
    {
        for (uint index = 0; index < 16; index++)
        {
            var value = new DisplayDevice
            {
                Size = (uint)Marshal.SizeOf<DisplayDevice>(),
                DeviceName = string.Empty,
                DeviceString = string.Empty,
                DeviceId = string.Empty,
                DeviceKey = string.Empty
            };
            if (!EnumDisplayDevices(connector, index, ref value, EddGetDeviceInterfaceName))
                break;

            var name = Normalize(value.DeviceString);
            var id = Normalize(value.DeviceId);
            if (name is not null || id is not null)
                return (id, name);
        }

        return default;
    }

    private static string? GetConnectorName(IntPtr monitor)
    {
        var info = new MonitorInfoEx
        {
            Size = (uint)Marshal.SizeOf<MonitorInfoEx>(),
            DeviceName = string.Empty
        };
        return GetMonitorInfo(monitor, ref info) ? Normalize(info.DeviceName) : null;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().TrimEnd('\0');

    private sealed record DisplayPath(
        string ConnectorName,
        string? StableId,
        string? FriendlyName,
        bool IsAvailable);

    private sealed record DisplayCatalog(IReadOnlyList<DisplayPath> Paths, bool AllSourceNamesResolved)
    {
        public static DisplayCatalog Unknown { get; } = new([], false);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Luid
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DisplayConfigPathSourceInfo
    {
        public Luid AdapterId;
        public uint Id;
        public uint ModeInfoIdx;
        public uint StatusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DisplayConfigRational
    {
        public uint Numerator;
        public uint Denominator;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DisplayConfigPathTargetInfo
    {
        public Luid AdapterId;
        public uint Id;
        public uint ModeInfoIdx;
        public uint OutputTechnology;
        public uint Rotation;
        public uint Scaling;
        public DisplayConfigRational RefreshRate;
        public uint ScanLineOrdering;
        public int TargetAvailable;
        public uint StatusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DisplayConfigPathInfo
    {
        public DisplayConfigPathSourceInfo SourceInfo;
        public DisplayConfigPathTargetInfo TargetInfo;
        public uint Flags;
    }

    // DISPLAYCONFIG_MODE_INFO's largest union member is DISPLAYCONFIG_TARGET_MODE (48 bytes).
    // EdgePilot does not inspect the mode table, but QueryDisplayConfig requires a correctly sized
    // output buffer alongside the path table.
    [StructLayout(LayoutKind.Explicit, Size = 64)]
    private struct DisplayConfigModeInfo
    {
        [FieldOffset(0)] public uint InfoType;
        [FieldOffset(4)] public uint Id;
        [FieldOffset(8)] public Luid AdapterId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DisplayConfigDeviceInfoHeader
    {
        public uint Type;
        public uint Size;
        public Luid AdapterId;
        public uint Id;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DisplayConfigSourceDeviceName
    {
        public DisplayConfigDeviceInfoHeader Header;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string ViewGdiDeviceName;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DisplayConfigTargetDeviceName
    {
        public DisplayConfigDeviceInfoHeader Header;
        public uint Flags;
        public uint OutputTechnology;
        public ushort EdidManufactureId;
        public ushort EdidProductCodeId;
        public uint ConnectorInstance;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string MonitorFriendlyDeviceName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string MonitorDevicePath;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DisplayDevice
    {
        public uint Size;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;

        public uint StateFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceId;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfoEx
    {
        public uint Size;
        public NativeRect Monitor;
        public NativeRect WorkArea;
        public uint Flags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    private static extern int GetDisplayConfigBufferSizes(uint flags, out uint pathCount, out uint modeCount);

    [DllImport("user32.dll")]
    private static extern int QueryDisplayConfig(uint flags, ref uint pathCount,
        [Out] DisplayConfigPathInfo[] paths, ref uint modeCount,
        [Out] DisplayConfigModeInfo[] modes, IntPtr currentTopologyId);

    [DllImport("user32.dll", EntryPoint = "DisplayConfigGetDeviceInfo")]
    private static extern int DisplayConfigGetSourceInfo(ref DisplayConfigSourceDeviceName request);

    [DllImport("user32.dll", EntryPoint = "DisplayConfigGetDeviceInfo")]
    private static extern int DisplayConfigGetTargetInfo(ref DisplayConfigTargetDeviceName request);

    [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfoEx info);

    [DllImport("user32.dll", EntryPoint = "EnumDisplayDevicesW", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayDevices(string device, uint deviceIndex,
        ref DisplayDevice displayDevice, uint flags);
}
