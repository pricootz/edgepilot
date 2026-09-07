# Publication preparation — 2026-09-07

## Completed preparation

- English README, installation and developer documentation.
- MIT license, contribution and security guidance, issue and pull request templates.
- Four current, maintainer-supplied Windows 11 screenshots: overview, complete settings, expanded notch and collapsed pill.
- Original icon preserved; adapted transparent master and generated PNG/multi-resolution ICO.
- Dependency metadata and available license/notice files included in packages.
- Relative documentation link checks and private commit-attribution checks.
- Gitleaks scan of the reachable development-branch history.
- Windows and Ubuntu build, UX, packaged launch and per-user installation checks.
- Draft preview automation gated on a successful main build.

## Review scope

The pre-preparation review inspected 24 development commits, 96 unique file blobs (95 text files and one ICO), both branch tips, and existing pull-request references. Pattern checks did not identify credential formats in the inspected text. One personal commit email was replaced in both active branch histories at the maintainer's request.

The two initial screenshot PNGs and original icon inspected during the pre-preparation review contained no text or EXIF metadata chunks. The screenshot gallery was subsequently replaced with four current Windows 11 captures. The adapted master retains generation provenance metadata; application PNG/ICO exports contain pixel data.

These checks reduce risk; they are not a guarantee that every possible secret format, external cache, fork or local clone has been examined.

## Publication hold

GitHub can retain old commits through closed pull-request references and cached commit pages after branch history is rewritten. The existing legacy pull-request reference still needs GitHub-side cleanup before a strict claim of complete email removal can be made.

Keep the repository private until that is resolved. GitHub Support determines whether it can remove the retained references; a clean replacement repository is an alternative if cleanup is unavailable. Do not copy the old Git history into that replacement.

Follow [GitHub's removal guidance](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/removing-sensitive-data-from-a-repository). No private email or old affected commit identifier is reproduced here.

## Still useful before a stable release

Ubuntu screenshots, real login/startup checks, multiple-display/DPI checks and wider desktop feedback. See [the release checklist](RELEASING.md).
