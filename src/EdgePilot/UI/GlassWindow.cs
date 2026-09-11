using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace EdgePilot.UI;

// Base for any full-window screen that should honour the Flat / Mica / Acrylic surface and
// the light/dark theme. It does the error-prone OS wiring once — the transparency hint, the
// window background and the theme variant — then hands the screen a ready GlassPalette to
// paint its own panels and cards. A new screen only has to:
//   1. derive from GlassWindow,
//   2. build its UI and keep references to the panels/cards it wants themed,
//   3. call ApplySurface(backdrop, theme) after building, and
//   4. assign the palette brushes in PaintChrome.
// It never re-implements the surface rules.
public abstract class GlassWindow : Window
{
    // Apply the chosen surface and theme to this window, then repaint the chrome.
    protected void ApplySurface(SettingsBackdrop backdrop, SettingsThemePreference theme)
    {
        RequestedThemeVariant = theme switch
        {
            SettingsThemePreference.Light => ThemeVariant.Light,
            SettingsThemePreference.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };

        var effectiveBackdrop = SettingsBackdropSupport.Coerce(backdrop);
        var dark = Glass.ResolveDark(theme);
        TransparencyLevelHint = Glass.WindowHint(effectiveBackdrop);
        Background = Glass.WindowBackground(effectiveBackdrop, dark);

        // ClearType/subpixel AA assumes an opaque RGB background. On Acrylic the pixels behind
        // the glyphs are continuously changing, which can produce the coloured/doubled fringe
        // visible in dark Settings. Use grayscale AA + strong hinting only for Acrylic, and
        // restore platform defaults for Flat/Mica.
        if (Content is Control root)
        {
            var acrylic = effectiveBackdrop == SettingsBackdrop.Acrylic;
            TextOptions.SetTextRenderingMode(root,
                acrylic ? TextRenderingMode.Antialias : TextRenderingMode.Unspecified);
            TextOptions.SetTextHintingMode(root,
                acrylic ? TextHintingMode.Strong : TextHintingMode.Unspecified);
            TextOptions.SetBaselinePixelAlignment(root,
                acrylic ? BaselinePixelAlignment.Aligned : BaselinePixelAlignment.Unspecified);
        }

        PaintChrome(Glass.Palette(effectiveBackdrop, dark));
    }

    // Paint this screen's own elements from the resolved palette.
    protected abstract void PaintChrome(GlassPalette palette);
}
