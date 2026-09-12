# Brand assets

The EdgePilot mark depicts a right-edge notch with three rounded bars suggesting E. Its palette combines graphite, off-white and orange.

- Canonical vector artwork: [src/EdgePilot/Assets/edgepilot.svg](../src/EdgePilot/Assets/edgepilot.svg)
- Windows multi-resolution ICO: `src/EdgePilot/Assets/edgepilot.ico`
- Small-size review sheet: [assets/icon-size-check.png](assets/icon-size-check.png)

The SVG is the single source of truth for the EdgePilot mark. It has a transparent background and is used directly by the Linux desktop launcher and by repository documentation. Do not add a raster PNG copy of the logo.

Windows executable, shortcut, tray and window icon integration still requires the generated ICO. `scripts/prepare_brand_assets.py` rasterizes the SVG only for that native format and for the small-size QA preview. The generated files must stay in sync with the SVG.

To regenerate the derived assets on a system with Python 3.12, Pillow and CairoSVG:

```bash
python scripts/prepare_brand_assets.py
```

CI regenerates the derived assets on Ubuntu and fails if the committed ICO or review sheet differs from the SVG source.

## Screenshots

The Windows 11 gallery was captured for the initial v0.1 preview. It remains useful as a visual baseline, but it is not a complete representation of v0.2:

- [Settings and expanded notch](assets/windows11-overview.png) predates the v0.2 multi-page Settings redesign.
- [Complete settings](assets/windows11-settings.png) predates the v0.2 multi-page Settings redesign.
- [Expanded notch](assets/windows11-notch.png) still illustrates the supported Flat System surface.
- [Collapsed pill](assets/windows11-pill.png) still illustrates the supported Flat System surface.

Do not use the old Settings/overview captures to claim the current v0.2 or Windows Glass appearance. Replace them with real unaltered captures of the redesigned Settings window under relevant Flat/Mica/Acrylic and Light/Dark combinations.

Add Ubuntu captures when available, including light/dark settings themes and a collapsed/expanded notch pair. Avoid private hostnames and drive labels. A checked startup option in a screenshot does not replace verification after an actual login.
