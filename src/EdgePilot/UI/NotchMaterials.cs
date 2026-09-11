using Avalonia.Media;

namespace EdgePilot.UI;

/// <summary>
/// Local notch materials. Full-window Mica/Acrylic remain available to rectangular settings
/// windows, but the edge notch renders its own material inside the antialiased notch geometry so
/// the visible surface is never clipped by a pixel-quantized native window region.
/// </summary>
internal static class NotchMaterials
{
    public static ExperimentalAcrylicMaterial Acrylic(bool dark) => new()
    {
        BackgroundSource = AcrylicBackgroundSource.Digger,
        TintColor = Color.Parse(dark ? "#090B0F" : "#F5F7FA"),
        TintOpacity = dark ? 0.50 : 0.58,
        MaterialOpacity = dark ? 0.30 : 0.24,
        FallbackColor = Color.Parse(dark ? "#24272D" : "#E7E9ED")
    };

    public static IBrush MicaFill(bool dark) => new SolidColorBrush(
        dark ? Color.Parse("#CC2B2D31") : Color.Parse("#E6ECEEF1"));

    public static IBrush MicaStroke(bool dark) => new SolidColorBrush(
        dark ? Color.FromArgb(0x72, 255, 255, 255) : Color.FromArgb(0x30, 0, 0, 0));

    public static IBrush AcrylicOverlay(bool dark) => new SolidColorBrush(
        dark ? Color.FromArgb(0x18, 0, 0, 0) : Color.FromArgb(0x12, 255, 255, 255));

    public static IBrush AcrylicStroke(bool dark) => new SolidColorBrush(
        dark ? Color.FromArgb(0x90, 255, 255, 255) : Color.FromArgb(0x38, 0, 0, 0));

    public static IBrush FlatFill => new SolidColorBrush(Color.Parse("#050608"));
    public static IBrush FlatStroke => new SolidColorBrush(Color.FromArgb(0x30, 255, 255, 255));
}
