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
                "signal.network.title",
                "signal.network.restored",
                snapshot.CapturedAt,
                TimeSpan.FromSeconds(3),
                "network.connectivity")
            : new Signal(
                "network.lost",
                SignalSource.Network,
                SignalSeverity.Warning,
                "signal.network.title",
                "signal.network.lost",
                snapshot.CapturedAt,
                TimeSpan.FromSeconds(7),
                "network.connectivity");
    }

    public void Reset() => _lastConnected = null;
}
