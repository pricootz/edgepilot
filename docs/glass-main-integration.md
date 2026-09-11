# Glass surface integration

This branch adapts ArnieGA's PR #15 (`feat/glass-backdrop`) onto the current EdgePilot `main` after the native input-safety work from #17 and the repository/release hardening from #20.

## Reused from #15

- Flat / Mica / Acrylic surface preference and Settings UI.
- Shared `Glass` palette and `GlassWindow` base.
- Theme-aware metric-ring and tooltip styling.
- EN / ES / FR / IT surface strings.

## Current platform availability

- Windows 10 build 17134 (1803)+: Flat + Acrylic.
- Windows 11 build 22000+: Flat + Acrylic + Mica.
- Other platforms: Flat only.

Unsupported stored choices are coerced safely to Flat. Surface selection does not override the user's independent `System` / `Light` / `Dark` Settings theme choice.

## Current Windows notch architecture

The visible notch no longer uses `SetWindowRgn` as its visual boundary. Native regions quantize curves to device pixels and produced visibly stepped Mica/Acrylic edges.

Instead:

- the visible `EdgeWindow` is an Avalonia/Skia GPU surface and is made fully click-through with the documented Win32 layered-window style;
- the notch keeps the same approved antialiased geometry in Flat, Mica and Acrylic;
- a separate invisible `EdgeInputOverlayWindow` owns native pointer input only over the live notch / tooltip regions;
- global cursor polling keeps Hover activation working without stealing clicks from the surrounding desktop;
- `SetWindowRgn` is used only on the invisible input overlay, where pixel quantization cannot affect visible shape quality.

Linux remains on the #17 X11/XWayland `ShapeInput` path and Flat surface.

## Settings window

Settings is rectangular, so it can use the real OS backdrop directly:

- Mica uses the native Windows Mica hint where available;
- Acrylic uses the native Acrylic/Blur hint with restrained translucent chrome so the desktop blur remains visible;
- cards, panels and metric tiles all resolve from the same Glass palette;
- Acrylic uses grayscale antialiasing, strong text hinting and aligned baselines to avoid coloured subpixel fringes over a changing backdrop.

The Settings palette is intentionally isolated from `NotchMaterials`; tuning Settings must not change the approved notch geometry or material.

## Validation completed

- Windows 10: Flat + Acrylic Settings, theme persistence and click-through confirmed on real hardware;
- Windows 11: Flat + Mica + Acrylic Settings, all four notch edges and display modes confirmed on real hardware;
- Ubuntu GNOME/XWayland: input-region and Flat behavior confirmed on a real desktop;
- Windows and Ubuntu CI: build, UX, localization, native input-region, package launch, single-instance and per-user installation checks passed.
