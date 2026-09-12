# Changelog

## 0.2.0-preview.2 — 2026-09-12

A focused stabilization update for the v0.2 preview. It does not add a new product module.

### Fixed

- The packaged `--version` diagnostic reports the full preview identity instead of dropping the prerelease suffix.

### Verification and documentation

- Package CI now verifies that the version reported by Windows and Linux executables exactly matches the project version.
- Settings regression coverage now includes the supported minimum window size and both responsive layout modes.
- Native input-region coverage now explicitly runs at 100%, 125%, 150% and 200% scaling.
- Documentation distinguishes basic display-topology awareness from selectable multi-monitor targeting, which is not implemented yet.
- The screenshot inventory now identifies the Settings/overview captures that predate the v0.2 redesign instead of presenting them as current.
- The publication review records the current public-release state and the remaining GitHub-managed historical attribution follow-up.

## 0.2.0-preview.1 — 2026-09-12

The v0.2 preview turns the initial system-monitor settings panel into a more coherent EdgePilot product surface and establishes localization as a first-class capability.

### Added

- Responsive Settings shell with dedicated General, Edge, Monitor, Behavior, Startup and About sections.
- General page for interface language and Settings appearance (`System`, `Light`, `Dark`).
- Windows surface choices with platform-aware fallback: Flat everywhere, Acrylic on Windows 10 1803+, and Mica on Windows 11 build 22000+.
- Interactive metric cards with contextual configuration; the disk-volume selector is hidden when Disk is disabled.
- Italian, English, French and Spanish interface localization with automatic system-language detection and a persisted manual override.
- File-based locale assets under `Assets/Locales`, discovered automatically at build time so a new language is contributed as one JSON file rather than a C# catalog edit.
- Safe language fallback: exact locale, neutral locale, English, then the key itself.
- Backward-compatible migration of the original v0.2 `Italian` / `English` / `French` preference values to `it` / `en` / `fr`.
- Localized tray, tooltips, installer/autostart messages, drive labels and metric captions.
- Localized Linux desktop-entry comments for every shipped locale.
- Translator documentation and repository checks for locale key parity and invalid/missing translation keys.
- Canonical SVG rendering inside Settings and About, while retaining a generated native ICO for Windows integration.
- Product-focused About page with author attribution, GitHub repository/profile links and local-first principles.
- Dedicated localization and native input-region regression suites running on Windows and Ubuntu CI.

### Changed

- Settings code is split into shell/state, page composition and reusable controls to prepare for future modules such as Signals.
- Language selection moved from Behavior to General, where app-level preferences belong.
- Changing language preserves the currently open Settings page when the window is rebuilt.
- Settings save/reset state, labels, descriptions, icons and responsive behavior were redesigned without removing existing preferences.
- Settings navigation now uses Fluent UI System Icons with the EdgePilot accent instead of platform-dependent Unicode glyphs.
- Glass Settings use the real rectangular OS Mica/Acrylic backdrop with restrained translucent panels/cards; metric tiles follow the same resolved palette.
- Backdrop and Settings theme are independent preferences, so Acrylic no longer silently forces Dark mode.
- Product positioning now describes EdgePilot as an edge-native desktop surface; the System monitor is the first module rather than the whole product.
- Preview release automation now derives its tag from the application version instead of carrying a hard-coded release tag.
- GitHub Actions workflows use current Node 24-compatible action generations.

### Fixed

- Transparent portions of the large edge window no longer intercept clicks intended for browser or desktop controls. Native input is constrained to live EdgePilot regions on Windows and X11/XWayland (#16/#17).
- Windows Glass no longer uses a pixel-quantized native region as the visible notch boundary: Avalonia/Skia keeps the approved antialiased silhouette while a separate invisible shaped overlay owns pointer input.
- The Exit EdgePilot action now consistently displays its Fluent sign-out icon.
- Single-instance activation is hardened so a second launch resolves to the existing per-user EdgePilot instance instead of leaving duplicate desktop surfaces.
- CI package smoke processes now terminate deterministically instead of depending on desktop-lifetime teardown timing.

### Contributors

- IT / EN / FR localization foundation contributed by [@IamArayel](https://github.com/IamArayel) in [PR #5](https://github.com/pricootz/edgepilot/pull/5), then reconciled with the v0.2 Settings redesign.
- File-based locale architecture, translator-friendly workflow, fallback strategy, Linux locale metadata and translation validation ideas contributed by [@ArnieGA](https://github.com/ArnieGA) in [PR #8](https://github.com/pricootz/edgepilot/pull/8), reconciled without dropping the French localization or the current Settings architecture.
- Spanish localization contributed by [@ArnieGA](https://github.com/ArnieGA) in [PR #12](https://github.com/pricootz/edgepilot/pull/12), using the file-per-language workflow against current `main`.
- Flat / Mica / Acrylic surface architecture and visual direction originally contributed by [@ArnieGA](https://github.com/ArnieGA) in [PR #15](https://github.com/pricootz/edgepilot/pull/15), then adapted in PR #19 to the post-#17 input-safety architecture after real Windows testing.

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
