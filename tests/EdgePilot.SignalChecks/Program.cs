using EdgePilot.Core;
using EdgePilot.Core.Signals;

var passed = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    Console.WriteLine("PASS " + name);
    passed++;
}

static SystemSnapshot Snapshot(bool connected, DateTimeOffset at) => new(
    "test-host",
    "test-os",
    10,
    16_000_000_000,
    8_000_000_000,
    TimeSpan.FromHours(1),
    new NetworkSnapshot("test0", connected, 0, 0, 1_000_000_000),
    Array.Empty<DriveSnapshot>(),
    at);

var t0 = new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);
var detector = new NetworkSignalDetector();
Check(detector.Observe(Snapshot(true, t0)) is null, "network detector establishes a silent baseline");

var lost = detector.Observe(Snapshot(false, t0.AddSeconds(1)));
Check(lost is not null, "network loss creates a signal");
Check(lost!.Id == "network.lost", "network loss id");
Check(lost.Severity == SignalSeverity.Warning, "network loss severity");
Check(lost.DedupeKey == "network.connectivity", "network connectivity dedupe key");
Check(detector.Observe(Snapshot(false, t0.AddSeconds(2))) is null, "unchanged offline state is silent");

var restored = detector.Observe(Snapshot(true, t0.AddSeconds(3)));
Check(restored is not null, "network restore creates a signal");
Check(restored!.Id == "network.restored", "network restore id");
Check(restored.Severity == SignalSeverity.Success, "network restore severity");

var manager = new SignalManager();
var changes = new List<string?>();
manager.ActiveChanged += signal => changes.Add(signal?.Id);
manager.Publish(lost, t0.AddSeconds(1));
Check(manager.Active?.Id == "network.lost", "first signal becomes active");
manager.Publish(restored, t0.AddSeconds(3));
Check(manager.Active?.Id == "network.restored", "same dedupe key replaces active signal regardless of severity");
Check(manager.PendingCount == 0, "dedupe replacement does not queue stale connectivity state");

var info = new Signal(
    "system.info", SignalSource.System, SignalSeverity.Info,
    "signal.test.title", "signal.test.message", t0, TimeSpan.FromSeconds(30));
var critical = new Signal(
    "storage.critical", SignalSource.Storage, SignalSeverity.Critical,
    "signal.test.title", "signal.test.message", t0.AddSeconds(1), TimeSpan.FromSeconds(5));
var warning = new Signal(
    "performance.warning", SignalSource.Performance, SignalSeverity.Warning,
    "signal.test.title", "signal.test.message", t0.AddSeconds(2), TimeSpan.FromSeconds(20));

var priority = new SignalManager();
priority.Publish(info, t0);
priority.Publish(critical, t0.AddSeconds(1));
Check(priority.Active?.Id == "storage.critical", "higher severity preempts active signal");
priority.Publish(warning, t0.AddSeconds(2));
Check(priority.Active?.Id == "storage.critical", "lower severity waits behind critical signal");
Check(priority.PendingCount == 1, "lower severity signal is queued");
priority.Tick(t0.AddSeconds(7));
Check(priority.Active?.Id == "performance.warning", "highest pending signal is promoted after expiry");
priority.Tick(t0.AddSeconds(23));
Check(priority.Active is null, "expired final signal clears active state");

var dismiss = new SignalManager();
dismiss.Publish(info, t0);
dismiss.Publish(warning, t0.AddSeconds(2));
dismiss.Dismiss("system.info", t0.AddSeconds(3));
Check(dismiss.Active?.Id == "performance.warning", "dismiss promotes the next queued signal");

Check(changes.SequenceEqual(new[] { "network.lost", "network.restored" }), "active-change notifications are deterministic");

Console.WriteLine($"{passed} signal checks passed.");
