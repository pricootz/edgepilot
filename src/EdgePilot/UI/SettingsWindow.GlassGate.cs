namespace EdgePilot.UI;

public sealed partial class SettingsWindow
{
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        // Surface availability is per-backdrop:
        // Windows 10 1803+ gets Flat + Acrylic; Windows 11 also gets Mica.
        _surfaceCard.IsVisible = SettingsBackdropSupport.HasSurfaceChoices;
        _backdropButtons[(int)SettingsBackdrop.Flat].IsVisible = true;
        _backdropButtons[(int)SettingsBackdrop.Mica].IsVisible = SettingsBackdropSupport.IsMicaSupported;
        _backdropButtons[(int)SettingsBackdrop.Acrylic].IsVisible = SettingsBackdropSupport.IsAcrylicSupported;
    }
}
