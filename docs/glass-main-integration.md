# Glass surface integration

This branch adapts ArnieGA's PR #15 (`feat/glass-backdrop`) onto the current EdgePilot `main` after the native input-safety work from #17.

## Reused from #15

- Flat / Mica / Acrylic surface preference and Settings UI.
- Shared `Glass` palette and `GlassWindow` base.
- Theme-aware metric-ring and tooltip styling.
- EN / ES / FR / IT surface strings.

## Deliberate integration changes

- `NotchRegion` is not imported. `PlatformInputRegion` remains the single owner of the native edge-window region.
- Mica/Acrylic are gated to Windows 11 build 22000+; unsupported stored choices are coerced to Flat.
- Flat preserves the existing dark EdgePilot notch regardless of the Settings-window theme.
- On Windows glass surfaces, the native window region contains only the visible notch silhouette plus the rounded tooltip. The invisible Hover hot-zone and bridge are omitted from `SetWindowRgn`; Windows' existing global cursor polling keeps those behaviors working without exposing or blocking an invisible rectangle.
- Linux remains on the #17 X11/XWayland `ShapeInput` path and Flat surface.

## Validation target

Before merge, verify Windows 11 Mica/Acrylic on all four edges and all display modes, including hover sensitivity, popup transitions, DPI scaling, and click-through over browser controls. Re-run the normal Windows/Linux CI and ensure Linux behavior remains unchanged.
