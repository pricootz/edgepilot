# Settings

Open Settings by right-clicking the notch, choosing **Settings / Impostazioni / Paramètres** from the tray menu, or launching EdgePilot with `--settings`. Starting EdgePilot a second time activates the Settings window of the running instance.

The v0.2 Settings experience is split into five sections.

## Edge

Controls where EdgePilot lives and how it appears.

- **Position:** right, left, top or bottom edge, with a live visual preview.
- **Panel behavior:** on hover, always visible or hidden.

## Monitor

Controls the current System module.

- CPU, Memory, Disk and Network are interactive cards; at least one metric must remain enabled.
- The notch resizes to the selected metric set.
- The disk-volume selector is contextual: it is visible only while the Disk metric is enabled.
- Explicit disk choices persist by volume path. If a selected volume disappears, EdgePilot reports it unavailable rather than silently switching to another disk.

## Behavior

- **Refresh frequency:** 0.5, 1, 2 or 5 seconds.
- **Edge sensitivity:** precise, normal or wide activation area.
- **Interface language:** Automatic (system), Italiano, English or Français.

Automatic mode detects Italian and French explicitly and falls back to English for other system UI languages. Selecting a language manually persists the override. When the language changes, Settings reopens so the whole interface refreshes consistently.

## Startup

- Optional current-user start at login.
- Explicit action to exit EdgePilot completely.

## About

Shows the EdgePilot product positioning, current version, local-first principles, project author and links to the GitHub repository and maintainer profile.

## Saving and recovery

Edits remain pending until **Save changes / Salva modifiche / Enregistrer** is pressed. The footer shows whether changes are pending and provides a reset action. Saving is atomic: a failed save does not replace the last valid preferences.

Preferences live in `EdgePilot/settings.json` under the user's application-data directory: `%APPDATA%` on Windows and normally `~/.config` on Linux. Missing fields in older files retain defaults; invalid files open Settings with an explanation.

Tray show/hide is temporary. Saving persists the selected visibility mode. With no tray fallback, closing Settings in hidden mode exits; otherwise the tray or a second launch provides recovery.

Linux Settings follow system theme information available to Avalonia. The edge itself retains its dedicated EdgePilot visual language. See [desktop integration](DESKTOP-INTEGRATION.md).

For development, `EDGEPILOT_EDGE` can override the initial edge with `right`, `left`, `top` or `bottom`.
