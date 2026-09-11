using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using EdgePilot.UI;
using EdgePilot.Core;
using EdgePilot.Platform;

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
object? SettingsField(SettingsWindow w, string name) => typeof(SettingsWindow).GetField(name, flags)!.GetValue(w);
object? SettingsCall(SettingsWindow w, string name, params object[] args) =>
    typeof(SettingsWindow).GetMethod(name, flags)!.Invoke(w, args);

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

var drives = new[]
{
    new DriveSnapshot("/mnt/parity", "Parity", 20_000_000_000_000, 1_000_000_000_000),
    new DriveSnapshot("/mnt/dati16", "Dati 16 TB", 16_000_000_000_000, 12_000_000_000_000)
};
Check(DriveSelection.Resolve(drives, "/mnt/dati16") == drives[1], "explicit disk overrides parity");
Check(DriveSelection.Resolve(drives, "/missing") is null, "missing disk never falls back");
Check(DriveSelection.Choices(drives, "/missing").Any(x => x.Name == "/missing"), "missing choice is retained");
var manyDrives = Enumerable.Range(0, 12).Select(i => new DriveSnapshot("/disk" + i, "Disco", 1000, 500)).ToArray();
Check(DriveSelection.Choices(manyDrives, null).Count == 13, "selector does not truncate disk list");
Check(DriveSelection.Choices(drives, null)[2].Caption.Contains("16 TB"), "decimal disk capacity is recognizable");
_ = AppIcon.Load();
Check(true, "embedded tray icon decodes");
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
Check(Canvas.GetTop(stack) == (620 - stack.Height) / 2, "metric stack centered in approved safe area");
for (var i = 0; i < 4; i++)
{
    var point = new Point(364, Canvas.GetTop(stack) + i * 86 + 38);
    Check((int)Call(window, "MetricIndexAt", point)! == i, "metric hit target " + i);
}
Call(window, "UpdatePointer", new Point(364, 181));
var bridge = (Rect)Call(window, "BridgeRect")!;
Check(bridge.Width > 0, "tooltip bridge exists");
Call(window, "UpdatePointer", bridge.Center);
Check((int?)Field(window, "_hoveredMetric") == 0, "tooltip survives crossing bridge");
Check(!((DispatcherTimer)Field(window, "_foldTimer")!).IsEnabled, "bridge keeps notch open");
Set(window, "_expansion", 0.4d);
Check(Call(window, "MetricIndexAt", new Point(364, 181)) is null, "clipped metrics are inactive");
var shape = (Avalonia.Controls.Shapes.Path)Field(window, "_notchShape")!;
var content = (Canvas)Field(window, "_notchContent")!;
var acrylicSurface = (ExperimentalAcrylicBorder)Field(window, "_notchAcrylicSurface")!;
foreach (var p in new[] { 0d, 0.25, 0.5, 0.75, 1, 1.025 })
{
    Set(window, "_expansion", p);
    Call(window, "UpdateNotchVisual");
    Check(ReferenceEquals(shape.Data, content.Clip), "shape and content clip share geometry " + p);
    Check(ReferenceEquals(shape.Data, acrylicSurface.Clip), "shape and acrylic clip share geometry " + p);
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
    foreach (var point in new[] { new Point(410, 310), new Point(364, 181), new Point(0, 0) })
        Check(NotchLayout.ToDesign(NotchLayout.ToScreen(point, edge), edge) == point, "coordinate round trip " + edge);
    for (var i = 0; i < 4; i++)
    {
        var point = NotchLayout.ToScreen(new Point(364, (620 - 334) / 2d + i * 86 + 38), edge);
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

foreach (var edge in Enum.GetValues<EdgeSide>())
foreach (var mask in Enumerable.Range(1, 15))
{
    var preferences = new NotchPreferences(edge) { Metrics = (VisibleMetrics)mask, RefreshIntervalMs = 2000,
        Sensitivity = HoverSensitivity.Precise };
    window.ApplyPreferences(preferences);
    Set(window, "_expansion", 1d);
    Call(window, "UpdateNotchVisual");
    Check(window.Preferences == preferences, "live preferences round trip");
    var indices = Enumerable.Range(0, 4).Where(i => (mask & (1 << i)) != 0).ToArray();
    var span = indices.Length * 76 + (indices.Length - 1) * 10;
    Check(stack.Children.Count == indices.Length, "selected rings only");
    for (var slot = 0; slot < indices.Length; slot++)
    {
        var point = NotchLayout.ToScreen(new Point(364, (620 - span) / 2d + slot * 86 + 38), edge);
        Check((int)Call(window, "MetricIndexAt", point)! == indices[slot], "filtered metric identity");
    }
    var bounds = shape.Data!.Bounds;
    Check(Math.Abs((NotchLayout.Horizontal(edge) ? bounds.Width : bounds.Height) - (span + 96)) < 0.001,
        "notch preserves 24-DIP safe margins around selected metrics");
}
foreach (var edge in Enum.GetValues<EdgeSide>())
{
    window.ApplyPreferences(new NotchPreferences(edge) { Sensitivity = HoverSensitivity.Precise });
    var small = (Rect)Call(window, "HotZoneRect")!;
    window.ApplyPreferences(new NotchPreferences(edge) { Sensitivity = HoverSensitivity.Wide });
    var large = (Rect)Call(window, "HotZoneRect")!;
    Check(large.Contains(small) && large.Width * large.Height > small.Width * small.Height,
        "sensitivity grows hit area on " + edge);
}
window.ApplyPreferences(new NotchPreferences());

window.ApplyPreferences(new NotchPreferences { SelectedDrive = "/mnt/dati16" });
var snapshot = new SystemSnapshot("host", "Ubuntu", 10, 1000, 500, TimeSpan.FromMinutes(10),
    new NetworkSnapshot("eth0", true, 0, 0, 1000), drives, DateTimeOffset.Now);
Call(window, "RenderSnapshot", snapshot);
Call(window, "RenderTooltip", 2);
Check(((TextBlock)Field(window, "_tooltipValue")!).Text == "25%", "disk tooltip uses selected disk");
Call(window, "RenderSnapshot", snapshot with { Drives = new[] { drives[0] } });
Call(window, "RenderTooltip", 2);
Check(((TextBlock)Field(window, "_tooltipValue")!).Text == "—", "unmounted disk shows unavailable");
window.ApplyPreferences(new NotchPreferences());

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
    File.WriteAllText(settingsPath, "{\"Edge\":\"Left\",\"Mode\":\"Hover\"}");
    Check(PreferenceStore.Load(settingsPath) == new NotchPreferences(EdgeSide.Left), "old settings retain defaults");
    foreach (var interval in new[] { 500, 1000, 2000, 5000 })
    {
        var extended = new NotchPreferences(EdgeSide.Top) { Metrics = VisibleMetrics.Network,
            Sensitivity = HoverSensitivity.Wide, RefreshIntervalMs = interval };
        PreferenceStore.Save(settingsPath, extended);
        Check(PreferenceStore.Load(settingsPath) == extended, "extended settings persist");
    }
    var lastGood = File.ReadAllText(settingsPath);
    foreach (var invalid in new[]
    {
        new NotchPreferences { Metrics = 0 }, new NotchPreferences { Metrics = (VisibleMetrics)16 },
        new NotchPreferences { RefreshIntervalMs = 0 }, new NotchPreferences { Sensitivity = (HoverSensitivity)999 }
    })
    {
        try { PreferenceStore.Save(settingsPath, invalid); Check(false, "invalid preference rejected"); }
        catch (InvalidDataException) { Check(File.ReadAllText(settingsPath) == lastGood, "invalid preferences preserve file"); }
    }
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
    Check(settings.Content is Grid, "settings uses structured shell");
    var applyButton = (Button)SettingsField(settings, "_applyButton")!;
    var resetButton = (Button)SettingsField(settings, "_resetButton")!;
    var refreshSelector = (ComboBox)SettingsField(settings, "_refresh")!;
    var metricChecks = (CheckBox[])SettingsField(settings, "_metrics")!;
    var previewNotch = (Border)SettingsField(settings, "_previewNotch")!;
    var exitButton = (Button)SettingsField(settings, "_exitButton")!;
    var exitContent = (StackPanel)exitButton.Content!;
    var exitIcon = exitContent.Children.OfType<FluentIcons.Avalonia.FluentIcon>().Single();
    Check(exitIcon.Icon == FluentIcons.Common.Icon.SignOut, "exit action uses SignOut icon");
    Check(exitIcon.IconSize == FluentIcons.Common.IconSize.Size20 && Math.Abs(exitIcon.FontSize - 16) < 0.001,
        "compact exit icon uses available glyph set at 16px");

    SettingsCall(settings, "SelectEdge", EdgeSide.Bottom, true);
    SettingsCall(settings, "SelectMode", NotchDisplayMode.Always, true);
    Check(previewNotch.VerticalAlignment == Avalonia.Layout.VerticalAlignment.Bottom,
        "settings preview follows selected edge");
    Check(applyButton.IsEnabled && resetButton.IsEnabled, "settings dirty state enables actions");
    applyButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
    Check(applied == new NotchPreferences(EdgeSide.Bottom, NotchDisplayMode.Always),
        "settings Apply invokes live update");
    Check(PreferenceStore.Load(settingsPath) == applied, "settings Apply persists chosen values");
    Check(!applyButton.IsEnabled && !resetButton.IsEnabled, "successful save clears dirty state");

    refreshSelector.SelectedIndex = 3;
    SettingsCall(settings, "SelectSensitivity", HoverSensitivity.Wide, true);
    foreach (var checkbox in metricChecks) checkbox.IsChecked = false;
    applied = null;
    applyButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
    Check(applied is null, "empty metric selection is not applied");
    Check(((TextBlock)SettingsField(settings, "_status")!).Text?.StartsWith("Seleziona almeno una metrica") == true,
        "empty metric selection is visible");
    metricChecks[3].IsChecked = true;
    applyButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
    Check(applied?.Metrics == VisibleMetrics.Network && applied.RefreshIntervalMs == 5000 &&
        applied.Sensitivity == HoverSensitivity.Wide, "new UI selections apply");
    Check(PreferenceStore.Load(settingsPath) == applied, "new UI selections persist");
    settings.Close();

    applied = null;
    var failingSettings = new SettingsWindow(new NotchPreferences(), value => applied = value,
        storagePath: temporaryDirectory);
    SettingsCall(failingSettings, "SelectEdge", EdgeSide.Left, true);
    ((Button)SettingsField(failingSettings, "_applyButton")!).RaiseEvent(
        new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
    Check(applied is null, "save failure does not apply changes");
    Check(((TextBlock)SettingsField(failingSettings, "_status")!).Text?.StartsWith("Impossibile salvare") == true,
        "save failure is visible");
    failingSettings.Close();

    var diskSettings = new SettingsWindow(new NotchPreferences { SelectedDrive = "/mnt/dati16" },
        value => applied = value, storagePath: settingsPath, drives: drives);
    var diskSelector = (ComboBox)SettingsField(diskSettings, "_drive")!;
    Check(((DriveChoice)diskSelector.SelectedItem!).Name == "/mnt/dati16", "saved disk selected in settings");
    SettingsCall(diskSettings, "SelectMode", NotchDisplayMode.Always, true);
    ((Button)SettingsField(diskSettings, "_applyButton")!).RaiseEvent(
        new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
    Check(applied?.SelectedDrive == "/mnt/dati16" && PreferenceStore.Load(settingsPath).SelectedDrive == "/mnt/dati16",
        "disk picker selection persists");
    diskSelector.SelectedItem = ((IReadOnlyList<DriveChoice>)diskSelector.ItemsSource!)[0];
    diskSettings.UpdateDrives(drives);
    Check(((DriveChoice)diskSelector.SelectedItem!).Name is null, "refresh preserves unsaved automatic choice");
    diskSettings.Close();
    if (OperatingSystem.IsWindows())
    {
        var caseSettings = new SettingsWindow(new NotchPreferences { SelectedDrive = @"c:\" },
            _ => { }, drives: new[] { new DriveSnapshot(@"C:\", "Sistema", 1000, 500) });
        Check(((DriveChoice)((ComboBox)SettingsField(caseSettings, "_drive")!).SelectedItem!).Name == @"C:\",
            "Windows drive selection ignores casing");
        caseSettings.Close();
    }

    var registration = new MemoryRegistration();
    var command = new LaunchCommand("/opt/Edge Pilot/EdgePilot", new[] { "--autostart" });
    var launchPreferences = new NotchPreferences { StartAtLogin = true, SelectedDrive = "/mnt/dati16" };
    DesktopPreferences.Save(settingsPath, launchPreferences, registration, command, windows: false);
    Check(registration.Content?.Contains("Exec=") == true, "autostart registration created");
    Check(PreferenceStore.Load(settingsPath) == launchPreferences, "disk and startup preferences persist");
    var previousRegistration = registration.Content;
    try
    {
        DesktopPreferences.Save(temporaryDirectory, new NotchPreferences(), registration, command, windows: false);
        Check(false, "failed save rejected");
    }
    catch (IOException) { Check(registration.Content == previousRegistration, "registration rolled back on save failure"); }
    catch (UnauthorizedAccessException) { Check(registration.Content == previousRegistration, "registration rolled back on denied save"); }
    DesktopPreferences.Save(settingsPath, new NotchPreferences(), registration, command, windows: false);
    Check(registration.Content is null, "autostart registration removed");
    Check(command.WindowsCommand().StartsWith("\"/opt/Edge Pilot/EdgePilot\""), "startup command quotes spaces");
    Check(new LaunchCommand("/opt/100%/EdgePilot", Array.Empty<string>()).DesktopEntry().Contains("100%%"),
        "desktop entry escapes percent field codes");
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
var trayRecovery = new EdgeWindow { HasTray = true };
trayRecovery.ApplyPreferences(new NotchPreferences(EdgeSide.Right, NotchDisplayMode.Hidden));
trayRecovery.ShowSettings();
((SettingsWindow)Field(trayRecovery, "_settingsWindow")!).Close();
Check(!((CancellationTokenSource)Field(trayRecovery, "_lifetime")!).IsCancellationRequested,
    "tray keeps hidden app alive after closing settings");
trayRecovery.RequestExit();
Console.WriteLine($"{passed} checks passed.");

public sealed class MemoryRegistration : IAutostartRegistration
{
    public string? Content { get; private set; }
    public string? Read() => Content;
    public void Write(string? content) => Content = content;
}
