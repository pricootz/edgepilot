using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using EdgePilot.Core;
using EdgePilot.Core.Signals;

namespace EdgePilot.UI.Signals;

internal sealed class SignalPresenter : Border
{
    private readonly TextBlock _icon;
    private readonly TextBlock _title;
    private readonly TextBlock _message;
    private readonly StackPanel _text;
    private readonly StackPanel _layout;

    public SignalPresenter()
    {
        IsVisible = false;
        Opacity = 0;
        IsHitTestVisible = false;
        Background = Brushes.Transparent;
        Padding = new Thickness(12);

        _icon = new TextBlock
        {
            FontSize = 28,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brush("#FF8A3D"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        _title = new TextBlock
        {
            FontSize = 10,
            FontWeight = FontWeight.Bold,
            Foreground = Brush("#8993A2"),
            TextWrapping = TextWrapping.Wrap
        };

        _message = new TextBlock
        {
            FontSize = 15,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brush("#F5F7FA"),
            TextWrapping = TextWrapping.Wrap,
            MaxLines = 3
        };

        _text = new StackPanel
        {
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { _title, _message }
        };

        _layout = new StackPanel
        {
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { _icon, _text }
        };

        Child = _layout;
    }

    public void Render(Signal signal)
    {
        _icon.Text = signal.Severity switch
        {
            SignalSeverity.Success => "✓",
            SignalSeverity.Warning => "!",
            SignalSeverity.Critical => "×",
            _ => "i"
        };

        _icon.Foreground = signal.Severity switch
        {
            SignalSeverity.Success => Brush("#79D69C"),
            SignalSeverity.Critical => Brush("#FF6B72"),
            _ => Brush("#FF8A3D")
        };

        _title.Text = Localization.T(signal.TitleKey).ToUpperInvariant();
        _message.Text = Localization.T(signal.MessageKey);
        IsVisible = true;
    }

    public void Configure(EdgeSide edge, Rect bounds)
    {
        Width = bounds.Width;
        Height = bounds.Height;

        var horizontalEdge = NotchLayout.Horizontal(edge);
        _layout.Orientation = horizontalEdge ? Orientation.Horizontal : Orientation.Vertical;
        _layout.Spacing = horizontalEdge ? 14 : 9;
        _text.HorizontalAlignment = horizontalEdge ? HorizontalAlignment.Left : HorizontalAlignment.Center;
        _title.TextAlignment = horizontalEdge ? TextAlignment.Left : TextAlignment.Center;
        _message.TextAlignment = horizontalEdge ? TextAlignment.Left : TextAlignment.Center;
        _message.MaxWidth = horizontalEdge ? Math.Max(120, bounds.Width - 72) : Math.Max(74, bounds.Width - 20);
    }

    public void Clear()
    {
        Opacity = 0;
        IsVisible = false;
    }

    private static IBrush Brush(string color) => new SolidColorBrush(Color.Parse(color));
}
