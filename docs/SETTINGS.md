# Settings

Open Settings by right-clicking the notch, choosing **Settings / Impostazioni / Paramètres / Ajustes** from the tray menu, or launching EdgePilot with `--settings`. Starting EdgePilot a second time activates the Settings window of the running instance.

The Settings experience introduced in v0.2 is split into six sections. v0.3 development extends the Panel section with display targeting.

## General

App-level preferences live here.

- **Interface language:** Automatic (system), plus every locale shipped under `src/EdgePilot/Assets/Locales`. Current `main` includes English, Italian, French and Spanish.
- **Settings appearance:** System, Light or Dark.

Automatic language mode follows the operating-system UI culture. EdgePilot first looks for an exact shipped language tag, then a neutral language tag, then English. If a previously saved language is no longer shipped, the Settings file remains valid and the interface falls back safely.

Changing language rebuilds the Settings content so every label refreshes consistently, while preserving the page that was open before the change.

The appearance setting applies to the Settings window. The edge/notch retains its dedicated EdgePilot dark visual language.

## Edge

Controls where EdgePilot lives and how it appears.

- **Position:** right, left, top or bottom edge, with a live visual preview.
- **Display:** Automatic follows the primary available display, or choose one connected display explicitly. **Use the display containing Settings** selects the screen where the Settings window is currently visible. On Windows, labels use the monitor's EDID model name when available, then its hardware model code, and otherwise fall back to `Display 1`, `Display 2`, and so on; every label also includes resolution, scale and the primary-display marker.
- **Panel behavior:** on hover, always visible or hidden.

An explicit display is matched across restarts using its stable Windows device identity when available, then its current session identity, unique friendly name, working area and scale. If it is disconnected or Windows reports the target unavailable, EdgePilot temporarily uses an available display but keeps the explicit choice; reconnecting it restores the target automatically. A low-frequency Windows check covers availability changes that do not emit a normal screen-topology event. A monitor that is switched off at its physical button can remain reported as active by Windows and therefore cannot be distinguished safely from an on-but-idle display. In that case open **Move EdgePilot to** in the Windows tray and select the visible display explicitly, or use the current-display button in Settings. A tray recovery move is applied to the chosen native monitor, verified and briefly revealed; a fully hidden panel is restored to On hover. EdgePilot still creates one surface total, not one independent surface per display.

## Monitor

Controls the current System module.

- CPU, Memory, Disk and Network are interactive cards; at least one metric must remain enabled.
- The notch resizes to the selected metric set.
- The disk-volume selector is contextual: it is visible only while the Disk metric is enabled.
- Explicit disk choices persist by volume path. If a selected volume disappears, EdgePilot reports it unavailable rather than silently switching to another disk.

## Behavior

- **Refresh frequency:** 0.5, 1, 2 or 5 seconds.
- **Edge sensitivity:** precise, normal or wide activation area.

## Startup

- Optional current-user start at login.
- Explicit action to exit EdgePilot completely.

## About

Shows the EdgePilot product positioning, current version, local-first principles, project author and links to the GitHub repository and maintainer profile.

## Saving and recovery

Edits remain pending until **Save changes / Salva modifiche / Enregistrer / Guardar cambios** is pressed. The footer shows whether changes are pending and provides a reset action. Saving is atomic: a failed save does not replace the last valid preferences.

Preferences live in `EdgePilot/settings.json` under the user's application-data directory: `%APPDATA%` on Windows and normally `~/.config` on Linux. Missing fields in older files retain defaults; a v0.2 file without a display target migrates to Automatic. Invalid files open Settings with an explanation.

The original v0.2 preview stored languages as `Italian`, `English` and `French`. The current loader accepts those values and migrates them to `it`, `en` and `fr`, so upgrading does not discard existing preferences. Spanish was added after the extensible locale-code format was introduced and therefore stores directly as `es`.

Tray show/hide is temporary. Saving persists the selected visibility mode. With no tray fallback, closing Settings in hidden mode exits; otherwise the tray or a second launch provides recovery.

For translation details, see [Translating EdgePilot](TRANSLATING.md). For desktop integration details, see [Desktop integration](DESKTOP-INTEGRATION.md).

For development, `EDGEPILOT_EDGE` can override the initial edge with `right`, `left`, `top` or `bottom`.
