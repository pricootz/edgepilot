# Contributing to EdgePilot

EdgePilot is an early Windows/Linux desktop preview. Small, focused fixes and real-desktop test reports are welcome. Please use English for issues, pull requests and documentation. Keep existing Italian application strings consistent; do not translate the app as part of an unrelated change.

## Development

Install the .NET 10 SDK, clone the repository, and create a branch from main. Committed icons are ready to build; Python is only needed to regenerate brand assets.

```bash
dotnet build src/EdgePilot/EdgePilot.csproj -c Release
dotnet run --project tests/EdgePilot.UxChecks -c Release
dotnet run --project src/EdgePilot/EdgePilot.csproj -c Release -- --settings
```

The tests are an executable headless suite, not a dotnet test project. CI additionally publishes and launches packages and checks per-user installation on disposable Windows and Ubuntu runners.

## Pull requests

Explain the problem, resulting behavior and verification. Keep platform-specific APIs in Platform, sampling contracts in Core, and interaction/rendering in UI. Preserve existing metrics and settings compatibility. Add regression coverage when a behavior fix merits it.

For visual changes, include screenshots and desktop checks at relevant scaling levels. State whether Linux testing used X11 or Wayland. Headless checks alone cannot validate compositor transparency or pointer behavior.

Discuss major new features in an issue first. Do not bundle unrelated formatting or dependency updates.

## Privacy and conduct

Never commit credentials, personal settings, diagnostic logs, generated packages or private machine information. Review screenshots before uploading. Use GitHub's private commit email; repository checks require private GitHub attribution to keep personal addresses out of commit history.

Be respectful and specific. Critique changes rather than people; harassment, personal attacks and publishing others' private information are not acceptable. The maintainer may close or moderate abusive discussions.

## License

Contributions are provided under the repository's MIT license. Preserve third-party notices.
