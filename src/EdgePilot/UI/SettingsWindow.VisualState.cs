using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using FluentIcons.Avalonia;
using FluentIcons.Common;

namespace EdgePilot.UI;

public sealed partial class SettingsWindow
{
    private enum ActionButtonRole
    {
        Secondary,
        Primary
    }

    // Settings owns an explicit interaction palette instead of relying on Fluent theme defaults.
    // That matters on Acrylic/Mica because the material behind a control can vary continuously.
    private static readonly SolidColorBrush PrimaryBrush = new(Color.Parse("#F5F7FA"));
    private static readonly SolidColorBrush ControlBorderBrush = new(Color.Parse("#474A50"));
    private static readonly SolidColorBrush ControlHoverBorderBrush = new(Color.Parse("#686D75"));
    private static readonly SolidColorBrush ControlHoverBrush = new(Color.Parse("#25272C"));
    private static readonly SolidColorBrush ControlPressedBrush = new(Color.Parse("#30333A"));
    private static readonly SolidColorBrush DisabledForegroundBrush = new(Color.Parse("#737983"));
    private static readonly SolidColorBrush DisabledBorderBrush = new(Color.Parse("#2A2D31"));
    private static readonly SolidColorBrush AccentHoverBrush = new(Color.Parse("#F57F36"));
    private static readonly SolidColorBrush AccentPressedBrush = new(Color.Parse("#DD6D2D"));
    private static readonly SolidColorBrush AccentSoftHoverBrush = new(Color.FromArgb(58, 255, 138, 61));

    private readonly Dictionary<Button, bool> _segmentSelection = new();
    private readonly Dictionary<Button, ActionButtonRole> _actionButtonRoles = new();
    private readonly HashSet<Button> _pressedButtons = new();
    private GlassPalette? _currentPalette;

    private void RegisterSegmentButton(Button button)
    {
        _segmentSelection[button] = false;
        button.PointerEntered += (_, _) => PaintSegmentButton(button);
        button.PointerExited += (_, _) =>
        {
            _pressedButtons.Remove(button);
            PaintSegmentButton(button);
        };
        button.PointerPressed += (_, _) =>
        {
            _pressedButtons.Add(button);
            PaintSegmentButton(button);
        };
        button.PointerReleased += (_, _) =>
        {
            _pressedButtons.Remove(button);
            PaintSegmentButton(button);
        };
    }

    private void RegisterActionButton(Button button, ActionButtonRole role)
    {
        _actionButtonRoles[button] = role;
        button.PointerEntered += (_, _) => PaintActionButton(button);
        button.PointerExited += (_, _) =>
        {
            _pressedButtons.Remove(button);
            PaintActionButton(button);
        };
        button.PointerPressed += (_, _) =>
        {
            _pressedButtons.Add(button);
            PaintActionButton(button);
        };
        button.PointerReleased += (_, _) =>
        {
            _pressedButtons.Remove(button);
            PaintActionButton(button);
        };
    }

    private void RegisterNavigationButton(Button button, SettingsPage page)
    {
        button.PointerEntered += (_, _) => PaintNavigationButton(page);
        button.PointerExited += (_, _) => PaintNavigationButton(page);
        button.PointerPressed += (_, _) =>
        {
            _pressedButtons.Add(button);
            PaintNavigationButton(page);
        };
        button.PointerReleased += (_, _) =>
        {
            _pressedButtons.Remove(button);
            PaintNavigationButton(page);
        };
    }

    private void RegisterMetricTile(Border tile)
    {
        tile.PointerEntered += (_, _) => UpdateMetricTileStates();
        tile.PointerExited += (_, _) => UpdateMetricTileStates();
    }

    private void SetSegmentSelection(IReadOnlyList<Button> buttons, int selectedIndex)
    {
        for (var i = 0; i < buttons.Count; i++)
        {
            _segmentSelection[buttons[i]] = i == selectedIndex;
            PaintSegmentButton(buttons[i]);
        }
    }

    private void PaintSegmentButton(Button button)
    {
        var selected = _segmentSelection.TryGetValue(button, out var value) && value;
        var pressed = _pressedButtons.Contains(button);
        var hovered = button.IsPointerOver;

        IBrush foreground;
        IBrush iconForeground;
        IBrush background;
        IBrush border;

        if (!button.IsEnabled)
        {
            foreground = DisabledForegroundBrush;
            iconForeground = DisabledForegroundBrush;
            background = Brushes.Transparent;
            border = DisabledBorderBrush;
        }
        else if (selected)
        {
            foreground = Brushes.White;
            iconForeground = Brushes.White;
            background = pressed ? AccentPressedBrush : hovered ? AccentHoverBrush : AccentBrush;
            border = background;
        }
        else
        {
            foreground = PrimaryBrush;
            iconForeground = AccentBrush;
            background = pressed ? ControlPressedBrush : hovered ? ControlHoverBrush : Brushes.Transparent;
            border = hovered || pressed ? ControlHoverBorderBrush : ControlBorderBrush;
        }

        button.Background = background;
        button.BorderBrush = border;
        button.BorderThickness = new Thickness(1);
        button.Foreground = foreground;
        PaintButtonContent(button, foreground, iconForeground, selected);
    }

    private void PaintActionButton(Button button)
    {
        if (!_actionButtonRoles.TryGetValue(button, out var role)) return;

        var pressed = _pressedButtons.Contains(button);
        var hovered = button.IsPointerOver;
        IBrush foreground;
        IBrush iconForeground;
        IBrush background;
        IBrush border;

        if (!button.IsEnabled)
        {
            foreground = DisabledForegroundBrush;
            iconForeground = DisabledForegroundBrush;
            background = Brushes.Transparent;
            border = DisabledBorderBrush;
        }
        else if (role == ActionButtonRole.Primary)
        {
            foreground = Brushes.White;
            iconForeground = Brushes.White;
            background = pressed ? AccentPressedBrush : hovered ? AccentHoverBrush : AccentBrush;
            border = background;
        }
        else
        {
            foreground = PrimaryBrush;
            iconForeground = AccentBrush;
            background = pressed ? ControlPressedBrush : hovered ? ControlHoverBrush : Brushes.Transparent;
            border = hovered || pressed ? ControlHoverBorderBrush : ControlBorderBrush;
        }

        button.Background = background;
        button.BorderBrush = border;
        button.BorderThickness = new Thickness(1);
        button.Foreground = foreground;
        PaintButtonContent(button, foreground, iconForeground, false);
    }

    private void PaintNavigationButton(SettingsPage page)
    {
        if (!_navButtons.TryGetValue(page, out var button)) return;
        var selected = page == _selectedPage;
        var hovered = button.IsPointerOver;
        var pressed = _pressedButtons.Contains(button);

        button.Background = selected
            ? (hovered ? AccentSoftHoverBrush : AccentSoftBrush)
            : pressed ? ControlPressedBrush : hovered ? ControlHoverBrush : Brushes.Transparent;
        button.BorderBrush = selected ? AccentBrush : Brushes.Transparent;
        button.BorderThickness = selected ? new Thickness(3, 0, 0, 0) : new Thickness(0);
        button.Foreground = PrimaryBrush;

        if (button.Content is StackPanel row)
        {
            foreach (var child in row.Children)
            {
                switch (child)
                {
                    case FluentIcon icon:
                        icon.Foreground = selected || hovered ? AccentBrush : MutedBrush;
                        icon.IconVariant = selected ? IconVariant.Filled : IconVariant.Regular;
                        break;
                    case TextBlock text:
                        text.Foreground = PrimaryBrush;
                        break;
                }
            }
        }
    }

    private static void PaintButtonContent(Button button, IBrush foreground, IBrush iconForeground, bool selected)
    {
        if (button.Content is not StackPanel content) return;

        foreach (var child in content.Children)
        {
            switch (child)
            {
                case FluentIcon icon:
                    icon.Foreground = iconForeground;
                    icon.IconVariant = selected ? IconVariant.Filled : IconVariant.Regular;
                    break;
                case TextBlock text:
                    text.Foreground = foreground;
                    break;
                case PathIcon path:
                    path.Foreground = iconForeground;
                    break;
            }
        }
    }

    private void RefreshActionButtonVisuals()
    {
        foreach (var button in _actionButtonRoles.Keys)
            PaintActionButton(button);
    }

    private void ApplyInteractivePalette(GlassPalette palette)
    {
        _currentPalette = palette;

        PrimaryBrush.Color = Color.Parse(palette.Dark ? "#F5F7FA" : "#191B20");
        ControlBorderBrush.Color = palette.Dark
            ? (palette.IsGlass ? Color.FromArgb(0x50, 255, 255, 255) : Color.Parse("#484C53"))
            : (palette.IsGlass ? Color.FromArgb(0x38, 0, 0, 0) : Color.Parse("#C7CAD0"));
        ControlHoverBorderBrush.Color = palette.Dark
            ? (palette.IsGlass ? Color.FromArgb(0x72, 255, 255, 255) : Color.Parse("#656B74"))
            : (palette.IsGlass ? Color.FromArgb(0x58, 0, 0, 0) : Color.Parse("#AEB2BA"));
        ControlHoverBrush.Color = palette.Dark
            ? (palette.IsGlass ? Color.FromArgb(0x22, 255, 255, 255) : Color.Parse("#25282E"))
            : (palette.IsGlass ? Color.FromArgb(0x10, 0, 0, 0) : Color.Parse("#EEF0F3"));
        ControlPressedBrush.Color = palette.Dark
            ? (palette.IsGlass ? Color.FromArgb(0x32, 255, 255, 255) : Color.Parse("#30343B"))
            : (palette.IsGlass ? Color.FromArgb(0x1D, 0, 0, 0) : Color.Parse("#E1E4E8"));
        DisabledForegroundBrush.Color = Color.Parse(palette.Dark ? "#747A84" : "#979CA5");
        DisabledBorderBrush.Color = palette.Dark
            ? (palette.IsGlass ? Color.FromArgb(0x25, 255, 255, 255) : Color.Parse("#2C3035"))
            : (palette.IsGlass ? Color.FromArgb(0x18, 0, 0, 0) : Color.Parse("#E2E4E8"));

        foreach (var combo in new[] { _drive, _refresh, _language })
        {
            combo.Foreground = PrimaryBrush;
            combo.Background = palette.Card;
            combo.BorderBrush = ControlBorderBrush;
            combo.BorderThickness = new Thickness(1);
        }

        _autostart.Foreground = PrimaryBrush;
        foreach (var metric in _metrics) metric.Foreground = PrimaryBrush;
        _status.Foreground = PrimaryBrush;

        RefreshSegmentVisuals();
        UpdateNavigationIconStates(_selectedPage);
        RefreshActionButtonVisuals();
        UpdateMetricTileStates();
    }

    private void PaintMetricTiles()
    {
        if (_metricTiles.Length == 0) return;
        var palette = _currentPalette ?? Glass.Palette(_selectedBackdrop, Glass.ResolveDark(_selectedTheme));

        for (var i = 0; i < _metricTiles.Length; i++)
        {
            var tile = _metricTiles[i];
            var selected = _metrics[i].IsChecked == true;
            var hovered = tile.IsPointerOver;

            tile.Background = selected
                ? AccentSoftBrush
                : hovered ? ControlHoverBrush : palette.Card;
            tile.BorderBrush = selected
                ? AccentBrush
                : hovered ? ControlHoverBorderBrush : palette.CardBorder;
            tile.BorderThickness = selected || hovered ? new Thickness(1) : palette.CardBorderThickness;
        }
    }
}
