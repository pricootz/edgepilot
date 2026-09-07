using EdgePilot.Core;

namespace EdgePilot.Core.Monitoring;

public sealed class SystemMonitorService
{
    private readonly ISystemMetricsProvider _provider;
    private readonly TimeSpan _interval;

    public SystemMonitorService(ISystemMetricsProvider provider, TimeSpan? interval = null)
    {
        _provider = provider;
        _interval = interval ?? TimeSpan.FromSeconds(1);
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
                await Task.Delay(_interval, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
