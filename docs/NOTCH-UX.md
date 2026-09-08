# Notch behavior and verification

The collapsed pill is a rounded half-capsule. Concave shoulders appear as it expands. One geometry drives both the background silhouette and metric clipping.

A damped spring retains position and velocity when the target changes. Bounded integration handles frame stalls. Metric cells remain stationary during reveal, with interaction gated until sufficiently visible.

Metric cells use 78 DIP spacing along their primary extent and 10 DIP gaps. The stack length adapts to visible metrics. Top/bottom positions use horizontal layouts with upright text; all edges share coordinate mapping for rendering and hit testing.

Pointer exit starts one 450 ms fold timer. Reentry cancels it. Pin prevents folding, and unpin under the pointer waits for exit. Tooltip bridges cover the gap between the notch and detail card.

## Native input safety

`EdgeWindow` intentionally remains larger than the visible notch because the same surface must accommodate spring animation, all four edge orientations and the detail tooltip. Transparency is visual only: the native top-level must also be prevented from receiving pointer input outside EdgePilot's live regions.

The live input model contains only the current notch, the configured hover hot-zone in Hover mode, and the visible tooltip plus its bridge. Hidden mode has no live input region. Windows and Linux consume this same model:

- Windows returns `HTTRANSPARENT` from `WM_NCHITTEST` everywhere outside the live regions.
- Linux X11/XWayland applies the live rectangles to the window's X Shape `ShapeInput` region, updating it during spring motion, tooltip changes, scaling and preference changes.
- If Linux cannot establish a safe X11 input region, EdgePilot fails closed by hiding the edge surface instead of leaving a large transparent topmost rectangle that could block the desktop.

The current Avalonia desktop configuration uses the X11 backend on Linux, including XWayland on Wayland desktops. Avalonia's native Wayland backend is experimental and is not enabled by this project. If a future native Wayland backend is enabled and no equivalent compositor-supported input-region mechanism is available, the same fail-closed rule must be preserved rather than falling back to a rectangular interactive window.

Single-instance protection is separate from pointer routing. The guard is scoped to the current user across OS sessions; a second terminal, desktop launcher or autostart session must activate the existing instance instead of creating another edge surface.

## Automated checks

Run:

```bash
dotnet run --project tests/EdgePilot.UxChecks -c Release
dotnet run --project tests/EdgePilot.InputChecks -c Release
```

The headless suites exercise spring stability and reversals, reveal gating, edge transforms, metric hit targets, tooltip bridges, pin/fold behavior, preferences and the live input-region model on all four edges. They explicitly verify that the transparent center of the larger native window never belongs to EdgePilot's interaction region.

CI additionally launches the packaged Linux build under Xvfb and requires the X11 input region to initialize successfully. The Linux package check launches a second process in a new Unix session to verify that single-instance protection is user-wide rather than session-local.

## Desktop checks

Check rapid entry/exit, tooltip transitions, pin/unpin, right-click Settings, every edge, Hidden/Hover/Always, mixed DPI and monitor changes on Windows and Ubuntu. On Linux, verify browser controls directly behind the transparent parts of the EdgePilot window remain clickable while the hover hot-zone still opens the notch.

Automated headless/Xvfb checks cannot certify compositor behavior on every real X11/XWayland desktop. Native Wayland behavior also requires explicit hands-on validation if that backend is enabled in the future.
