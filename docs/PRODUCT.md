# EdgePilot product definition

## Product sentence

EdgePilot is a small desktop control surface attached to a screen edge. It keeps useful machine state visible without requiring a dashboard window, expands only when the user reaches for it, and later becomes a host for additional modules and actions.

## v0.1 goal

Prove the desktop shell and the first provider using data that already exists on the local machine.

### Included

- Windows and Linux desktop target.
- Borderless, topmost edge-attached window.
- Compact collapsed state and expanded detail state.
- Hover to expand, delayed fold, optional pin-open.
- CPU usage.
- Physical memory usage.
- Fixed-drive free/used capacity.
- Active network interface and live receive/send rate.
- System uptime, host name and OS description.
- 1 second sampling interval.
- Working-area aware placement so the notch avoids taskbars/docks.
- Four edge placement primitives. Right edge is the default; `EDGEPILOT_EDGE=left|top|bottom|right` is available for development/testing.

### Deliberately excluded

- Server monitoring.
- Docker/Tailscale.
- Clipboard history.
- Command palette.
- AUC and school modules.
- AI agents.
- Temperatures/fan sensors.
- Autostart and installer.
- Full settings UI.

Those are follow-up milestones after the shell behaves correctly on both operating systems.

## Interaction contract

1. At rest EdgePilot occupies only a small strip at the selected screen edge.
2. Pointer entry expands immediately.
3. Pointer exit folds after a short grace period so the UI does not feel twitchy.
4. Pin keeps the expanded view open.
5. The window remains attached to the usable work area when screen configuration or scaling changes.
6. A provider failure is shown as degraded state; it must never crash the shell.
