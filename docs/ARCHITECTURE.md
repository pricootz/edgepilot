# Architecture

EdgePilot uses C#/.NET 10 and Avalonia. The UI and platform sampling are separated so the shell does not depend on one operating system's APIs.

## Core

SystemSnapshot holds immutable sampled data. ISystemMetricsProvider describes capture; SystemMonitorService centralizes refresh and error delivery.

## Platform

SystemMetricsProvider combines CPU, memory, drive and network readers. Windows CPU/RAM use Win32 APIs; Linux uses /proc. DriveInfo and network APIs provide volume capacity and throughput sampling.

AutostartRegistration manages current-user startup entries. DesktopInstaller copies packages into a stable per-user directory and creates a launcher. SingleInstance owns the per-user mutex and activation pipe.

## UI

EdgeWindow owns interaction state and rendering. EdgeNotchGeometry supplies the shared silhouette and clip; NotchLayout maps geometry and hit targets to all four edges while preserving upright text. NotchSpring retains motion continuity when a transition reverses. MetricRing and DisplayFormat present snapshots.

SettingsWindow edits NotchPreferences, saved atomically by PreferenceStore. DriveSelection resolves the explicit mount path without a silent substitute.

## Boundaries

- Sampling does not create UI timers.
- Platform details remain outside Core.
- Placement uses the OS working area.
- Failure should leave settings and recovery accessible.
- Extend a module contract only when another real module establishes its requirements.

## Verification

The executable headless suite covers geometry, motion, interaction, settings and disk selection. CI adds native packaged launch, second-instance activation and per-user installation on disposable runners. Compositor behavior and actual login sessions require hands-on testing.

The edge-attached interaction was informed by prior notch-style UI exploration. EdgePilot is maintained as its own codebase.
