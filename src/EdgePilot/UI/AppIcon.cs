using Avalonia.Controls;
using EdgePilot.Localization;

namespace EdgePilot.UI;

public static class AppIcon
{
    public static WindowIcon Load()
    {
        using var stream = typeof(AppIcon).Assembly.GetManifestResourceStream("EdgePilot.Assets.edgepilot.ico")
            ?? throw new IOException(Strings.Get("ui.error.icon"));
        return new WindowIcon(stream);
    }
}
