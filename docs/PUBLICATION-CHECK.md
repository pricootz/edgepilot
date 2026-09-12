# Publication preparation and v0.2 follow-up — 2026-09-12

## Completed preparation

- English README, installation and developer documentation.
- MIT license, contribution and security guidance, issue and pull request templates.
- Four maintainer-supplied Windows 11 captures from the initial preview. The notch/pill still illustrate the Flat System surface; Settings/overview predate the v0.2 redesign and require replacement.
- Original icon preserved; adapted transparent master and generated PNG/multi-resolution ICO.
- Dependency metadata and available license/notice files included in packages.
- Relative documentation link checks and private commit-attribution checks.
- Gitleaks scan of the reachable development-branch history.
- Windows and Ubuntu build, UX, packaged launch and per-user installation checks.
- Draft preview automation gated on a successful main build.

## Review scope

The pre-preparation review inspected 24 development commits, 96 unique file blobs (95 text files and one ICO), both branch tips, and existing pull-request references. Pattern checks did not identify credential formats in the inspected text. One personal commit email was replaced in both active branch histories at the maintainer's request.

The two initial screenshot PNGs and original icon inspected during the pre-preparation review contained no text or EXIF metadata chunks. The gallery was subsequently expanded to four initial-preview Windows 11 captures; its Settings/overview images now predate the v0.2 redesign. The adapted master retains generation provenance metadata; application PNG/ICO exports contain pixel data.

These checks reduce risk; they are not a guarantee that every possible secret format, external cache, fork or local clone has been examined.

## Historical attribution follow-up

GitHub can retain old commits through closed pull-request references and cached commit pages after branch history is rewritten. The legacy pull-request references still need GitHub-side cleanup before a strict claim of complete email removal can be made.

The repository and v0.2 preview are now public. The history reachable from current `main` passes the private-attribution check. A 2026-09-12 audit still found non-noreply attribution fields in two closed pull-request references; ordinary branch pushes cannot rewrite GitHub-managed pull references. GitHub Support determines whether those retained references can be removed if strict historical cleanup is still desired.

Follow [GitHub's removal guidance](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/removing-sensitive-data-from-a-repository). No private email or old affected commit identifier is reproduced here.

## Still useful before a stable release

Current v0.2 Windows screenshots, Ubuntu screenshots, real login/startup checks, display-topology/DPI checks and wider desktop feedback. Selectable multi-monitor targeting is not part of v0.2. See [the release checklist](RELEASING.md).
