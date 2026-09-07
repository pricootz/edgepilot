using Avalonia.Controls;

namespace EdgePilot.UI;

public static class AppIcon
{
    public static WindowIcon Load()
    {
        using var stream = typeof(AppIcon).Assembly.GetManifestResourceStream("EdgePilot.Assets.edgepilot.ico")
            ?? throw new IOException("Icona dell’app non disponibile.");
        return new WindowIcon(stream);
    }
}
