using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using FluentIcons.Avalonia;
using FluentIcons.Common;

namespace EdgePilot.UI;

public sealed partial class SettingsWindow
{
    private Button NavButton(Icon icon, string text, SettingsPage page)
    {
        var content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Children =
            {
                new FluentIcon
                {
                    Icon = icon,
                    IconVariant = IconVariant.Regular,
                    IconSize = IconSize.Size20,
                    FontSize = 18,
                    Width = 20,
                    Height = 20,
                    Foreground = AccentBrush,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                },
                new TextBlock
                {
                    Text = text,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center
                }
            }
        };

        var button = new Button
        {
            Content = content,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(11, 10),
            BorderThickness = new Thickness(0)
        };
        button.Click += (_, _) => ShowPage(page);
        _navButtons[page] = button;
        return button;
    }
}
