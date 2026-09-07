# Notch UX refinement — 2026-09-07

Branch: feat/v0.1-system-monitor. Scope: existing notch UX only.

## Changes
- Preserve the compact 10 × 82 DIP pill, its edge anchor and fixed window. Expanded silhouette is 88 × 408 DIP (previously 430 long), with 20 DIP body corners and 24 DIP flares instead of 22/34.
- Four explicit 78 DIP metric cells, 10 DIP gaps, centered as one 342 DIP stack. Hit targets and tooltip anchors use the same dimensions. Ring track and progress now share a radius and stroke width.
- Replace restarted easing tasks with a damped spring driven on the UI thread. Retargeting retains velocity and position; a monotonic clock and bounded integration handle frame stalls.
- Clip stationary metric cells using the very same geometry as the silhouette, removing their sideways translation during reveal. Metrics remain inactive until sufficiently revealed.
- Replace asynchronous delayed-fold callbacks with one UI timer (450 ms). Reentry cancels it immediately, pin prevents fold, and unpin under the pointer stays open until exit.
- Route polling and Avalonia pointer events through one handler. PointerExited arms folding on Linux as well as Windows; a transparent canvas makes the pointer-event fallback reachable.
- Preserve the tooltip through the full gap between card and notch and through metric spacing. Cards have 14 DIP corners, wrap all existing details and measure their height for positioning.
- Stop motion/fold timers on close; only unsubscribe screen notifications after subscribing.

No changes to metric providers, refresh, CPU/RAM/storage/network information, host/OS or uptime. No new settings, modules or dependencies in the application. Avalonia.Headless is test-only.

## Verification
- Release build on Windows: zero warnings/errors.
- Release cross-build targeting linux-x64: zero warnings/errors. This is not execution on Ubuntu.
- Executable Avalonia Headless regression suite: 1,235 assertions, including spring stability at 30/60/144 Hz and frame stalls, interrupted motion, hover reentry, pinned folding, all four metric hit targets, tooltip bridge retention, reveal gating and shared centered clip geometry.
- CI runs these checks on its existing Windows/Ubuntu matrix.
- Rendered closed, partial and open previews; inspected the expanded layout.
- git diff --check.

Reproduce:
    dotnet build src/EdgePilot/EdgePilot.csproj -c Release
    dotnet build src/EdgePilot/EdgePilot.csproj -c Release -r linux-x64 --self-contained false
    dotnet run --project tests/EdgePilot.UxChecks -c Release

An optional directory argument to the checks saves three PNG previews; these use headless placeholder data.

## Desktop verification still required
Headless checks do not certify native transparency, cross-process click-through, real pointer timing or compositor flicker. Check rapid entry/exit, pin/unpin, moving to each tooltip, display scaling and monitor changes on Windows and Ubuntu (X11/Wayland). The existing edge-placement selector is preserved; the pre-existing right-oriented silhouette/layout for other edges is not redesigned by this patch.

Changes are local; no commit or push was performed.
