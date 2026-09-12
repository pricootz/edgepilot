# Publishing a preview

## Repository readiness

- [ ] Confirm the latest `main` build is green, including repository checks and both packaged desktop checks.
- [ ] Confirm the application version in `src/EdgePilot/EdgePilot.csproj` is the intended preview version.
- [ ] Confirm `docs/releases/v<version>.md` exists and matches the changelog.
- [ ] Review README screenshots and known limitations.
- [ ] Review the MIT license and third-party notices included in the archives.
- [ ] Complete the manual Windows/Ubuntu checks below.
- [ ] Review the generated draft pre-release and its four downloadable files.

The repository ruleset requires pull requests and successful Windows, Ubuntu and repository checks before `main` can move. A green CI run does not publish a release automatically.

## Draft release automation

After a successful **push** build on `main`, `.github/workflows/draft-preview.yml` downloads the packages produced by that exact build, verifies both SHA-256 files and reads the release version directly from `src/EdgePilot/EdgePilot.csproj`.

For version `X.Y.Z-preview.N`, the workflow uses:

- tag `vX.Y.Z-preview.N`;
- notes file `docs/releases/vX.Y.Z-preview.N.md`.

If the matching release does not exist, the workflow creates a **draft pre-release**. If it is still a draft, the workflow refreshes its target, notes and assets. If that version has already been published, the workflow verifies the new build artifacts and exits without modifying the published release.

The workflow never publishes a release. Review the draft manually before publishing it.

### Starting a new preview version

Update these together:

1. `<Version>` in `src/EdgePilot/EdgePilot.csproj`;
2. `CHANGELOG.md`;
3. `docs/releases/v<version>.md`.

Do **not** hard-code the new tag in the workflow; release automation derives it from the project version.

Archives contain the .NET runtime, application icon/assets, installation instructions, project license and dependency notices. SHA-256 files use the archive basename so verification still works after download.

## Manual desktop checks

- Windows 11: launch, all four edges, hover/fold/pin, tooltip transitions, Settings, tray, hidden-mode recovery and relevant Glass modes.
- Windows 10 when supported by the feature set: launch, Acrylic/Flat availability and fallback behavior.
- Ubuntu: record distribution and desktop session; verify transparency, native input region, tray support and Settings under light/dark themes.
- Both: test common scaling where available, choose the intended volume, restart, confirm selection, and test the unavailable-volume state.
- With more than one display: test Automatic and an explicit secondary display, restart, change the primary display, power off/on and dock/undock the selected display, then verify fallback/restore and safe repositioning at mixed scaling levels.
- Both: verify a second launch activates the existing instance instead of creating a duplicate surface.
- Both: install for the current user, enable startup, log out/in, then disable startup and confirm it is removed.
- Exit before upgrading; verify the installed launcher still opens the new build.

CI smoke checks do not replace actual login, compositor, scaling or physical display-topology testing.
