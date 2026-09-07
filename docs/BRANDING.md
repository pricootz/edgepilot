# Brand assets

The EdgePilot mark depicts a right-edge notch with three rounded bars suggesting E. Its palette combines graphite, off-white and orange.

- [Original supplied artwork](assets/edgepilot-original.png)
- [Application master](assets/edgepilot-master.png)
- Windows multi-resolution ICO: src/EdgePilot/Assets/edgepilot.ico
- Linux launcher image: src/EdgePilot/Assets/edgepilot.png

The original was supplied by the maintainer as AI-generated artwork. The master was adapted with the built-in image-generation tool, preserving the silhouette and bar arrangement while increasing its use of the canvas. It is raster artwork, not a hand-drawn vector.

Adaptation prompt: preserve the right-edge notch, concave top/bottom flares and three E-like bars; use graphite, off-white and orange; remove the surrounding background; center the tall mark with reduced vertical padding; preserve its proportions and identity; no text, shadow or mockup.

The packaging script converts the approved master into PNG and ICO sizes on GitHub Actions. It preserves alpha and checks icon decoding. These technical conversions do not require an image-generation service.

## Screenshots

The Windows 11 screenshots are maintainer-supplied, unaltered application captures of the current preview:

- [Settings and expanded notch](assets/windows11-overview.png)
- [Complete settings](assets/windows11-settings.png), including disk selection and start at login
- [Expanded notch](assets/windows11-notch.png), with CPU, memory and disk enabled
- [Collapsed pill](assets/windows11-pill.png)

The interface remains Italian. These captures replace the earlier Windows settings image.

Add Ubuntu captures when available, including light/dark settings themes and a collapsed/expanded notch pair. Avoid private hostnames and drive labels. A checked startup option in a screenshot does not replace verification after an actual login.
