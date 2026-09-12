# Notch behavior and verification

The collapsed pill is a rounded half-capsule. Concave shoulders appear as it expands. One geometry drives both the background silhouette and metric clipping.

A damped spring retains position and velocity when the target changes. Bounded integration handles frame stalls. Metric cells remain stationary during reveal, with interaction gated until sufficiently visible.

Metric cells use 76 DIP along their primary extent and 10 DIP gaps. The stack length adapts to visible metrics. Top/bottom positions use horizontal layouts with upright text; all edges share coordinate mapping for rendering and hit testing.

Pointer exit starts one 450 ms fold timer. Reentry cancels it. Pin prevents folding, and unpin under the pointer waits for exit. Tooltip bridges cover the gap between the notch and detail card.

## Native input safety

`EdgeWindow` intentionally remains larger than the visible notch because the same surface must accommodate spring animation, all four edge orientations and the detail tooltip. Transparency is visual only: the native top-level must also be prevented from receiving pointer input outside EdgePilot's live regions.

The logical live-input model contains only the current notch silhouette, the configured Hover hot-zone in Hover mode, and the visible tooltip plus its bridge. Hidden mode has no live input region. The curved/concave notch is represented by conservative one-DIP scanline strips rather than its rectangular bounds, so transparent notch corners and shoulders are not accidentally promoted to native input.

Windows and Linux enforce that safety with platform-specific paths:

- Windows makes the visible `EdgeWindow` fully click-through and also constrains that render-only HWND to a close-fitting region with a two-DIP antialiasing bleed. A separate invisible overlay uses `SetWindowRgn` for the notch silhouette inset two DIPs from its contour and for the rounded tooltip; it validates native-handle changes and reapplies at the overlay's own settled DPI. Global cursor polling handles the transparent Hover hot-zone and tooltip bridge without intercepting the desktop.
- Linux X11/XWayland applies the full logical live-input model to the visible window's X Shape `ShapeInput` region and updates it during spring motion, tooltip changes, scaling and preference changes.
- Logical strip boundaries are quantized to the nearest native pixel. Adjacent strips therefore share a boundary even at fractional DPI instead of creating one-pixel seams through the animated notch.
- Windows reads the overlay region back once and verifies it; Linux sends a checked XCB SHAPE request so native package smoke tests fail if the display server rejects the region.
- If Windows or Linux cannot establish its safe native region, EdgePilot fails closed by hiding the edge surface instead of leaving a large transparent topmost rectangle that could block the desktop.

The intentional Hover hot-zone is the small transparent edge area used to wake the collapsed notch. Linux includes it in `ShapeInput`; Windows observes it through polling, so it never becomes an invisible input blocker. Outside the native live regions, the large layout surface must never receive pointer input.

The current Avalonia desktop configuration uses the X11 backend on Linux, including XWayland on Wayland desktops. The XCB Shape binding requires `libxcb-shape.so.0` (`libxcb-shape0` on Debian/Ubuntu/Parrot), which the package installer checks. Avalonia's native Wayland backend is experimental and is not enabled by this project. If a future native Wayland backend is enabled and no equivalent compositor-supported input-region mechanism is available, the same fail-closed rule must be preserved rather than falling back to a rectangular interactive window.

Single-instance protection is separate from pointer routing. The guard is scoped to the current user across OS sessions; a second terminal, desktop launcher or autostart session must activate the existing instance instead of creating another edge surface.

## Automated checks

Run:

```bash
dotnet run --project tests/EdgePilot.UxChecks -c Release
dotnet run --project tests/EdgePilot.InputChecks -c Release
```

The headless suites exercise spring stability and reversals, reveal gating, edge transforms, metric hit targets, tooltip bridges, pin/fold behavior, preferences and the live input-region model on all four edges. They explicitly verify that the transparent center of the larger native window and points inside the old rectangular notch bounds but outside the visible shoulder never belong to EdgePilot's interaction region.

CI launches packaged builds on both platforms and requires the native input-region guard to initialize successfully. Ubuntu performs the X11 check under Xvfb with the declared XCB Shape runtime dependency. The Linux package check also launches a second process in a new Unix session to verify that single-instance protection is user-wide rather than session-local.

## Desktop checks

Check rapid entry/exit, tooltip transitions, pin/unpin, right-click Settings, every edge, Hidden/Hover/Always, mixed DPI and monitor changes on Windows and Ubuntu. On both platforms, place browser controls directly behind transparent portions of the larger EdgePilot layout surface and verify they remain clickable while the configured Hover hot-zone still opens the notch.

Automated package/Xvfb checks cannot certify compositor/window-manager behavior on every real desktop. Real Windows and Linux X11/XWayland validation is required before #16 is considered resolved. Native Wayland behavior also requires explicit hands-on validation if that backend is enabled in the future.
