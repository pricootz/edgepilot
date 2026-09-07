using System.Net.NetworkInformation;

namespace EdgePilot.Platform;

internal sealed class NetworkSampler
{
    private string? _lastInterfaceId;
    private long _lastReceive;
    private long _lastSend;
    private DateTimeOffset _lastAt;

    public EdgePilot.Core.NetworkSnapshot Capture()
    {
        var candidates = NetworkInterface.GetAllNetworkInterfaces()
            .Where(static nic => nic.OperationalStatus == OperationalStatus.Up)
            .Where(static nic => nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .OrderByDescending(HasGateway)
            .ThenByDescending(SafeSpeed)
            .ToArray();

        var selected = candidates.FirstOrDefault();
        if (selected is null)
        {
            Reset();
            return new EdgePilot.Core.NetworkSnapshot("Offline", false, 0, 0, 0);
        }

        try
        {
            var stats = selected.GetIPStatistics();
            var now = DateTimeOffset.UtcNow;
            var receive = stats.BytesReceived;
            var send = stats.BytesSent;
            long receiveRate = 0;
            long sendRate = 0;

            if (_lastInterfaceId == selected.Id && _lastAt != default)
            {
                var seconds = (now - _lastAt).TotalSeconds;
                if (seconds > 0)
                {
                    receiveRate = Math.Max(0, (long)((receive - _lastReceive) / seconds));
                    sendRate = Math.Max(0, (long)((send - _lastSend) / seconds));
                }
            }

            _lastInterfaceId = selected.Id;
            _lastReceive = receive;
            _lastSend = send;
            _lastAt = now;

            return new EdgePilot.Core.NetworkSnapshot(
                selected.Name,
                true,
                receiveRate,
                sendRate,
                Math.Max(0, SafeSpeed(selected)));
        }
        catch
        {
            Reset();
            return new EdgePilot.Core.NetworkSnapshot(selected.Name, true, 0, 0, Math.Max(0, SafeSpeed(selected)));
        }
    }

    private static bool HasGateway(NetworkInterface nic)
    {
        try { return nic.GetIPProperties().GatewayAddresses.Count > 0; }
        catch { return false; }
    }

    private static long SafeSpeed(NetworkInterface nic)
    {
        try { return nic.Speed; }
        catch { return 0; }
    }

    private void Reset()
    {
        _lastInterfaceId = null;
        _lastReceive = 0;
        _lastSend = 0;
        _lastAt = default;
    }
}
