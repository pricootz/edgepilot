# EdgePilot

EdgePilot is a cross-platform desktop edge companion for **Windows and Linux**.

Instead of opening a dashboard, EdgePilot stays attached to a screen edge in a compact state and expands when you reach for it. The first release proves the shell with local system resources that require no server or account.

## v0.1

The current development branch is `feat/v0.1-system-monitor`.

It shows:

- CPU usage
- RAM usage
- fixed-drive capacity
- active network interface
- live download/upload throughput
- system uptime
- host name and OS

Interaction:

- hover to expand
- 450 ms delayed fold
- PIN/UNPIN expanded state
- topmost, borderless, hidden from taskbar
- anchored to the OS working area

## Stack

- .NET 10 LTS
- Avalonia 12.1.2
- C#
- no hardware-monitor dependency in v0.1

Windows CPU/RAM readings use Win32 APIs. Linux CPU/RAM readings use `/proc`. Storage and network use .NET APIs.

## Run

Requirements: .NET 10 SDK.

```bash
git clone https://github.com/pricootz/edgepilot.git
cd edgepilot
git switch feat/v0.1-system-monitor
dotnet restore src/EdgePilot/EdgePilot.csproj
dotnet run --project src/EdgePilot/EdgePilot.csproj
```

The default position is the right edge. For development you can select another edge before starting the app:

### PowerShell

```powershell
$env:EDGEPILOT_EDGE="left"
dotnet run --project src/EdgePilot/EdgePilot.csproj
```

### Bash

```bash
EDGEPILOT_EDGE=top dotnet run --project src/EdgePilot/EdgePilot.csproj
```

Supported values: `right`, `left`, `top`, `bottom`.

## Project docs

- `docs/PRODUCT.md` — product scope and interaction contract
- `docs/ARCHITECTURE.md` — boundaries and Codenotch reference lessons
- `docs/ROADMAP.md` — staged roadmap

## Linux note

Avalonia supports topmost/borderless windows on Linux, but transparency and exact compositor behavior can differ under X11/XWayland/Wayland. v0.1 intentionally keeps the platform surface small so we can test it on the real Ubuntu machine before adding more modules.

## Pacchetti pronti e integrazione desktop

I workflow verdi di GitHub Actions pubblicano gli artefatti **EdgePilot-win-x64** e **EdgePilot-linux-x64**, con runtime .NET incluso. Scarica il pacchetto dal workflow e segui [le istruzioni in italiano](packaging/LEGGIMI.md).

Sono disponibili selezione persistente del disco, tema di sistema nelle impostazioni Ubuntu, menu nell’area di notifica, avvio all’accesso opzionale e collegamenti nel menu del desktop. Un secondo avvio riapre le impostazioni dell’istanza attiva.
