using Avalonia.Media;
using FluentIcons.Avalonia;
using FluentIcons.Common;
using FluentIconName = FluentIcons.Common.Icon;

namespace EdgePilot.UI;

public sealed partial class SettingsWindow
{
    private readonly Dictionary<SettingsPage, FluentIcon> _navIcons = new();
    private bool _supplementalIconsInstalled;

    private void InstallSettingsIcons()
    {
        if (!_supplementalIconsInstalled)
        {
            _sensitivityButtons[(int)HoverSensitivity.Precise].Content =
                ButtonContent(FluentIconName.Target, Localization.T("sensitivity.precise"));
            _sensitivityButtons[(int)HoverSensitivity.Normal].Content =
                ButtonContent(FluentIconName.Gesture, Localization.T("sensitivity.normal"));
            _sensitivityButtons[(int)HoverSensitivity.Wide].Content =
                ButtonContent(FluentIconName.ArrowsBidirectional, Localization.T("sensitivity.wide"));

            _backdropButtons[(int)SettingsBackdrop.Flat].Content =
                ButtonContent(FluentIconName.Desktop, Localization.T("surface.flat"));
            _backdropButtons[(int)SettingsBackdrop.Mica].Content =
                ButtonContent(FluentIconName.Layer, Localization.T("surface.mica"));
            _backdropButtons[(int)SettingsBackdrop.Acrylic].Content =
                ButtonContent(FluentIconName.Glance, Localization.T("surface.acrylic"));

            _supplementalIconsInstalled = true;
        }

        UpdateNavigationIconStates(_selectedPage);
    }

    private void UpdateNavigationIconStates(SettingsPage selectedPage)
    {
        foreach (var (page, icon) in _navIcons)
        {
            var selected = page == selectedPage;
            icon.Foreground = selected ? AccentBrush : MutedBrush;
            icon.IconVariant = selected ? IconVariant.Filled : IconVariant.Regular;
        }
    }
}
