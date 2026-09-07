# Third-party software

EdgePilot's own code is MIT-licensed. Dependencies and bundled runtimes retain their respective licenses.

Direct application dependencies:

- Avalonia, Avalonia.Desktop and Avalonia.Themes.Fluent 12.1.2: [Avalonia license](https://github.com/AvaloniaUI/Avalonia/blob/master/licence.md).
- .NET runtime: [license](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT) and [third-party notices](https://github.com/dotnet/runtime/blob/main/THIRD-PARTY-NOTICES.TXT).
- Avalonia.Headless is used by the regression suite and is not an application feature.
- Pillow is only used for developer asset conversion, not shipped as an application dependency.

Self-contained archives include transitive managed/native dependencies. Packaging generates a dependency inventory and copies available package/runtime license and notice files into third-party-licenses. Consult those notices for component-specific terms. This document does not replace their licenses.
