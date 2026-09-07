# Publishing a preview

## Repository readiness

- [ ] Resolve any remaining private-history references before changing visibility.
- [ ] Confirm the latest main build is green, including repository checks and both packaged desktop checks.
- [ ] Review README screenshots and known limitations.
- [ ] Review the MIT license and third-party notices included in the archives.
- [ ] Complete the manual Windows/Ubuntu checks below.
- [ ] Review the draft preview release and its four downloadable files.

Repository preparation does not automatically make the repository public or publish a draft release.

## Suggested GitHub settings

Description: **A compact screen-edge system monitor for Windows and Linux, built with Avalonia and .NET.**

Topics: edgepilot, system-monitor, desktop, windows, linux, avalonia, dotnet, csharp.

Keep main as the default branch. Enable private vulnerability reporting and available secret scanning/push protection. After history cleanup, consider requiring pull requests and successful checks before merging into main. Availability depends on repository visibility and GitHub plan.

## Draft release automation

After a successful push build on main, the draft-preview workflow downloads that exact run's verified packages and creates or updates the **draft**, **pre-release** named v0.1.0-preview.1. It refuses to overwrite an already published release.

The workflow does not publish the release. Review its notes and assets, then publish manually when ready. Future versions require updating the application Version, changelog, release notes and workflow tag together.

Archives contain the .NET runtime, the icon, installation instructions, project license and dependency notices. SHA-256 files use the archive basename so verification works after download.

## Manual desktop checks

- Windows 11: launch, all four edges, hover/fold/pin, tooltip transitions, scaling, tray and hidden-mode recovery.
- Ubuntu: record distribution and desktop session; verify transparency, tray support and settings under light/dark themes.
- Both: choose the intended volume, restart, confirm selection, and test the unavailable-volume state.
- Both: install for the current user, enable startup, log out/in, then disable startup and confirm it is removed.
- Exit before upgrading; verify the installed launcher still opens the app.

CI smoke checks do not replace actual login or compositor testing.

## Updating copies after a history cleanup

Do not merge an old local branch into the cleaned history. The safest route is a fresh clone into a **new** directory, keeping the old directory untouched until any local work has been recovered:

```bash
git clone https://github.com/pricootz/edgepilot.git edgepilot-clean
cd edgepilot-clean
git config user.email "58367058+pricootz@users.noreply.github.com"
dotnet run --project src/EdgePilot/EdgePilot.csproj -c Release -- --settings
```

Run this from the parent projects directory, choosing another unused name if edgepilot-clean already exists. Copy only reviewed uncommitted source changes if necessary; do not push old branches or .git data.
