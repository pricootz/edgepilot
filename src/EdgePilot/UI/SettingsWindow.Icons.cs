using Avalonia.Controls;
using Avalonia.Media;
using EdgePilot.Core;
using FluentIcons.Common;
using FluentIconName = FluentIcons.Common.Icon;

namespace EdgePilot.UI;

public sealed partial class SettingsWindow
{
    private readonly Dictionary<SettingsPage, FluentIcons.Avalonia.FluentIcon> _navIcons = new();
    private bool _settingsIconsInstalled;

    private void InstallSettingsIcons()
    {
        if (!_settingsIconsInstalled)
        {
            _modeButtons[(int)NotchDisplayMode.Hover].Content =
                ButtonContent(FluentIconName.CursorHover, Localization.T("mode.hover"));
            _modeButtons[(int)NotchDisplayMode.Always].Content =
                ButtonContent(FluentIconName.Eye, Localization.T("mode.always"));
            _modeButtons[(int)NotchDisplayMode.Hidden].Content =
                ButtonContent(FluentIconName.EyeOff, Localization.T("mode.hidden"));

            _sensitivityButtons[(int)HoverSensitivity.Precise].Content =
                ButtonContent(FluentIconName.Target, Localization.T("sensitivity.precise"));
            _sensitivityButtons[(int)HoverSensitivity.Normal].Content =
                ButtonContent(FluentIconName.Cursor, Localization.T("sensitivity.normal"));
            _sensitivityButtons[(int)HoverSensitivity.Wide].Content =
                ButtonContent(FluentIconName.CursorHover, Localization.T("sensitivity.wide"));

            _themeButtons[(int)SettingsThemePreference.System].Content =
                ButtonContent(FluentIconName.Desktop, Localization.T("theme.system"));
            _themeButtons[(int)SettingsThemePreference.Light].Content =
                ButtonContent(FluentIconName.WeatherSunny, Localization.T("theme.light"));
            _themeButtons[(int)SettingsThemePreference.Dark].Content =
                ButtonContent(FluentIconName.WeatherMoon, Localization.T("theme.dark"));

            _backdropButtons[(int)SettingsBackdrop.Flat].Content =
                ButtonContent(FluentIconName.Desktop, Localization.T("surface.flat"));
            _backdropButtons[(int)SettingsBackdrop.Mica].Content =
                ButtonContent(FluentIconName.Layer, Localization.T("surface.mica"));
            _backdropButtons[(int)SettingsBackdrop.Acrylic].Content =
                ButtonContent(FluentIconName.Glance, Localization.T("surface.acrylic"));

            _applyButton.Content = ButtonContent(
                FluentIconName.Checkmark,
                Localization.T("settings.saveChanges"),
                Brushes.White);
            _resetButton.Content = ButtonContent(
                FluentIconName.ArrowUndo,
                Localization.T("settings.resetChanges"));

            _settingsIconsInstalled = true;
        }

        RefreshSegmentVisuals();
        UpdateNavigationIconStates(_selectedPage);
        RefreshActionButtonVisuals();
    }

    private void RefreshSegmentVisuals()
    {
        SetSegmentSelection(_edgeButtons, (int)_selectedEdge);
        SetSegmentSelection(_modeButtons, (int)_selectedMode);
        SetSegmentSelection(_sensitivityButtons, (int)_selectedSensitivity);
        SetSegmentSelection(_themeButtons, (int)_selectedTheme);
        SetSegmentSelection(_backdropButtons, (int)_selectedBackdrop);
    }

    private void UpdateNavigationIconStates(SettingsPage selectedPage)
    {
        foreach (var page in _navButtons.Keys)
            PaintNavigationButton(page);
    }
}
