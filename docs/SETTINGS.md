# Settings

Right-click the notch, choose **Settings** in the tray menu, or start with --settings. A second launch opens settings in the running instance.

The window has a sidebar of pages. Save changes and Discard sit in the footer and stay reachable from every page.

| Page | Setting | Meaning |
| --- | --- | --- |
| Edge | Position on screen | Right, left, top or bottom, with a live preview |
| Edge | Panel behavior | On hover, always visible or hidden |
| Monitor | Visible metrics | CPU, memory, disk and network; at least one is required |
| Monitor | Volume for the Disk metric | Automatic largest volume or an explicit volume path; shown only while Disk is on |
| Behavior | Interface language | System language, English or Italian; applied immediately |
| Behavior | Refresh rate | 0.5, 1, 2 or 5 seconds |
| Behavior | Edge sensitivity | Precise, normal or wide activation area |
| Startup | Automatic start | Optional current-user start at login |
| Startup | Current session | Exit EdgePilot completely |
| About | — | The project, its author and the version |

Save changes saves atomically and updates the live panel. Discard returns every control to the last saved state, and closing the window without saving discards edits. Visible metric count determines panel length; sampling remains available for hidden metrics. A refresh wait already in progress finishes before the new interval takes effect.

The language list holds the system entry and every file in `src/EdgePilot/Assets/Locales`, each named in its own language. The system entry follows the operating system, falling back to English where that language is not shipped. Changing the language relabels the notch, the tray menu and the settings window without a restart, and moves number formatting with it: the same disk reads 16.5 TB in English and 16,5 TB in Italian. A saved language that a later build no longer ships falls back to English rather than blocking the settings. Adding a language is described in [Translating](TRANSLATING.md).

Disk choices show labels, paths and decimal capacity. A missing explicit selection remains unavailable rather than switching to another disk. Mount paths are case-sensitive on Linux and case-insensitive on Windows.

Preferences live in EdgePilot/settings.json under the user's application-data directory: %APPDATA% on Windows, normally ~/.config on Linux. Missing fields in older files retain defaults, so a file written before the language setting existed keeps working and follows the system. Invalid files open settings with an explanation; failed saves leave active preferences unchanged.

Tray show/hide is temporary. Apply persists the selected visibility mode. With no tray fallback, closing settings in hidden mode exits; otherwise the tray or a second launch provides recovery.

Linux settings follow the system theme information available to Avalonia. The notch retains its own dark styling. See [desktop integration](DESKTOP-INTEGRATION.md).

For development, EDGEPILOT_EDGE can override the initial edge with right, left, top or bottom.
