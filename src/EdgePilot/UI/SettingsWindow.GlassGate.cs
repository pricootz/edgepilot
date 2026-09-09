namespace EdgePilot.UI;

public sealed partial class SettingsWindow
{
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        // The PR originally exposed the selector on every Windows version. The adapted
        // integration keeps the option strictly Windows 11+, matching the feature contract.
        _surfaceCard.IsVisible = SettingsBackdropSupport.IsSupported;
    }
}
