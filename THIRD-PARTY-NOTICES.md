# Third-party software

EdgePilot's own code is MIT-licensed. Dependencies and bundled runtimes retain their respective licenses.

Direct application dependencies:

- Avalonia, Avalonia.Desktop and Avalonia.Themes.Fluent 12.1.2: [Avalonia license](https://github.com/AvaloniaUI/Avalonia/blob/master/licence.md).
- FluentIcons.Avalonia 2.1.339.1: MIT-licensed Avalonia wrapper for [Microsoft Fluent UI System Icons](https://github.com/microsoft/fluentui-system-icons), used for the Settings navigation iconography.
- .NET runtime: [license](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT) and [third-party notices](https://github.com/dotnet/runtime/blob/main/THIRD-PARTY-NOTICES.TXT).
- Avalonia.Headless is used by the regression suite and is not an application feature.
- Pillow is only used for developer asset conversion, not shipped as an application dependency.

Display targeting resolver:

- Portions of `src/EdgePilot/UI/DisplayTarget.cs` are adapted from [`electron/main/geometry.ts`](https://github.com/Deepender25/Edge-Drop/blob/15ad660bdf643624355c31383c7f2a768c820ea8/electron/main/geometry.ts) in Edge-Drop.
- Copyright the Edge-Drop contributors. Licensed under Apache License 2.0.
- The source was ported to C#/Avalonia and modified for persistent choices, four-edge placement, cross-platform handles, localized labels and reconnect-safe fallback behavior.
- A copy of Apache License 2.0 is included at [`licenses/Apache-2.0.txt`](licenses/Apache-2.0.txt).

Self-contained archives include transitive managed/native dependencies. Packaging generates a dependency inventory and copies available package/runtime license and notice files into third-party-licenses. Consult those notices for component-specific terms. This document does not replace their licenses.
