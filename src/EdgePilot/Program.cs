using Avalonia;
using EdgePilot.Platform;

namespace EdgePilot;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Contains("--version"))
        {
            Console.WriteLine("EdgePilot " + typeof(Program).Assembly.GetName().Version);
            return;
        }
        using var instance = new SingleInstance();
        if (!instance.IsPrimary)
        {
            if (!args.Contains("--autostart") && !instance.NotifyPrimary())
                Console.Error.WriteLine("EdgePilot è già aperto ma non risponde. Chiudilo e riprova.");
            return;
        }
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder
        .Configure<App>()
        .UsePlatformDetect()
        .LogToTrace();
}
