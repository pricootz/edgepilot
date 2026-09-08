# Product scope

EdgePilot is an **edge-native desktop surface for Windows and Linux**. It uses the screen edge as a quiet place for local information, contextual Signals and, later, focused actions.

The System monitor is the first module, not the final product definition.

## Product principles

- **Edge-native:** information should emerge from the selected screen edge rather than requiring a dashboard window.
- **Local-first:** no account, required cloud backend or telemetry uploader.
- **Quiet by default:** EdgePilot should remain unobtrusive until the user asks for detail or something meaningful changes.
- **Context over volume:** Signals should represent transitions or sustained conditions, not repeat every polling cycle.
- **Return to state:** temporary presentation must restore the user's previous EdgePilot state when it is finished.
- **Cross-platform where practical:** Windows and Linux are first-class desktop targets; platform-specific implementation details should not leak into product behavior unnecessarily.

## v0.1 foundation

The first preview established:

- CPU, physical memory, disk capacity, network throughput/interface details, uptime and machine identity;
- four-edge placement;
- hover expansion, delayed fold, pinning and tooltips;
- persistent display preferences and disk selection;
- tray integration, optional start at login and single-instance activation;
- self-contained x64 packaging and per-user installation on Windows and Linux.

## v0.2 product surface

v0.2 expands EdgePilot from a compact monitor into a coherent configurable product surface:

- responsive Settings with General, Edge, Monitor, Behavior, Startup and About sections;
- configurable Settings theme while the edge keeps its dedicated dark visual language;
- interactive metric selection and contextual disk configuration;
- Italian, English, French and Spanish interface localization;
- automatic system-language selection and persisted manual override;
- translator-friendly JSON locale files with safe fallback and CI key-parity validation;
- clearer product identity and community attribution.

Refresh intervals remain 0.5, 1, 2 or 5 seconds. Repository documentation and contribution discussion use English.

## Interaction contract

1. The collapsed pill occupies a narrow strip at the selected edge of the usable working area.
2. Pointer entry expands it; pointer exit folds after a grace period unless pinned or configured otherwise.
3. Moving into related interactive UI must not accidentally fold the active surface.
4. Always-visible and hidden modes remain available independently of hover behavior.
5. Reopening the executable activates the already-running instance rather than creating a duplicate.
6. Missing readings and missing selected volumes degrade visibly without inventing replacement data.
7. A temporary Signal may surface proactively, but must restore the previous EdgePilot state after expiry.

## v0.3 Signals direction

Signals implement **Observe → Decide → Surface → Act**.

The initial network flow is intentionally narrow:

- establish connectivity baseline silently;
- surface only when connectivity changes;
- distinguish lost and restored states;
- deduplicate equivalent events;
- apply severity/priority rules;
- expire automatically;
- restore the previous edge state.

The first implementation is under real-desktop review. It must prove that proactive edge presentation feels seamless before additional Signal types are promoted.

After network behavior is proven, the next planned Signals are low disk space and sustained unusual CPU activity. These should use threshold crossing, hysteresis or duration rules rather than naïve instant-value alerts.

## Not currently in scope

GPU, temperature and fan sensors are not implemented. Remote-server dashboards, Docker, Git, VPN/Tailscale, removable-device workflows, clipboard tools, Edge Shelf / Smart Drop and richer quick actions remain exploratory future modules rather than current shipped features.

macOS, ARM packages and headless sessions are also not current supported targets.
