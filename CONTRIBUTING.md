# Contributing to EdgePilot

EdgePilot is an early Windows/Linux desktop preview. Small, focused fixes and real-desktop test reports are welcome. Please use English for issues, pull requests and documentation.

Interface text lives in `src/EdgePilot/Assets/Locales`, one JSON file per language. Do not add hardcoded interface text or edit one language in isolation as part of an unrelated change. See [Translating EdgePilot](docs/TRANSLATING.md) for the translation workflow.

## Development

Install the .NET 10 SDK, clone the repository, and create a branch from `main`. Committed icons are ready to build; Python is only needed to regenerate brand assets and to run repository validation.

```bash
dotnet build src/EdgePilot/EdgePilot.csproj -c Release
dotnet run --project tests/EdgePilot.UxChecks -c Release
dotnet run --project tests/EdgePilot.LocalizationChecks -c Release
python scripts/check_repository.py
dotnet run --project src/EdgePilot/EdgePilot.csproj -c Release -- --settings
```

The tests are executable headless suites, not `dotnet test` projects. CI additionally publishes and launches packages and checks per-user installation on disposable Windows and Ubuntu runners.

## Pull requests

Explain the problem, resulting behavior and verification. Keep platform-specific APIs in `Platform`, sampling contracts in `Core`, and interaction/rendering in `UI`. Preserve existing metrics, settings and preference-file compatibility. Add regression coverage when a behavior fix merits it.

For visual changes, include screenshots and desktop checks at relevant scaling levels. State whether Linux testing used X11 or Wayland. Headless checks alone cannot validate compositor transparency or pointer behavior.

For localization contributions, keep all locale files key-compatible with `en.json`. Adding a language should normally mean adding a locale file plus any necessary documentation/test evidence, not introducing a new language enum or C# translation catalog.

Discuss major new features in an issue first. Do not bundle unrelated formatting or dependency updates.

## Privacy and conduct

Never commit credentials, personal settings, diagnostic logs, generated packages or private machine information. Review screenshots before uploading. Use GitHub's private commit email; repository checks require private GitHub attribution to keep personal addresses out of commit history.

Be respectful and specific. Critique changes rather than people; harassment, personal attacks and publishing others' private information are not acceptable. The maintainer may close or moderate abusive discussions.

## License

Contributions are provided under the repository's MIT license. Preserve third-party notices.
