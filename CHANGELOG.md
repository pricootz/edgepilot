# Changelog

## 0.2.0-preview.1 — Unreleased

The v0.2 preview turns the initial system-monitor settings panel into a more coherent EdgePilot product surface and establishes localization as a first-class capability.

### Added

- Responsive Settings shell with dedicated General, Edge, Monitor, Behavior, Startup and About sections.
- General page for interface language and Settings appearance (`System`, `Light`, `Dark`).
- Interactive metric cards with contextual configuration; the disk-volume selector is hidden when Disk is disabled.
- Italian, English and French interface localization with automatic system-language detection and a persisted manual override.
- File-based locale assets under `Assets/Locales`, discovered automatically at build time so a new language is contributed as one JSON file rather than a C# catalog edit.
- Safe language fallback: exact locale, neutral locale, English, then the key itself.
- Backward-compatible migration of the original v0.2 `Italian` / `English` / `French` preference values to `it` / `en` / `fr`.
- Localized tray, tooltips, installer/autostart messages, drive labels and metric captions.
- Localized Linux desktop-entry comments for every shipped locale.
- Translator documentation and repository checks for locale key parity and invalid/missing translation keys.
- Canonical SVG rendering inside Settings and About, while retaining a generated native ICO for Windows integration.
- Product-focused About page with author attribution, GitHub repository/profile links and local-first principles.
- Dedicated localization regression suite running on Windows and Ubuntu CI.

### Changed

- Settings code is split into shell/state, page composition and reusable controls to prepare for future modules such as Signals.
- Language selection moved from Behavior to General, where app-level preferences belong.
- Changing language preserves the currently open Settings page when the window is rebuilt.
- Settings save/reset state, labels, descriptions, icons and responsive behavior were redesigned without removing existing preferences.
- Product positioning now describes EdgePilot as an edge-native desktop surface; the System monitor is the first module rather than the whole product.

### Contributors

- IT / EN / FR localization foundation contributed by [@IamArayel](https://github.com/IamArayel) in [PR #5](https://github.com/pricootz/edgepilot/pull/5), then reconciled with the v0.2 Settings redesign.
- File-based locale architecture, translator-friendly workflow, fallback strategy, Linux locale metadata and translation validation ideas contributed by [@ArnieGA](https://github.com/ArnieGA) in [PR #8](https://github.com/pricootz/edgepilot/pull/8), reconciled without dropping the French localization or the current Settings architecture.

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
