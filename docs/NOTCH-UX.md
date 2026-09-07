# Notch behavior and verification

The collapsed pill is a rounded half-capsule. Concave shoulders appear as it expands. One geometry drives both the background silhouette and metric clipping.

A damped spring retains position and velocity when the target changes. Bounded integration handles frame stalls. Metric cells remain stationary during reveal, with interaction gated until sufficiently visible.

Metric cells use 78 DIP spacing along their primary extent and 10 DIP gaps. The stack length adapts to visible metrics. Top/bottom positions use horizontal layouts with upright text; all edges share coordinate mapping for rendering and hit testing.

Pointer exit starts one 450 ms fold timer. Reentry cancels it. Pin prevents folding, and unpin under the pointer waits for exit. Tooltip bridges cover the gap between the notch and detail card.

## Automated checks

Run:

```bash
dotnet run --project tests/EdgePilot.UxChecks -c Release
```

The headless suite exercises spring stability and reversals, reveal gating, edge transforms, metric hit targets, tooltip bridges, pin/fold behavior and preferences. An optional output-directory argument writes synthetic preview images, not real desktop screenshots.

## Desktop checks

Check rapid entry/exit, tooltip transitions, pin/unpin, every edge, mixed DPI and monitor changes on Windows and Ubuntu. Automated headless checks cannot certify compositor transparency, cross-process click-through or flicker in every desktop session.
