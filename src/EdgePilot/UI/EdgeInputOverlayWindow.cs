using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using EdgePilot.Platform;

namespace EdgePilot.UI;

/// <summary>
/// Invisible Windows-only input surface. The visible EdgeWindow is fully click-through; this
/// overlay owns pointer input only inside the native region supplied by EdgeWindow.
/// </summary>
internal sealed class EdgeInputOverlayWindow : Window, IDisposable
{
    private readonly PlatformInputRegion _inputRegion;
    private readonly Action<Point, bool, bool> _pointerPressed;
    private bool _disposed;

    public EdgeInputOverlayWindow(Action<Point, bool, bool> pointerPressed)
    {
        _pointerPressed = pointerPressed;

        Width = 1;
        Height = 1;
        CanResize = false;
        WindowDecorations = WindowDecorations.None;
        ShowInTaskbar = false;
        Topmost = true;
        ShowActivated = false;
        Background = Brushes.Transparent;
        TransparencyBackgroundFallback = Brushes.Transparent;
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
        Opacity = 0.001;
        Position = new PixelPoint(-32000, -32000);
        Content = new Canvas { Background = Brushes.Transparent };

        _inputRegion = new PlatformInputRegion(this);

        PointerPressed += (_, e) =>
        {
            var current = e.GetCurrentPoint(this);
            var left = current.Properties.IsLeftButtonPressed;
            var right = current.Properties.IsRightButtonPressed;
            if (!left && !right)
                return;

            _pointerPressed(e.GetPosition(this), left, right);
            e.Handled = true;
        };
    }

    public bool IsReady => _inputRegion.IsReady;

    public bool TryApply(IReadOnlyList<Rect> logicalRects, double scaling,
        Size logicalWindowSize, PixelPoint targetPosition)
    {
        if (_disposed)
            return false;

        Width = logicalWindowSize.Width;
        Height = logicalWindowSize.Height;

        // Create the HWND off-screen so there is never a frame where the full transparent
        // rectangle can intercept the user's desktop before the native input region is applied.
        if (!IsVisible)
        {
            Position = new PixelPoint(-32000, -32000);
            Show();
        }

        if (!_inputRegion.TryApply(logicalRects, scaling, logicalWindowSize))
            return false;

        Position = targetPosition;
        return true;
    }

    public void Sync(Size logicalWindowSize, PixelPoint position)
    {
        if (_disposed || !IsVisible)
            return;

        Width = logicalWindowSize.Width;
        Height = logicalWindowSize.Height;
        Position = position;
    }

    public void Suspend()
    {
        if (!_disposed && IsVisible)
            Hide();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _inputRegion.Dispose();
        try { Close(); }
        catch (InvalidOperationException) { }
        GC.SuppressFinalize(this);
    }
}
