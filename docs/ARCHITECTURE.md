# Architecture

EdgePilot uses C#/.NET 10 and Avalonia. The UI and platform sampling are separated so the shell does not depend on one operating system's APIs.

## Core

`SystemSnapshot` holds immutable sampled data. `ISystemMetricsProvider` describes capture; `SystemMonitorService` centralizes refresh and error delivery.

Localization is also core application state. `Language` is an extensible locale-code value object rather than a closed enum, and `Localization` discovers embedded JSON catalogs from `src/EdgePilot/Assets/Locales`. Resolution follows exact locale -> neutral locale -> English -> key, while selected culture controls formatting.

## Platform

`SystemMetricsProvider` combines CPU, memory, drive and network readers. Windows CPU/RAM use Win32 APIs; Linux uses `/proc`. `DriveInfo` and network APIs provide volume capacity and throughput sampling.

`AutostartRegistration` manages current-user startup entries. `DesktopInstaller` copies packages into a stable per-user directory and creates a launcher. `SingleInstance` owns the per-user mutex and activation pipe.

Linux desktop-entry comments are generated for every shipped locale so desktop metadata remains consistent with the application language set.

## UI

`EdgeWindow` owns interaction state and rendering. `EdgeNotchGeometry` supplies the shared silhouette and clip; `NotchLayout` maps geometry and hit targets to all four edges while preserving upright text. `NotchSpring` retains motion continuity when a transition reverses. `MetricRing` and `DisplayFormat` present snapshots.

`DisplayTargetResolver` separates saved display intent from Avalonia's live screen objects. Automatic mode follows the primary display. Explicit choices are reconciled in order by session handle, unique friendly name, then working-area geometry and scaling. If no match exists, placement temporarily uses the primary display while preserving the saved target for a later reconnect. Screen-collection change bursts are debounced before relocation and Settings refresh.

`SettingsWindow` owns Settings state and navigation. Page composition lives under `UI/Settings`, keeping General, Edge, Monitor, Behavior, Startup and About separate from reusable controls.

`NotchPreferences` stores persisted interface state including edge, display mode, selected display, visible metrics, refresh, hover sensitivity, selected disk, language and Settings theme. `PreferenceStore` saves atomically; the nullable display target keeps v0.2 settings compatible and means Automatic.

Language choices are populated from the embedded locale catalogs, so shipping a new translation normally means adding one JSON file rather than changing Settings or introducing a new language enum.

## Current localization assets

Current `main` ships:

- `en.json` — English
- `it.json` — Italiano
- `fr.json` — Français
- `es.json` — Español

`en.json` is the canonical key set. Repository validation requires every shipped locale to match it key-for-key and checks that source localization keys exist.

## Boundaries

- Sampling does not create UI timers.
- Platform details remain outside Core.
- Placement uses the OS working area.
- Display fallback must never erase an unavailable explicit target.
- Failure should leave settings and recovery accessible.
- Localization catalogs contain text; application behavior remains in code.
- New languages should not require a closed language enum or Settings rewrite.
- Extend a module contract only when another real module establishes its requirements.

## Verification

The executable headless suites cover geometry, motion, interaction, settings, disk selection and localization behavior. CI adds repository/privacy validation, native packaged launch, second-instance activation and per-user installation on disposable Windows and Ubuntu runners. Compositor behavior and actual login sessions require hands-on testing.

Signals / ambient awareness continue on a separate draft branch until validated; selectable display targeting is the first isolated v0.3 foundation developed from current `main`.

The edge-attached interaction was informed by prior notch-style UI exploration. The v0.3 display reconciliation strategy is adapted from Edge-Drop under Apache-2.0 and modified for Avalonia, four-edge placement and persisted reconnect behavior; packaging retains the corresponding notice and license. EdgePilot remains MIT-licensed.
