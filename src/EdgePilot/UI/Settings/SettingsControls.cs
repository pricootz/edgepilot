using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using EdgePilot.Core;
using FluentIcons.Avalonia;
using FluentIcons.Common;
using FluentIconName = FluentIcons.Common.Icon;

namespace EdgePilot.UI;

public sealed partial class SettingsWindow
{
    private const string GitHubMarkPath =
        "M6.766 11.328c-2.063-.25-3.516-1.734-3.516-3.656 0-.781.281-1.625.75-2.188-.203-.515-.172-1.609.063-2.062.625-.078 1.468.25 1.968.703.594-.187 1.219-.281 1.985-.281.765 0 1.39.094 1.953.265.484-.437 1.344-.765 1.969-.687.218.422.25 1.515.046 2.047.5.593.766 1.39.766 2.203 0 1.922-1.453 3.375-3.547 3.64.531.344.89 1.094.89 1.954v1.625c0 .468.391.734.86.547C13.781 14.359 16 11.53 16 8.03 16 3.61 12.406 0 7.984 0 3.563 0 0 3.61 0 8.031a7.88 7.88 0 0 0 5.172 7.422c.422.156.828-.125.828-.547v-1.25c-.219.094-.5.156-.75.156-1.031 0-1.64-.562-2.078-1.609-.172-.422-.36-.672-.719-.719-.187-.015-.25-.093-.25-.187 0-.188.313-.328.625-.328.453 0 .844.281 1.25.86.313.452.64.655 1.031.655s.641-.14 1-.5c.266-.265.47-.5.657-.656";

    private Border Card(params Control[] children)
    {
        var stack = new StackPanel { Spacing = 12 };
        foreach (var child in children) stack.Children.Add(child);
        var card = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(18),
            Child = stack
        };
        _cards.Add(card);
        return card;
    }

    private Border MetricTile(CheckBox checkbox, FluentIconName icon, string description)
    {
        var heading = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            ColumnSpacing = 9
        };
        heading.Children.Add(UiIcon(icon, 18));
        var title = new TextBlock
        {
            Text = checkbox.Content?.ToString(),
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        heading.Children.Add(title);
        Grid.SetColumn(title, 1);
        checkbox.Content = null;
        heading.Children.Add(checkbox);
        Grid.SetColumn(checkbox, 2);

        var tile = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(11),
            Padding = new Thickness(14),
            Cursor = new Cursor(StandardCursorType.Hand),
            Child = new StackPanel
            {
                Spacing = 7,
                Children = { heading, Description(description) }
            }
        };
        tile.PointerPressed += (_, e) =>
        {
            if (e.Source is CheckBox) return;
            checkbox.IsChecked = checkbox.IsChecked != true;
            OnMetricChanged();
            e.Handled = true;
        };
        return tile;
    }

    private static Control SettingField(string label, Control control)
    {
        var stack = new StackPanel { Spacing = 6 };
        stack.Children.Add(Label(label));
        stack.Children.Add(control);
        return stack;
    }

    private static StackPanel SectionTitle(FluentIconName icon, string text)
    {
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 9,
            Children =
            {
                UiIcon(icon, 18),
                new TextBlock
                {
                    Text = text,
                    FontSize = 16,
                    FontWeight = FontWeight.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center
                }
            }
        };
    }

    private static TextBlock Label(string text) => new()
    {
        Text = text,
        FontWeight = FontWeight.SemiBold
    };

    private static TextBlock Description(string text) => new()
    {
        Text = text,
        Foreground = MutedBrush,
        FontSize = 12,
        TextWrapping = TextWrapping.Wrap
    };

    private static Border Divider() => new()
    {
        Height = 1,
        Background = new SolidColorBrush(Color.FromArgb(60, 128, 128, 128)),
        Margin = new Thickness(0, 4)
    };

    private static WrapPanel SegmentRow(IEnumerable<Button> buttons)
    {
        var row = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var button in buttons)
        {
            button.Margin = new Thickness(0, 0, 8, 8);
            row.Children.Add(button);
        }
        return row;
    }

    private static WrapPanel ChipRow(params string[] labels)
    {
        var row = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var label in labels)
        {
            row.Children.Add(new Border
            {
                Background = AccentSoftBrush,
                CornerRadius = new CornerRadius(999),
                Padding = new Thickness(9, 4),
                Margin = new Thickness(0, 4, 7, 0),
                Child = new TextBlock { Text = label, FontSize = 11, FontWeight = FontWeight.SemiBold }
            });
        }
        return row;
    }

    private static StackPanel FeatureRow(FluentIconName icon, string title, string description)
    {
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Children =
            {
                UiIcon(icon, 18),
                new StackPanel
                {
                    Spacing = 3,
                    Children =
                    {
                        new TextBlock { Text = title, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap },
                        Description(description)
                    }
                }
            }
        };
    }

    private static StackPanel ButtonContent(FluentIconName icon, string text, IBrush? foreground = null)
    {
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 7,
            Children =
            {
                UiIcon(icon, 16, foreground),
                new TextBlock { Text = text, Foreground = foreground, VerticalAlignment = VerticalAlignment.Center }
            }
        };
    }

    private static Button SegmentButton(FluentIconName icon, string text, Action select)
    {
        var button = new Button
        {
            Content = ButtonContent(icon, text),
            Padding = new Thickness(13, 8),
            MinWidth = 72
        };
        button.Click += (_, _) => select();
        return button;
    }

    private Button NavButton(FluentIconName icon, string text, SettingsPage page)
    {
        var fluent = UiIcon(icon, 18, MutedBrush);
        _navIcons[page] = fluent;

        var content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Children =
            {
                fluent,
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
        button.Click += (_, _) =>
        {
            ShowPage(page);
            UpdateNavigationIconStates(page);
        };
        _navButtons[page] = button;
        return button;
    }

    private Button LinkButton(string text, string url)
    {
        var button = new Button
        {
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children =
                {
                    new PathIcon { Data = Geometry.Parse(GitHubMarkPath), Width = 16, Height = 16 },
                    new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center }
                }
            },
            Padding = new Thickness(13, 8)
        };
        button.Click += (_, _) => OpenUrl(url);
        return button;
    }

    private void OpenUrl(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            System.Diagnostics.Trace.WriteLine(ex);
            _status.Text = Localization.T("settings.browserFailed");
        }
    }

    private static FluentIcon UiIcon(FluentIconName icon, double size = 18, IBrush? foreground = null,
        IconVariant variant = IconVariant.Regular)
    {
        return new FluentIcon
        {
            Icon = icon,
            IconVariant = variant,
            IconSize = size <= 16 ? IconSize.Size16 : size <= 20 ? IconSize.Size20 : IconSize.Size24,
            FontSize = size,
            Width = Math.Max(18, size),
            Height = Math.Max(18, size),
            Foreground = foreground ?? AccentBrush,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private static void UpdateSegmentContentVisual(Button button, bool selected)
    {
        if (button.Content is not StackPanel content) return;

        foreach (var child in content.Children)
        {
            switch (child)
            {
                case FluentIcon icon:
                    icon.Foreground = selected ? Brushes.White : AccentBrush;
                    icon.IconVariant = selected ? IconVariant.Filled : IconVariant.Regular;
                    break;
                case TextBlock text:
                    text.Foreground = selected ? Brushes.White : null;
                    break;
            }
        }
    }
}
