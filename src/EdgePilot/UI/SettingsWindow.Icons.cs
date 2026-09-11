using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using FluentIcons.Avalonia;
using FluentIcons.Common;

namespace EdgePilot.UI;

public sealed partial class SettingsWindow
{
    private bool _sidebarIconsInstalled;

    private void InstallSidebarIcons()
    {
        if (_sidebarIconsInstalled) return;

        foreach (var (page, button) in _navButtons)
        {
            if (button.Content is not StackPanel row || row.Children.Count == 0) continue;

            var icon = page switch
            {
                SettingsPage.General => Icon.Settings,
                SettingsPage.Edge => Icon.Target,
                SettingsPage.Monitor => Icon.DesktopPulse,
                SettingsPage.Behavior => Icon.ArrowSync,
                SettingsPage.Startup => Icon.Power,
                SettingsPage.About => Icon.Info,
                _ => Icon.AppGeneric
            };

            row.Children.RemoveAt(0);
            row.Children.Insert(0, new FluentIcon
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
            });
            row.Spacing = 10;
        }

        _sidebarIconsInstalled = true;
    }
}
