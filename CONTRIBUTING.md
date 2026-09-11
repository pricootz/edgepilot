# Contributing to EdgePilot

EdgePilot is an early Windows/Linux desktop preview. Small, focused fixes, translations and real-desktop test reports are welcome. Please use English for issues, pull requests and documentation.

Interface text lives in `src/EdgePilot/Assets/Locales`, one JSON file per language. Do not add hardcoded interface text or edit one language in isolation as part of an unrelated change. See [Translating EdgePilot](docs/TRANSLATING.md) for the translation workflow.

## Development

Install the .NET 10 SDK, clone the repository, and create a branch from `main`. Committed icons are ready to build; Python is only needed to regenerate brand assets and to run repository validation.

```bash
dotnet build src/EdgePilot/EdgePilot.csproj -c Release
dotnet run --project tests/EdgePilot.UxChecks -c Release
dotnet run --project tests/EdgePilot.LocalizationChecks -c Release
dotnet run --project tests/EdgePilot.SignalChecks -c Release
python scripts/check_repository.py
dotnet run --project src/EdgePilot/EdgePilot.csproj -c Release -- --settings
```

The tests are executable headless suites, not `dotnet test` projects. CI additionally publishes and launches packages, exercises the Signal surface when present, and checks per-user installation on disposable Windows and Ubuntu runners.

## Pull requests

Explain the problem, resulting behavior and verification. Keep platform-specific APIs in `Platform`, normalized monitoring/Signal contracts in `Core`, and interaction/rendering in `UI`. Preserve existing metrics, settings and preference-file compatibility. Add regression coverage when a behavior fix merits it.

For visual changes, include screenshots or GIFs and desktop checks at relevant scaling levels. State whether Linux testing used X11 or Wayland. Headless checks alone cannot validate compositor transparency, click-through behavior or physical multi-monitor placement.

For localization contributions, keep all locale files key-compatible with `en.json`. Adding a language should normally mean adding one locale file plus any necessary documentation/test evidence, not introducing a new language enum or C# translation catalog.

For Signals contributions:

- detectors should emit normalized `Signal` values rather than Avalonia UI;
- the first observation of state should generally establish a baseline instead of creating a surprise notification;
- repeated polling must not repeatedly surface the same condition;
- use threshold crossing, duration or hysteresis where an instantaneous value would be noisy;
- keep priority/deduplication/lifecycle behavior inside the Signal engine rather than duplicating it in individual detectors;
- user-facing Signal text must use localization keys;
- temporary presentation must restore the user's previous EdgePilot state.

Discuss major new features in an issue first. Do not bundle unrelated formatting or dependency updates.

## Privacy and commit attribution

Never commit credentials, personal settings, diagnostic logs, generated packages or private machine information. Review screenshots before uploading.

Use GitHub's private `noreply` commit email. Repository checks intentionally reject reachable history containing a personal Git email address so contributors do not accidentally publish it through EdgePilot's history.

A typical GitHub noreply address looks like:

```text
12345678+username@users.noreply.github.com
```

## Conduct

Be respectful and specific. Critique changes rather than people; harassment, personal attacks and publishing others' private information are not acceptable. The maintainer may close or moderate abusive discussions.

## License

Contributions are provided under the repository's MIT license. Preserve third-party notices.
