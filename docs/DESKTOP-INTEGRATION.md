# Desktop integration

## Volumes

The provider lists accessible mounted volumes with positive capacity, excluding optical and RAM drives. There is no five-volume limit. Automatic mode selects the largest volume; explicit mode resolves the saved drive letter or mount path. EdgePilot never mounts, unmounts or modifies disks.

## Theme and tray

Linux settings follow the light/dark variant and colors exposed to Avalonia by the desktop. Window decorations belong to the desktop. Controls are Avalonia, not native GTK/Yaru widgets.

The tray provides Settings, Show/Hide and Exit (in Italian). Linux requires AppIndicator/StatusNotifier support. A tray backend does not guarantee that every desktop displays an icon, so launching the executable again always provides an activation route.

## Start at login

Disabled by default. Windows uses only the current user's EdgePilot Run value. Linux uses io.github.pricootz.EdgePilot.desktop under the XDG configuration autostart directory, normally ~/.config/autostart.

No administrator privilege, service or other-user registration is used. If settings persistence fails, the previous startup registration is restored. Install to a stable location before enabling startup.

## Packages and installation

Self-contained Windows ZIP and Linux tar.gz x64 packages include the .NET runtime. The --install command copies files into the current user's profile and creates a Start/Applications launcher. Existing startup commands are updated without enabling startup when previously disabled.

Close the previous version before upgrading. Installation is a file copy, not an atomic update or an automatic updater. See [installation instructions](../packaging/README.md).

## Verification limits

CI builds and runs the UX suite on Windows/Ubuntu, starts packaged UI windows (Xvfb on Linux), checks second-instance activation and installs into disposable runner profiles.

Actual logout/login, tray extensions, desktop theme exposure, scaling and compositor behavior still need real desktop checks.
