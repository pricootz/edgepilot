# Four-edge notch and persistent settings

The notch now maps its silhouette, clipping, metric layout, hit targets and tooltip bridges to all four edges. Text stays upright; top/bottom use a horizontal row. The rounded closed pill is unchanged.

## Settings — first increment
Right-click the notch to open Settings. Choose the edge and one of:
- Show on hover: current hover, click-to-pin and delayed-fold behavior.
- Always open: stays expanded without pointer interaction.
- Hidden: hides the notch and leaves Settings available.

Apply saves preferences and updates the notch immediately. Closing Settings without Apply discards edits. If the notch is hidden, closing Settings exits EdgePilot. Launching again in Hidden mode opens Settings, so the app cannot become inaccessible.

You can also launch directly into Settings:
    dotnet run --project src/EdgePilot/EdgePilot.csproj -c Release -- --settings

Preferences are stored under the user's application-data folder in EdgePilot/settings.json (%APPDATA% on Windows, normally ~/.config on Linux). Writes replace the file atomically. Missing files use defaults. Invalid or unreadable settings use defaults and open Settings with an explanation; Apply can repair the saved file. Save failures leave active preferences unchanged. The existing EDGEPILOT_EDGE development override still applies at startup.

Refresh interval, configurable hot-zone, optional UI scaling and visible-metric selection remain subsequent settings work. Metric providers and existing details are unchanged.

## Verification
GitHub Actions runs Release builds and executable headless UX checks on Windows and Ubuntu. Added checks cover four-edge coordinate mapping, metric hit targets, tooltip containment and bridges, edge anchoring, display-mode transitions, persistence, malformed files and invalid values.

Native compositor transparency, mixed-DPI behavior and actual pointer feel still require desktop testing by the user. No local PC access was used for this work.
