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
bool Interactive(EdgeWindow window, Point point) => (bool)Call(window, "IsInteractive", point)!;

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

    var collapsed = Regions(window);
    var hot = (Rect)Call(window, "HotZoneRect")!;
    var shape = (Rect)Call(window, "ShapeRect")!;
    Check(collapsed.Length == 2, $"hover collapsed exposes only hot-zone and shape on {edge}");
    Check(collapsed.All(windowRect.Contains), $"collapsed regions stay inside window on {edge}");
    Check(collapsed.All(r => r.Width * r.Height < size.Width * size.Height),
        $"collapsed regions never become the full transparent window on {edge}");
    Check(Interactive(window, hot.Center), $"hover hot-zone remains interactive on {edge}");
    Check(Interactive(window, shape.Center), $"collapsed notch remains interactive on {edge}");
    Check(!Interactive(window, windowRect.Center), $"transparent window center passes through on {edge}");

    Set(window, "_expanded", true);
    Set(window, "_expansion", 1d);
    Call(window, "UpdateNotchVisual");
    var expanded = Regions(window);
    Check(expanded.Length == 2, $"expanded hover keeps hot-zone and notch only on {edge}");
    Check(expanded.All(windowRect.Contains), $"expanded regions stay inside window on {edge}");
    Check(!Interactive(window, windowRect.Center), $"expanded transparent area passes through on {edge}");

    Call(window, "SetHoveredMetric", 0);
    var withTooltip = Regions(window);
    var tooltip = (Rect)Call(window, "TooltipLiveRect")!;
    var bridge = (Rect)Call(window, "BridgeRect")!;
    Check(withTooltip.Length == 4, $"tooltip adds only card and bridge on {edge}");
    Check(tooltip.Width > 0 && tooltip.Height > 0 && Interactive(window, tooltip.Center),
        $"tooltip stays interactive on {edge}");
    Check(bridge.Width > 0 && bridge.Height > 0 && Interactive(window, bridge.Center),
        $"tooltip bridge stays interactive on {edge}");
    Check(withTooltip.All(windowRect.Contains), $"tooltip regions stay inside window on {edge}");

    window.ApplyPreferences(new NotchPreferences(edge, NotchDisplayMode.Hidden));
    Check(Regions(window).Length == 0, $"hidden mode has an empty native input region on {edge}");
    Check(!Interactive(window, hot.Center), $"hidden mode accepts no pointer input on {edge}");

    window.ApplyPreferences(new NotchPreferences(edge, NotchDisplayMode.Always));
    Set(window, "_expanded", true);
    Set(window, "_expansion", 1d);
    Call(window, "UpdateNotchVisual");
    var always = Regions(window);
    Check(always.Length == 1, $"always mode exposes only the current notch without tooltip on {edge}");
    Check(always.All(windowRect.Contains), $"always region stays inside window on {edge}");
    Check(!Interactive(window, windowRect.Center), $"always mode transparent area passes through on {edge}");
}

Console.WriteLine($"{passed} input-region checks passed.");
