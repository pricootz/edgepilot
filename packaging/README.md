# EdgePilot for Windows and Linux (x64)

These preview packages include .NET. You do not need the SDK or VS Code. Extract all files together, not just the executable. Close any previous instance before upgrading. The application interface is Italian.

## Windows

Run EdgePilot.exe from the extracted folder. To install for your user and create a Start menu shortcut:

```powershell
.\EdgePilot.exe --install
```

Or right-click Install.ps1 and choose Run with PowerShell. Administrator privileges are not required. Installation goes to %LOCALAPPDATA%\Programs\EdgePilot. Start EdgePilot from the Start menu.

## Linux / Ubuntu

A graphical desktop session and its native libraries are required; this is not a terminal-only server application.

```bash
./EdgePilot
```

To install for your user and create an Applications launcher:

```bash
bash install.sh
```

Installation goes to ~/.local/share/edgepilot. X11/XWayland, fontconfig and the desktop's native dependencies are still needed. Linux desktop/compositor support varies. GNOME may require an AppIndicator extension for tray visibility.

## Settings and recovery

Right-click the notch or choose Impostazioni from the tray. Launching the executable again opens settings in the existing instance. Use Applica to save, or Esci to exit.

Choose your volume under Disco da visualizzare; selection is saved by path. If it is missing, no substitute is selected. Start at login is optional and should be enabled after installation.

Linux settings follow system theme information exposed to Avalonia. They are not native GTK/Yaru controls.

## Checksums

Download the .sha256 file alongside its archive, keeping both in one folder.

On Windows, compare Get-FileHash output with the first field in the checksum file:

```powershell
Get-FileHash .\EdgePilot-win-x64.zip -Algorithm SHA256
Get-Content .\EdgePilot-win-x64.sha256
```

On Linux:

```bash
sha256sum -c EdgePilot-linux-x64.sha256
```

Checksums detect mismatched downloads. Preview packages are not code-signed.

## Updates and removal

Updates are manual: exit EdgePilot, extract the new package, and run its installer again.

To remove: disable Avvia all’accesso, click Applica, exit, and remove the installed directory and menu shortcut. On Windows the shortcut is EdgePilot.lnk in the user's Start Menu Programs folder; on Linux it is ~/.local/share/applications/io.github.pricootz.EdgePilot.desktop.

Preferences are retained in EdgePilot/settings.json in the user's application-data directory (%APPDATA% on Windows; normally ~/.config on Linux).
