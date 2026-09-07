# Settings

Right-click the notch, choose **Impostazioni** in the tray menu, or start with --settings. A second launch opens settings in the running instance.

| Italian label | Meaning |
| --- | --- |
| Bordo dello schermo | Right, left, top or bottom screen edge |
| Visualizzazione | Hover, always open or hidden |
| Metriche visibili | CPU, memory, disk and network; at least one is required |
| Aggiornamento dei dati | 0.5, 1, 2 or 5 seconds |
| Sensibilità di apertura | Precise, normal or wide activation area |
| Disco da visualizzare | Automatic largest volume or an explicit volume path |
| Avvia all’accesso | Optional current-user start at login |
| Applica | Save and apply |
| Esci | Exit EdgePilot |

Apply saves atomically and updates the live panel. Closing without Apply discards edits. Visible metric count determines panel length; sampling remains available for hidden metrics. A refresh wait already in progress finishes before the new interval takes effect.

Disk choices show labels, paths and decimal capacity. A missing explicit selection remains unavailable rather than switching to another disk. Mount paths are case-sensitive on Linux and case-insensitive on Windows.

Preferences live in EdgePilot/settings.json under the user's application-data directory: %APPDATA% on Windows, normally ~/.config on Linux. Missing fields in older files retain defaults. Invalid files open settings with an explanation; failed saves leave active preferences unchanged.

Tray show/hide is temporary. Apply persists the selected visibility mode. With no tray fallback, closing settings in hidden mode exits; otherwise the tray or a second launch provides recovery.

Linux settings follow the system theme information available to Avalonia. The notch retains its own dark styling. See [desktop integration](DESKTOP-INTEGRATION.md).

For development, EDGEPILOT_EDGE can override the initial edge with right, left, top or bottom.
