using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace EdgePilot.UI;

// The resolved set of brushes for one surface + theme. A screen assigns these to its own
// panels, cards and text; it never needs to know the Flat/Mica/Acrylic rules itself.
public sealed record GlassPalette(
    bool IsGlass, bool IsMica, bool Dark,
    IBrush Panel, IBrush Card, IBrush CardBorder, Thickness CardBorderThickness,
    IBrush Line, Color MutedForeground);

// Shared "glass" styling so the settings shell, its cards and the notch/tooltip
// all read as one frosted surface over the Windows 11 desktop.
internal static class Glass
{
    public static bool IsGlass(SettingsBackdrop backdrop) => backdrop != SettingsBackdrop.Flat;

    // Resolve a theme choice to light/dark; System reads the OS.
    public static bool ResolveDark(SettingsThemePreference theme) => theme switch
    {
        SettingsThemePreference.Light => false,
        SettingsThemePreference.Dark => true,
        _ => Application.Current?.PlatformSettings?.GetColorValues().ThemeVariant
             != Avalonia.Platform.PlatformThemeVariant.Light
    };

    // The OS backdrop hint for a full-window (rectangular) screen, with fallbacks.
    public static IReadOnlyList<WindowTransparencyLevel> WindowHint(SettingsBackdrop backdrop) => backdrop switch
    {
        SettingsBackdrop.Mica =>
            [WindowTransparencyLevel.Mica, WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.Transparent],
        SettingsBackdrop.Acrylic =>
            [WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.Blur, WindowTransparencyLevel.Transparent],
        _ => [WindowTransparencyLevel.None]
    };

    // The window background: Mica stays transparent so the OS paints the wallpaper base,
    // Acrylic lays the luminosity veil over the live blur, Flat is a plain solid.
    public static IBrush WindowBackground(SettingsBackdrop backdrop, bool dark) => backdrop switch
    {
        SettingsBackdrop.Mica => Brushes.Transparent,
        SettingsBackdrop.Acrylic => AcrylicBase(dark),
        _ => new SolidColorBrush(Color.Parse(dark ? "#121212" : "#F4F4F5"))
    };

    // Everything a screen paints onto: panels, cards, separators and muted text.
    public static GlassPalette Palette(SettingsBackdrop backdrop, bool dark)
    {
        var glass = IsGlass(backdrop);
        var mica = backdrop == SettingsBackdrop.Mica;
        return new GlassPalette(glass, mica, dark,
            Panel: glass
                ? (mica ? Brushes.Transparent : AcrylicPanel(dark))
                : new SolidColorBrush(Color.Parse(dark ? "#151515" : "#FFFFFF")),
            Card: glass
                ? (mica ? MicaCard(dark) : AcrylicCard(dark))
                : new SolidColorBrush(Color.Parse(dark ? "#191919" : "#FFFFFF")),
            CardBorder: glass
                ? (mica ? MicaCardStroke(dark) : EdgeBrush(dark))
                : new SolidColorBrush(Color.Parse(dark ? "#2D2D2D" : "#E2E2E4")),
            CardBorderThickness: glass && !mica ? EdgeThickness : new Thickness(1),
            Line: mica ? MicaCardStroke(dark)
                : glass ? GlassLine(dark) : new SolidColorBrush(Color.Parse(dark ? "#2C2C2C" : "#DDDDDF")),
            MutedForeground: dark
                ? (backdrop == SettingsBackdrop.Acrylic ? Lighten(Color.Parse("#8D9096"), 0.45) : Color.Parse("#8D9096"))
                : glass ? Color.Parse("#44474D") : Color.Parse("#8D9096"));
    }

    // Bright thin top edge, a defined thicker bottom, no sides — the Windows 11 glass cue.
    public static Thickness EdgeThickness => new(0, 0.8, 0, 2);

    // Dark theme lifts a card with a bright rim; light theme needs a dark hairline instead,
    // since a white rim vanishes over a light surface.
    public static IBrush EdgeBrush(bool dark = true) => dark
        ? new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Color.FromArgb(0x6E, 255, 255, 255), 0.0),
                new GradientStop(Color.FromArgb(0x0F, 255, 255, 255), 0.35),
                new GradientStop(Color.FromArgb(0x4A, 255, 255, 255), 1.0)
            }
        }
        : new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Color.FromArgb(0x20, 0, 0, 0), 0.0),
                new GradientStop(Color.FromArgb(0x0A, 0, 0, 0), 0.35),
                new GradientStop(Color.FromArgb(0x24, 0, 0, 0), 1.0)
            }
        };

    // The window-wide luminosity veil that keeps Acrylic text readable over ANY wallpaper.
    // Windows' own light acrylic clamps the blurred backdrop toward a fixed luminance so
    // text contrast never depends on what is behind; we can't sample that, so we lay a
    // white sheet over the live blur. 0x99 (~60%) is the floor: even over a black desktop
    // the ground stays light enough for dark text, while ~40% of the blur still shows so
    // the acrylic actually reads as glass. Dark theme keeps light text over the raw blur.
    public static IBrush AcrylicBase(bool dark = true) =>
        dark ? Brushes.Transparent : new SolidColorBrush(Color.FromArgb(0x8C, 255, 255, 255));

    // Acrylic keeps the live blur; a thin veil separates chrome from content. Dark veils
    // with black, light with white, so text keeps its contrast either way.
    public static IBrush AcrylicPanel(bool dark = true) => new SolidColorBrush(
        dark ? Color.FromArgb(0x26, 0, 0, 0) : Color.FromArgb(0x7A, 255, 255, 255));
    public static IBrush AcrylicCard(bool dark = true) => new SolidColorBrush(
        dark ? Color.FromArgb(0x30, 0, 0, 0) : Color.FromArgb(0x59, 255, 255, 255));

    // Hairline between glass panels: bright over dark, dark over light.
    public static IBrush GlassLine(bool dark = true) => new SolidColorBrush(
        dark ? Color.FromArgb(0x24, 255, 255, 255) : Color.FromArgb(0x1F, 0, 0, 0));

    // Mica is the real Windows 11 OS backdrop: opaque, wallpaper-tinted, theme-aware — the
    // window keeps a transparent background and the OS paints it (see EdgeWindow/SettingsWindow
    // TransparencyLevelHint). Content then sits on translucent layers over that base, using
    // the exact WinUI theme values so it matches Windows itself:
    //   CardBackgroundFillColorDefault  Dark #0DFFFFFF  Light #B3FFFFFF
    //   CardStrokeColorDefault          Dark #19000000  Light #0F000000
    public static IBrush MicaCard(bool dark = true) => new SolidColorBrush(
        dark ? Color.FromArgb(0x0D, 255, 255, 255) : Color.FromArgb(0xB3, 255, 255, 255));
    public static IBrush MicaCardStroke(bool dark = true) => new SolidColorBrush(
        dark ? Color.FromArgb(0x19, 0, 0, 0) : Color.FromArgb(0x0F, 0, 0, 0));

    // Nudge a colour toward white by `amount` (0..1), keeping its alpha. Used to keep
    // muted text readable over the bright blur without flattening everything to pure white.
    public static Color Lighten(Color c, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromArgb(c.A,
            (byte)(c.R + (255 - c.R) * amount),
            (byte)(c.G + (255 - c.G) * amount),
            (byte)(c.B + (255 - c.B) * amount));
    }
}
