# Architecture

EdgePilot uses C#/.NET 10 and Avalonia. UI, platform sampling, localization and ambient Signal decisions are separated so the desktop shell does not depend on one operating system's APIs or one presentation path.

## Core monitoring

`SystemSnapshot` holds immutable sampled data. `ISystemMetricsProvider` describes capture; `SystemMonitorService` centralizes refresh and error delivery.

The System module currently supplies CPU, memory, drive and network state to the edge UI. Other modules should consume normalized state rather than reaching directly into platform readers from UI code.

## Signals

Signals follow the product model **Observe → Decide → Surface → Act**.

`Core/Signals` contains the presentation-independent model:

- `Signal` is immutable and carries identity, source, severity, localization keys, creation time, duration and an optional deduplication group.
- `SignalSeverity` defines the priority vocabulary used by the manager.
- `SignalSource` identifies the originating subsystem.
- `SignalManager` owns active/pending lifecycle, expiry, deduplication, priority and preemption.
- `NetworkSignalDetector` observes `SystemSnapshot` and emits only on connectivity transitions. Its first observation establishes baseline state and intentionally emits nothing.

The current network implementation is the first concrete detector. Low-disk and sustained-CPU detectors should reuse the same normalized Signal contract instead of adding presentation-specific branches.

## Platform

`SystemMetricsProvider` combines CPU, memory, drive and network readers. Windows CPU/RAM use Win32 APIs; Linux uses `/proc`. `DriveInfo` and network APIs provide volume capacity and throughput sampling.

`AutostartRegistration` manages current-user startup entries. `DesktopInstaller` copies packages into a stable per-user directory and creates a launcher. `SingleInstance` owns the per-user mutex and activation pipe.

Platform-specific behavior should remain under `Platform` unless it is purely window-system integration required by an Avalonia surface.

## Localization

Interface catalogs live under `src/EdgePilot/Assets/Locales` and are embedded as JSON resources. `Localization` discovers shipped locales, resolves exact/neutral/fallback language codes and applies the selected culture for formatting.

English is the canonical key set. Repository validation requires every shipped locale to have key parity with `en.json`, which allows new languages to be contributed as data rather than by adding language-specific C# branches.

Legacy v0.2 enum-style language values are normalized by the preference layer so existing settings remain usable.

## UI

`EdgeWindow` owns the main edge interaction state and System-module rendering. `EdgeNotchGeometry` supplies the shared silhouette and clip; `NotchLayout` maps geometry and hit targets to all four edges while preserving upright text. `NotchSpring` retains motion continuity when a transition reverses. `MetricRing` and `DisplayFormat` present snapshots.

`SettingsWindow` edits `NotchPreferences`, saved atomically by `PreferenceStore`. `DriveSelection` resolves the explicit mount path without a silent substitute.

Signal presentation is isolated under `UI/Signals`:

- `SignalCoordinator` connects live snapshots/detectors to the Signal lifecycle.
- `SignalPresenter` converts the active normalized Signal into localized presentation state.
- `SignalWindow` renders the transient edge surface, handles edge orientation and remains non-activating/click-through.

The current development approach uses a separate transient Signal surface that temporarily replaces the normal edge presentation and then restores the previous main-window state. This is intentionally still under visual review; the core Signal contract does not depend on keeping that exact window strategy.

## Boundaries

- Sampling does not create UI timers.
- Detectors create normalized Signals, not Avalonia controls.
- `SignalManager` does not know how a Signal is rendered.
- Platform details remain outside Core.
- Placement uses the OS working area.
- Failure should leave settings and recovery accessible.
- Signal expiry must restore the user's prior EdgePilot state rather than inventing a new state.
- Extend a module contract only when another real module establishes its requirements.

## Verification

The executable headless suites cover geometry, motion, interaction, settings, disk selection, localization and Signal lifecycle behavior. CI adds Signal-surface smoke checks plus native packaged launch, second-instance activation and per-user installation on disposable Windows and Ubuntu runners.

Automated runners can verify lifecycle and basic surface construction, but compositor transparency, click-through behavior, physical multi-monitor placement and real connectivity transitions still require hands-on desktop testing.

The edge-attached interaction was informed by prior notch-style UI exploration. EdgePilot is maintained as its own codebase.
