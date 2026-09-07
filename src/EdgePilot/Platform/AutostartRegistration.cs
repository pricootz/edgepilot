using System.Reflection;
using Microsoft.Win32;
using EdgePilot.UI;

namespace EdgePilot.Platform;

public interface IAutostartRegistration
{
    string? Read();
    void Write(string? content);
}

public sealed record LaunchCommand(string Executable, IReadOnlyList<string> Arguments)
{
    public static LaunchCommand Current()
    {
        var executable = Environment.ProcessPath ?? throw new IOException("Percorso dell’app non disponibile.");
        var arguments = new List<string>();
        if (string.Equals(Path.GetFileNameWithoutExtension(executable), "dotnet", StringComparison.OrdinalIgnoreCase))
        {
            var assembly = Assembly.GetEntryAssembly()?.Location;
            if (string.IsNullOrEmpty(assembly)) throw new IOException("Percorso dell’app non disponibile.");
            arguments.Add(assembly);
        }
        arguments.Add("--autostart");
        return new(executable, arguments);
    }

    public string WindowsCommand()
    {
        var result = string.Join(" ", new[] { Executable }.Concat(Arguments).Select(QuoteWindows));
        if (result.Length > 260) throw new IOException("Percorso troppo lungo per l’avvio automatico di Windows.");
        return result;
    }

    public string DesktopEntry()
    {
        var command = string.Join(" ", new[] { Executable }.Concat(Arguments).Select(QuoteDesktop));
        return "[Desktop Entry]\nType=Application\nName=EdgePilot\nComment=Monitoraggio del sistema\nExec=" +
            command.Replace("\\", "\\\\") +
            "\nTerminal=false\nX-GNOME-Autostart-enabled=true\n";
    }

    private static void ValidateArgument(string value)
    {
        if (value.IndexOfAny(['\r', '\n', '\0']) >= 0)
            throw new IOException("Il percorso dell’app contiene caratteri non supportati.");
    }

    public static string QuoteWindows(string value)
    {
        ValidateArgument(value);
        var output = new System.Text.StringBuilder("\"");
        var slashes = 0;
        foreach (var ch in value)
        {
            if (ch == '\\') { slashes++; continue; }
            output.Append('\\', ch == '"' ? slashes * 2 + 1 : slashes);
            output.Append(ch);
            slashes = 0;
        }
        output.Append('\\', slashes * 2).Append('"');
        return output.ToString();
    }

    private static string QuoteDesktop(string value)
    {
        ValidateArgument(value);
        var output = new System.Text.StringBuilder("\"");
        foreach (var ch in value)
        {
            if (ch is '\\' or '"' or '$' or '\u0060') output.Append('\\');
            if (ch == '%') output.Append('%');
            output.Append(ch);
        }
        return output.Append('"').ToString();
    }
}

public sealed class AutostartRegistration : IAutostartRegistration
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "EdgePilot";
    public static string LinuxPath => Path.Combine(
        Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") is { } path && Path.IsPathRooted(path)
            ? path : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config"),
        "autostart", "io.github.pricootz.EdgePilot.desktop");

    public string? Read()
    {
        if (OperatingSystem.IsWindows())
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) as string;
        }
        if (OperatingSystem.IsLinux())
            return File.Exists(LinuxPath) ? File.ReadAllText(LinuxPath) : null;
        return null;
    }

    public void Write(string? content)
    {
        if (OperatingSystem.IsWindows())
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey)
                ?? throw new IOException("Impossibile aggiornare l’avvio automatico.");
            if (content is null) key.DeleteValue(ValueName, false);
            else key.SetValue(ValueName, content, RegistryValueKind.String);
            return;
        }
        if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException();
        if (content is null) { File.Delete(LinuxPath); return; }
        Directory.CreateDirectory(Path.GetDirectoryName(LinuxPath)!);
        var temporary = LinuxPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, content);
            File.Move(temporary, LinuxPath, true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}

public static class DesktopPreferences
{
    public static void Save(string path, NotchPreferences preferences,
        IAutostartRegistration registration, LaunchCommand command, bool windows)
    {
        PreferenceStore.Validate(preferences);
        var previous = registration.Read();
        var next = preferences.StartAtLogin
            ? (windows ? command.WindowsCommand() : command.DesktopEntry()) : null;
        try
        {
            if (next != previous) registration.Write(next);
            PreferenceStore.Save(path, preferences);
        }
        catch
        {
            if (next != previous) registration.Write(previous);
            throw;
        }
    }
}
