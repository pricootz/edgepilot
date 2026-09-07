# EdgePilot architecture

## Reference lessons from Codenotch

`pricootz/codenotch-reference` is a design and architecture reference, not the EdgePilot codebase.

The first EdgePilot version intentionally carries forward these ideas:

1. **Placement is its own concern.** Codenotch maps an edge-independent coordinate model to the real screen only in its placement layer. EdgePilot starts with the same separation in `UI/EdgePlacement.cs`.
2. **The shell owns interaction state.** Expand/fold/pin behavior is independent from the data source.
3. **Polling is centralized.** Providers do not create their own UI timers. `SystemMonitorService` controls cadence and prevents the UI from owning sampling logic.
4. **Failures degrade visibly.** A capture exception is surfaced to the shell while the app remains alive.
5. **Use the working area.** Placement uses the OS usable screen rectangle rather than blindly covering the taskbar/dock.

## v0.1 layers

```text
UI
  EdgeWindow
  EdgePlacement
  DisplayFormat

Core
  SystemSnapshot
  ISystemMetricsProvider
  SystemMonitorService

Platform
  SystemMetricsProvider
  CpuUsageReader
  MemoryReader
  NetworkSampler
```

### Core

Contains data contracts and orchestration that do not know about Avalonia, Windows APIs, Linux `/proc`, or any future module UI.

### Platform

Contains the OS-facing implementation. Windows CPU/RAM use Win32 APIs; Linux CPU/RAM read `/proc`; storage and network use cross-platform .NET APIs where practical.

### UI

Owns the screen-edge window, hover/pin behavior, rendering, placement and display formatting.

## Next architectural step

After v0.1 is stable, generalize `ISystemMetricsProvider` into the first implementation of an EdgePilot module contract:

```text
Module
  id
  status
  summary
  details
  actions
  events
  settings
```

Do not build that abstraction before the local system module gives us real requirements.
