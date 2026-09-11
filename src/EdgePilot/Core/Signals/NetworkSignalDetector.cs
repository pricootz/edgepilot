namespace EdgePilot.Core.Signals;

public sealed class NetworkSignalDetector
{
    private bool? _lastConnected;

    public Signal? Observe(SystemSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var connected = snapshot.Network.Connected;

        if (_lastConnected is null)
        {
            _lastConnected = connected;
            return null;
        }

        if (_lastConnected.Value == connected) return null;
        _lastConnected = connected;

        return connected
            ? new Signal(
                "network.restored",
                SignalSource.Network,
                SignalSeverity.Success,
                "tooltip.network",
                "network.connected",
                snapshot.CapturedAt,
                TimeSpan.FromSeconds(3),
                "network.connectivity")
            : new Signal(
                "network.lost",
                SignalSource.Network,
                SignalSeverity.Warning,
                "tooltip.network",
                "network.disconnected",
                snapshot.CapturedAt,
                TimeSpan.FromSeconds(7),
                "network.connectivity");
    }

    public void Reset() => _lastConnected = null;
}
