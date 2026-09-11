using Avalonia;

namespace EdgePilot.UI;

public sealed partial class SettingsWindow
{
    private bool _glassTileHooksInstalled;

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        // Surface availability is per-backdrop:
        // Windows 10 1803+ gets Flat + Acrylic; Windows 11 also gets Mica.
        _surfaceCard.IsVisible = SettingsBackdropSupport.HasSurfaceChoices;
        _backdropButtons[(int)SettingsBackdrop.Flat].IsVisible = true;
        _backdropButtons[(int)SettingsBackdrop.Mica].IsVisible = SettingsBackdropSupport.IsMicaSupported;
        _backdropButtons[(int)SettingsBackdrop.Acrylic].IsVisible = SettingsBackdropSupport.IsAcrylicSupported;

        // Metric tiles used to repaint themselves with opaque legacy colours after the rest of
        // Settings had switched to Glass. Keep them on the same resolved palette after every
        // interaction that can change selection, theme or backdrop.
        if (!_glassTileHooksInstalled)
        {
            foreach (var metric in _metrics)
                metric.Click += (_, _) => ApplyMetricTileGlass();
            foreach (var button in _backdropButtons)
                button.Click += (_, _) => ApplyMetricTileGlass();
            foreach (var button in _themeButtons)
                button.Click += (_, _) => ApplyMetricTileGlass();
            _resetButton.Click += (_, _) => ApplyMetricTileGlass();
            ActualThemeVariantChanged += (_, _) => ApplyMetricTileGlass();
            _glassTileHooksInstalled = true;
        }

        ApplyMetricTileGlass();
    }

    private void ApplyMetricTileGlass()
    {
        if (_metricTiles.Length == 0) return;

        var backdrop = SettingsBackdropSupport.Coerce(_selectedBackdrop);
        var palette = Glass.Palette(backdrop, Glass.ResolveDark(_selectedTheme));

        for (var i = 0; i < _metricTiles.Length; i++)
        {
            var selected = _metrics[i].IsChecked == true;
            _metricTiles[i].Background = selected ? AccentSoftBrush : palette.Card;
            _metricTiles[i].BorderBrush = selected ? AccentBrush : palette.CardBorder;
            _metricTiles[i].BorderThickness = selected ? new Thickness(1) : palette.CardBorderThickness;
        }
    }
}