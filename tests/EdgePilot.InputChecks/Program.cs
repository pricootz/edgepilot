using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using EdgePilot.Core;
using EdgePilot.UI;

AppBuilder.Configure<Application>()
    .UseSkia()
    .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
    .SetupWithoutStarting();

const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
var passed = 0;

void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    Console.WriteLine("PASS " + name);
    passed++;
}

object? Call(EdgeWindow window, string name, params object[] args) =>
    typeof(EdgeWindow).GetMethod(name, Flags)!.Invoke(window, args);

void Set(EdgeWindow window, string name, object value) =>
    typeof(EdgeWindow).GetField(name, Flags)!.SetValue(window, value);

Rect[] Regions(EdgeWindow window) => (Rect[])Call(window, "InteractiveRects")!;
Rect[] ShapeRegions(EdgeWindow window) => (Rect[])Call(window, "ShapeInputRects")!;
Rect[] GlassRegions(EdgeWindow window) => (Rect[])Call(window, "GlassVisibleRects")!;
bool Interactive(EdgeWindow window, Point point) => (bool)Call(window, "IsInteractive", point)!;

Point? FindPoint(Rect area, Func<Point, bool> predicate)
{
    for (var y = area.Top + 0.5; y < area.Bottom; y += 2)
    for (var x = area.Left + 0.5; x < area.Right; x += 2)
    {
        var point = new Point(x, y);
        if (predicate(point)) return point;
    }
    return null;
}

var window = new EdgeWindow();

foreach (var edge in Enum.GetValues<EdgeSide>())
{
    var size = NotchLayout.WindowSize(edge);
    var windowRect = new Rect(size);

    window.ApplyPreferences(new NotchPreferences(edge, NotchDisplayMode.Hover)
    {
        Sensitivity = HoverSensitivity.Normal
    });
    Set(window, "_expanded", false);
    Set(window, "_expansion", 0d);
    Call(window, "UpdateNotchVisual");

    var collapsedShape = ShapeRegions(window);
    var collapsed = Regions(window);
    var hot = (Rect)Call(window, "HotZoneRect")!;
    var shapeBounds = (Rect)Call(window, "ShapeRect")!;
    Check(collapsedShape.Length > 1, $"collapsed notch is represented by silhouette strips on {edge}");
    Check(collapsed.Length == collapsedShape.Length + 1,
        $"hover collapsed exposes only hot-zone and silhouette on {edge}");
    Check(collapsed.All(windowRect.Contains), $"collapsed regions stay inside window on {edge}");
    Check(collapsed.All(r => r.Width * r.Height < size.Width * size.Height),
        $"collapsed regions never become the full transparent window on {edge}");
    Check(Interactive(window, hot.Center), $"hover hot-zone remains interactive on {edge}");
    Check(Interactive(window, shapeBounds.Center), $"collapsed notch remains interactive on {edge}");
    Check(!Interactive(window, windowRect.Center), $"transparent window center passes through on {edge}");

    // Windows glass uses SetWindowRgn as both visual clipping and hit testing. The adapted
    // integration must therefore omit the invisible Hover hot-zone from the native glass
    // region; global cursor polling keeps Hover working on Windows without exposing it.
    Set(window, "_backdrop", SettingsBackdrop.Mica);
    var glassCollapsed = GlassRegions(window);
    var hotOnly = FindPoint(hot, point => !collapsedShape.Any(rect => rect.Contains(point)));
    Check(glassCollapsed.Length == collapsedShape.Length,
        $"glass collapsed region contains only visible notch strips on {edge}");
    Check(hotOnly is not null, $"collapsed hover has an invisible hot-zone sample on {edge}");
    Check(hotOnly is { } hotPoint && !glassCollapsed.Any(rect => rect.Contains(hotPoint)),
        $"glass excludes invisible hover hot-zone on {edge}");
    Set(window, "_backdrop", SettingsBackdrop.Flat);

    Set(window, "_expanded", true);
    Set(window, "_expansion", 1d);
    Call(window, "UpdateNotchVisual");
    var expandedShape = ShapeRegions(window);
    var expanded = Regions(window);
    Check(expandedShape.Length > collapsedShape.Length, $"expanded silhouette grows with the notch on {edge}");
    Check(expanded.Length == expandedShape.Length + 1,
        $"expanded hover keeps hot-zone and silhouette only on {edge}");
    Check(expanded.All(windowRect.Contains), $"expanded regions stay inside window on {edge}");
    Check(!Interactive(window, windowRect.Center), $"expanded transparent area passes through on {edge}");

    Call(window, "SetHoveredMetric", 0);
    var withTooltip = Regions(window);
    var tooltip = (Rect)Call(window, "TooltipLiveRect")!;
    var bridge = (Rect)Call(window, "BridgeRect")!;
    Check(withTooltip.Length == expandedShape.Length + 3,
        $"tooltip adds only card and bridge to live regions on {edge}");
    Check(tooltip.Width > 0 && tooltip.Height > 0 && Interactive(window, tooltip.Center),
        $"tooltip stays interactive on {edge}");
    Check(bridge.Width > 0 && bridge.Height > 0 && Interactive(window, bridge.Center),
        $"tooltip bridge stays interactive on {edge}");
    Check(withTooltip.All(windowRect.Contains), $"tooltip regions stay inside window on {edge}");

    // The glass bounding region includes the visible popup, but not the invisible bridge.
    // Rounded scanline clipping also prevents Mica/Acrylic from showing through the card's
    // square corner pixels.
    Set(window, "_backdrop", SettingsBackdrop.Acrylic);
    var glassWithTooltip = GlassRegions(window);
    Check(glassWithTooltip.Any(rect => rect.Contains(tooltip.Center)),
        $"glass includes tooltip center on {edge}");
    Check(!glassWithTooltip.Any(rect => rect.Contains(bridge.Center)),
        $"glass excludes tooltip bridge on {edge}");
    var tooltipCorner = new Point(tooltip.Left + 0.1, tooltip.Top + 0.1);
    Check(!glassWithTooltip.Any(rect => rect.Contains(tooltipCorner)),
        $"glass clips rounded tooltip corner on {edge}");
    Check(glassWithTooltip.All(windowRect.Contains), $"glass visible regions stay inside window on {edge}");
    Set(window, "_backdrop", SettingsBackdrop.Flat);

    window.ApplyPreferences(new NotchPreferences(edge, NotchDisplayMode.Hidden));
    Check(Regions(window).Length == 0, $"hidden mode has an empty native input region on {edge}");
    Check(!Interactive(window, hot.Center), $"hidden mode accepts no pointer input on {edge}");

    window.ApplyPreferences(new NotchPreferences(edge, NotchDisplayMode.Always));
    Set(window, "_expanded", true);
    Set(window, "_expansion", 1d);
    Call(window, "UpdateNotchVisual");
    var alwaysShape = ShapeRegions(window);
    var always = Regions(window);
    Check(always.Length == alwaysShape.Length,
        $"always mode exposes only the exact current notch without tooltip on {edge}");
    Check(always.All(windowRect.Contains), $"always region stays inside window on {edge}");
    Check(!Interactive(window, windowRect.Center), $"always mode transparent area passes through on {edge}");

    // This point is inside the rectangular bounds used by the old hit testing, but outside the
    // visible expanded notch shoulder. It must never become an invisible input blocker.
    const double expandedDepth = 88;
    const double expandedLength = 408; // 4*78 + 3*10 + 66 with all metrics visible.
    var transparentShoulderDesign = new Point(
        NotchLayout.DesignWidth - expandedDepth + 1,
        (NotchLayout.DesignHeight - expandedLength) / 2 + 1);
    var transparentShoulder = NotchLayout.ToScreen(transparentShoulderDesign, edge);
    var currentBounds = (Rect)Call(window, "ShapeRect")!;
    Check(currentBounds.Contains(transparentShoulder),
        $"regression point remains inside old rectangular notch bounds on {edge}");
    Check(!Interactive(window, transparentShoulder),
        $"transparent notch shoulder passes through on {edge}");
}

// Appearance state must survive the live EdgeWindow -> SettingsWindow round trip. Unsupported
// backdrops intentionally coerce to Flat rather than persisting a dead choice.
foreach (var theme in Enum.GetValues<SettingsThemePreference>())
{
    window.ApplyPreferences(new NotchPreferences { SettingsTheme = theme });
    Check(window.Preferences.SettingsTheme == theme, $"settings theme round trips {theme}");
}
foreach (var backdrop in Enum.GetValues<SettingsBackdrop>())
{
    window.ApplyPreferences(new NotchPreferences { Backdrop = backdrop });
    var expected = SettingsBackdropSupport.IsSupported(backdrop) ? backdrop : SettingsBackdrop.Flat;
    Check(window.Preferences.Backdrop == expected, $"backdrop coerces or round trips {backdrop}");
}
window.ApplyPreferences(new NotchPreferences());

// Native regions use integral device pixels. Verify that two adjacent logical scanlines remain
// adjacent after fractional-DPI quantization instead of creating a one-pixel hole between them.
var platformType = typeof(EdgeWindow).Assembly.GetType("EdgePilot.Platform.PlatformInputRegion")!;
var toNative = platformType.GetMethod("ToNativeRectangles", BindingFlags.Static | BindingFlags.NonPublic)!;
var nativeArray = (Array)toNative.Invoke(null, new object[]
{
    new[]
    {
        new Rect(100.2, 50.1, 20.4, 1.0),
        new Rect(100.2, 51.1, 20.4, 1.0)
    },
    1.25d,
    new Size(NotchLayout.DesignWidth, NotchLayout.DesignHeight)
})!;
Check(nativeArray.Length == 2, "fractional-DPI scanlines survive native quantization");
var nativeType = nativeArray.GetType().GetElementType()!;
var first = nativeArray.GetValue(0)!;
var second = nativeArray.GetValue(1)!;
var bottom = (int)nativeType.GetField("Bottom", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(first)!;
var top = (int)nativeType.GetField("Top", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(second)!;
Check(bottom == top, "fractional-DPI scanlines share a native pixel boundary");

Console.WriteLine($"{passed} input-region checks passed.");
