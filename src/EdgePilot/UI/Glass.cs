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
// all read as one frosted surface over the Windows desktop.
internal static class Glass
{
    private static SettingsBackdrop Effective(SettingsBackdrop backdrop) =>
        SettingsBackdropSupport.Coerce(backdrop);

    public static bool IsGlass(SettingsBackdrop backdrop) => Effective(backdrop) != SettingsBackdrop.Flat;

    // Resolve a theme choice to light/dark; System reads the OS.
    public static bool ResolveDark(SettingsThemePreference theme) => theme switch
    {
        SettingsThemePreference.Light => false,
        SettingsThemePreference.Dark => true,
        _ => Application.Current?.PlatformSettings?.GetColorValues().ThemeVariant
             != Avalonia.Platform.PlatformThemeVariant.Light
    };

    // The OS backdrop hint for a full-window (rectangular) screen, with fallbacks.
    public static IReadOnlyList<WindowTransparencyLevel> WindowHint(SettingsBackdrop backdrop) => Effective(backdrop) switch
    {
        SettingsBackdrop.Mica =>
            [WindowTransparencyLevel.Mica, WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.Transparent],
        SettingsBackdrop.Acrylic =>
            [WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.Blur, WindowTransparencyLevel.Transparent],
        _ => [WindowTransparencyLevel.None]
    };

    // Acrylic deliberately keeps a dark graphite veil. The blur remains visible but EdgePilot
    // still reads as the same black notch instead of inheriting the wallpaper colour.
    public static IBrush WindowBackground(SettingsBackdrop backdrop, bool dark) => Effective(backdrop) switch
    {
        SettingsBackdrop.Mica => Brushes.Transparent,
        SettingsBackdrop.Acrylic => AcrylicBase(dark),
        _ => new SolidColorBrush(Color.Parse(dark ? "#121212" : "#F4F4F5"))
    };

    // Everything a screen paints onto: panels, cards, separators and muted text.
    public static GlassPalette Palette(SettingsBackdrop backdrop, bool dark)
    {
        backdrop = Effective(backdrop);
        var glass = backdrop != SettingsBackdrop.Flat;
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
                ? (backdrop == SettingsBackdrop.Acrylic ? Lighten(Color.Parse("#8D9096"), 0.20) : Color.Parse("#8D9096"))
                : glass ? Color.Parse("#44474D") : Color.Parse("#8D9096"));
    }

    // Restrained glass edge: just enough contrast to separate the material from bright wallpaper.
    public static Thickness EdgeThickness => new(0, 0.6, 0, 1.2);

    public static IBrush EdgeBrush(bool dark = true) => dark
        ? new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Color.FromArgb(0x28, 255, 255, 255), 0.0),
                new GradientStop(Color.FromArgb(0x08, 255, 255, 255), 0.45),
                new GradientStop(Color.FromArgb(0x20, 255, 255, 255), 1.0)
            }
        }
        : new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Color.FromArgb(0x18, 0, 0, 0), 0.0),
                new GradientStop(Color.FromArgb(0x08, 0, 0, 0), 0.45),
                new GradientStop(Color.FromArgb(0x20, 0, 0, 0), 1.0)
            }
        };

    // Dark graphite material stack. Values are intentionally dense: Acrylic should read as
    // black glass, not as a wallpaper-coloured translucent slab.
    public static IBrush AcrylicBase(bool dark = true) => new SolidColorBrush(
        dark ? Color.FromArgb(0x50, 0x05, 0x06, 0x08) : Color.FromArgb(0x8C, 255, 255, 255));

    public static IBrush AcrylicPanel(bool dark = true) => new SolidColorBrush(
        dark ? Color.FromArgb(0x86, 0x05, 0x06, 0x08) : Color.FromArgb(0x7A, 255, 255, 255));

    public static IBrush AcrylicCard(bool dark = true) => new SolidColorBrush(
        dark ? Color.FromArgb(0xB8, 0x05, 0x06, 0x08) : Color.FromArgb(0x59, 255, 255, 255));

    // Hairline between glass panels: bright over dark, dark over light.
    public static IBrush GlassLine(bool dark = true) => new SolidColorBrush(
        dark ? Color.FromArgb(0x20, 255, 255, 255) : Color.FromArgb(0x1F, 0, 0, 0));

    // Mica is the real Windows 11 OS backdrop: opaque, wallpaper-tinted, theme-aware.
    public static IBrush MicaCard(bool dark = true) => new SolidColorBrush(
        dark ? Color.FromArgb(0x0D, 255, 255, 255) : Color.FromArgb(0xB3, 255, 255, 255));
    public static IBrush MicaCardStroke(bool dark = true) => new SolidColorBrush(
        dark ? Color.FromArgb(0x19, 0, 0, 0) : Color.FromArgb(0x0F, 0, 0, 0));

    // Nudge a colour toward white by `amount` (0..1), keeping its alpha.
    public static Color Lighten(Color c, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromArgb(c.A,
            (byte)(c.R + (255 - c.R) * amount),
            (byte)(c.G + (255 - c.G) * amount),
            (byte)(c.B + (255 - c.B) * amount));
    }
}