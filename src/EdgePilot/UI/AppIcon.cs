using Avalonia.Controls;
using EdgePilot.Core;

namespace EdgePilot.UI;

public static class AppIcon
{
    public static WindowIcon Load()
    {
        using var stream = typeof(AppIcon).Assembly.GetManifestResourceStream("EdgePilot.Assets.edgepilot.ico")
            ?? throw new IOException(Localization.T("appicon.unavailable"));
        return new WindowIcon(stream);
    }
}
