# Product scope

EdgePilot is an edge-native desktop surface for Windows and Linux. The current `main` preview starts with local system monitoring, but the product direction is broader: surface useful state and actions from the screen edge only when they matter.

## Included today

Windows and Linux desktop support; CPU, physical memory, disk capacity, network throughput and interface details; uptime and machine identity; four-edge placement; hover expansion, delayed fold, pinning and tooltips; persistent settings; disk selection; tray integration; optional start at login; per-user installation.

The v0.2 product surface also includes a responsive Settings experience with General, Edge, Monitor, Behavior, Startup and About sections, automatic/manual language selection, and System/Light/Dark Settings appearance.

Current `main` ships **English, Italian, French and Spanish** through embedded JSON locale catalogs. New languages are designed to be added as one locale file without changing Settings architecture or a closed C# language enum.

Refresh intervals are 0.5, 1, 2 or 5 seconds. Repository documentation uses English.

## Interaction contract

1. The collapsed pill occupies a narrow strip at the selected edge of the usable working area.
2. Pointer entry expands it; pointer exit folds after a 450 ms grace period.
3. Pinning retains the expanded view; moving into the tooltip must not fold the notch.
4. Always-open and hidden modes are available independently of pointer interaction.
5. Reopening the executable recovers settings through the running instance.
6. Missing readings and missing selected volumes degrade visibly without inventing replacement data.
7. Language and Settings appearance are persisted independently from the notch's dedicated visual style.

## Product direction

The next major direction is **Signals / ambient awareness** following the model:

**Observe → Decide → Surface → Act**

Signals are intended to be temporary, deduplicated and context-aware rather than another notification center. The first planned cases are Internet lost/restored, low disk space and sustained unusual CPU activity.

Signals are being developed separately and are not part of the current `main` product behavior yet.

## Not current scope

Remote servers, Docker, clipboard tools, VPN/Tailscale, removable-device workflows and quick actions are future ideas, not shipped features. Selectable multi-monitor targeting and per-display EdgePilot surfaces are not implemented. GPU, temperature and fan sensors are not part of this preview. macOS and ARM packages are not current supported targets.
