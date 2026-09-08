namespace EdgePilot.Core.Signals;

public sealed class SignalManager
{
    private readonly object _gate = new();
    private readonly List<Signal> _pending = [];
    private Signal? _active;

    public event Action<Signal?>? ActiveChanged;

    public Signal? Active
    {
        get
        {
            lock (_gate) return _active;
        }
    }

    public int PendingCount
    {
        get
        {
            lock (_gate) return _pending.Count;
        }
    }

    public void Publish(Signal signal, DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(signal);
        var timestamp = now ?? DateTimeOffset.UtcNow;
        Signal? changedTo = null;
        var notify = false;

        lock (_gate)
        {
            RemoveExpiredPending(timestamp);
            if (signal.ExpiresAt <= timestamp) return;

            if (_active is null || _active.ExpiresAt <= timestamp)
            {
                _active = signal;
                changedTo = signal;
                notify = true;
            }
            else if (SameDedupeKey(_active, signal))
            {
                _active = signal;
                changedTo = signal;
                notify = true;
            }
            else if (signal.Severity > _active.Severity)
            {
                _active = signal;
                changedTo = signal;
                notify = true;
            }
            else
            {
                _pending.RemoveAll(candidate => SameDedupeKey(candidate, signal));
                _pending.Add(signal);
                SortPending();
            }
        }

        if (notify) ActiveChanged?.Invoke(changedTo);
    }

    public void Tick(DateTimeOffset? now = null)
    {
        var timestamp = now ?? DateTimeOffset.UtcNow;
        Signal? changedTo = null;
        var notify = false;

        lock (_gate)
        {
            RemoveExpiredPending(timestamp);
            if (_active is not null && _active.ExpiresAt > timestamp) return;

            changedTo = PromotePending(timestamp);
            if (!Equals(_active, changedTo))
            {
                _active = changedTo;
                notify = true;
            }
            else if (_active is not null)
            {
                _active = changedTo;
                notify = true;
            }
        }

        if (notify) ActiveChanged?.Invoke(changedTo);
    }

    public void Dismiss(string id, DateTimeOffset? now = null)
    {
        if (string.IsNullOrWhiteSpace(id)) return;
        var timestamp = now ?? DateTimeOffset.UtcNow;
        Signal? changedTo = null;
        var notify = false;

        lock (_gate)
        {
            RemoveExpiredPending(timestamp);
            _pending.RemoveAll(signal => signal.Id.Equals(id, StringComparison.Ordinal));

            if (_active is null || !_active.Id.Equals(id, StringComparison.Ordinal)) return;

            changedTo = PromotePending(timestamp);
            _active = changedTo;
            notify = true;
        }

        if (notify) ActiveChanged?.Invoke(changedTo);
    }

    private Signal? PromotePending(DateTimeOffset now)
    {
        RemoveExpiredPending(now);
        if (_pending.Count == 0) return null;
        var next = _pending[0];
        _pending.RemoveAt(0);
        return next;
    }

    private void RemoveExpiredPending(DateTimeOffset now) =>
        _pending.RemoveAll(signal => signal.ExpiresAt <= now);

    private void SortPending() => _pending.Sort((left, right) =>
    {
        var severity = right.Severity.CompareTo(left.Severity);
        return severity != 0 ? severity : left.CreatedAt.CompareTo(right.CreatedAt);
    });

    private static bool SameDedupeKey(Signal left, Signal right) =>
        left.DedupeKey.Equals(right.DedupeKey, StringComparison.Ordinal);
}
