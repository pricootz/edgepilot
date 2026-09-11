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

    // Rectangular Settings windows can use the real OS backdrop. Acrylic therefore keeps only a
    // restrained tint here; the native blur remains the dominant material instead of being buried
    // under a nearly opaque graphite layer.
    public static IBrush WindowBackground(SettingsBackdrop backdrop, bool dark) => Effective(backdrop) switch
    {
        SettingsBackdrop.Mica => Brushes.Transparent,
        SettingsBackdrop.Acrylic => SettingsAcrylicBase(dark),
        _ => new SolidColorBrush(Color.Parse(dark ? "#121212" : "#F4F4F5"))
    };

    // Everything a full-window screen paints onto: panels, cards, separators and muted text.
    // Acrylic uses Settings-only layers here. The notch owns an independent NotchMaterials stack,
    // so tuning this palette cannot alter its approved geometry or material.
    public static GlassPalette Palette(SettingsBackdrop backdrop, bool dark)
    {
        backdrop = Effective(backdrop);
        var glass = backdrop != SettingsBackdrop.Flat;
        var mica = backdrop == SettingsBackdrop.Mica;
        var acrylic = backdrop == SettingsBackdrop.Acrylic;
        return new GlassPalette(glass, mica, dark,
            Panel: glass
                ? (mica ? Brushes.Transparent : SettingsAcrylicPanel(dark))
                : new SolidColorBrush(Color.Parse(dark ? "#151515" : "#FFFFFF")),
            Card: glass
                ? (mica ? MicaCard(dark) : SettingsAcrylicCard(dark))
                : new SolidColorBrush(Color.Parse(dark ? "#191919" : "#FFFFFF")),
            CardBorder: glass
                ? (mica ? MicaCardStroke(dark) : EdgeBrush(dark))
                : new SolidColorBrush(Color.Parse(dark ? "#2D2D2D" : "#E2E2E4")),
            CardBorderThickness: glass && !mica ? EdgeThickness : new Thickness(1),
            Line: mica ? MicaCardStroke(dark)
                : glass ? GlassLine(dark) : new SolidColorBrush(Color.Parse(dark ? "#2C2C2C" : "#DDDDDF")),
            // Secondary copy must remain readable over both solid and blurred backgrounds.
            // Keep Acrylic a little brighter because its backdrop luminance varies with the desktop.
            MutedForeground: dark
                ? (acrylic ? Color.Parse("#D4D8DF") : Color.Parse("#A9AFB8"))
                : glass ? Color.Parse("#4A4F57") : Color.Parse("#61666E"));
    }

    // Restore Arnie's stronger glass edge. It is part of what makes the blurred material read
    // as a deliberate surface instead of a grey translucent fill.
    public static Thickness EdgeThickness => new(0, 0.8, 0, 2);

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

    // Full-window Settings Acrylic: native Acrylic first, dark chrome second.
    // Dark mode uses roughly 6% base tint, 17% panel tint and 33% card tint. These are brush
    // alpha values, never Control.Opacity, so the real backdrop remains visible while glyphs stay
    // fully opaque and benefit from the Acrylic-specific grayscale text rendering in GlassWindow.
    private static IBrush SettingsAcrylicBase(bool dark) => new SolidColorBrush(
        dark ? Color.FromArgb(0x10, 7, 10, 15) : Color.FromArgb(0x8C, 255, 255, 255));

    private static IBrush SettingsAcrylicPanel(bool dark) => new SolidColorBrush(
        dark ? Color.FromArgb(0x2C, 9, 13, 19) : Color.FromArgb(0x7A, 255, 255, 255));

    private static IBrush SettingsAcrylicCard(bool dark) => new SolidColorBrush(
        dark ? Color.FromArgb(0x54, 13, 17, 24) : Color.FromArgb(0x59, 255, 255, 255));

    // Legacy/local Acrylic brushes remain intentionally lighter. They are used outside the
    // rectangular Settings shell (for example tooltip styling), where a heavy scrim is unwanted.
    public static IBrush AcrylicBase(bool dark = true) =>
        dark ? new SolidColorBrush(Color.FromArgb(0x24, 0, 0, 0))
             : new SolidColorBrush(Color.FromArgb(0x8C, 255, 255, 255));

    public static IBrush AcrylicPanel(bool dark = true) => new SolidColorBrush(
        dark ? Color.FromArgb(0x30, 0, 0, 0) : Color.FromArgb(0x7A, 255, 255, 255));

    public static IBrush AcrylicCard(bool dark = true) => new SolidColorBrush(
        dark ? Color.FromArgb(0x46, 0, 0, 0) : Color.FromArgb(0x59, 255, 255, 255));

    // Hairline between glass panels: bright over dark, dark over light.
    public static IBrush GlassLine(bool dark = true) => new SolidColorBrush(
        dark ? Color.FromArgb(0x24, 255, 255, 255) : Color.FromArgb(0x1F, 0, 0, 0));

    // Mica stays on Arnie's original Windows-11 treatment.
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
