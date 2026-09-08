namespace EdgePilot.Core.Monitoring;

public sealed class SystemMonitorService
{
    private readonly ISystemMetricsProvider _provider;
    private int _intervalMs = 1000;
    public TimeSpan RefreshInterval
    {
        get => TimeSpan.FromMilliseconds(Volatile.Read(ref _intervalMs));
        set
        {
            if (value.TotalMilliseconds < 100 || value.TotalMilliseconds > 60000)
                throw new ArgumentOutOfRangeException(nameof(value));
            Volatile.Write(ref _intervalMs, (int)value.TotalMilliseconds);
        }
    }

    public SystemMonitorService(ISystemMetricsProvider provider, TimeSpan? interval = null)
    {
        _provider = provider;
        RefreshInterval = interval ?? TimeSpan.FromSeconds(1);
    }

    public event Action<SystemSnapshot>? SnapshotUpdated;
    public event Action<Exception>? CaptureFailed;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var snapshot = _provider.Capture();
                SnapshotUpdated?.Invoke(snapshot);
            }
            catch (Exception ex)
            {
                CaptureFailed?.Invoke(ex);
            }

            try
            {
                await Task.Delay(RefreshInterval, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
