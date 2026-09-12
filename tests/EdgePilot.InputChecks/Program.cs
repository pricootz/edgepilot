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
Rect[] VisibleInputRegions(EdgeWindow window) => (Rect[])Call(window, "VisibleInputRects")!;
Rect[] VisibleWindowRegions(EdgeWindow window) => (Rect[])Call(window, "VisibleWindowRects")!;
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
    Check(Interactive(window, hot.Center), $"hover hot-zone remains logically interactive on {edge}");
    Check(Interactive(window, shapeBounds.Center), $"collapsed notch remains interactive on {edge}");
    Check(!Interactive(window, windowRect.Center), $"transparent window center passes through on {edge}");

    // Windows uses a render-only click-through visual HWND plus a second invisible shaped input
    // overlay. The overlay must never include Hover's invisible hot-zone; global cursor polling
    // keeps Hover working without stealing clicks from applications behind EdgePilot.
    var visibleCollapsed = VisibleInputRegions(window);
    var hotOnly = FindPoint(hot, point => !collapsedShape.Any(rect => rect.Contains(point)));
    Check(visibleCollapsed.Length > 0 && visibleCollapsed.Length <= collapsedShape.Length &&
          visibleCollapsed.All(rect => collapsedShape.Any(shapeRect => shapeRect.Contains(rect))),
        $"Windows collapsed input overlay stays conservatively inside notch strips on {edge}");
    var contourOnly = FindPoint(shapeBounds, point =>
        collapsedShape.Any(rect => rect.Contains(point)) &&
        !visibleCollapsed.Any(rect => rect.Contains(point)));
    Check(contourOnly is not null,
        $"Windows collapsed input overlay leaves a two-DIP contour safety margin on {edge}");
    Check(hotOnly is not null, $"collapsed hover has an invisible hot-zone sample on {edge}");
    Check(hotOnly is { } hotPoint && !visibleCollapsed.Any(rect => rect.Contains(hotPoint)),
        $"Windows input overlay excludes invisible hover hot-zone on {edge}");
    var visualCollapsed = VisibleWindowRegions(window);
    Check(visualCollapsed.Length > 0 &&
          visibleCollapsed.All(input => visualCollapsed.Any(visual => visual.Contains(input))),
        $"Windows visual safety region contains the shaped input overlay on {edge}");
    Check(!visualCollapsed.Any(rect => rect.Contains(windowRect.Center)),
        $"Windows visual safety region excludes the transparent window center on {edge}");

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
        $"tooltip adds only card and bridge to logical live regions on {edge}");
    Check(tooltip.Width > 0 && tooltip.Height > 0 && Interactive(window, tooltip.Center),
        $"tooltip stays logically interactive on {edge}");
    Check(bridge.Width > 0 && bridge.Height > 0 && Interactive(window, bridge.Center),
        $"tooltip bridge stays logically interactive on {edge}");
    Check(withTooltip.All(windowRect.Contains), $"tooltip regions stay inside window on {edge}");

    // The invisible Windows input overlay includes the visible popup, but not the invisible
    // bridge. Rounded scanline clipping prevents square tooltip-corner pixels from taking input.
    var visibleWithTooltip = VisibleInputRegions(window);
    Check(visibleWithTooltip.Any(rect => rect.Contains(tooltip.Center)),
        $"Windows input overlay includes tooltip center on {edge}");
    Check(!visibleWithTooltip.Any(rect => rect.Contains(bridge.Center)),
        $"Windows input overlay excludes tooltip bridge on {edge}");
    var tooltipCorner = new Point(tooltip.Left + 0.1, tooltip.Top + 0.1);
    Check(!visibleWithTooltip.Any(rect => rect.Contains(tooltipCorner)),
        $"Windows input overlay clips rounded tooltip corner on {edge}");
    Check(visibleWithTooltip.All(windowRect.Contains), $"Windows input overlay stays inside window on {edge}");

    window.ApplyPreferences(new NotchPreferences(edge, NotchDisplayMode.Hidden));
    Check(Regions(window).Length == 0, $"hidden mode has an empty logical input region on {edge}");
    Check(VisibleInputRegions(window).Length == 0, $"hidden mode has an empty Windows input overlay on {edge}");
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
    var visibleAlways = VisibleInputRegions(window);
    Check(visibleAlways.Length > 0 && visibleAlways.Length <= alwaysShape.Length &&
          visibleAlways.All(rect => alwaysShape.Any(shapeRect => shapeRect.Contains(rect))),
        $"Windows always-mode overlay stays inside the visible notch on {edge}");

    // This point is inside the expanded notch's old rectangular bounds, but outside its curved
    // shoulder. It must never become an invisible input blocker.
    const double expandedDepth = 92;
    const double expandedLength = 430; // 4*76 + 3*10 + 2*24 flare + 2*24 safe padding.
    var transparentShoulderDesign = new Point(
        NotchLayout.DesignWidth - expandedDepth + 1,
        (NotchLayout.DesignHeight - expandedLength) / 2 + 1);
    var transparentShoulder = NotchLayout.ToScreen(transparentShoulderDesign, edge);
    var currentBounds = (Rect)Call(window, "ShapeRect")!;
    Check(currentBounds.Contains(transparentShoulder),
        $"regression point remains inside rectangular notch bounds on {edge}");
    Check(!Interactive(window, transparentShoulder),
        $"transparent notch shoulder passes through on {edge}");
    Check(!VisibleInputRegions(window).Any(rect => rect.Contains(transparentShoulder)),
        $"Windows input overlay excludes transparent notch shoulder on {edge}");
    Check(!VisibleWindowRegions(window).Any(rect =>
              rect.Width >= size.Width * 0.9 && rect.Height >= size.Height * 0.9),
        $"Windows visual safety region never becomes the full transparent window on {edge}");
}

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

var platformType = typeof(EdgeWindow).Assembly.GetType("EdgePilot.Platform.PlatformInputRegion")!;
var toNative = platformType.GetMethod("ToNativeRectangles", BindingFlags.Static | BindingFlags.NonPublic)!;
foreach (var scale in new[] { 1d, 1.25d, 1.5d, 2d })
{
    var nativeArray = (Array)toNative.Invoke(null, new object[]
    {
        new[]
        {
            new Rect(100.2, 50.1, 20.4, 1.0),
            new Rect(100.2, 51.1, 20.4, 1.0)
        },
        scale,
        new Size(NotchLayout.DesignWidth, NotchLayout.DesignHeight)
    })!;
    Check(nativeArray.Length == 2, $"DPI {scale:0.##} scanlines survive native quantization");
    var nativeType = nativeArray.GetType().GetElementType()!;
    var first = nativeArray.GetValue(0)!;
    var second = nativeArray.GetValue(1)!;
    var bottom = (int)nativeType.GetField("Bottom", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(first)!;
    var top = (int)nativeType.GetField("Top", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(second)!;
    Check(bottom == top, $"DPI {scale:0.##} scanlines share a native pixel boundary");
}

Console.WriteLine($"{passed} input-region checks passed.");
