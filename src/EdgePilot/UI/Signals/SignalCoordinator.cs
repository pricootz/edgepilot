using Avalonia.Threading;
using EdgePilot.Core;
using EdgePilot.Core.Monitoring;
using EdgePilot.Core.Signals;
using EdgePilot.Platform;

namespace EdgePilot.UI.Signals;

internal sealed class SignalCoordinator : IDisposable
{
    private readonly EdgeWindow _mainWindow;
    private readonly SignalWindow _signalWindow;
    private readonly SystemMonitorService _monitor = new(new SystemMetricsProvider());
    private readonly NetworkSignalDetector _networkDetector = new();
    private readonly SignalManager _manager = new();
    private readonly CancellationTokenSource _lifetime = new();
    private Task? _monitorTask;
    private Task? _tickTask;
    private bool _started;
    private bool _disposed;
    private bool _restoreMainVisible;
    private volatile bool _enabled;

    public SignalCoordinator(EdgeWindow mainWindow)
    {
        _mainWindow = mainWindow;
        var preferences = mainWindow.Preferences;
        _enabled = preferences.Mode != NotchDisplayMode.Hidden;
        _monitor.RefreshInterval = TimeSpan.FromMilliseconds(preferences.RefreshIntervalMs);
        _signalWindow = new SignalWindow(preferences.Edge);

        _monitor.SnapshotUpdated += OnSnapshotUpdated;
        _monitor.CaptureFailed += OnCaptureFailed;
        _manager.ActiveChanged += OnActiveChanged;
        _mainWindow.PreferencesChanged += OnPreferencesChanged;
        _signalWindow.Collapsed += OnSignalCollapsed;
    }

    public void Start()
    {
        if (_started || _disposed) return;
        _started = true;
        _monitorTask = Task.Run(() => _monitor.RunAsync(_lifetime.Token));
        _tickTask = Task.Run(() => TickAsync(_lifetime.Token));
    }

    private async Task TickAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(200, cancellationToken).ConfigureAwait(false);
                _manager.Tick(DateTimeOffset.UtcNow);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private void OnSnapshotUpdated(SystemSnapshot snapshot)
    {
        var signal = _networkDetector.Observe(snapshot);
        if (signal is null || !_enabled) return;
        _manager.Publish(signal, snapshot.CapturedAt);
    }

    private static void OnCaptureFailed(Exception exception) =>
        System.Diagnostics.Trace.WriteLine($"Signal monitor capture failed: {exception}");

    private void OnActiveChanged(Signal? signal) =>
        Dispatcher.UIThread.Post(() => Present(signal));

    private void Present(Signal? signal)
    {
        if (_disposed) return;

        if (signal is null || !_enabled)
        {
            _signalWindow.HideSignal();
            return;
        }

        if (!_signalWindow.IsVisible)
        {
            _restoreMainVisible = _mainWindow.IsVisible;
            if (_restoreMainVisible) _mainWindow.Hide();
        }

        _signalWindow.ConfigureEdge(_mainWindow.Preferences.Edge);
        _signalWindow.ShowSignal(signal);
    }

    private void OnPreferencesChanged()
    {
        if (_disposed) return;
        var preferences = _mainWindow.Preferences;
        _enabled = preferences.Mode != NotchDisplayMode.Hidden;
        _monitor.RefreshInterval = TimeSpan.FromMilliseconds(preferences.RefreshIntervalMs);
        _signalWindow.ConfigureEdge(preferences.Edge);

        if (!_enabled)
        {
            if (_manager.Active is { } active) _manager.Dismiss(active.Id);
            _restoreMainVisible = false;
            _signalWindow.HideSignal();
            return;
        }

        if (_manager.Active is { } signal && _signalWindow.IsVisible)
            _signalWindow.ShowSignal(signal); // refresh translated copy after language changes
    }

    private void OnSignalCollapsed()
    {
        if (_disposed) return;
        if (_restoreMainVisible && _mainWindow.Preferences.Mode != NotchDisplayMode.Hidden && !_mainWindow.IsVisible)
            _mainWindow.Show();
        _restoreMainVisible = false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _lifetime.Cancel();
        _monitor.SnapshotUpdated -= OnSnapshotUpdated;
        _monitor.CaptureFailed -= OnCaptureFailed;
        _manager.ActiveChanged -= OnActiveChanged;
        _mainWindow.PreferencesChanged -= OnPreferencesChanged;
        _signalWindow.Collapsed -= OnSignalCollapsed;
        _signalWindow.Close();
        _lifetime.Dispose();
    }
}
