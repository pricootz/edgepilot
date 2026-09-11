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
- [x] File-per-language JSON locale architecture with automatic discovery and safe fallback.
- [x] Localized tray, tooltip, installer and system-monitor strings.
- [x] Split Settings implementation into shell, pages and reusable controls.
- [x] Localization regression checks alongside existing UX/package CI.

## Before promoting v0.2 beyond preview

- [ ] Broader Windows 11 and Ubuntu desktop testing.
- [ ] Verify real logout/login startup and tray behavior.
- [ ] Test Settings at common scaling levels (100%, 125%, 150%) and smaller window sizes.
- [ ] Validate all four edges and monitor changes on physical multi-monitor systems.
- [ ] Confirm language switching and Automatic mode on Italian, English, French and Spanish desktop locales.
- [ ] Add current v0.2 Windows and Ubuntu screenshots/GIFs.
- [ ] Gather feedback from the preview release.

## v0.3 — Signals / ambient awareness — in development

Signals follow **Observe → Decide → Surface → Act**. They should be temporary, deduplicated and context-aware rather than behaving like a traditional notification center.

### Foundation

- [x] Immutable Signal model with source/severity metadata.
- [x] Priority, deduplication, preemption, pending promotion and expiry.
- [x] Network connectivity transition detector with silent initial state.
- [x] Live coordination from `SystemSnapshot` updates.
- [x] Transient edge Signal presentation and automatic restoration of the previous EdgePilot state.
- [x] Four-edge Signal placement/orientation.
- [x] Non-activating/click-through Signal surface.
- [x] Deterministic demo and CI smoke modes.
- [x] Dedicated Signal regression suite in Windows/Ubuntu CI.

### Before the first Signals preview

- [ ] Review Signal visuals on a physical Windows desktop.
- [ ] Test real Internet disconnect/reconnect transitions.
- [ ] Validate physical Linux desktop behavior on X11/Wayland where available.
- [ ] Decide whether the separate transient Signal surface is visually seamless enough or should be folded into the main notch window.
- [ ] Finalize dedicated localized Signal copy across all shipped locales.
- [ ] Add focused regression coverage for issues found during manual QA.
- [ ] Capture screenshots/GIFs of the final network Signal flow.

### Next Signals after network is proven

- [ ] Low disk space, with threshold crossing rather than repeated polling notifications.
- [ ] Sustained unusual CPU activity, using duration/hysteresis rather than a single instant threshold.
- [ ] Signal-specific actions only after the presentation and lifecycle model are stable.

## Later exploration

Potential modules include contextual actions, Edge Shelf / Smart Drop, Docker, Git, VPN/Tailscale, removable devices and richer system providers. These remain exploratory until the Signals foundation is proven.
