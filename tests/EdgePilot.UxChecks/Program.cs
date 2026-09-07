using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using EdgePilot.UI;

AppBuilder.Configure<Application>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).SetupWithoutStarting();
var passed = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    if (!name.StartsWith("spring bounded")) Console.WriteLine("PASS " + name);
    passed++;
}
const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
object? Field(EdgeWindow w, string name) => typeof(EdgeWindow).GetField(name, flags)!.GetValue(w);
object? Call(EdgeWindow w, string name, params object[] args) =>
    typeof(EdgeWindow).GetMethod(name, flags)!.Invoke(w, args);
void Set(EdgeWindow w, string name, object value) => typeof(EdgeWindow).GetField(name, flags)!.SetValue(w, value);

foreach (var frame in new[] { 1d / 30, 1d / 60, 1d / 144, 0.5 })
{
    var spring = new NotchSpring { Target = 1 };
    for (var i = 0; i < 300; i++)
    {
        spring.Advance(frame);
        Check(double.IsFinite(spring.Position) && spring.Position >= -0.025 && spring.Position <= 1.025,
            $"spring bounded ({frame:F3}, {i})");
    }
    Check(spring.Position == 1 && spring.IsSettled, "opening settles");
    spring.Target = 0;
    for (var i = 0; i < 300; i++) spring.Advance(frame);
    Check(spring.Position == 0 && spring.IsSettled, "closing settles");
}
var reversing = new NotchSpring { Target = 1 };
reversing.Advance(0.06);
var position = reversing.Position;
var velocity = reversing.Velocity;
reversing.Target = 0;
Check(reversing.Position == position && reversing.Velocity == velocity, "retarget preserves motion");
for (var i = 0; i < 200; i++) reversing.Advance(0.016);
Check(reversing.IsSettled, "reversal settles");

var window = new EdgeWindow();
Call(window, "UpdatePointer", new Point(405, 310));
Check((bool)Field(window, "_expanded")!, "hot-zone opens");
Call(window, "ScheduleFold");
Check(((DispatcherTimer)Field(window, "_foldTimer")!).IsEnabled, "exit arms fold");
Call(window, "UpdatePointer", new Point(405, 310));
Check(!((DispatcherTimer)Field(window, "_foldTimer")!).IsEnabled, "reentry cancels fold immediately");
Set(window, "_pinned", true);
Call(window, "ScheduleFold");
Check(!((DispatcherTimer)Field(window, "_foldTimer")!).IsEnabled, "pin blocks fold");
Set(window, "_pinned", false);
Set(window, "_expansion", 1d);
Call(window, "UpdateNotchVisual");
var stack = (StackPanel)Field(window, "_metricStack")!;
Check(Canvas.GetTop(stack) == (620 - stack.Height) / 2, "metric stack centered");
for (var i = 0; i < 4; i++)
{
    var point = new Point(366, Canvas.GetTop(stack) + i * 88 + 39);
    Check((int)Call(window, "MetricIndexAt", point)! == i, "metric hit target " + i);
}
Call(window, "UpdatePointer", new Point(366, 181));
var bridge = (Rect)Call(window, "BridgeRect")!;
Check(bridge.Width > 0, "tooltip bridge exists");
Call(window, "UpdatePointer", bridge.Center);
Check((int?)Field(window, "_hoveredMetric") == 0, "tooltip survives crossing bridge");
Check(!((DispatcherTimer)Field(window, "_foldTimer")!).IsEnabled, "bridge keeps notch open");
Set(window, "_expansion", 0.4d);
Check(Call(window, "MetricIndexAt", new Point(366, 181)) is null, "clipped metrics are inactive");
var shape = (Avalonia.Controls.Shapes.Path)Field(window, "_notchShape")!;
var content = (Canvas)Field(window, "_notchContent")!;
foreach (var p in new[] { 0d, 0.25, 0.5, 0.75, 1, 1.025 })
{
    Set(window, "_expansion", p);
    Call(window, "UpdateNotchVisual");
    Check(ReferenceEquals(shape.Data, content.Clip), "shape and clip share geometry " + p);
    Check(Math.Abs(shape.Data!.Bounds.Center.Y - 310) < 0.001, "geometry remains centered " + p);
}
foreach (var edge in Enum.GetValues<EdgeSide>())
{
    Set(window, "_edge", edge);
    Call(window, "ConfigureLayout");
    Set(window, "_expansion", 1d);
    Call(window, "UpdateNotchVisual");
    var size = NotchLayout.WindowSize(edge);
    Check(window.Width == size.Width && window.Height == size.Height, "window orientation " + edge);
    foreach (var point in new[] { new Point(410, 310), new Point(366, 181), new Point(0, 0) })
        Check(NotchLayout.ToDesign(NotchLayout.ToScreen(point, edge), edge) == point, "coordinate round trip " + edge);
    for (var i = 0; i < 4; i++)
    {
        var point = NotchLayout.ToScreen(new Point(366, (620 - 342) / 2d + i * 88 + 39), edge);
        Check((int)Call(window, "MetricIndexAt", point)! == i, "oriented metric " + edge + i);
        Call(window, "UpdatePointer", point);
        var tip = (Rect)Call(window, "TooltipLiveRect")!;
        Check(new Rect(size).Contains(tip), "tooltip within window " + edge + i);
        var corridor = (Rect)Call(window, "BridgeRect")!;
        Check(corridor.Width > 0 && corridor.Height > 0, "tooltip bridge " + edge + i);
        Call(window, "UpdatePointer", corridor.Center);
        Check((int?)Field(window, "_hoveredMetric") == i, "bridge retains tooltip " + edge + i);
    }
    var silhouette = shape.Data!.Bounds;
    Check(edge switch
    {
        EdgeSide.Left => Math.Abs(silhouette.Left) < 0.001,
        EdgeSide.Top => Math.Abs(silhouette.Top) < 0.001,
        EdgeSide.Bottom => Math.Abs(silhouette.Bottom - size.Height) < 0.001,
        _ => Math.Abs(silhouette.Right - size.Width) < 0.001
    }, "silhouette anchored " + edge);
}
Set(window, "_edge", EdgeSide.Right);
Call(window, "ConfigureLayout");
if (args.Length > 0)
{
    Directory.CreateDirectory(args[0]);
    var root = (Canvas)window.Content!;
    window.Content = null;
    var preview = new Window { Width = 410, Height = 620, Content = root };
    preview.Show();
    foreach (var state in new[] { ("collapsed", 0d), ("reveal", 0.5), ("expanded", 1d) })
    {
        Set(window, "_expansion", state.Item2);
        Call(window, "UpdateNotchVisual");
        root.Measure(new Size(410, 620));
        root.Arrange(new Rect(0, 0, 410, 620));
        using var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize(410, 620));
        bitmap.Render(root);
        bitmap.Save(Path.Combine(args[0], state.Item1 + ".png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
    }
    preview.Close();
}
foreach (var edge in Enum.GetValues<EdgeSide>())
{
    window.ApplyPreferences(new NotchPreferences(edge, NotchDisplayMode.Always));
    Check(window.Preferences.Edge == edge && (bool)Field(window, "_expanded")!, "always opens " + edge);
    Call(window, "ScheduleFold");
    Check(!((DispatcherTimer)Field(window, "_foldTimer")!).IsEnabled, "always ignores exit " + edge);
    window.ApplyPreferences(new NotchPreferences(edge, NotchDisplayMode.Hidden));
    Check(!(bool)Call(window, "IsInteractive", NotchLayout.ToScreen(new Point(405, 310), edge))!,
        "hidden has no hit targets " + edge);
    Call(window, "UpdatePointer", NotchLayout.ToScreen(new Point(405, 310), edge));
    Check(!(bool)Field(window, "_expanded")!, "hidden ignores hover " + edge);
    window.ApplyPreferences(new NotchPreferences(edge, NotchDisplayMode.Hover));
    Call(window, "UpdatePointer", NotchLayout.ToScreen(new Point(405, 310), edge));
    Check((bool)Field(window, "_expanded")!, "hover restored " + edge);
}

var temporaryDirectory = Path.Combine(Path.GetTempPath(), "edgepilot-checks-" + Guid.NewGuid().ToString("N"));
var settingsPath = Path.Combine(temporaryDirectory, "settings.json");
try
{
    Check(PreferenceStore.Load(settingsPath) == new NotchPreferences(), "missing settings use defaults");
    foreach (var edge in Enum.GetValues<EdgeSide>())
    foreach (var mode in Enum.GetValues<NotchDisplayMode>())
    {
        var saved = new NotchPreferences(edge, mode);
        PreferenceStore.Save(settingsPath, saved);
        Check(PreferenceStore.Load(settingsPath) == saved, "settings round trip " + edge + mode);
    }
    var lastGood = File.ReadAllText(settingsPath);
    try
    {
        PreferenceStore.Save(settingsPath, new NotchPreferences((EdgeSide)999));
        Check(false, "invalid settings rejected");
    }
    catch (InvalidDataException) { Check(File.ReadAllText(settingsPath) == lastGood, "invalid save preserves last good file"); }
    Check(Directory.GetFiles(temporaryDirectory, "*.tmp").Length == 0, "atomic saves leave no temporary files");
    File.WriteAllText(settingsPath, "{bad json");
    try { PreferenceStore.Load(settingsPath); Check(false, "corrupt settings rejected"); }
    catch (System.Text.Json.JsonException) { Check(true, "corrupt settings reported"); }
    File.WriteAllText(settingsPath, "{\"Edge\":999,\"Mode\":0}");
    try { PreferenceStore.Load(settingsPath); Check(false, "unknown enum rejected"); }
    catch (InvalidDataException) { Check(true, "unknown enum reported"); }
    NotchPreferences? applied = null;
    var settings = new SettingsWindow(new NotchPreferences(), value => applied = value,
        storagePath: settingsPath);
    var settingsPanel = (StackPanel)((ScrollViewer)settings.Content!).Content!;
    var selectors = settingsPanel.Children.OfType<ComboBox>().ToArray();
    selectors[0].SelectedIndex = (int)EdgeSide.Bottom;
    selectors[1].SelectedIndex = (int)NotchDisplayMode.Always;
    settingsPanel.Children.OfType<Button>().Single().RaiseEvent(
        new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
    Check(applied == new NotchPreferences(EdgeSide.Bottom, NotchDisplayMode.Always),
        "settings Apply invokes live update");
    Check(PreferenceStore.Load(settingsPath) == applied, "settings Apply persists chosen values");
    settings.Close();

    applied = null;
    var failingSettings = new SettingsWindow(new NotchPreferences(), value => applied = value,
        storagePath: temporaryDirectory);
    var failingPanel = (StackPanel)((ScrollViewer)failingSettings.Content!).Content!;
    failingPanel.Children.OfType<Button>().Single().RaiseEvent(
        new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
    Check(applied is null, "save failure does not apply changes");
    Check(failingPanel.Children.OfType<TextBlock>().Any(x => x.Text?.StartsWith("Could not save") == true),
        "save failure is visible");
    failingSettings.Close();
    // Replacing an invalid file via Apply restores usable settings.
    PreferenceStore.Save(settingsPath, new NotchPreferences());
    Check(PreferenceStore.Load(settingsPath) == new NotchPreferences(), "corrupt file can be recovered");
}
finally
{
    if (Directory.Exists(temporaryDirectory)) Directory.Delete(temporaryDirectory, true);
}
window.Close();
var recovery = new EdgeWindow();
recovery.ApplyPreferences(new NotchPreferences(EdgeSide.Right, NotchDisplayMode.Hidden));
recovery.ShowSettings();
Check(((SettingsWindow)Field(recovery, "_settingsWindow")!).IsVisible, "hidden has settings recovery");
((SettingsWindow)Field(recovery, "_settingsWindow")!).Close();
Check(((CancellationTokenSource)Field(recovery, "_lifetime")!).IsCancellationRequested,
    "closing hidden settings exits the notch");
Console.WriteLine($"{passed} checks passed.");
