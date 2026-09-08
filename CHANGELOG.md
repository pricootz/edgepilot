# Changelog

## 0.2.0-preview.1 — Unreleased

The v0.2 preview turns the initial system-monitor settings panel into a more coherent EdgePilot product surface and establishes localization as a first-class capability.

### Added

- Responsive Settings shell with dedicated Edge, Monitor, Behavior, Startup and About sections.
- Interactive metric cards with contextual configuration; the disk-volume selector is hidden when Disk is disabled.
- Italian, English and French interface localization with automatic system-language detection and a persisted manual override.
- Localized tray, tooltips, installer/autostart messages, drive labels and metric captions.
- Canonical SVG rendering inside Settings and About, while retaining a generated native ICO for Windows integration.
- Product-focused About page with author attribution, GitHub repository/profile links and local-first principles.
- Dedicated localization regression suite running on Windows and Ubuntu CI.

### Changed

- Settings code is split into shell/state, page composition and reusable controls to prepare for future modules such as Signals.
- Settings save/reset state, labels, descriptions, icons and responsive behavior were redesigned without removing existing preferences.
- Product positioning now describes EdgePilot as an edge-native desktop surface; the System monitor is the first module rather than the whole product.

### Contributors

- IT / EN / FR localization foundation contributed by [@IamArayel](https://github.com/IamArayel) in [PR #5](https://github.com/pricootz/edgepilot/pull/5), then reconciled with the v0.2 Settings redesign.

## 0.1.0-preview.1

Initial public preview foundation.

- Local CPU, memory, disk and network monitoring on Windows and Linux.
- Four-edge notch with rounded collapsed geometry, spring motion, reveal clipping, tooltips and pin/hover/fold behavior.
- Persistent display mode, metric visibility, refresh rate, hover sensitivity and disk selection.
- Linux system theme integration for settings.
- Tray menu, single-instance activation and optional start at login.
- Self-contained x64 archives and per-user installation.
- Windows/Ubuntu CI builds, headless UX regression checks and package launch/install checks.
- EdgePilot branding and canonical SVG asset workflow.
