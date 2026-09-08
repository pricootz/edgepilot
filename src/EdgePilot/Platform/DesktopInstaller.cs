namespace EdgePilot.Platform;

public static class DesktopInstaller
{
    public static string Install()
    {
        var source = Path.GetFullPath(AppContext.BaseDirectory).TrimEnd(Path.DirectorySeparatorChar);
        var target = OperatingSystem.IsWindows()
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "EdgePilot")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "edgepilot");
        target = Path.GetFullPath(target);
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!string.Equals(source, target, comparison))
        {
            if (target.StartsWith(source + Path.DirectorySeparatorChar, comparison))
                throw new IOException("Estrai il pacchetto in una cartella separata prima di installarlo.");
            Directory.CreateDirectory(target);
            var options = new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = FileAttributes.ReparsePoint };
            foreach (var file in Directory.EnumerateFiles(source, "*", options))
            {
                var destination = Path.Combine(target, Path.GetRelativePath(source, file));
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(file, destination, true);
            }
        }
        var executable = Path.Combine(target, OperatingSystem.IsWindows() ? "EdgePilot.exe" : "EdgePilot");
        if (OperatingSystem.IsWindows()) CreateWindowsShortcut(executable);
        else if (OperatingSystem.IsLinux())
        {
            File.SetUnixFileMode(executable, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
            var applications = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".local", "share", "applications");
            Directory.CreateDirectory(applications);
            var entry = new LaunchCommand(executable, new[] { "--settings" }).DesktopEntry()
                .Replace("X-GNOME-Autostart-enabled=true\n", "Categories=System;Monitor;\n");
            entry += "Icon=" + Path.Combine(target, "Assets", "edgepilot.svg").Replace("\\", "\\\\") + "\n";
            File.WriteAllText(Path.Combine(applications, "io.github.pricootz.EdgePilot.desktop"), entry);
        }
        else throw new PlatformNotSupportedException();
        var registration = new AutostartRegistration();
        if (registration.Read() is not null)
        {
            var command = new LaunchCommand(executable, new[] { "--autostart" });
            registration.Write(OperatingSystem.IsWindows() ? command.WindowsCommand() : command.DesktopEntry());
        }
        return target;
    }

    private static void CreateWindowsShortcut(string executable)
    {
        if (!OperatingSystem.IsWindows()) return;
        var programs = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
        Directory.CreateDirectory(programs);
        var type = Type.GetTypeFromProgID("WScript.Shell") ?? throw new IOException("Menu Start non disponibile.");
        dynamic shell = Activator.CreateInstance(type)!;
        dynamic shortcut = shell.CreateShortcut(Path.Combine(programs, "EdgePilot.lnk"));
        shortcut.TargetPath = executable;
        shortcut.Arguments = "--settings";
        shortcut.WorkingDirectory = Path.GetDirectoryName(executable);
        shortcut.IconLocation = executable + ",0";
        shortcut.Save();
    }
}
