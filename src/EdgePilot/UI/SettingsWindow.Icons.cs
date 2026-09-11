using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using FluentIcons.Avalonia;
using FluentIcons.Common;
using FluentIconName = FluentIcons.Common.Icon;

namespace EdgePilot.UI;

public sealed partial class SettingsWindow
{
    private bool _sidebarIconsInstalled;
    private readonly Dictionary<SettingsPage, FluentIcon> _sidebarIcons = new();

    private void InstallSidebarIcons()
    {
        if (_sidebarIconsInstalled) return;

        foreach (var (page, button) in _navButtons)
        {
            if (button.Content is not StackPanel row || row.Children.Count == 0) continue;

            var iconName = page switch
            {
                SettingsPage.General => FluentIconName.Settings,
                SettingsPage.Edge => FluentIconName.Target,
                SettingsPage.Monitor => FluentIconName.DesktopPulse,
                SettingsPage.Behavior => FluentIconName.ArrowSync,
                SettingsPage.Startup => FluentIconName.Power,
                SettingsPage.About => FluentIconName.Info,
                _ => FluentIconName.AppGeneric
            };

            var icon = new FluentIcon
            {
                Icon = iconName,
                IconVariant = IconVariant.Regular,
                IconSize = IconSize.Size20,
                FontSize = 18,
                Width = 20,
                Height = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            row.Children.RemoveAt(0);
            row.Children.Insert(0, icon);
            row.Spacing = 10;
            _sidebarIcons[page] = icon;

            // ShowPage is registered before this handler, so the selected page has already changed
            // when we repaint the navigation icon state.
            button.Click += (_, _) => RefreshSidebarIconStates();
        }

        _sidebarIconsInstalled = true;
        RefreshSidebarIconStates();
    }

    private void RefreshSidebarIconStates()
    {
        foreach (var (page, icon) in _sidebarIcons)
        {
            var selected = page == _selectedPage;
            icon.Foreground = selected ? AccentBrush : MutedBrush;
            icon.IconVariant = selected ? IconVariant.Filled : IconVariant.Regular;
        }
    }
}
