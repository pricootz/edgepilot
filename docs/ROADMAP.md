# Roadmap

This roadmap is directional, not a delivery schedule.

## v0.1 foundation — implemented

- [x] Local CPU, memory, disk, network and machine details.
- [x] Four-edge notch, rounded pill, spring motion, clipping and tooltips.
- [x] Hover, delayed fold, pin and visibility modes.
- [x] Persistent settings, refresh, sensitivity and visible metrics.
- [x] Persistent volume selection and unavailable-volume handling.
- [x] Linux settings theme integration.
- [x] Tray, optional start at login and single-instance recovery.
- [x] Self-contained x64 packages and per-user installation.
- [x] Windows/Ubuntu build, UX and package checks.

## v0.2 product surface — implemented for preview

- [x] Responsive Settings redesign with dedicated navigation and clearer hierarchy.
- [x] Edge preview and direct edge/display controls.
- [x] Interactive metric cards and conditional disk configuration.
- [x] Canonical SVG branding inside the app and native Windows icon generation.
- [x] Product-focused About page with author and GitHub links.
- [x] Italian, English, French and Spanish application localization.
- [x] Automatic system-language detection plus persisted manual language selection.
- [x] File-based locale discovery with one JSON file per shipped language.
- [x] Localized tray, tooltip, installer and system-monitor strings.
- [x] Split Settings implementation into shell, pages and reusable controls.
- [x] Localization regression checks alongside existing UX/package CI.

## Before promoting v0.2 beyond preview

- [ ] Broader Windows 11 and Ubuntu desktop testing.
- [ ] Verify real logout/login startup and tray behavior.
- [ ] Test Settings at common scaling levels (100%, 125%, 150%) and smaller window sizes.
- [ ] Validate all four edges and monitor changes on physical multi-monitor systems.
- [ ] Confirm language switching and Automatic mode on Italian, English, French and Spanish desktop locales.
- [ ] Add current v0.2 Windows and Ubuntu screenshots.
- [ ] Gather feedback from the preview release.

## v0.3 direction — Signals / ambient awareness

The next major feature is a signal engine following the model **Observe → Decide → Surface → Act**. Signals should be temporary, deduplicated and context-aware rather than behaving like a traditional notification center.

Initial scope:

- Internet connection lost / restored.
- Low disk space.
- Sustained unusual CPU activity.

Architecture direction:

- Sensors / detectors publish normalized signals.
- A signal service handles priority, deduplication, coalescing, rate limiting and expiry.
- The edge temporarily morphs from its normal content into the active signal, then returns automatically.
- Later signals may expose actions.

Signals are being developed outside `main` until real-desktop behavior and CI are considered ready.

## Later exploration

Potential modules include contextual actions, Edge Shelf / Smart Drop, Docker, Git, VPN/Tailscale, removable devices and richer system providers. These remain exploratory until the Signals foundation is proven.
