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

The Windows 11 screenshots are maintainer-supplied, unaltered application captures of the current preview:

- [Settings and expanded notch](assets/windows11-overview.png)
- [Complete settings](assets/windows11-settings.png), including disk selection and start at login
- [Expanded notch](assets/windows11-notch.png), with CPU, memory and disk enabled
- [Collapsed pill](assets/windows11-pill.png)

The interface remains Italian. These captures replace the earlier Windows settings image.

Add Ubuntu captures when available, including light/dark settings themes and a collapsed/expanded notch pair. Avoid private hostnames and drive labels. A checked startup option in a screenshot does not replace verification after an actual login.
